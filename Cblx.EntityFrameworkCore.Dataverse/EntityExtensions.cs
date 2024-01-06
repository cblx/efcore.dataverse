using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cblx.EntityFrameworkCore.Dataverse;

internal static class EntityExtensions
{
    public static void ToEntitySet(this EntityTypeBuilder entityType,
        string value) => entityType.Metadata.AddAnnotation("entitySetName", value);
    public static PropertyBuilder<T> HasODataBindPropertyName<T>(this PropertyBuilder<T> property, string value)
    {
        property.Metadata.AddAnnotation("ODataBindPropertyName", value);
        return property;
    }


    public static string GetEntitySetName(this IEntityType entityType)
        => entityType.FindAnnotation("entitySetName")?.Value?.ToString()
            ?? throw new InvalidOperationException($"EntitySetName not defined for {entityType.Name}");


   
    public static string? GetODataBindPropertyName(this PropertyEntry property)
    {
        var value = property.Metadata.FindAnnotation("ODataBindPropertyName")?.Value?.ToString();
        return value;
    }
}
