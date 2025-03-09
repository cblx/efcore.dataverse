
namespace Cblx.EntityFrameworkCore.Dataverse.Tests.Offline.OData;

public class ContactConfiguration : IEntityTypeConfiguration<Contact>
{
    public void Configure(EntityTypeBuilder<Contact> builder)
    {
        builder.ToEntitySet("contacts");
        builder.Property(e => e.Id).HasColumnName("contactid");
        builder.Property(e => e.FirstName).HasColumnName("firstname");
        builder.Property(e => e.Age).HasColumnName("age");
        builder.Property(e => e.Birthdate).HasColumnName("birthdate");
    }
}
