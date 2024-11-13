namespace Cblx.EntityFrameworkCore.Dataverse.Tests.Offline.ManyToMany;

internal class CnaeConfiguration : IEntityTypeConfiguration<Cnae>
{
    public void Configure(EntityTypeBuilder<Cnae> builder)
    {
        builder.ToTable("cnae").ToEntitySet("cnaes");
        builder.Property(e => e.Id).HasColumnName("cnaeid").ValueGeneratedOnAdd();
    }
}