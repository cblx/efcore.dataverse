﻿using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace Cblx.EntityFrameworkCore.Dataverse;

public static class DataverseDbSetExtensions {
    private const DynamicallyAccessedMemberTypes DynamicallyAccessedMemberTypes =
       System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors
       | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicConstructors
       | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties
       | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields
       | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicProperties
       | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicFields
       | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.Interfaces;

    public static async Task<ChoiceOption[]> GetOptionsAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes)] TEntity>(
       this DbSet<TEntity> set,
       Expression<Func<TEntity, object>> memberExpression)
       where TEntity : class
    {
        return await GetOptionsAsyncInternal(set, memberExpression);
    }

    public static async Task<ChoiceOption<TEnum>[]> GetOptionsAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes)] TEntity, TEnum>(
        this DbSet<TEntity> set,
        Expression<Func<TEntity, IEnumerable<TEnum>?>> memberExpression)
        where TEntity : class where TEnum : struct
    {
        var choices = await GetOptionsAsyncInternal(set, memberExpression);
        return choices.Select(c => c.To<TEnum>()).ToArray();
    }

    public static async Task<ChoiceOption<TEnum>[]> GetOptionsAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes)] TEntity, TEnum>(
        this DbSet<TEntity> set,
        Expression<Func<TEntity, TEnum?>> memberExpression)
        where TEntity : class where TEnum : struct
    {
        var choices = await GetOptionsAsyncInternal(set, memberExpression);
        return choices.Select(c => c.To<TEnum>()).ToArray();
    }

    public static async Task<ChoiceOption<TEnum>[]> GetOptionsAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes)] TEntity, TEnum>(
        this DbSet<TEntity> set,
        Expression<Func<TEntity, TEnum>> memberExpression)
        where TEntity : class where TEnum : struct
    {
        var choices = await GetOptionsAsyncInternal(set, memberExpression);
        return choices.Select(c => c.To<TEnum>()).ToArray();
    }


    private static async Task<ChoiceOption[]> GetOptionsAsyncInternal<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes)] TEntity, TProperty>(
       IInfrastructure<IServiceProvider> set,
       Expression<Func<TEntity, TProperty>> memberExpression) where TEntity : class
    {
        var dbContext = set.GetService<ICurrentDbContext>().Context;
        var entityType = dbContext.Model.FindEntityType(typeof(TEntity))!;
        var tableName = entityType.GetTableName();
        var memberInfo = (memberExpression.Body as MemberExpression)!.Member;
        var columnName = entityType.FindProperty(memberInfo)!.GetColumnName();

        return await dbContext.Database.SqlQuery<ChoiceOption>($"""
            SELECT choicevalue Value, choicename Name
            FROM choicelabels
            WHERE tablename = {tableName} AND columnname = {columnName}
            """).ToArrayAsync();
    }

}
