using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Cblx.EntityFrameworkCore.Dataverse;

public class DataverseDbContext(DbContextOptions options) : DbContext(options) // Yep, and what's the problem?
{
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        if (!ChangeTracker.HasChanges()) { return 0; }
        HttpClient httpClient = CreateHttpClient();
        HttpRequestMessage request = CreateBatchRequest();
        var entries = ChangeTracker.Entries().ToArray();
        var batchContent = new MultipartContent("mixed", $"batch_{Guid.NewGuid()}");
        var changeSetContent = new MultipartContent("mixed", $"changeset_{Guid.NewGuid()}");
        var deleted = entries.Where(e => e.State == EntityState.Deleted).ToArray();
        int contentId = 0;
        foreach (var entry in deleted)
        {
            changeSetContent.Add(CreateHttpMessageContent(httpClient, HttpMethod.Delete, contentId++, entry));
        }
        var added = entries.Where(e => e.State == EntityState.Added).ToArray();
        foreach (var entry in added)
        {
            var properties = entry
                .Properties
                .Where(p => p.CurrentValue != p.Metadata.GetDefaultValue())
                .ToArray();
            var httpMessageContent = CreateHttpMessageContent(
                httpClient,
                HttpMethod.Post,
                contentId++,
                entry, CreateJsonWithProperties(properties));
            changeSetContent.Add(httpMessageContent);
        }
        var modified = entries.Where(e => e.State == EntityState.Modified).ToArray();
        foreach (var entry in modified)
        {
            var properties = entry.Properties.Where(p => p.IsModified).ToArray();
            var httpMessageContent = CreateHttpMessageContent(
                httpClient,
                HttpMethod.Patch,
                contentId++,
                entry, CreateJsonWithProperties(properties));
            changeSetContent.Add(httpMessageContent);
        }

        batchContent.Add(changeSetContent);
        request.Content = batchContent;
        var response = await httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(content);
        }
        ChangeTracker.AcceptAllChanges();
        return -1;
    }

    private static StringBuilder CreateJsonWithProperties(PropertyEntry[] properties)
    {
        var sbContent = new StringBuilder();
        sbContent.AppendLine("{");
        foreach (var property in properties)
        {
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
        return sbContent;
    }

    private static HttpMessageContent CreateHttpMessageContent(
        HttpClient httpClient,
        HttpMethod httpMethod,
        int contentId,
        EntityEntry entry,
        StringBuilder? sbContent = null
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
        if (sbContent == null) { return httpMessageContent; }
        httpRequestMessage.Content = new StringContent(
            sbContent.ToString(),
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
        var extension = options.FindExtension<DataverseOptionsExtension>();
        var httpClientFactory = this.GetService<IHttpClientFactory>();
        var httpClient = httpClientFactory!.CreateClient(extension!.HttpClientName!);
        return httpClient;
    }
}
