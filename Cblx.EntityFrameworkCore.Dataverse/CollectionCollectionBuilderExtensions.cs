using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cblx.EntityFrameworkCore.Dataverse;

public static class CollectionCollectionBuilderExtensions
{
    public static EntityTypeBuilder UsingDataverseManyToManyRelationshipEntity<TRelatedEntity, TEntity>(
        this CollectionCollectionBuilder<TRelatedEntity, TEntity> collectionCollectionBuilder,
        string joinTable,
        string rightForeignKey,
        string leftForeignKey,
        string navigationFromLeft
    )
        where TRelatedEntity : class
        where TEntity : class
    {
        // This part will be used by EF for Read operations, when it needs to know how to join the tables
        var entityTypeBuilder = collectionCollectionBuilder.UsingEntity(joinTable,
            right => right.HasOne(typeof(TRelatedEntity)).WithMany().HasForeignKey(rightForeignKey),
            left => left.HasOne(typeof(TEntity)).WithMany().HasForeignKey(leftForeignKey));

        // This part will be used by EF for Write operations, when it needs to know how to name the navigation property and main table
        entityTypeBuilder.Metadata.AddAnnotation(nameof(ManyToManyEntityData), new ManyToManyEntityData { 
            LeftEntityType = typeof(TEntity),
            LeftForeignKey = leftForeignKey,
            RightEntityType = typeof(TRelatedEntity),
            RightForeignKey = rightForeignKey,
            NavigationFromLeft = navigationFromLeft
        });
        return entityTypeBuilder;
    }

    internal static bool IsManyToManyJoinEntity(this IEntityType entityType)
        => entityType.FindAnnotation(nameof(ManyToManyEntityData)) != null;

    internal static ManyToManyEntityData? GetManyToManyEntityData(this IEntityType entityType)
        => entityType.FindAnnotation(nameof(ManyToManyEntityData))?.Value as ManyToManyEntityData;


    internal class ManyToManyEntityData
    {
        public required Type LeftEntityType { get; init; }
        public required string LeftForeignKey { get; init; }
        public required Type RightEntityType { get; init; }
        public required string RightForeignKey { get; init; }
        public required string NavigationFromLeft { get; init; }
    }
}
