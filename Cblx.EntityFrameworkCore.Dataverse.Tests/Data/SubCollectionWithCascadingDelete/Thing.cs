namespace Cblx.EntityFrameworkCore.Dataverse.Tests.Data.SubCollectionWithCascadingDelete;

[StronglyTypedId(Template.Guid, "guid-efcore")]
public readonly partial struct ThingId { }

public class Thing
{
    public ThingId Id { get; set; }
    public string? Name { get; set; }
    public List<ChildThing> Children { get; set; } = [];
}
