namespace Cblx.EntityFrameworkCore.Dataverse.Tests.Data.Default;

internal class DefaultTestContext(DbContextOptions options) : DataverseDbContext(options)
{
    public DbSet<Thing> Things => Set<Thing>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ThingConfiguration());
    }
}
