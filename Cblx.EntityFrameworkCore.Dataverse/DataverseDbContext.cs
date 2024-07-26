using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
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
        if (!ChangeTracker.HasChanges())
        {
            return 0;
        }

        var httpClient = CreateHttpClient();
        var request = CreateBatchRequest();
        var entries = ChangeTracker.Entries().ToArray();
        var batchContent = new MultipartContent("mixed", $"batch_{Guid.NewGuid()}");
        var changeSetContent = new MultipartContent("mixed", $"changeset_{Guid.NewGuid()}");
        var deleted = entries.Where(e => e.State == EntityState.Deleted).ToArray();
        int contentId = 0;
        Log(DataverseEventId.CreatingBatchRequest, $"Context '{GetType().Name}' started creating batch request for saving changes.");
        foreach (var entry in deleted)
        {
            var httpMessageContent = CreateHttpMessageContent(httpClient, HttpMethod.Delete, contentId++, entry);
            changeSetContent.Add(httpMessageContent);
            Log(DataverseEventId.CreatingBatchRequestMessageContentItem,
               loggingOptions.IsSensitiveDataLoggingEnabled ?
               $"""
                '{GetType().Name}' created a request message content for deleting a '{entry.Metadata.ShortName()}' entity.
                {httpMessageContent.HttpRequestMessage.Method} {httpMessageContent.HttpRequestMessage.RequestUri}
                """ :
               $"'{GetType().Name}' created a request message content for deleting a '{entry.Metadata.ShortName()}' entity. Consider using 'DbContextOptionsBuilder.EnableSensitiveDataLogging' to see all values."
            );
        }
        var added = entries.Where(e => e.State == EntityState.Added).ToArray();
        foreach (var entry in added)
        {
            var properties = GetPropertiesRecusively(entry)
                .Where(p => p.CurrentValue != p.Metadata.GetDefaultValue())
                .Where(p => !p.IsDataverseReadOnly())
                .ToArray();
            var json = CreateJsonWithProperties(properties);
            var httpMessageContent = CreateHttpMessageContent(
                httpClient,
                HttpMethod.Post,
                contentId++,
                entry, json);
            changeSetContent.Add(httpMessageContent);
            Log(DataverseEventId.CreatingBatchRequestMessageContentItem,
               loggingOptions.IsSensitiveDataLoggingEnabled ?
               $"""
                '{GetType().Name}' created a request message content for inserting a '{entry.Metadata.ShortName()}' entity.
                {httpMessageContent.HttpRequestMessage.Method} {httpMessageContent.HttpRequestMessage.RequestUri}
                {json}
                """ :
               $"'{GetType().Name}' created a request message content for inserting a '{entry.Metadata.ShortName()}' entity. Consider using 'DbContextOptionsBuilder.EnableSensitiveDataLogging' to see all values."
            );
        }
        var modified = entries.Where(e => e.State == EntityState.Modified).ToArray();
        foreach (var entry in modified)
        {
            var properties = GetPropertiesRecusively(entry)
                .Where(p => p.IsModified)
                .Where(p => !p.IsDataverseReadOnly())
                .ToArray();
            var json = CreateJsonWithProperties(properties);
            var httpMessageContent = CreateHttpMessageContent(
                httpClient,
                HttpMethod.Patch,
                contentId++,
                entry, json);
            changeSetContent.Add(httpMessageContent);
            Log(DataverseEventId.CreatingBatchRequestMessageContentItem,
                loggingOptions.IsSensitiveDataLoggingEnabled ?
                $"""
                '{GetType().Name}' created a request message content for updating a '{entry.Metadata.ShortName()}' entity.
                {httpMessageContent.HttpRequestMessage.Method} {httpMessageContent.HttpRequestMessage.RequestUri}
                {json}
                """ :
                $"'{GetType().Name}' created a request message content for updating a '{entry.Metadata.ShortName()}' entity. Consider using 'DbContextOptionsBuilder.EnableSensitiveDataLogging' to see all values."
            );
        }

        batchContent.Add(changeSetContent);
        request.Content = batchContent;
        var response = await httpClient.SendAsync(request, cancellationToken);
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
                var multiPartResponse = await response.Content.ReadAsMultipartAsync(cancellationToken);
                var firstContent = multiPartResponse.Contents[0];
                var firstPart = await firstContent.ReadAsMultipartAsync(cancellationToken);
                var firstContentAsString = await firstPart.Contents[0].ReadAsStringAsync(cancellationToken);
                firstContentAsString = firstContentAsString.Split("\r\n")[^1];
                var json = JsonSerializer.Deserialize<JsonObject>(firstContentAsString);
                // Ex: {"error":{"code":"0x80040237","message":"A record with matching key values already exists."}}
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

    private static HttpMessageContent CreateHttpMessageContent(
        HttpClient httpClient,
        HttpMethod httpMethod,
        int contentId,
        EntityEntry entry,
        string? content = null
        )
    {
        var primeryKeyProperty = entry.Metadata.FindPrimaryKey()!.Properties[0];
        var primaryKeyValue = entry.Property(primeryKeyProperty).CurrentValue;
        var identificationPart = httpMethod == HttpMethod.Post
            ? string.Empty
            : $"({primaryKeyValue})";

        var httpRequestMessage = new HttpRequestMessage(
            httpMethod,
            $"{httpClient.BaseAddress}{entry.Metadata.GetEntitySetName()}{identificationPart}"
        );
        var httpMessageContent = new HttpMessageContent(httpRequestMessage);
        httpMessageContent.Headers.ContentType = new MediaTypeHeaderValue("application/http");
        httpMessageContent.Headers.Add("Content-Transfer-Encoding", "binary");
        httpMessageContent.Headers.Add("Content-ID", contentId.ToString());
        if (content == null) { return httpMessageContent; }
        httpRequestMessage.Content = new StringContent(
            content,
            Encoding.UTF8,
            "application/json");
        return httpMessageContent;
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
