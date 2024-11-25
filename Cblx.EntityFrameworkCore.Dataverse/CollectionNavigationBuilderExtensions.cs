using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Linq.Expressions;

namespace Cblx.EntityFrameworkCore.Dataverse;

public static class CollectionNavigationBuilderExtensions
{
    /// <summary>
    /// Configures the other entity as a relationship table in a many-to-many
    /// relationship that is not desired to have the other side table stronglyh linked or even mapped.
    /// </summary>
    /// <typeparam name="TLeftEntity"></typeparam>
    /// <typeparam name="TRightEntity"></typeparam>
    /// <param name="builder"></param>
    /// <param name="principalNavigation"></param>
    /// <param name="relFkToTarget"></param>
    /// <param name="targetEntitySet"></param>
    /// <returns></returns>
    public static ReferenceCollectionBuilder<TLeftEntity, TRightEntity> ODataBindManyToMany<TLeftEntity, TRightEntity>(
        this ReferenceCollectionBuilder<TLeftEntity, TRightEntity> builder,
        string principalNavigation,
        Expression<Func<TRightEntity, object>> relFkToTarget,
        string targetEntitySet
    )
        where TLeftEntity : class
        where TRightEntity : class
    {
        var relForeignKeyToTarget = (relFkToTarget.Body as UnaryExpression)!.Operand as MemberExpression;

        builder.Metadata.PrincipalToDependent
                        .TargetEntityType
                        .AddAnnotation(nameof(ODataBindManyToManyData), new ODataBindManyToManyData
                        {
                            PrincipalType = typeof(TLeftEntity),
                            PrincipalNavigationPropertyName = builder.Metadata.PrincipalToDependent.Name,
                            PrincipalNavigationLogicalName = principalNavigation,
                            TargetEntitySet = targetEntitySet,
                            RelForeignKeyToTarget = relForeignKeyToTarget
                        });
        return builder.IsRequired();
    }
}

internal class ODataBindManyToManyData
{
    public required Type PrincipalType { get; set; }
    public required string PrincipalNavigationPropertyName { get; set; }
    public required string PrincipalNavigationLogicalName { get; set; }
    public required string TargetEntitySet { get; set; }
    public required MemberExpression RelForeignKeyToTarget { get; set; }
}