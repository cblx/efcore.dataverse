namespace Cblx.EntityFrameworkCore.Dataverse.Tests.Data.SubCollectionWithDivergentNavigationName;

internal class TestContext(DbContextOptions<TestContext> options) : DataverseDbContext(options)
{
    public DbSet<Thing> Things => Set<Thing>();
    // Just to test if it was deleted or updated
    public DbSet<ChildThing> ChildThings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ThingConfiguration());
        modelBuilder.ApplyConfiguration(new ChildThingConfiguration());
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<ThingId>().HaveConversion<ThingId.EfCoreValueConverter>();
        configurationBuilder.Properties<ChildThingId>().HaveConversion<ChildThingId.EfCoreValueConverter>();
    }
}
