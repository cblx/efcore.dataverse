using Cblx.Blocks;

namespace Cblx.EntityFrameworkCore.Dataverse.Tests.Data.Default;
[GenerateTypedId]
public class Thing
{
    public ThingId Id { get; set; }
    public string? Name { get; set; }
}
