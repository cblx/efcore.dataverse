namespace Cblx.EntityFrameworkCore.Dataverse.Tests.Offline.ManyToManyPrivateNav;
internal class Account
{
    public Guid Id { get; set; }

    public List<CnaeRel> CnaeRels { get; set; } = [];

}
