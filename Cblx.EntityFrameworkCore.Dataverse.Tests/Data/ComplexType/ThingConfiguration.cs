﻿using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cblx.EntityFrameworkCore.Dataverse.Tests.Data.ComplexType;

public class ThingConfiguration : IEntityTypeConfiguration<Thing>
{
    public void Configure(EntityTypeBuilder<Thing> builder)
    {
        builder.ToTable("creec_thing");
        builder.ToEntitySet("creec_things");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedOnAdd();
        builder.Property(x => x.Id).HasColumnName("creec_thingid");
        
        builder.ComplexProperty(x => x.Data, complextData =>
        {
            complextData.Property(x => x.Name).HasColumnName("new_name");
            complextData.Property(x => x.ParentId).HasColumnName("creec_AscendentParent")
                .HasODataBindPropertyName("creec_AscendentParent")
                .HasForeignEntitySet("creec_things");
        });
    }
}