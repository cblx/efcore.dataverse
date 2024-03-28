namespace Cblx.EntityFrameworkCore.Dataverse.Tests.Data.WithDateOnlyConverter;

[StronglyTypedId(Template.Guid, "guid-efcore")]
public readonly partial struct ThingId { }

public class Thing
{
    public ThingId Id { get; set; }
    public string? Name { get; set; }
    public ThingId? ParentId { get; set; }
    public DateOnly? Date { get; set; }
}