using Cblx.EntityFrameworkCore.Dataverse.Tests.Data.Default;
namespace Cblx.EntityFrameworkCore.Dataverse.Tests;
public class UnitTest1
{
    [Fact]
    public async Task Test1()
    {
        var db = Helpers.GetDefaultContext();
        db.Things.Add(new Thing { Name = "Test"});
        await db.SaveChangesAsync();
    }
}