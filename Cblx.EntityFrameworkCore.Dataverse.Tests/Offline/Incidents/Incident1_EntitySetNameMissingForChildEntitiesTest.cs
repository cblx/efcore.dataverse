namespace Cblx.EntityFrameworkCore.Dataverse.Tests.Offline.Incidents;
public class Incident1_EntitySetNameMissingForChildEntitiesTest
{
    [Fact]
    public async Task CheckProblemTest()
    {
        var builder = new DbContextOptionsBuilder<TestDbContext>();
        builder.UseDataverse(dataverse => dataverse.ResourceUrl("https://fake.api.crm.dynamics.com/"));
        await using var db = new TestDbContext(builder.Options);
        var childEntity = new ChildEntity();
        db.Add(childEntity);
        var batch = await db.GetBatchCommandForAssertionAsync(); batch.Should().Be($$"""
            --batch_00000000-0000-0000-0000-000000000000
            Content-Type: multipart/mixed; boundary="changeset_00000000-0000-0000-0000-000000000000"

            --changeset_00000000-0000-0000-0000-000000000000
            Content-Type: application/http
            Content-Transfer-Encoding: binary
            Content-ID: 0

            POST /api/data/v9.2/baseentities HTTP/1.1
            Host: fake.api.crm.dynamics.com
            Content-Type: application/json; charset=utf-8
            Content-Length: 72

            {
                "Id": "{{childEntity.Id}}",
                "Type": 1
            }

            --changeset_00000000-0000-0000-0000-000000000000--

            --batch_00000000-0000-0000-0000-000000000000--
            
            """);
    }

}

file abstract class BaseEntity
{
    public Guid Id { get; set; }
    public int Type { get; set; }
}


file class ChildEntity : BaseEntity
{
    public string Prop1 { get; set; }
}

file class TestDbContext : DataverseDbContext
{
    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
    {
        
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var baseEntity = modelBuilder.Entity<BaseEntity>();
        baseEntity.ToTable("baseentity").ToEntitySet("baseentities");
        baseEntity.HasDiscriminator(baseEntity => baseEntity.Type)
                  .HasValue<ChildEntity>(1);
    }
}