using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Linq.Expressions;

namespace Cblx.EntityFrameworkCore.Dataverse;

public class DataverseValueConverter<TModel, TProvider, TDataverseWebApi> : ValueConverter<TModel, TProvider>, IDataverseValueConverter
{
    public DataverseValueConverter(
        Expression<Func<TModel, TProvider>> convertToProviderExpression,
        Expression<Func<TProvider, TModel>> convertFromProviderExpression,
        Expression<Func<TModel, TDataverseWebApi>> convertToDataverseWebApiExpression,
        ConverterMappingHints? mappingHints = null) : base(convertToProviderExpression, 
                                                           convertFromProviderExpression, 
                                                           mappingHints)
    {
        ConvertToDataverseWebApiExpression = convertToDataverseWebApiExpression;
    }

    public Expression<Func<TModel, TDataverseWebApi>> ConvertToDataverseWebApiExpression { get; set; }
    public Func<object?, object?> ConvertToDataverseWebApi =>
        (object? from) =>
        {
            var typedFunc = ConvertToDataverseWebApiExpression.Compile();
            return typedFunc((TModel)from!);
        };
}