//namespace Cblx.EntityFrameworkCore.Dataverse.Tests.Data.Relationship;

//public class Tests
//{
//    static TestContext CreateContext()
//    {
//        var options = new DbContextOptionsBuilder<TestContext>().UseTestDataverse().Options;
//        return new TestContext(options);
//    }

//    [Fact]
//    public async Task Test1()
//    {
//        // Create a thing
//        var thingId = await CreateAThing();
//        // Check thing state
//        await CheckThingState(thingId);
//        // Update thing
//        await UpdateThing(thingId);
//        // Check thing state after update
//        await CheckThingStateAfterUpdate(thingId);
//        ThingId childThingId = await CreateChildThing(thingId);
//        // Check child thing state
//        await CheckChildThingState(thingId, childThingId);
//        // Delete child thing
//        await DeleteChildThing(childThingId);
//        // Check child thing state after delete
//        await CheckIfChildThingIsDeleted(childThingId);
//        // Delete thing
//        await DeleteThing(thingId);
//        // Check thing state after delete
//        await CheckIfThingIsDeleted(thingId);
//    }

//    private static async Task CheckIfThingIsDeleted(ThingId thingId)
//    {
//        using var db = CreateContext();
//        var thing = await db.Things.FindAsync(thingId);
//        thing.Should().BeNull();
//    }

//    private static async Task DeleteThing(ThingId thingId)
//    {
//        using var db = CreateContext();
//        var thing = await db.Things.FindAsync(thingId);
//        db.Things.Remove(thing!);
//        await db.SaveChangesAsync();
//    }

//    private static async Task CheckIfChildThingIsDeleted(ThingId childThingId)
//    {
//        using var db = CreateContext();
//        var childThing = await db.Things.FindAsync(childThingId);
//        childThing.Should().BeNull();
//    }

//    private static async Task DeleteChildThing(ThingId childThingId)
//    {
//        using var db = CreateContext();
//        var childThing = await db.Things.FindAsync(childThingId);
//        db.Things.Remove(childThing!);
//        await db.SaveChangesAsync();
//    }

//    private static async Task CheckChildThingState(ThingId thingId, ThingId childThingId)
//    {
//        using var db = CreateContext();
//        var childThing = await db.Things.FindAsync(childThingId);
//        childThing.Should().NotBeNull();
//        childThing!.Name.Should().Be("Child");
//        childThing.ParentId.Should().Be(thingId);
//    }

//    private static async Task<ThingId> CreateChildThing(ThingId thingId)
//    {
//        ThingId childThingId;
//        // Create a child thing
//        var childThing = new Thing { Name = "Child", ParentId = thingId };
//        using var db = CreateContext();
//        db.Things.Add(childThing);
//        await db.SaveChangesAsync();
//        childThingId = childThing.Id;
//        return childThingId;
//    }

//    private static async Task CheckThingStateAfterUpdate(ThingId thingId)
//    {
//        using var db = CreateContext();
//        var thing = await db.Things.FindAsync(thingId);
//        thing.Should().NotBeNull();
//        thing!.Name.Should().Be("Test2");
//    }

//    private static async Task UpdateThing(ThingId thingId)
//    {
//        using var db = CreateContext();
//        var thing = await db.Things.FindAsync(thingId);
//        thing!.Name = "Test2";
//        await db.SaveChangesAsync();
//    }

//    private static async Task CheckThingState(ThingId thingId)
//    {
//        using var db = CreateContext();
//        var thing = await db.Things.FindAsync(thingId);
//        thing.Should().NotBeNull();
//        thing!.Name.Should().Be("Test");
//    }

//    private static async Task<ThingId> CreateAThing()
//    {
//        var thing = new Thing { Name = "Test" };
//        using var db = CreateContext();
//        db.Things.Add(thing);
//        await db.SaveChangesAsync();
//        return thing.Id;
//    }
//}