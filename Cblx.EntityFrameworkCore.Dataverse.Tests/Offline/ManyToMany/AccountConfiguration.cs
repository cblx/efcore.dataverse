namespace Cblx.EntityFrameworkCore.Dataverse.Tests.Offline.ManyToMany;

internal class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("account").ToEntitySet("accounts");
        builder.Property(e => e.Id).HasColumnName("accountid").ValueGeneratedOnAdd();

        builder.HasMany(e => e.Cnaes)
            .WithMany()
            .UsingDataverseManyToManyRelationshipEntity(
                joinTable: "account_cnae", 
                rightForeignKey: "cnaeid", 
                leftForeignKey: "accountid", 
                navigationFromLeft: "cnaes");
    }
}