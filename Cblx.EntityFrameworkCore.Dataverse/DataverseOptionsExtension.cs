using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Cblx.EntityFrameworkCore.Dataverse;

public class DataverseOptionsExtension : IDbContextOptionsExtension
{
    public string? ClientId { get; private set; }
    public string? ClientSecret { get; private set; }
    public string? ResourceUrl { get; private set; }
    public string? Host => ResourceUrl?.Replace("https://", "").Replace("/", "");
    public string? Authority { get; private set; }

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

    private DataverseOptionsExtension Clone()
    {
        return new DataverseOptionsExtension(this);
    }

    public void ApplyServices(IServiceCollection services)
    {
        // No services to apply
    }

    public void Validate(IDbContextOptions options)
    {
        // No validation needed
    }

    class ExtensionInfo(IDbContextOptionsExtension extension) : DbContextOptionsExtensionInfo(extension)
    {
        public override bool IsDatabaseProvider => false;

        public override string LogFragment => "Dataverse log fragment";

        public override int GetServiceProviderHashCode() => 0;

        public override void PopulateDebugInfo(IDictionary<string, string> debugInfo)
        {
            debugInfo["Dataverse"] = "1";
        }
        public override bool ShouldUseSameServiceProvider(DbContextOptionsExtensionInfo other) => other is ExtensionInfo;
    }
}

