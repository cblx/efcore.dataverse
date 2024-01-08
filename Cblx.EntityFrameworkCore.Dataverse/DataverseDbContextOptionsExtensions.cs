using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Cblx.EntityFrameworkCore.Dataverse;

public static class DataverseDbContextOptionsExtensions
{
    public static DbContextOptionsBuilder UseDataverse(
        this DbContextOptionsBuilder optionsBuilder,
        Action<DataverseDbContextOptionsBuilder> dataverseOptionsAction)
    {
        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(GetOrCreateExtension(optionsBuilder));
        optionsBuilder = ApplyConfiguration(optionsBuilder, dataverseOptionsAction);
        var extension = GetOrCreateExtension(optionsBuilder);
        var connectionString = $"""
            Server={extension.Host}; Authentication=Active Directory Service Principal; Encrypt=True; User Id={extension.ClientId}; Password={extension.ClientSecret}
            """;
        return optionsBuilder.UseSqlServer(connectionString, sqlOptions =>
        {
            if (extension.CommandTimeout.HasValue)
            {
                sqlOptions.CommandTimeout(extension.CommandTimeout.Value);
            }
        });
    }

    private static DbContextOptionsBuilder ApplyConfiguration(
        DbContextOptionsBuilder optionsBuilder,
        Action<DataverseDbContextOptionsBuilder> dataverseOptionsAction)
    {
        dataverseOptionsAction.Invoke(new DataverseDbContextOptionsBuilder(optionsBuilder));
        var extension = GetOrCreateExtension(optionsBuilder);
        ((IDbContextOptionsBuilderInfrastructure)optionsBuilder).AddOrUpdateExtension(extension);
        return optionsBuilder;
    }

    private static DataverseOptionsExtension GetOrCreateExtension(DbContextOptionsBuilder optionsBuilder)
       => optionsBuilder.Options.FindExtension<DataverseOptionsExtension>() ?? new DataverseOptionsExtension();
}