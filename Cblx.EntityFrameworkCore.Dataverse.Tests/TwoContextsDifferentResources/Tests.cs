//namespace Cblx.EntityFrameworkCore.Dataverse.Tests.TwoContextsDifferentResources;
//public class Tests
//{
//    [Fact]
//    public async Task ShouldBeAbleToUse2ContextsForDifferentConnectionsTest()
//    {
//        var options2 = new DbContextOptionsBuilder<TestContext>().UseTestDataverse2().Options;
//        using var context2 = new TestContext(options2);
//        var options = new DbContextOptionsBuilder<TestContext>().UseTestDataverse().Options;
//        using var context = new TestContext(options);
//        context.Add(new Thing { Id = ThingId.New(), Name = "Test" });
//        var s = () => context.SaveChangesAsync();
//        await s.Should().NotThrowAsync<InvalidOperationException>();
//    }
//}
