# Cblx.EntityFrameworkCore.Dataverse
Extends SqlServer provider for Dataverse
---

```
public class DynamicsContext(DbContextOptions options) : DataverseDbContext(options){
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
