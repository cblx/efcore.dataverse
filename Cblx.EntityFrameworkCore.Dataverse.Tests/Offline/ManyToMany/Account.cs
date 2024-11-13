using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Cblx.EntityFrameworkCore.Dataverse.Tests.Offline.ManyToMany;
internal class Account
{
    public Guid Id { get; set; }

    public List<Cnae> Cnaes { get; set; } = [];
}
