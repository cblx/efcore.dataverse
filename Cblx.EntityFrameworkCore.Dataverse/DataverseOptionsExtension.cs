using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Cblx.EntityFrameworkCore.Dataverse;

public class DataverseOptionsExtension : IDbContextOptionsExtension
{
    public int? CommandTimeout { get; private set; }
    public TimeSpan? HttpRequestTimeout { get; private set; }
    public string? ClientId { get; private set; }
    public string? ClientSecret { get; private set; }
    public string? ResourceUrl { get; private set; }
    public string? Host => ResourceUrl?.Replace("https://", "").Replace("/", "");
    public string? Authority { get; private set; }

    internal string? HttpClientName => $"Cblx.EntityFrameworkCore.Dataverse|{ResourceUrl}";

    public DataverseOptionsExtension()
    {
        
    }

    public DataverseOptionsExtension(DataverseOptionsExtension copyFrom)
    {
        ClientId = copyFrom.ClientId;
        ClientSecret = copyFrom.ClientSecret;
        ResourceUrl = copyFrom.ResourceUrl;
        Authority = copyFrom.Authority;
    }

    public DbContextOptionsExtensionInfo Info => new ExtensionInfo(this);

    public DataverseOptionsExtension WithClientId(string clientId)
    {
        var clone = Clone();
        clone.ClientId = clientId;
        return clone;
    }

    public DataverseOptionsExtension WithClientSecret(string clientSecret)
    {
        var clone = Clone();
        clone.ClientSecret = clientSecret;
        return clone;
    }

    public DataverseOptionsExtension WithResourceUrl(string resourceUrl)
    {
        var clone = Clone();
        clone.ResourceUrl = resourceUrl;
        return clone;
    }

    public DataverseOptionsExtension WithAuthority(string authority)
    {
        var clone = Clone();
        clone.Authority = authority;
        return clone;
    }

    public DataverseOptionsExtension WithCommandTimeout(int commandTimeout)
    {
        var clone = Clone();
        clone.CommandTimeout = commandTimeout;
        return clone;
    }

    public DataverseOptionsExtension WithHttpRequestTimeout(TimeSpan httpRequestTimeout)
    {
        var clone = Clone();
        clone.HttpRequestTimeout = httpRequestTimeout;
        return clone;
    }

    private DataverseOptionsExtension Clone()
    {
        return new DataverseOptionsExtension(this);
    }
    
    public void ApplyServices(IServiceCollection services)
    {
        services.AddHttpClient(HttpClientName!, client =>
        {
            client.BaseAddress = new Uri($"{ResourceUrl}{Consts.ApiDataPath}");
            if (HttpRequestTimeout.HasValue)
            {
                client.Timeout = HttpRequestTimeout.Value;
            }
            
        }).AddHttpMessageHandler(sp => new DynamicsAuthorizationMessageHandler(this));
    }

    public void Validate(IDbContextOptions options)
    {
        // No validation needed
    }

    class ExtensionInfo(IDbContextOptionsExtension extension) : DbContextOptionsExtensionInfo(extension)
    {
        private new DataverseOptionsExtension Extension
         => (DataverseOptionsExtension)base.Extension;

        public override bool IsDatabaseProvider => false;

        public override string LogFragment => "Dataverse log fragment";

        public override int GetServiceProviderHashCode()
        {
            var hashCode = new HashCode();
            hashCode.Add(Extension.ClientId);
            hashCode.Add(Extension.ClientSecret);
            hashCode.Add(Extension.ResourceUrl);
            hashCode.Add(Extension.Authority);
            return hashCode.ToHashCode();
        }

        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
        {
            debugInfo["Dataverse"] = "1";
        }
        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other) => other is ExtensionInfo;
    }
}

