namespace Cblx.EntityFrameworkCore.Dataverse.Tests.Data.SubCollection;

public class ChildThingConfiguration : IEntityTypeConfiguration<ChildThing>
{
    public void Configure(EntityTypeBuilder<ChildThing> builder)
    {
        builder.ToTable("creec_child_thing");
        builder.ToEntitySet("creec_child_things");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Id).HasColumnName("creec_child_thingid");
        builder.Property(x => x.Name).HasColumnName("creec_name");
    }
}