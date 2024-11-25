namespace Cblx.EntityFrameworkCore.Dataverse.Tests.Offline.ManyToManyPrivateNav;

internal class CnaeRelConfiguration : IEntityTypeConfiguration<CnaeRel>
{
    public void Configure(EntityTypeBuilder<CnaeRel> builder)
    {
        builder.ToTable("account_cnae").ToEntitySet("account_cnae_set");
        builder.Property<Guid>("Id").HasColumnName("account_cnaeid") ;
        builder.Property(rel => rel.CnaeId).HasColumnName("cnaeid");
        //builder.SaveThroughParentDataverseNavigation(
        //    (Account a) => a.CnaeRels
        //);
    }
}