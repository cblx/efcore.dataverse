using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cblx.EntityFrameworkCore.Dataverse;

public static class EntityExtensions
{
    /// <summary>
    /// Configures the endpoint/entityset that the entity type maps to when saving to Dataverse.
    /// If this is not set, the table name will be used to infer the entity set name, applying pluralization over it.
    /// E.g. ToTable("contact") will map to "contacts" entity set.
    /// </summary>
    /// <param name="entityType"></param>
    /// <param name="value"></param>
    public static void ToEntitySet(this EntityTypeBuilder entityType,
        string value) => entityType.Metadata.AddAnnotation("entitySetName", value);

    /// <summary>
    /// Define the navigation name when saving this relationship through odata.bind attribute.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="property"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static PropertyBuilder<T> HasODataBindPropertyName<T>(this PropertyBuilder<T> property, string value)
    {
        property.Metadata.AddAnnotation("ODataBindPropertyName", value);
        return property;
    }

    /// <summary>
    /// Define the navigation name when saving this relationship through odata.bind attribute.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="property"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static ComplexTypePropertyBuilder<T> HasODataBindPropertyName<T>(this ComplexTypePropertyBuilder<T> property, string value)
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

    /// <summary>
    /// Defines the name of the foreign entity set.
    /// This is used together with <see cref="HasODataBindPropertyName{T}(PropertyBuilder{T}, string)"/>
    /// when the foreign table is not defined in the model so a relationship cannot be set.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="property"></param>
    /// <param name="value"></param>
    /// <returns></returns>
    public static ComplexTypePropertyBuilder<T> HasForeignEntitySet<T>(this ComplexTypePropertyBuilder<T> property, string value)
    {
        property.Metadata.AddAnnotation("ForeignEntitySet", value);
        return property;
    }

    public static string GetEntitySetName(this IEntityType entityType)
        => entityType.FindAnnotation("entitySetName")?.Value?.ToString()
            ?? entityType.GetTableName() 
               // I don't really know if GetTableName can return null values at this point.
               // If it is possible, we should add a better explanation here, and how the user can fix it.
               ?? throw new InvalidOperationException($"EntitySetName could not be resolved for {entityType.Name}.");
   
    internal static string? GetForeignEntitySet(this PropertyEntry property)
    {
        var value = property.Metadata.FindAnnotation("ForeignEntitySet")?.Value?.ToString();
        return value;
    }

    internal static object? GetCurrentConvertedValue(this PropertyEntry property)
    {
        var converter = property.Metadata.GetValueConverter();
        if(converter is null)
        {
            return property.CurrentValue;
        }
        return converter.ConvertToProvider(property.CurrentValue);
    }
   
    internal static string? GetODataBindPropertyName(this PropertyEntry property)
    {
        var value = property.Metadata.FindAnnotation("ODataBindPropertyName")?.Value?.ToString();
        return value;
    }
}
