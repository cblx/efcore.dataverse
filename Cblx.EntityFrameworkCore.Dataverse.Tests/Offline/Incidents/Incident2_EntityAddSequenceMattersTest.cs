using System;
using System.Collections.Generic;
using System.Text;

namespace Cblx.EntityFrameworkCore.Dataverse.Tests.Offline.Incidents;

public class Incident2_EntityAddSequenceTest
{
    [Fact]
    public async Task CheckProblemTest()
    {
        var builder = new DbContextOptionsBuilder<TestDbContext>();
        builder.UseDataverse(dataverse => dataverse.ResourceUrl("https://fake.api.crm.dynamics.com/"));
        await using var db = new TestDbContext(builder.Options);
        db.Entities1.Add(new());
        db.Entities2.Add(new());
        db.Entities3.Add(new());
        db.ChangeTracker.AcceptAllChanges();
        var entity1 = new Entity1();
        db.Entities1.Add(entity1);
        var entity2 = new Entity2() { Entity1Id = entity1.Id };
        db.Entities2.Add(entity2);
        var entity3 = new Entity3() { Entity2Id = entity2.Id };
        db.Entities3.Add(entity3);
        var entity3_2 = new Entity3();
        db.Entities3.Add(entity3_2);

        var batch = db.GetBatchCommandForAssertion();
        batch.Should().Be($$"""
            --batch_00000000-0000-0000-0000-000000000000
            Content-Type: multipart/mixed; boundary=changeset_00000000-0000-0000-0000-000000000000

            --changeset_00000000-0000-0000-0000-000000000000
            Content-Type: application/http
            Content-Transfer-Encoding: binary
            Content-ID: 0

            POST /api/data/v9.2/entities1 HTTP/1.1
            Content-Type: application/json

            {
                "Id": "{{entity1.Id}}"
            }

            --changeset_00000000-0000-0000-0000-000000000000
            Content-Type: application/http
            Content-Transfer-Encoding: binary
            Content-ID: 1

            POST /api/data/v9.2/entities2 HTTP/1.1
            Content-Type: application/json

            {
                "Id": "{{entity2.Id}}",
                "Entity1Id@odata.bind": "entities1({{entity1.Id}})"
            }

            --changeset_00000000-0000-0000-0000-000000000000
            Content-Type: application/http
            Content-Transfer-Encoding: binary
            Content-ID: 2

            POST /api/data/v9.2/entities3 HTTP/1.1
            Content-Type: application/json

            {
                "Id": "{{entity3.Id}}",
                "Entity2Id@odata.bind": "entities2({{entity2.Id}})"
            }
            
            --changeset_00000000-0000-0000-0000-000000000000
            Content-Type: application/http
            Content-Transfer-Encoding: binary
            Content-ID: 3
            
            POST /api/data/v9.2/entities3 HTTP/1.1
            Content-Type: application/json
            
            {
                "Id": "{{entity3_2.Id}}"
            }
            
            --changeset_00000000-0000-0000-0000-000000000000--

            --batch_00000000-0000-0000-0000-000000000000--
            
            """);
    }
}


file class Entity1
{
    public Guid Id { get; set; }
}

file class Entity2
{
    public Guid Id { get; set; }
    public Guid Entity1Id { get; set; }
}

file class Entity3
{
    public Guid Id { get; set; }
    public Guid? Entity2Id { get; set; }
}

file class TestDbContext : DataverseDbContext
{
    public DbSet<Entity1> Entities1 { get; set; }
    public DbSet<Entity2> Entities2 { get; set; }
    public DbSet<Entity3> Entities3 { get; set; }

    public TestDbContext(DbContextOptions<TestDbContext> options) : base(options)
    {

    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var entity1 = modelBuilder.Entity<Entity1>();
        entity1.ToTable("entity1").ToEntitySet("entities1");

        var entity2 = modelBuilder.Entity<Entity2>();
        entity2.ToTable("entity2").ToEntitySet("entities2");
        entity2.HasOne<Entity1>().WithMany().HasForeignKey(e => e.Entity1Id);

        var entity3 = modelBuilder.Entity<Entity3>();
        entity3.ToTable("entity3").ToEntitySet("entities3");
        entity3.HasOne<Entity2>().WithMany().HasForeignKey(e => e.Entity2Id);
    }
}