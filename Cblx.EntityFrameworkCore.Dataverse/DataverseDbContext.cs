using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cblx.EntityFrameworkCore.Dataverse;

public class DataverseDbContext(DbContextOptions options) : DbContext(options)
{
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var loggingOptions = this.GetService<ILoggingOptions>();
        if (!ChangeTracker.HasChanges()) { return 0; }
        using var httpClient = CreateHttpClient();
        Log(DataverseEventId.CreatingBatchRequest, $"Context '{GetType().Name}' started creating batch request for saving changes.");
        var batchId = Guid.NewGuid();
        var batchContent = CreateBatchContent(
            batchId, 
            httpClient.BaseAddress?.ToString() ?? "");
        if (batchContent == null) { return 0; } // Won't happen... review?
        Log(DataverseEventId.SendingBatchRequest,
                   loggingOptions.IsSensitiveDataLoggingEnabled ?
                    $"""
                    Context '{GetType().Name}' is sending the following batch request:
                    {batchContent}
                    """ : $"Context '{GetType().Name}' is sending a batch request. Consider using 'DbContextOptionsBuilder.EnableSensitiveDataLogging' to see all values.");
        var response = await httpClient.PostAsync(
            "$batch", 
            new StringContent(batchContent, Encoding.UTF8, new MediaTypeHeaderValue("multipart/mixed")
            {
                Parameters = { new NameValueHeaderValue("boundary", $"batch_{batchId}") }
            }), cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            Log(DataverseEventId.BatchRequestFailed,
                loggingOptions.IsSensitiveDataLoggingEnabled ?
                $"""
                '{GetType().Name}' batch request for saving changes has failed.
                {await response.Content.ReadAsStringAsync(cancellationToken)}
                """ : $"'{GetType().Name}' batch request for saving changes has failed.");
            string message;
            try
            {
                // We expect to receive just one error message for the first failed operation, read more:
                // https://learn.microsoft.com/en-us/power-apps/developer/data-platform/webapi/execute-batch-operations-using-web-api#handling-errors
                var multiPartResponseString = await response.Content.ReadAsStringAsync(cancellationToken);
                /*
                 EXAMPLE OF AN ERROR MESSAGE
                --batchresponse_b4c45a7a-bf5c-40ac-8911-8e8a1d5c4a6e
                Content-Type: multipart/mixed; boundary=changesetresponse_77a8eaf7-e71a-44fe-a96f-d8c9bb55dec0

                --changesetresponse_77a8eaf7-e71a-44fe-a96f-d8c9bb55dec0
                Content-Type: application/http
                Content-Transfer-Encoding: binary
                Content-ID: 1

                HTTP/1.1 404 Not Found
                Content-Type: application/json; odata.metadata=minimal
                OData-Version: 4.0

                {"error":{"code":"0x80060888","message":"Resource not found for the segment 'xxxxxxx'."}}
                --changesetresponse_77a8eaf7-e71a-44fe-a96f-d8c9bb55dec0--
                --batchresponse_b4c45a7a-bf5c-40ac-8911-8e8a1d5c4a6e--
                */
                // Get the json response content inside the multipart content
                var jsonIndex = multiPartResponseString.IndexOf("{");
                var jsonError = multiPartResponseString[jsonIndex..].Split("\r\n")[0];

                var json = JsonSerializer.Deserialize<JsonObject>(jsonError);
                message = json?["error"]?["message"]?.GetValue<string>() ?? "Unknown error | EFCore.Dataverse";
            }
            catch
            {
                message = await response.Content.ReadAsStringAsync(cancellationToken);
            }
            throw new DbUpdateException(message);
        }
        Log(DataverseEventId.BatchRequestSucceeded,
            loggingOptions.IsSensitiveDataLoggingEnabled ?
            $"""
            Context '{GetType().Name}' successfully completed batch request for saving changes.
            {await response.Content.ReadAsStringAsync(cancellationToken)}
            """ :
            $"Context '{GetType().Name}' successfully completed batch request for saving changes."
        );
        ChangeTracker.AcceptAllChanges();
        return -1;
    }

    internal string GetBatchCommandForAssertion()
    {
        using var httpClient = CreateHttpClient();
        return CreateBatchContent(Guid.Empty, httpClient.BaseAddress?.ToString() ?? "", guidCreator: () => Guid.Empty) ?? "";
    }

    private string? CreateBatchContent(Guid batchId,string baseAddress, Func<Guid>? guidCreator = null)
    {
        guidCreator ??= Guid.NewGuid;
        if (!ChangeTracker.HasChanges()) { return null; }

        var request = CreateBatchRequest();
        var entries = ChangeTracker.Entries().ToArray();
        var changeSetId = guidCreator();
        var sbBatch = new StringBuilder($"""
            --batch_{batchId}
            Content-Type: multipart/mixed; boundary=changeset_{changeSetId}


            """);

        var deleted = entries.Where(e => e.State == EntityState.Deleted).ToArray();
        int contentId = 0;
        var deletedManyToManyRelationships = deleted.Where(d => d.Metadata.IsManyToManyJoinEntity()).ToArray();
        foreach (var deletedRelationship in deletedManyToManyRelationships)
        {
            var manyToManyRelationshipData = deletedRelationship.Metadata.GetManyToManyEntityData() ??
               throw new InvalidOperationException("ManyToManyEntityData not found.");
            var leftEntityType = Model.GetEntityTypes().First(entity => entity.ClrType == manyToManyRelationshipData.LeftEntityType);
            var leftKeyValue = deletedRelationship.Property(manyToManyRelationshipData.LeftForeignKey).CurrentValue;
            var leftEntitySet = leftEntityType.GetEntitySetName();
            var rightKeyValue = deletedRelationship.Property(manyToManyRelationshipData.RightForeignKey).CurrentValue;
            var path = $"{leftEntitySet}({leftKeyValue})/{manyToManyRelationshipData.NavigationFromLeft}({rightKeyValue})/$ref";
            var requestContent = $"""
                {ContentHeader(changeSetId, contentId++)}
                {ContentDeleteAction(path)}
                """;
            sbBatch.Append(requestContent);
        }
        deleted = deleted.Except(deletedManyToManyRelationships).ToArray();
        foreach (var entry in deleted)
        {
            if (entry.Metadata.FindAnnotation(nameof(ODataBindManyToManyData))?.Value is ODataBindManyToManyData oDataBindManyToManyData)
            {
                // weak many to many relationship
                var principalEntityType = Model.GetEntityTypes().First(entity => entity.ClrType == oDataBindManyToManyData.PrincipalType);
                var principalEntitySet = principalEntityType.GetEntitySetName();
                var navigation = principalEntityType.FindNavigation(oDataBindManyToManyData.PrincipalNavigationPropertyName);
                var fkToParentName = navigation.ForeignKey.Properties[0].Name;
                var fkToParentValue = entry.Property(fkToParentName).CurrentValue;
                var targetEntityId = entry.Property(oDataBindManyToManyData.RelForeignKeyToTarget.Member.Name);
                var path = $"{principalEntitySet}({fkToParentValue})/{oDataBindManyToManyData.PrincipalNavigationLogicalName}({targetEntityId.CurrentValue})/$ref";
                var requestContent = $"""
                    {ContentHeader(changeSetId, contentId++)}
                    {ContentDeleteAction(path)}
                    """;
                sbBatch.Append(requestContent);
            }
            else
            {
                var primeryKeyProperty = entry.Metadata.FindPrimaryKey()!.Properties[0];
                var primaryKeyValue = entry.Property(primeryKeyProperty).CurrentValue;
                var requestContent = $"""
                    {ContentHeader(changeSetId, contentId++)}
                    {ContentDeleteAction($"{entry.Metadata.GetEntitySetName()}({primaryKeyValue})")}
                    """;
                sbBatch.Append(requestContent);
            }
        }

        var added = entries.Where(e => e.State == EntityState.Added).ToArray();
        var addedManyToManyRelationships = added.Where(d => d.Metadata.IsManyToManyJoinEntity()).ToArray();
        added = added.Except(addedManyToManyRelationships).ToArray();
        added = FkOnlyEntryOrderer.OrderAddedByForeignKeysOnly(added);
        foreach (var entry in added)
        {
            if (entry.Metadata.FindAnnotation(nameof(ODataBindManyToManyData))?.Value is ODataBindManyToManyData oDataBindManyToManyData)
            {
                var principalEntityType = Model.GetEntityTypes().First(entity => entity.ClrType == oDataBindManyToManyData.PrincipalType);
                var principalEntitySet = principalEntityType.GetEntitySetName();
                var navigation = principalEntityType.FindNavigation(oDataBindManyToManyData.PrincipalNavigationPropertyName);
                var fkToParentName = navigation.ForeignKey.Properties[0].Name;
                var fkToParentValue = entry.Property(fkToParentName).CurrentValue;
                var path = $"{principalEntitySet}({fkToParentValue})/{oDataBindManyToManyData.PrincipalNavigationLogicalName}/$ref";
                var targetEntityId = entry.Property(oDataBindManyToManyData.RelForeignKeyToTarget.Member.Name);
                var body = $$"""
                    {
                        "@odata.id": "{{baseAddress}}{{oDataBindManyToManyData.TargetEntitySet}}({{targetEntityId.CurrentValue}})"
                    }

                    """;
                var requestContent = $$"""
                    {{ContentHeader(changeSetId, contentId++)}}
                    {{ContentPostAction(path, body)}}
                    """;
                sbBatch.Append(requestContent);
            }
            else
            {
                var properties = GetPropertiesRecusively(entry)
                    .Where(p => p.CurrentValue != p.Metadata.GetDefaultValue())
                    .Where(p => !p.IsDataverseReadOnly())
                    .ToArray();
                var json = CreateJsonWithProperties(properties);

                var primeryKeyProperty = entry.Metadata.FindPrimaryKey()!.Properties[0];
                var primaryKeyValue = entry.Property(primeryKeyProperty).CurrentValue;

                var requestContent = $$"""
                    {{ContentHeader(changeSetId, contentId++)}}
                    {{ContentPostAction(entry.Metadata.GetEntitySetName(), json)}}
                    """;
                sbBatch.Append(requestContent);
            }
        }
        foreach (var addedRelationship in addedManyToManyRelationships)
        {
            var manyToManyRelationshipData = addedRelationship.Metadata.GetManyToManyEntityData() ??
                throw new InvalidOperationException("ManyToManyEntityData not found.");
            var leftEntityType = Model.GetEntityTypes().First(entity => entity.ClrType == manyToManyRelationshipData.LeftEntityType);
            var leftKeyValue = addedRelationship.Property(manyToManyRelationshipData.LeftForeignKey).CurrentValue;
            var leftEntitySet = leftEntityType.GetEntitySetName();
            var rightEntityType = Model.GetEntityTypes().First(entity => entity.ClrType == manyToManyRelationshipData.RightEntityType);
            var rightKeyValue = addedRelationship.Property(manyToManyRelationshipData.RightForeignKey).CurrentValue;
            var rightEntitySet = rightEntityType.GetEntitySetName();
            var path = $"{leftEntitySet}({leftKeyValue})/{manyToManyRelationshipData.NavigationFromLeft}/$ref";
            var body = $$"""
                {
                    "@odata.id": "{{baseAddress}}{{rightEntitySet}}({{rightKeyValue}})"
                }

                """;
            var requestContent = $$"""
                {{ContentHeader(changeSetId, contentId++)}}
                {{ContentPostAction(path, body)}}
                """;
            sbBatch.Append(requestContent);
        }
        var modified = entries.Where(e => e.State == EntityState.Modified).ToArray();
        foreach (var entry in modified)
        {
            var properties = GetPropertiesRecusively(entry)
                .Where(p => p.IsModified)
                .Where(p => !p.IsDataverseReadOnly())
                .ToArray();
            var json = CreateJsonWithProperties(properties);

            var primeryKeyProperty = entry.Metadata.FindPrimaryKey()!.Properties[0];
            var primaryKeyValue = entry.Property(primeryKeyProperty).CurrentValue;
            var path = $"{entry.Metadata.GetEntitySetName()}({primaryKeyValue})";
            var requestContent = $$"""
                {{ContentHeader(changeSetId, contentId++)}}
                {{ContentPatchAction(path, json)}}
                """;
            sbBatch.Append(requestContent);
        }

        sbBatch.Append($"""
            --changeset_{changeSetId}--

            --batch_{batchId}--

            """);

        return sbBatch.ToString();
    }

    private static string GetRelativePath(string path) => $"/{Consts.ApiDataPath}{path}";
    private static string ContentDeleteAction(string path) => $"""
        DELETE {GetRelativePath(path)} HTTP/1.1


        """;
    private static string ContentPostAction(string path, string body) => $"""
        POST {GetRelativePath(path)} HTTP/1.1
        Content-Type: application/json

        {body}

        """;
    private static string ContentPatchAction(string path, string body) => $"""
        PATCH {GetRelativePath(path)} HTTP/1.1
        Content-Type: application/json

        {body}

        """;

    private static string ContentHeader(Guid changeSetId, int contentId) => $"""
        --changeset_{changeSetId}
        Content-Type: application/http
        Content-Transfer-Encoding: binary
        Content-ID: {contentId}

        """;
    
    private PropertyEntry[] GetPropertiesRecusively(EntityEntry entry)
    {
        var properties = entry.Properties.ToArray();
        var innerProperties = entry.ComplexProperties.SelectMany(GetPropertiesRecusively).ToArray();
        return [.. properties, .. innerProperties];
    }

    private PropertyEntry[] GetPropertiesRecusively(ComplexPropertyEntry entry)
    {
        var properties = entry.Properties.ToArray();
        var innerProperties = entry.ComplexProperties.SelectMany(GetPropertiesRecusively).ToArray();
        return [.. properties, .. innerProperties];
    }

    private void Log(DataverseEventId eventId, string message)
    {
        var logger = this.GetService<IDbContextLogger>();
        var loggingOptions = this.GetService<ILoggingOptions>();
        logger.Log(
            new EventData(
                new DataverseEventDefinition(loggingOptions,
                    new EventId((int)eventId, typeof(DataverseEventId).FullName),
                    LogLevel.Debug,
                    $"{nameof(DataverseEventId)}.{eventId}"),
                (def, data) => message
            )
        );
    }

    private static string CreateJsonWithProperties(PropertyEntry[] properties)
    {
        var sbContent = new StringBuilder();
        sbContent.AppendLine("{");
        foreach (var property in properties)
        {
            // Has a foreign key configured
            if (property.Metadata.IsForeignKey())
            {
                var foreignEntitySetName = property.Metadata.GetContainingForeignKeys()
                                 .First()
                                 .PrincipalEntityType.GetEntitySetName();
                var propName = property.GetODataBindPropertyName() ?? property.Metadata.GetColumnName();
                if (property.CurrentValue is null)
                {
                    sbContent.Append(
                    $"""
                        "{propName}": null
                    """);
                }
                else
                {
                    sbContent.Append(
                    $"""
                        "{propName}@odata.bind": "{foreignEntitySetName}({property.CurrentValue})"
                    """);
                }
            }
            // No foreign key but has odatabind configuration
            else if (property.GetODataBindPropertyName() is { } propName)
            {
                var foreignEntitySetName = property.GetForeignEntitySet() ??
                    throw new InvalidOperationException($"ForeignEntitySet not defined for {property.Metadata.Name}. Use HasForeignEntitySet if a relationship is not defined with HasOne/WithMany");
                if (property.CurrentValue is null)
                {
                    sbContent.Append(
                    $"""
                        "{propName}": null
                    """);
                }
                else
                {

                    sbContent.Append(
                    $"""
                        "{propName}@odata.bind": "{foreignEntitySetName}({property.CurrentValue})"
                    """);
                }
            }
            else
            {
                sbContent.Append(
                $"""
                    "{property.Metadata.GetColumnName()}": {JsonSerializer.Serialize(property.GetCurrentConvertedValueForWrite())}
                """);
            }
            if (property != properties[^1])
            {
                sbContent.AppendLine(",");
            }
            else
            {
                sbContent.AppendLine();
            }
        }
        sbContent.AppendLine("}");
        return sbContent.ToString();
    }

   
    private static HttpRequestMessage CreateBatchRequest()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "$batch");
        request.Headers.Add("OData-MaxVersion", "4.0");
        request.Headers.Add("OData-Version", "4.0");
        request.Headers.Add("Accept", "application/json");
        return request;
    }

    public HttpClient CreateHttpClient()
    {
        var extension = options.FindExtension<DataverseOptionsExtension>() ?? throw new InvalidOperationException("DataverseDbContextOptionsBuilder not used. Are you using 'UseSqlServer' instead of 'UseDataverse'?");
        var httpClientFactory = this.GetService<IHttpClientFactory>();
        var httpClient = httpClientFactory!.CreateClient(extension.HttpClientName!);
        return httpClient;
    }
}

