namespace Cblx.EntityFrameworkCore.Dataverse.Tests.Data.SubCollectionWithDivergentNavigationName;
public class Tests
{
    static TestContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TestContext>().UseTestDataverse().Options;
        return new TestContext(options);
    }

    [Fact]
    public async Task CrudTest()
    {
        using var db = CreateContext();
        var thing = new Thing { Id = ThingId.New(), Name = "Thing 1" };
        thing.Children.Add(new () { Id = ChildThingId.New(), Name = "Child Thing 1.1" });
        db.Things.Add(thing);
        var execution = async () => await db.SaveChangesAsync();
        await execution.Should().ThrowAsync<DbUpdateException>().WithMessage("*An undeclared property 'creec_wrong_name'*");
    }
}
