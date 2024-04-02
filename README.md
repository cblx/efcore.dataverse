# Cblx.EntityFrameworkCore.Dataverse
Extends SqlServer provider for Dataverse

[![NuGet Version](https://img.shields.io/nuget/v/Cblx.EntityFrameworkCore.Dataverse)](https://www.nuget.org/packages/Cblx.EntityFrameworkCore.Dataverse)
![NuGet Downloads](https://img.shields.io/nuget/dt/Cblx.EntityFrameworkCore.Dataverse)



---

```csharp
public class DynamicsContext(DbContextOptions<DynamicsContext> options) : DataverseDbContext(options){
    ...
}
```

```csharp
services.AddDbContext<DynamicsContext>(options => options.UseDataverse(dataverse =>
  dataverse.ClientId(configuration["ClientId"])
           .ClientSecret(configuration["ClientSecret"])
           .ResourceUrl(configuration["ResourceUrl"])
           .Authority(configuration["Authority"])
);
```
---
# Model Configuration

For saving data, this libs needs to know more information about the entities, like the entity set name and the Dataverse navigation property name for relationships.

## ToEntitySet

This library overrides SaveChangesAsync and perform a request to `$batch` Dataverse API endpoint for saving data.
To do so, it needs to know the entity set for write operations.

```csharp
entityBuilder.ToTable("account");
entityBuilder.ToEntitySet("accounts");
```

## HasODataBindPropertyName

When the entity has a relationship, the lib needs to know what's the navigaiton name to update the FK property.

Desired write body:

```json
{
   "other_entity@odata.bind": "other_entities(<guid>)",
   "etc": "other values"
}
```
Configuring relationship
```csharp
entityBuilder.Property(x => x.OtherEntityId)
             .HasColumnName("other_entity")
             .HasODataBindPropertyName("other_entity");
// The lib will infer the entity set configured for the other entity
entityBuilder.HasOne(x => x.OtherEntity).WithMany().HasForeignKey(x => x.OtherEntityId);
```
### HasForeignEntitySet
This is used when the FK has no correspondent Entity defined in them model:
```csharp
entityBuilder.Property(x => x.OtherEntityId)
             .HasColumnName("other_entity")
             .HasODataBindPropertyName("other_entity")
             .HasForeignEntitySet("other_entities");
```