file static class FkOnlyEntryOrderer
{
    /// <summary>
    /// Orders Added entries so principals are placed before dependents.
    ///
    /// Determinism goal:
    /// - Preserve the input order (snapshot order of addedEntries) whenever there is a tie.
    /// - This makes the resulting command/batch sequence stable for tests, given the same addedEntries snapshot.
    ///
    /// Notes:
    /// - addedEntries is expected to be a snapshot (e.g. ChangeTracker.Entries().Where(...).ToArray()).
    /// - This does NOT guarantee "order of Add calls"; it guarantees stable output for a given input order.
    /// </summary>
    public static EntityEntry[] OrderAddedByForeignKeysOnly(EntityEntry[] addedEntries)
    {
        if (addedEntries is null) throw new ArgumentNullException(nameof(addedEntries));

        // Tie-breaker index: preserve the input order whenever multiple nodes are ready.
        // Use reference equality because EntityEntry does not have a useful value equality for dictionary keys here.
        var orderIndex = new Dictionary<EntityEntry, int>(ReferenceEqualityComparer<EntityEntry>.Instance);
        for (int i = 0; i < addedEntries.Length; i++)
            orderIndex[addedEntries[i]] = i;

        // Build index of Added principals by (EntityType, PrimaryKeyValues)
        var principalByPk = new Dictionary<EntityKey, EntityEntry>(EntityKey.Comparer);

        foreach (var e in addedEntries)
        {
            var pk = e.Metadata.FindPrimaryKey();
            if (pk is null) continue;

            var pkValues = GetCurrentValues(e, pk.Properties);
            if (pkValues is null) continue; // ignore if PK incomplete / nulls

            principalByPk[new EntityKey(e.Metadata, pkValues)] = e;
        }

        // Graph: principal -> dependents
        var outgoing = new Dictionary<EntityEntry, HashSet<EntityEntry>>(ReferenceEqualityComparer<EntityEntry>.Instance);
        var indegree = new Dictionary<EntityEntry, int>(ReferenceEqualityComparer<EntityEntry>.Instance);

        foreach (var e in addedEntries)
        {
            outgoing[e] = new HashSet<EntityEntry>(ReferenceEqualityComparer<EntityEntry>.Instance);
            indegree[e] = 0;
        }

        foreach (var dependent in addedEntries)
        {
            foreach (var fk in dependent.Metadata.GetForeignKeys())
            {
                // FK properties live on dependent; principal key is usually the principal PK (or alternate key)
                var fkValues = GetCurrentValues(dependent, fk.Properties);
                if (fkValues is null) continue; // FK not fully set

                var principalType = fk.PrincipalEntityType;

                // Match dependent.FK values to principal key values
                if (principalByPk.TryGetValue(new EntityKey(principalType, fkValues), out var principal))
                {
                    // principal must come before dependent
                    if (outgoing[principal].Add(dependent))
                        indegree[dependent]++;
                }
            }
        }

        // Topological sort (Kahn)
        var ready = indegree.Where(kv => kv.Value == 0).Select(kv => kv.Key).ToList();
        SortBySnapshotOrder(ready, orderIndex);

        var result = new List<EntityEntry>(addedEntries.Length);

        while (ready.Count > 0)
        {
            var n = ready[0];
            ready.RemoveAt(0);
            result.Add(n);

            // Iterate dependents in snapshot order too (HashSet doesn't preserve order)
            // so we sort them by orderIndex before consuming.
            var dependents = outgoing[n].ToList();
            SortBySnapshotOrder(dependents, orderIndex);

            foreach (var m in dependents)
            {
                indegree[m]--;
                if (indegree[m] == 0)
                {
                    ready.Add(m);
                    SortBySnapshotOrder(ready, orderIndex);
                }
            }
        }

        if (result.Count == addedEntries.Length)
            return result.ToArray();

        // Cycle (or missing PK info) => no strict FK-based order exists for all nodes.
        // Append remaining nodes in snapshot order (stable for tests).
        var remaining = addedEntries.Except(result).ToList();
        SortBySnapshotOrder(remaining, orderIndex);
        result.AddRange(remaining);
        return result.ToArray();
    }

    private static object?[]? GetCurrentValues(EntityEntry entry, IReadOnlyList<IProperty> properties)
    {
        var values = new object?[properties.Count];
        for (int i = 0; i < properties.Count; i++)
        {
            var v = entry.Property(properties[i].Name).CurrentValue;
            if (v is null) return null; // require all parts filled
            values[i] = v;
        }
        return values;
    }

    /// <summary>
    /// Stable tie-breaker based on the input snapshot order.
    /// </summary>
    private static void SortBySnapshotOrder(List<EntityEntry> list, Dictionary<EntityEntry, int> orderIndex)
    {
        list.Sort((a, b) => orderIndex[a].CompareTo(orderIndex[b]));
    }

    private readonly record struct EntityKey(IEntityType EntityType, object?[] KeyValues)
    {
        public static IEqualityComparer<EntityKey> Comparer { get; } = new KeyComparer();

        private sealed class KeyComparer : IEqualityComparer<EntityKey>
        {
            public bool Equals(EntityKey x, EntityKey y)
            {
                if (!ReferenceEquals(x.EntityType, y.EntityType)) return false;
                if (x.KeyValues.Length != y.KeyValues.Length) return false;

                for (int i = 0; i < x.KeyValues.Length; i++)
                    if (!Equals(x.KeyValues[i], y.KeyValues[i])) return false;

                return true;
            }

            public int GetHashCode(EntityKey obj)
            {
                var h = obj.EntityType.GetHashCode();
                foreach (var v in obj.KeyValues)
                    h = (h * 397) ^ (v?.GetHashCode() ?? 0);
                return h;
            }
        }
    }

    /// <summary>
    /// Reference comparer helper (generic).
    /// </summary>
    private sealed class ReferenceEqualityComparer<T> : IEqualityComparer<T>
        where T : class
    {
        public static ReferenceEqualityComparer<T> Instance { get; } = new();

        public bool Equals(T? x, T? y) => ReferenceEquals(x, y);

        public int GetHashCode(T obj) => RuntimeHelpers.GetHashCode(obj);
    }
}