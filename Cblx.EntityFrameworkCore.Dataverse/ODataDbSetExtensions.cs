using Cblx.EntityFrameworkCore.Dataverse.OData;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Cblx.EntityFrameworkCore.Dataverse;

public static class ODataDbSetExtensions
{
    /// <summary>
    /// Query entities using OData.
    /// This is a very preliminar and experimental feature.
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <param name="entityDbSet"></param>
    /// <returns></returns>
    public static FromODataBuilder<TEntity> FromOData<TEntity>(this DbSet<TEntity> entityDbSet) where TEntity : class
    {
        if (entityDbSet.GetService<ICurrentDbContext>().Context is not DataverseDbContext db)
        {
            throw new InvalidOperationException("This method is only available for DataverseDbContext.");
        }
        return new FromODataBuilder<TEntity>(db, entityDbSet);
    }

    internal static DataverseDbContext GetDbContext<TEntity>(this DbSet<TEntity> entityDbSet) where TEntity : class
    {
        if (entityDbSet.GetService<ICurrentDbContext>().Context is not DataverseDbContext db)
        {
            throw new InvalidOperationException("This method is only available for DataverseDbContext.");
        }
        return db;
    }

    public static IEnumerable<TEntity> MaterializeODataResult<TEntity>(this DbSet<TEntity> entityDbSet,
                                                                       [StringSyntax("json")]
                                                                       string odataResult) where TEntity : class
    {
        var db = entityDbSet.GetDbContext();
        var entityType = db.Model.FindEntityType(typeof(TEntity)) ?? throw new InvalidOperationException($"Entity type {typeof(TEntity).Name} not found in the model when materializing from OData result string.");
        var jsonObject = JsonSerializer.Deserialize<JsonObject>(odataResult);
        var value = jsonObject!["value"]!.AsArray();
        var emptyConstructor = typeof(TEntity).GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, Type.EmptyTypes);
        foreach (var item in value)
        {
            var entity = emptyConstructor!.Invoke(null);
            foreach (var property in entityType.GetProperties())
            {
                var columnName = property.GetColumnName();
                if (item![columnName] is JsonValue jsonValue)
                {
                    // This will happen with shadow properties
                    if (property.PropertyInfo is null)
                    {
                        continue;
                    }
                    property.PropertyInfo!.SetValue(entity, jsonValue.Deserialize(property.ClrType));
                }
            }
            yield return (TEntity)entity;
        }

    }
}