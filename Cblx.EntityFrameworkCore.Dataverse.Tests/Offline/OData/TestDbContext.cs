
namespace Cblx.EntityFrameworkCore.Dataverse.Tests.Offline.OData;

internal class TestDbContext(DbContextOptions<TestDbContext> options) : DataverseDbContext(options)
{

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new ContactConfiguration());
    }
}
