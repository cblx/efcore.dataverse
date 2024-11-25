namespace Cblx.EntityFrameworkCore.Dataverse.Tests.Offline.ManyToManyPrivateNav;

internal class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("account").ToEntitySet("accounts");
        builder.Property(e => e.Id).HasColumnName("accountid").ValueGeneratedOnAdd();
        builder.HasMany<CnaeRel>("CnaeRels")
               .WithOne()
               .HasForeignKey("accountId")
               .ODataBindManyToMany(
                    principalNavigation: "cnaes",
                    relFkToTarget: cnaeRel => cnaeRel.CnaeId,
                    targetEntitySet: "cnaes"
                );
    }
}