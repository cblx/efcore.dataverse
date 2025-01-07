//namespace Cblx.EntityFrameworkCore.Dataverse.Tests.Data.Default;
//public class Tests
//{
//    static TestContext CreateContext()
//    {
//        var options = new DbContextOptionsBuilder<TestContext>().UseTestDataverse().Options;
//        return new TestContext(options);
//    }
//    [Fact]
//    public async Task ExceptionHandlingTest()
//    {
//        // Creating a thing with fixed id
//        var id = ThingId.New();
//        var thing = new Thing { Id = id, Name = "Test" };
//        var id2 = ThingId.New();
//        var thing2 = new Thing { Id = id2, Name = "Test2" };
//        await Add(thing, thing2);
//        await AddAgainWithAssertion(thing, thing2);
//        await Delete(thing);
//        await Delete(thing2);

//        static async Task Delete(Thing thing)
//        {
//            // Add thing to database
//            using var context = CreateContext();
//            context.Things.Attach(thing);
//            context.Things.Remove(thing);
//            await context.SaveChangesAsync();
//        }

//        static async Task AddAgainWithAssertion(Thing thing, Thing thing2)
//        {
//            // Try to add thing with same id to database
//            using var context = CreateContext();
//            // Add one more before to see if affects somehow the response error
//            // (we've seen tha only the first failed request will be shown in the response)
//            context.Things.Add(new Thing { Id = ThingId.New(), Name = "Test" }); // TODO: We're not cleaning up this thing after the test.
//            context.Things.Add(thing);
//            context.Things.Add(thing2);
//            // Assertion with fluent validation
//            var execution = () => context.SaveChangesAsync();
//            await execution.Should().ThrowAsync<DbUpdateException>().WithMessage("A record with matching key values already exists.");

//        }

//        static async Task Add(Thing thing, Thing thing2)
//        {

//            // Add thing to database
//            using var context = CreateContext();
//            context.Things.Add(thing);
//            context.Things.Add(thing2);
//            await context.SaveChangesAsync();
//        }
//    }
//    [Fact]
//    public async Task CrudTest()
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
