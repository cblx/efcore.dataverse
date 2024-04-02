namespace Cblx.EntityFrameworkCore.Dataverse.Tests.Data.SubCollectionWithDivergentNavigationName;
[StronglyTypedId(Template.Guid, "guid-efcore")]
public readonly partial struct ChildThingId { }
public class ChildThing { 
    public ChildThingId Id { get; set; }
    public string? Name { get; set; }
}