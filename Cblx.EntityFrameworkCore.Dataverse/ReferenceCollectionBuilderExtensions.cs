using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cblx.EntityFrameworkCore.Dataverse;
public static class ReferenceCollectionBuilderExtensions
{
    public static ReferenceCollectionBuilder<TPrincipalEntity, TDependentEntity> HasODataBindPropertyName<TPrincipalEntity, TDependentEntity>(
        this ReferenceCollectionBuilder<TPrincipalEntity, TDependentEntity> referenceCollectionBuilder, string value)
        where TDependentEntity : class
        where TPrincipalEntity : class
    {
        referenceCollectionBuilder.Metadata.PrincipalToDependent!.ForeignKey.Properties[0].AddAnnotation("ODataBindPropertyName", value);
        return referenceCollectionBuilder.HasAnnotation("ODataBindPropertyName", value);
    }
}
