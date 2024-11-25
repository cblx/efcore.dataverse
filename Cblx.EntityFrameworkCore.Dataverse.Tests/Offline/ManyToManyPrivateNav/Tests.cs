namespace Cblx.EntityFrameworkCore.Dataverse.Tests.Offline.ManyToManyPrivateNav;
public class Tests
{
    [Fact]
    public async Task CreateTest()
    {
        var builder = new DbContextOptionsBuilder<TestDbContext>();
        builder.UseDataverse(dataverse => dataverse.ResourceUrl("https://fake.api.crm.dynamics.com/"));
        await using var db = new TestDbContext(builder.Options);
        var account = new Account();
        var cnaeRel = new CnaeRel() { CnaeId = Guid.NewGuid() };
        account.CnaeRels.Add(cnaeRel);
        db.Add(account);
        var batch = await db.GetBatchCommandForAssertionAsync();
        batch.Should().Be($$"""
            --batch_00000000-0000-0000-0000-000000000000
            Content-Type: multipart/mixed; boundary="changeset_00000000-0000-0000-0000-000000000000"

            --changeset_00000000-0000-0000-0000-000000000000
            Content-Type: application/http
            Content-Transfer-Encoding: binary
            Content-ID: 0

            POST /api/data/v9.2/accounts HTTP/1.1
            Host: fake.api.crm.dynamics.com
            Content-Type: application/json; charset=utf-8
            Content-Length: 63

            {
                "accountid": "{{account.Id}}"
            }

            --changeset_00000000-0000-0000-0000-000000000000
            Content-Type: application/http
            Content-Transfer-Encoding: binary
            Content-ID: 1

            POST /api/data/v9.2/accounts({{account.Id}})/cnaes/$ref HTTP/1.1
            Host: fake.api.crm.dynamics.com
            Content-Type: application/json; charset=utf-8
            Content-Length: 116

            {
                "@odata.id": "https://fake.api.crm.dynamics.com/api/data/v9.2/cnaes({{cnaeRel.CnaeId}})"
            }
            --changeset_00000000-0000-0000-0000-000000000000--

            --batch_00000000-0000-0000-0000-000000000000--
            
            """);
    }

    [Fact]
    public async Task DeleteTest()
    {
        var builder = new DbContextOptionsBuilder<TestDbContext>();
        builder.UseDataverse(dataverse => dataverse.ResourceUrl("https://fake.api.crm.dynamics.com/"));
        await using var db = new TestDbContext(builder.Options);
        var account = new Account();
        var cnaeRel = new CnaeRel { CnaeId = Guid.NewGuid() };
        account.CnaeRels.Add(cnaeRel);
        db.Add(account);
        db.ChangeTracker.AcceptAllChanges();
        account.CnaeRels.Clear();
        var batch = await db.GetBatchCommandForAssertionAsync();
        batch.Should().Be($$"""
            --batch_00000000-0000-0000-0000-000000000000
            Content-Type: multipart/mixed; boundary="changeset_00000000-0000-0000-0000-000000000000"

            --changeset_00000000-0000-0000-0000-000000000000
            Content-Type: application/http
            Content-Transfer-Encoding: binary
            Content-ID: 0

            DELETE /api/data/v9.2/accounts({{account.Id}})/cnaes({{cnaeRel.CnaeId}})/$ref HTTP/1.1
            Host: fake.api.crm.dynamics.com


            --changeset_00000000-0000-0000-0000-000000000000--

            --batch_00000000-0000-0000-0000-000000000000--
            
            """);

    }
}
