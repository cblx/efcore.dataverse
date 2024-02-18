using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection.Metadata;
using System.Text;
using System.Text.Json;

namespace Cblx.EntityFrameworkCore.Dataverse;


public class DataverseEventDefinition(ILoggingOptions loggingOptions, EventId eventId, LogLevel level, string eventIdCode) : EventDefinitionBase(
    loggingOptions,
    eventId,
    level, eventIdCode
    )
{
}

public enum DataverseEventId
{
    CreatingBatchRequest = 83_001,
    SendingBatchRequest = 83_002,
    BatchRequestSucceeded = 83_003,
    BatchRequestFailed = 83_004,
    CreatingBatchRequestMessageContentItem = 83_101
}

public class DataverseDbContext(DbContextOptions options) : DbContext(options)
{
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var logger = this.GetService<IDbContextLogger>();
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
            var properties = entry
                .Properties
                .Where(p => p.CurrentValue != p.Metadata.GetDefaultValue())
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
            var properties = entry.Properties.Where(p => p.IsModified).ToArray();
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

        try
        {
            var response = await httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            Log(DataverseEventId.BatchRequestSucceeded, $"Context '{GetType().Name}' successfully sent batch request for saving changes.");
        }catch (Exception ex)
        {
            Log(DataverseEventId.BatchRequestFailed, $"Context '{GetType().Name}' failed to send batch request for saving changes. {ex.Message}");
            throw;
        }
        //if (!response.IsSuccessStatusCode)
        //{
        //    var content = await response.Content.ReadAsStringAsync(cancellationToken);
        //    throw new InvalidOperationException(content);
        //}
        ChangeTracker.AcceptAllChanges();
        return -1;
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
                    "{property.Metadata.GetColumnName()}": {JsonSerializer.Serialize(property.CurrentValue)}
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
