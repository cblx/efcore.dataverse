namespace Cblx.EntityFrameworkCore.Dataverse.Tests.Data.ComplexType;

[StronglyTypedId(Template.Guid, "guid-efcore")]
public readonly partial struct ThingId { }

public class Thing
{
    public ThingId Id { get; set; }
    public ComplextData Data { get; set; } = new();
}