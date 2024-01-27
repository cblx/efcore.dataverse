﻿using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cblx.EntityFrameworkCore.Dataverse;

public static class EntityExtensions
{
    public static void ToEntitySet(this EntityTypeBuilder entityType,
        string value) => entityType.Metadata.AddAnnotation("entitySetName", value);
    public static PropertyBuilder<T> HasODataBindPropertyName<T>(this PropertyBuilder<T> property, string value)
    {
        property.Metadata.AddAnnotation("ODataBindPropertyName", value);
        return property;
    }
    /// <summary>
    /// Defines the name of the foreign entity set.
    /// This is used together with <see cref="HasODataBindPropertyName{T}(PropertyBuilder{T}, string)"/>
    /// when the foreign table is not defined in the model so a relationship cannot be set.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="property"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static PropertyBuilder<T> HasForeignEntitySet<T>(this PropertyBuilder<T> property, string value)
    {
        property.Metadata.AddAnnotation("ForeignEntitySet", value);
        return property;
    }

    internal static string GetEntitySetName(this IEntityType entityType)
        => entityType.FindAnnotation("entitySetName")?.Value?.ToString()
            ?? throw new InvalidOperationException($"EntitySetName not defined for {entityType.Name}");

    internal static string? GetForeignEntitySet(this PropertyEntry property)
    {
        var value = property.Metadata.FindAnnotation("ForeignEntitySet")?.Value?.ToString();
        return value;
    }
   
    internal static string? GetODataBindPropertyName(this PropertyEntry property)
    {
        var value = property.Metadata.FindAnnotation("ODataBindPropertyName")?.Value?.ToString();
        return value;
    }
}
