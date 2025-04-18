


//namespace Cblx.EntityFrameworkCore.Dataverse.Tests.Data.SubCollectionWithCascadingDelete;
//public class Tests
//{
//    static TestContext CreateContext()
//    {
//        var options = new DbContextOptionsBuilder<TestContext>().UseTestDataverse().Options;
//        return new TestContext(options);
//    }

//    [Fact]
//    public async Task CrudTest()
//    {
//        var parentId = ThingId.New();
//        var childId = ChildThingId.New();
//        var childId2 = ChildThingId.New();
//        await CreateWithChildrenAsync(parentId, childId, childId2);
//        await AssertHasChildrenAsync(parentId, 2);
//        await RemoveChildAsync(parentId, childId);
//        await AssertHasChildrenAsync(parentId, 1);
//        await AssertDeletedAsync(childId);
//        await DeleteAsync(parentId);
//        await AssertDeletedAsync(childId2);
//    }

//    private static async Task DeleteAsync(ThingId parentId)
//    {
//        using var db = CreateContext();
//        var parent = await db.Things.Where(t => t.Id == parentId).Include(t => t.Children).SingleAsync();
//        db.Things.Remove(parent);
//        await db.SaveChangesAsync();
//    }

//    private static async Task AssertDeletedAsync(ChildThingId childId)
//    {
//        using var db = CreateContext();
//        var child = await db.ChildThings.Where(t => t.Id == childId).FirstOrDefaultAsync();
//        child.Should().BeNull();
//    }

//    private static async Task RemoveChildAsync(ThingId parentId, ChildThingId childId)
//    {
//        using var db = CreateContext();
//        var parent = await db.Things.Where(t => t.Id == parentId).Include(t => t.Children).SingleAsync();
//        var child = parent.Children.Single(t => t.Id == childId);
//        parent.Children.Remove(child);
//        await db.SaveChangesAsync();
//    }

//    private static async Task AssertHasChildrenAsync(ThingId parentId, int count)
//    {
//        using var db = CreateContext();
//        var thing = await db.Things.Where(t => t.Id == parentId).Include(t => t.Children).SingleAsync();
//        thing.Children.Should().HaveCount(count);
//    }

//    private static async Task CreateWithChildrenAsync(ThingId parentId, params ChildThingId[] childId)
//    {
//        using var db = CreateContext();
//        var thing = new Thing { Id = parentId, Name = "Thing 1" };
//        db.Things.Add(thing);
//        int i = 1;
//        foreach (var id in childId)
//        {
//            thing.Children.Add(new () { Id = id, Name = $"Child Thing 1.{i++}" });
//        }
//        await db.SaveChangesAsync();
//    }
//}
