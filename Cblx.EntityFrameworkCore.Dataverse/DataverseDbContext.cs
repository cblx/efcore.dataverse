using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Headers;
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
        //var request = CreateBatchRequestMessage(
        //    out var httpClient,
        //    onStart: () => Log(DataverseEventId.CreatingBatchRequest, $"Context '{GetType().Name}' started creating batch request for saving changes."),
        //    onDeletedContentCreated: (entry, httpMessageContent) => Log(DataverseEventId.CreatingBatchRequestMessageContentItem,
        //        loggingOptions.IsSensitiveDataLoggingEnabled ?
        //        $"""
        //        '{GetType().Name}' created a request message content for deleting a '{entry.Metadata.ShortName()}' entity.
        //        {httpMessageContent.HttpRequestMessage.Method} {httpMessageContent.HttpRequestMessage.RequestUri}
        //        """ :
        //        $"'{GetType().Name}' created a request message content for deleting a '{entry.Metadata.ShortName()}' entity. Consider using 'DbContextOptionsBuilder.EnableSensitiveDataLogging' to see all values."
        //    ),
        //    onAddedContentCreated: (entry, httpMessageContent, json) => Log(DataverseEventId.CreatingBatchRequestMessageContentItem,
        //        loggingOptions.IsSensitiveDataLoggingEnabled ?
        //        $"""
        //        '{GetType().Name}' created a request message content for inserting a '{entry.Metadata.ShortName()}' entity.
        //        {httpMessageContent.HttpRequestMessage.Method} {httpMessageContent.HttpRequestMessage.RequestUri}
        //        {json}
        //        """ :
        //        $"'{GetType().Name}' created a request message content for inserting a '{entry.Metadata.ShortName()}' entity. Consider using 'DbContextOptionsBuilder.EnableSensitiveDataLogging' to see all values."
        //    ),
        //    onModifiedContentCreated: (entry, httpMessageContent, json) => Log(DataverseEventId.CreatingBatchRequestMessageContentItem,
        //        loggingOptions.IsSensitiveDataLoggingEnabled ?
        //        $"""
        //        '{GetType().Name}' created a request message content for updating a '{entry.Metadata.ShortName()}' entity.
        //        {httpMessageContent.HttpRequestMessage.Method} {httpMessageContent.HttpRequestMessage.RequestUri}
        //        {json}
        //        """ :
        //        $"'{GetType().Name}' created a request message content for updating a '{entry.Metadata.ShortName()}' entity. Consider using 'DbContextOptionsBuilder.EnableSensitiveDataLogging' to see all values."
        //    )
        //);
        //var batchContent = CreateBatchContent(Guid.NewGuid, )
        using var httpClient = CreateHttpClient();
        Log(DataverseEventId.CreatingBatchRequest, $"Context '{GetType().Name}' started creating batch request for saving changes.");
        var batchId = Guid.NewGuid();
        var batchContent = CreateBatchContent(
            batchId, 
            httpClient.BaseAddress?.ToString() ?? "" //,
            //onDeletedContentCreated: (entry, requestContent) => Log(DataverseEventId.CreatingBatchRequestMessageContentItem,
            //    loggingOptions.IsSensitiveDataLoggingEnabled ?
            //    $"""
            //    '{GetType().Name}' created a request message content for deleting a '{entry.Metadata.ShortName()}' entity.

            //    {requestContent}

            //    """ :
            //    $"'{GetType().Name}' created a request message content for deleting a '{entry.Metadata.ShortName()}' entity. Consider using 'DbContextOptionsBuilder.EnableSensitiveDataLogging' to see all values."
            //),
            //onAddedContentCreated: (entry, requestContent) => Log(DataverseEventId.CreatingBatchRequestMessageContentItem,
            //    loggingOptions.IsSensitiveDataLoggingEnabled ?
            //    $"""
            //    '{GetType().Name}' created a request message content for inserting a '{entry.Metadata.ShortName()}' entity.

            //    {requestContent}

            //    """ :
            //    $"'{GetType().Name}' created a request message content for inserting a '{entry.Metadata.ShortName()}' entity. Consider using 'DbContextOptionsBuilder.EnableSensitiveDataLogging' to see all values."
            //),
            //onModifiedContentCreated: (entry, requestContent) => Log(DataverseEventId.CreatingBatchRequestMessageContentItem,
            //    loggingOptions.IsSensitiveDataLoggingEnabled ?
            //    $"""
            //    '{GetType().Name}' created a request message content for updating a '{entry.Metadata.ShortName()}' entity.

            //    {requestContent}

            //    """ :
            //    $"'{GetType().Name}' created a request message content for updating a '{entry.Metadata.ShortName()}' entity. Consider using 'DbContextOptionsBuilder.EnableSensitiveDataLogging' to see all values."
            //)
        );
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
                //var multiPartResponse = await response.Content.ReadAsMultipartAsync(cancellationToken);

       
                var multiPartResponseString = await response.Content.ReadAsStringAsync(cancellationToken);

                /*
                 EXAMPLE OF AN ERROR MESSAGEM
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
                //var jsonError = multiPartResponseString.Split("\r\n")[^1]; <- didnt work
                // var jsonError = multiPartResponseString.Split("--batchresponse_")[^1].Split("--")[0]; <-- didnt work, this returned the batch id
                // var jsonError = multiPartResponseString.Split("--changesetresponse_")[^1].Split("--")[0]; <-- didnt work, this returns the changeset id, I want the JSON!
                // var jsonError = multiPartResponseString.Split("--changesetresponse_")[^1].Split("--")[0]; <-- didnt work, this returns the changeset id, I want the JSON!
                var jsonIndex = multiPartResponseString.IndexOf("{");
                var jsonError = multiPartResponseString[jsonIndex..].Split("\r\n")[0];

                var json = JsonSerializer.Deserialize<JsonObject>(jsonError);
                message = json?["error"]?["message"]?.GetValue<string>() ?? "Unknown error | EFCore.Dataverse";

                //message = "ERROR";
                //var firstContent = multiPartResponse.Contents[0];
                //var firstPart = await firstContent.ReadAsMultipartAsync(cancellationToken);
                //var firstContentAsString = await firstPart.Contents[0].ReadAsStringAsync(cancellationToken);
                //firstContentAsString = firstContentAsString.Split("\r\n")[^1];
                //var json = JsonSerializer.Deserialize<JsonObject>(firstContentAsString);
                //// Ex: {"error":{"code":"0x80040237","message":"A record with matching key values already exists."}}
                //message = json?["error"]?["message"]?.GetValue<string>() ?? "Unknown error | EFCore.Dataverse";
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

    /// <summary>
    /// Currently used internally for testing
    /// </summary>
    /// <returns></returns>
    //internal async Task<string> GetBatchCommandForAssertionAsync()
    //{
    //    var request = CreateBatchRequestMessage(out var _, guidCreator: () => Guid.Empty);
    //    if (request == null) { return ""; }
    //    return await request.Content!.ReadAsStringAsync();
    //}

    internal string GetBatchCommandForAssertion()
    {
        using var httpClient = CreateHttpClient();
        return CreateBatchContent(Guid.Empty, httpClient.BaseAddress?.ToString() ?? "", guidCreator: () => Guid.Empty) ?? "";
    }

    private string? CreateBatchContent(
      //out HttpClient? httpClient,
      Guid batchId,
      string baseAddress,
      // TODO: should we put it back inside the method?
      //Action<EntityEntry, string>? onDeletedContentCreated = null,
      //Action<EntityEntry, string>? onAddedContentCreated = null,
      //Action<EntityEntry, string>? onModifiedContentCreated = null,
      Func<Guid>? guidCreator = null)
    {
        guidCreator ??= Guid.NewGuid;
        if (!ChangeTracker.HasChanges())
        {
            //httpClient = null;
            return null;
        }

        //httpClient = CreateHttpClient();
        var request = CreateBatchRequest();
        var entries = ChangeTracker.Entries().ToArray();
        //var batchContent = new MultipartContent("mixed", $"batch_{guidCreator()}");
        var changeSetId = guidCreator();
        var sbBatch = new StringBuilder($"""
            --batch_{batchId}
            Content-Type: multipart/mixed; boundary=changeset_{changeSetId}


            """);

        //var changeSetContent = new MultipartContent("mixed", $"changeset_{guidCreator()}");
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
            //var httpRequestMessage = new HttpRequestMessage(HttpMethod.Delete, path);
            //var httpMessageContent = new HttpMessageContent(httpRequestMessage);
            //httpMessageContent.Headers.ContentType = new MediaTypeHeaderValue("application/http");
            //httpMessageContent.Headers.Add("Content-Transfer-Encoding", "binary");
            //httpMessageContent.Headers.Add("Content-ID", contentId++.ToString());
            var requestContent = $"""
                {ContentHeader(changeSetId, contentId++)}
                {ContentDeleteAction(path)}
                """;
            //changeSetContent.Add(httpMessageContent);
            sbBatch.AppendLine(requestContent);
            //onDeletedContentCreated?.Invoke(deletedRelationship, requestContent);
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
                sbBatch.AppendLine(requestContent);
                //onDeletedContentCreated?.Invoke(entry, requestContent);
            }
            else
            {
                var primeryKeyProperty = entry.Metadata.FindPrimaryKey()!.Properties[0];
                var primaryKeyValue = entry.Property(primeryKeyProperty).CurrentValue;
                var requestContent = $"""
                    {ContentHeader(changeSetId, contentId++)}
                    {ContentDeleteAction($"{entry.Metadata.GetEntitySetName()}({primaryKeyValue})")}
                    """;
                sbBatch.AppendLine(requestContent);
                //onDeletedContentCreated?.Invoke(entry, requestContent);
            }

            //var httpMessageContent = CreateHttpMessageContent(httpClient, HttpMethod.Delete, contentId++, entry);
            //changeSetContent.Add(httpMessageContent);
            //onDeletedContentCreated?.Invoke(entry, httpMessageContent);
        }

        var added = entries.Where(e => e.State == EntityState.Added).ToArray();
        var addedManyToManyRelationships = added.Where(d => d.Metadata.IsManyToManyJoinEntity()).ToArray();
        added = added.Except(addedManyToManyRelationships).ToArray();
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
                sbBatch.AppendLine(requestContent);
                //onAddedContentCreated?.Invoke(entry, requestContent);
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
                sbBatch.AppendLine(requestContent);
                //onAddedContentCreated?.Invoke(entry, requestContent);

                //var httpMessageContent = CreateHttpMessageContent(
                //    httpClient,
                //    HttpMethod.Post,
                //    contentId++,
                //    entry, json);
                //changeSetContent.Add(httpMessageContent);
                //onAddedContentCreated?.Invoke(entry, httpMessageContent, json);
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
            sbBatch.AppendLine(requestContent);
            //onAddedContentCreated?.Invoke(addedRelationship, requestContent);

            //var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, path);
            //var httpMessageContent = new HttpMessageContent(httpRequestMessage);
            //httpMessageContent.Headers.ContentType = new MediaTypeHeaderValue("application/http");
            //httpMessageContent.Headers.Add("Content-Transfer-Encoding", "binary");
            //httpMessageContent.Headers.Add("Content-ID", contentId++.ToString());
            //var json = $$"""
            //    {
            //        "@odata.id": "{{baseAddress}}{{rightEntitySet}}({{rightKeyValue}})"
            //    }
            //    """;
            //httpRequestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");
            //changeSetContent.Add(httpMessageContent);
            //onAddedContentCreated?.Invoke(addedRelationship, httpMessageContent, json);
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
            sbBatch.AppendLine(requestContent);
            //onModifiedContentCreated?.Invoke(entry, requestContent);
            //var httpMessageContent = CreateHttpMessageContent(
            //    httpClient,
            //    HttpMethod.Patch,
            //    contentId++,
            //    entry, json);
            //changeSetContent.Add(httpMessageContent);
            //onModifiedContentCreated?.Invoke(entry, httpMessageContent, json);
        }
        //batchContent.Add(changeSetContent);
        //request.Content = batchContent;

        sbBatch.AppendLine($"""
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

    //private HttpRequestMessage? CreateBatchRequestMessage(
    //    out HttpClient? httpClient,
    //    Action? onStart = null,
    //    Action<EntityEntry, HttpMessageContent>? onDeletedContentCreated = null,
    //    Action<EntityEntry, HttpMessageContent, string>? onAddedContentCreated = null,
    //    Action<EntityEntry, HttpMessageContent, string>? onModifiedContentCreated = null,
    //    Func<Guid>? guidCreator = null)
    //{
    //    guidCreator ??= Guid.NewGuid;
    //    if (!ChangeTracker.HasChanges())
    //    {
    //        httpClient = null;
    //        return null;
    //    }

    //    httpClient = CreateHttpClient();
    //    var request = CreateBatchRequest();
    //    var entries = ChangeTracker.Entries().ToArray();
    //    var batchContent = new MultipartContent("mixed", $"batch_{guidCreator()}");
    //    var changeSetContent = new MultipartContent("mixed", $"changeset_{guidCreator()}");
    //    var deleted = entries.Where(e => e.State == EntityState.Deleted).ToArray();
    //    int contentId = 0;
    //    onStart?.Invoke();
    //    var deletedManyToManyRelationships = deleted.Where(d => d.Metadata.IsManyToManyJoinEntity()).ToArray();
    //    foreach (var deletedRelationship in deletedManyToManyRelationships)
    //    {
    //        var manyToManyRelationshipData = deletedRelationship.Metadata.GetManyToManyEntityData() ??
    //           throw new InvalidOperationException("ManyToManyEntityData not found.");
    //        var leftEntityType = Model.GetEntityTypes().First(entity => entity.ClrType == manyToManyRelationshipData.LeftEntityType);
    //        var leftKeyValue = deletedRelationship.Property(manyToManyRelationshipData.LeftForeignKey).CurrentValue;
    //        var leftEntitySet = leftEntityType.GetEntitySetName();
    //        var rightKeyValue = deletedRelationship.Property(manyToManyRelationshipData.RightForeignKey).CurrentValue;
    //        var path = $"{httpClient.BaseAddress}{leftEntitySet}({leftKeyValue})/{manyToManyRelationshipData.NavigationFromLeft}({rightKeyValue})/$ref";
    //        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Delete, path);
    //        var httpMessageContent = new HttpMessageContent(httpRequestMessage);
    //        httpMessageContent.Headers.ContentType = new MediaTypeHeaderValue("application/http");
    //        httpMessageContent.Headers.Add("Content-Transfer-Encoding", "binary");
    //        httpMessageContent.Headers.Add("Content-ID", contentId++.ToString());
    //        changeSetContent.Add(httpMessageContent);
    //        onDeletedContentCreated?.Invoke(deletedRelationship, httpMessageContent);
    //    }
    //    deleted = deleted.Except(deletedManyToManyRelationships).ToArray();
    //    foreach (var entry in deleted)
    //    {
    //        if (entry.Metadata.FindAnnotation(nameof(ODataBindManyToManyData))?.Value is ODataBindManyToManyData oDataBindManyToManyData)
    //        {
    //            DeleteWeakManyToManyRel(httpClient,
    //                       oDataBindManyToManyData,
    //                       entry,
    //                       changeSetContent,
    //                       onDeletedContentCreated,
    //                       contentId++);
    //            continue;
    //        }

    //        var httpMessageContent = CreateHttpMessageContent(httpClient, HttpMethod.Delete, contentId++, entry);
    //        changeSetContent.Add(httpMessageContent);
    //        onDeletedContentCreated?.Invoke(entry, httpMessageContent);
    //    }
    //    var added = entries.Where(e => e.State == EntityState.Added).ToArray();
    //    var addedManyToManyRelationships = added.Where(d => d.Metadata.IsManyToManyJoinEntity()).ToArray();
    //    added = added.Except(addedManyToManyRelationships).ToArray();
    //    foreach (var entry in added)
    //    {
    //        if (entry.Metadata.FindAnnotation(nameof(ODataBindManyToManyData))?.Value is ODataBindManyToManyData oDataBindManyToManyData)
    //        {
    //            AddWeakManyToManyRel(httpClient,
    //                       oDataBindManyToManyData,
    //                       entry,
    //                       changeSetContent,
    //                       onAddedContentCreated,
    //                       contentId++);
    //            continue;
    //        }

    //        var properties = GetPropertiesRecusively(entry)
    //            .Where(p => p.CurrentValue != p.Metadata.GetDefaultValue())
    //            .Where(p => !p.IsDataverseReadOnly())
    //            .ToArray();
    //        var json = CreateJsonWithProperties(properties);
    //        var httpMessageContent = CreateHttpMessageContent(
    //            httpClient,
    //            HttpMethod.Post,
    //            contentId++,
    //            entry, json);
    //        changeSetContent.Add(httpMessageContent);
    //        onAddedContentCreated?.Invoke(entry, httpMessageContent, json);
    //    }
    //    foreach (var addedRelationship in addedManyToManyRelationships)
    //    {
    //        var manyToManyRelationshipData = addedRelationship.Metadata.GetManyToManyEntityData() ??
    //            throw new InvalidOperationException("ManyToManyEntityData not found.");
    //        var leftEntityType = Model.GetEntityTypes().First(entity => entity.ClrType == manyToManyRelationshipData.LeftEntityType);
    //        var leftKeyValue = addedRelationship.Property(manyToManyRelationshipData.LeftForeignKey).CurrentValue;
    //        var leftEntitySet = leftEntityType.GetEntitySetName();
    //        var rightEntityType = Model.GetEntityTypes().First(entity => entity.ClrType == manyToManyRelationshipData.RightEntityType);
    //        var rightKeyValue = addedRelationship.Property(manyToManyRelationshipData.RightForeignKey).CurrentValue;
    //        var rightEntitySet = rightEntityType.GetEntitySetName();
    //        var path = $"{httpClient.BaseAddress}{leftEntitySet}({leftKeyValue})/{manyToManyRelationshipData.NavigationFromLeft}/$ref";
    //        var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, path);
    //        var httpMessageContent = new HttpMessageContent(httpRequestMessage);
    //        httpMessageContent.Headers.ContentType = new MediaTypeHeaderValue("application/http");
    //        httpMessageContent.Headers.Add("Content-Transfer-Encoding", "binary");
    //        httpMessageContent.Headers.Add("Content-ID", contentId++.ToString());
    //        var json = $$"""
    //            {
    //                "@odata.id": "{{httpClient.BaseAddress}}{{rightEntitySet}}({{rightKeyValue}})"
    //            }
    //            """;
    //        httpRequestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");
    //        changeSetContent.Add(httpMessageContent);
    //        onAddedContentCreated?.Invoke(addedRelationship, httpMessageContent, json);
    //    }
    //    var modified = entries.Where(e => e.State == EntityState.Modified).ToArray();
    //    foreach (var entry in modified)
    //    {
    //        var properties = GetPropertiesRecusively(entry)
    //            .Where(p => p.IsModified)
    //            .Where(p => !p.IsDataverseReadOnly())
    //            .ToArray();
    //        var json = CreateJsonWithProperties(properties);
    //        var httpMessageContent = CreateHttpMessageContent(
    //            httpClient,
    //            HttpMethod.Patch,
    //            contentId++,
    //            entry, json);
    //        changeSetContent.Add(httpMessageContent);
    //        onModifiedContentCreated?.Invoke(entry, httpMessageContent, json);
    //    }

    //    batchContent.Add(changeSetContent);
    //    request.Content = batchContent;
    //    return request;
    //}

    //private void DeleteWeakManyToManyRel(HttpClient httpClient,
    //                        ODataBindManyToManyData oDataBindManyToManyData,
    //                        EntityEntry entry,
    //                        MultipartContent changeSetContent,
    //                        Action<EntityEntry, HttpMessageContent>? onDeletedContentCreated,
    //                        int contentId)
    //{
    //    var principalEntityType = Model.GetEntityTypes().First(entity => entity.ClrType == oDataBindManyToManyData.PrincipalType);
    //    var principalEntitySet = principalEntityType.GetEntitySetName();
    //    var navigation = principalEntityType.FindNavigation(oDataBindManyToManyData.PrincipalNavigationPropertyName);
    //    var fkToParentName = navigation.ForeignKey.Properties[0].Name;
    //    var fkToParentValue = entry.Property(fkToParentName).CurrentValue;
    //    var targetEntityId = entry.Property(oDataBindManyToManyData.RelForeignKeyToTarget.Member.Name);
    //    var path = $"{httpClient.BaseAddress}{principalEntitySet}({fkToParentValue})/{oDataBindManyToManyData.PrincipalNavigationLogicalName}({targetEntityId.CurrentValue})/$ref";
    //    var httpRequestMessage = new HttpRequestMessage(HttpMethod.Delete, path);
    //    var httpMessageContent = new HttpMessageContent(httpRequestMessage);
    //    httpMessageContent.Headers.ContentType = new MediaTypeHeaderValue("application/http");
    //    httpMessageContent.Headers.Add("Content-Transfer-Encoding", "binary");
    //    httpMessageContent.Headers.Add("Content-ID", contentId.ToString());
    //    changeSetContent.Add(httpMessageContent);
    //    onDeletedContentCreated?.Invoke(entry, httpMessageContent);
    //}

    //private void AddWeakManyToManyRel(HttpClient httpClient, 
    //                        ODataBindManyToManyData oDataBindManyToManyData,
    //                        EntityEntry entry,
    //                        MultipartContent changeSetContent,
    //                        Action<EntityEntry, HttpMessageContent, string>? onAddedContentCreated,
    //                        int contentId)
    //{
    //    var principalEntityType = Model.GetEntityTypes().First(entity => entity.ClrType == oDataBindManyToManyData.PrincipalType);
    //    var principalEntitySet = principalEntityType.GetEntitySetName();
    //    var navigation = principalEntityType.FindNavigation(oDataBindManyToManyData.PrincipalNavigationPropertyName);
    //    var fkToParentName = navigation.ForeignKey.Properties[0].Name;
    //    var fkToParentValue = entry.Property(fkToParentName).CurrentValue;
    //    var path = $"{httpClient.BaseAddress}{principalEntitySet}({fkToParentValue})/{oDataBindManyToManyData.PrincipalNavigationLogicalName}/$ref";
    //    var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, path);
    //    var httpMessageContent = new HttpMessageContent(httpRequestMessage);
    //    httpMessageContent.Headers.ContentType = new MediaTypeHeaderValue("application/http");
    //    httpMessageContent.Headers.Add("Content-Transfer-Encoding", "binary");
    //    httpMessageContent.Headers.Add("Content-ID", contentId.ToString());
    //    var targetEntityId = entry.Property(oDataBindManyToManyData.RelForeignKeyToTarget.Member.Name);
    //    var json = $$"""
    //            {
    //                "@odata.id": "{{httpClient.BaseAddress}}{{oDataBindManyToManyData.TargetEntitySet}}({{targetEntityId.CurrentValue}})"
    //            }
    //            """;
    //    httpRequestMessage.Content = new StringContent(json, Encoding.UTF8, "application/json");
    //    changeSetContent.Add(httpMessageContent);
    //    onAddedContentCreated?.Invoke(entry, httpMessageContent, json);
    //}

    //private static HttpMessageContent CreateHttpMessageContent(
    //   HttpClient httpClient,
    //   HttpMethod httpMethod,
    //   int contentId,
    //   EntityEntry entry,
    //   string? content = null
    //   )
    //{
    //    var primeryKeyProperty = entry.Metadata.FindPrimaryKey()!.Properties[0];
    //    var primaryKeyValue = entry.Property(primeryKeyProperty).CurrentValue;
    //    var identificationPart = httpMethod == HttpMethod.Post
    //        ? string.Empty
    //        : $"({primaryKeyValue})";

    //    var httpRequestMessage = new HttpRequestMessage(
    //        httpMethod,
    //        $"{httpClient.BaseAddress}{entry.Metadata.GetEntitySetName()}{identificationPart}"
    //    );
    //    var httpMessageContent = new HttpMessageContent(httpRequestMessage);
    //    httpMessageContent.Headers.ContentType = new MediaTypeHeaderValue("application/http");
    //    httpMessageContent.Headers.Add("Content-Transfer-Encoding", "binary");
    //    httpMessageContent.Headers.Add("Content-ID", contentId.ToString());
    //    if (content == null) { return httpMessageContent; }
    //    httpRequestMessage.Content = new StringContent(
    //        content,
    //        Encoding.UTF8,
    //        "application/json");
    //    return httpMessageContent;
    //}



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
