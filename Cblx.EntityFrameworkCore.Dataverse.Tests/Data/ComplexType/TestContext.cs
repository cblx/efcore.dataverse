namespace Cblx.EntityFrameworkCore.Dataverse.Tests.Data.ComplexType;

internal class TestContext(DbContextOptions<TestContext> options) : DataverseDbContext(options)
{
    public DbSet<Thing> Things => Set<Thing>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ThingConfiguration());
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<ThingId>().HaveConversion<ThingId.EfCoreValueConverter>();
    }
}
