namespace Cblx.EntityFrameworkCore.Dataverse.Tests.Data.SubCollectionWithDivergentNavigationName;

public class ThingConfiguration : IEntityTypeConfiguration<Thing>
{
    public void Configure(EntityTypeBuilder<Thing> builder)
    {
        builder.ToTable("creec_thing");
        builder.ToEntitySet("creec_things");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Id).HasColumnName("creec_thingid");
        builder.Property(x => x.Name).HasColumnName("new_name");

        builder.HasMany(x => x.Children)
               .WithOne()
               .IsRequired()
               .HasForeignKey("creec_parent")
               .HasODataBindPropertyName("creec_wrong_name")
               .OnDelete(DeleteBehavior.Cascade);
    }
}
