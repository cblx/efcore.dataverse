using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cblx.EntityFrameworkCore.Dataverse.Tests.Data.Default;

public class ThingConfiguration : IEntityTypeConfiguration<Thing>
{
    public void Configure(EntityTypeBuilder<Thing> builder)
    {
        builder.ToTable("creec_thing");
        builder.ToEntitySet("creec_things");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Id).HasConversion(id => id.Guid, guid => new ThingId(guid));
        builder.Property(x => x.Id).HasColumnName("creec_thingid");
        builder.Property(x => x.Name).HasColumnName("new_name");
    }
}