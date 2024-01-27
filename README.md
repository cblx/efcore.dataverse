# Cblx.EntityFrameworkCore.Dataverse
Extends SqlServer provider for Dataverse
---

```
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

