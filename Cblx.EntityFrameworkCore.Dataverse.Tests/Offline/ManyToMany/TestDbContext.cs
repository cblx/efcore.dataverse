namespace Cblx.EntityFrameworkCore.Dataverse.Tests.Offline.ManyToMany;
internal class TestDbContext(DbContextOptions<TestDbContext> options) : DataverseDbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new AccountConfiguration());
        modelBuilder.ApplyConfiguration(new CnaeConfiguration());
    }
}
