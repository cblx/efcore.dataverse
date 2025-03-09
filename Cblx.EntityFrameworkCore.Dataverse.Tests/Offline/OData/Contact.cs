namespace Cblx.EntityFrameworkCore.Dataverse.Tests.Offline.OData;

public class Contact
{
    public Contact() { }

    public Guid Id { get; set; }
    public string? FirstName { get; set; }
    public int? Age { get; set; }
    public DateOnly? Birthdate { get; set; }
}
