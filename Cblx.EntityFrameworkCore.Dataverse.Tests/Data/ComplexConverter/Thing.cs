namespace Cblx.EntityFrameworkCore.Dataverse.Tests.Data.ComplexConverter;

[StronglyTypedId(Template.Guid, "guid-efcore")]
public readonly partial struct ThingId { }

public class Thing
{
    public ThingId Id { get; set; }
    public IEnumerable<Letter>? Name { get; set; }
    public ThingId? ParentId { get; set; }
}

public enum Letter
{
    A,
    B,
    C,
    D, E,
    F,
    G,
    H,
    I,
    J,
    K,
    L,
    M,
    N,
    O,
    P,
    Q,
    R,
    S,
    T,
    U,
}
