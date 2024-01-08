using Cblx.EntityFrameworkCore.Dataverse.Tests.Data.Default;
using FluentAssertions;
namespace Cblx.EntityFrameworkCore.Dataverse.Tests;
public class UnitTest1
{
    [Fact]
    public async Task Test1()
    {
        var thing = new Thing { Name = "Test" };
        await Create(thing);
        //await Task.Delay(1000);
        await CheckState(thing);
        //await Task.Delay(1000);
        await Update(thing);
        //await Task.Delay(1000);
        await CheckState(thing);
        //await Task.Delay(1000);
        await Delete(thing);
        //await Task.Delay(1000);
        await CheckIsDeleted(thing);
    }

    private static async Task CheckState(Thing thing)
    {
        var db = Helpers.GetDefaultContext();
        var thing2 = await db.Things.FirstOrDefaultAsync(t => t.Id == thing.Id);
        thing2.Should().BeEquivalentTo(thing);
    }

    private static async Task Create(Thing thing)
    {
        var db = Helpers.GetDefaultContext();
        db.Things.Add(thing);
        await db.SaveChangesAsync();
    }

    private static async Task Update(Thing thing)
    {
        var db = Helpers.GetDefaultContext();
        thing.Name = "Test2";
        await db.SaveChangesAsync();
    }

    private static async Task Delete(Thing thing)
    {
        var db = Helpers.GetDefaultContext();
        db.Things.Remove(thing);
        await db.SaveChangesAsync();
    }

    private static async Task CheckIsDeleted(Thing thing)
    {
        var db = Helpers.GetDefaultContext();
        var thing2 = await db.Things.FirstOrDefaultAsync(t => t.Id == thing.Id);
        thing2.Should().BeNull();
    }
}