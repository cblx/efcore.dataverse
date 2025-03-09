namespace Cblx.EntityFrameworkCore.Dataverse.Tests.Offline.OData;
public class Tests
{
    [Fact]
    public void QueryEntityTest()
    {
        var builder = new DbContextOptionsBuilder<TestDbContext>();
        builder.UseDataverse(dataverse => dataverse.ResourceUrl("https://fake.api.crm.dynamics.com/"));
        using var db = new TestDbContext(builder.Options);
        var odata = db.Set<Contact>().FromOData().ToODataString();
        odata.Should().Be("contacts?$select=contactid,age,birthdate,firstname");
    }

    [Fact]
    public void MaterializeTest()
    {
        var builder = new DbContextOptionsBuilder<TestDbContext>();
        builder.UseDataverse(dataverse => dataverse.ResourceUrl("https://fake.api.crm.dynamics.com/"));
        using var db = new TestDbContext(builder.Options);
        var contacts = db.Set<Contact>().MaterializeODataResult("""
            {
                "value": [
                    {
                        "contactid": "baea883a-3a49-4df0-8fea-799e5050d215",
                        "firstname": "John",
                        "age": 30,
                        "birthdate": "1990-01-01"
                    },
                    {
                        "contactid": "ac1f7d82-7344-49c9-9451-6bf33adeeb9d",
                        "firstname": "Jane",
                        "age": 25,
                        "birthdate": "1995-01-01"
                    },
                    {
                        "contactid": "ac1f7d82-7344-49c9-9451-6bf33adeeb9e",
                        "firstname": null,
                        "age": null,
                        "birthdate": null
                    }
                ]
            }
            """).ToArray();
        contacts.Should().HaveCount(3);
        contacts[0].Id.Should().Be(new Guid("baea883a-3a49-4df0-8fea-799e5050d215"));
        contacts[0].FirstName.Should().Be("John");
        contacts[0].Age.Should().Be(30);
        contacts[0].Birthdate.Should().Be(new DateOnly(1990, 1, 1));

        contacts[1].Id.Should().Be(new Guid("ac1f7d82-7344-49c9-9451-6bf33adeeb9d"));
        contacts[1].FirstName.Should().Be("Jane");
        contacts[1].Age.Should().Be(25);
        contacts[1].Birthdate.Should().Be(new DateOnly(1995, 1, 1));

        contacts[2].Id.Should().Be(new Guid("ac1f7d82-7344-49c9-9451-6bf33adeeb9e"));
        contacts[2].FirstName.Should().BeNull();
        contacts[2].Age.Should().BeNull();
        contacts[2].Birthdate.Should().BeNull();
    }
}
