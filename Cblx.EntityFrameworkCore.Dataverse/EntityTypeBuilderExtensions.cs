//using Microsoft.EntityFrameworkCore.Metadata.Builders;
//using System.Linq.Expressions;

//namespace Cblx.EntityFrameworkCore.Dataverse;

//public static class EntityTypeBuilderExtensions
//{
//    public static EntityTypeBuilder<TEntity> SaveThroughParentDataverseNavigation<TEntity, TParent>(this EntityTypeBuilder<TEntity> entityType,
//      Expression<Func<TParent, IEnumerable<TEntity>>> parentNavigationProperty
//      )
//      where TParent : class
//      where TEntity : class
//    {
//        var data = new SaveThroughParentDataverseNavigationData
//        {
//            ParentType = typeof(TParent),
//            NavigationProperty = ((MemberExpression)parentNavigationProperty.Body).Member.Name
//        };
//        entityType.Metadata.AddAnnotation(nameof(SaveThroughParentDataverseNavigationData), data);
//        return entityType;
//    }

//    public static EntityTypeBuilder SaveThroughParentDataverseNavigation(this EntityTypeBuilder entityType, Type parentType, string parentNavigationProperty)
//    {
//        entityType.Metadata.AddAnnotation(nameof(SaveThroughParentDataverseNavigationData), new SaveThroughParentDataverseNavigationData
//        {
//            ParentType = parentType,
//            NavigationProperty = parentNavigationProperty
//        });
//        return entityType;
//    }

//    internal class SaveThroughParentDataverseNavigationData
//    {
//        public Type ParentType { get; set; }
//        public string NavigationProperty { get; set; }
//    }
//}