//using Microsoft.EntityFrameworkCore;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Cblx.EntityFrameworkCore.Dataverse.OData;
public class FromODataBuilder<TEntity> where TEntity : class
{
    private readonly DataverseDbContext _db;
    private readonly DbSet<TEntity> _entityDbSet;

    internal FromODataBuilder(DataverseDbContext db, DbSet<TEntity> entityDbSet)
    {
        _db = db;
        _entityDbSet = entityDbSet;
    }

    //public FromODataBuilder<TEntity, TProperty> Filter<TProperty>(Expression<Func<TEntity, TProperty>> memberExpression)
    //{

    //    return this;
    //}

    public async Task<TEntity[]> ToArrayAsync()
    {
        using var httpClient = _db.CreateHttpClient();
        var odata = ToODataString();
        var response = await httpClient.GetAsync(odata);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync();
        return _entityDbSet.MaterializeODataResult(json).ToArray();
    }

    public string ToODataString()
    {
        var entityType = _db.Model.FindEntityType(typeof(TEntity)) ?? throw new InvalidOperationException($"Entity type {typeof(TEntity).Name} not found in the model when querying with OData.");
        var entitySetName = entityType.GetEntitySetName();
        var properties = entityType.GetProperties().Select(p => p.GetColumnName()).ToArray();
        return $"{entitySetName}?$select={string.Join(',', properties)}";
    }
}

