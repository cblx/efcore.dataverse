using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Cblx.EntityFrameworkCore.Dataverse;

public class DataverseDbContextOptionsBuilder(DbContextOptionsBuilder optionsBuilder)
{
    public DataverseDbContextOptionsBuilder ClientId(string clientId)
        => WithOption(e => e.WithClientId(clientId));

    public DataverseDbContextOptionsBuilder ClientSecret(string clientSecret)
        => WithOption(e => e.WithClientSecret(clientSecret));

    public DataverseDbContextOptionsBuilder ResourceUrl(string resourceUrl)
        => WithOption(e => e.WithResourceUrl(resourceUrl));

    public DataverseDbContextOptionsBuilder Authority(string authority)
        => WithOption(e => e.WithAuthority(authority));

    public DataverseDbContextOptionsBuilder CommandTimeout(int commandTimeout)
        => WithOption(e => e.WithCommandTimeout(commandTimeout));

    public DataverseDbContextOptionsBuilder HttpRequestTimeout(TimeSpan httpRequestTimeout)
        => WithOption(e => e.WithHttpRequestTimeout(httpRequestTimeout));

    private DataverseDbContextOptionsBuilder WithOption(Func<DataverseOptionsExtension, DataverseOptionsExtension> setAction)
    {
        var extension = optionsBuilder.Options.FindExtension<DataverseOptionsExtension>();
        extension = setAction(extension ?? new DataverseOptionsExtension());
        var builderInfrastructure = (IDbContextOptionsBuilderInfrastructure)optionsBuilder;
        builderInfrastructure.AddOrUpdateExtension(extension);
        return this;
    }
}

