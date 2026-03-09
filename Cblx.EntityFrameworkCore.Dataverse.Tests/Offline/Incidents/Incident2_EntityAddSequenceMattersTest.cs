using System;
using System.Collections.Generic;
using System.Text;

namespace Cblx.EntityFrameworkCore.Dataverse.Tests.Offline.Incidents;

public class Incident2_EntityAddSequenceMattersTest
{
    [Fact]
    public async Task CheckProblemTest()
    {
        var builder = new DbContextOptionsBuilder<TestDbContext>();
        builder.UseDataverse(dataverse => dataverse.ResourceUrl("https://fake.api.crm.dynamics.com/"));
        await using var db = new TestDbContext(builder.Options);
        var entity1 = new Entity1();
        db.Entities1.Add(entity1);
        var entity3 = new Entity3() { FkId = entity1.Id };
        db.Entities3.Add(entity3);
        var entity2 = new Entity2() { FkId = entity1.Id };
        db.Entities2.Add(entity2);
        var batch = db.GetBatchCommandForAssertion();
        batch.Should().Be($$"""
             x           
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
    public Guid FkId { get; set; }
}

file class Entity3
{
    public Guid Id { get; set; }
    public Guid FkId { get; set; }

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
        var entity2 = modelBuilder.Entity<Entity2>();
        entity2.ToTable("entity2").ToEntitySet("entities2");
        var entity1 = modelBuilder.Entity<Entity1>();
        entity1.ToTable("entity1").ToEntitySet("entities1");
        var entity3 = modelBuilder.Entity<Entity3>();
        entity3.ToTable("entity3").ToEntitySet("entities3");
    }
}