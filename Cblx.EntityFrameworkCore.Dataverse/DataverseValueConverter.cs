using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Linq.Expressions;

namespace Cblx.EntityFrameworkCore.Dataverse;

public class DataverseValueConverter<TModel, TProvider> : ValueConverter<TModel, TProvider>, IDataverseValueConverter
{
    public DataverseValueConverter(
        Expression<Func<TModel, TProvider>> convertToProviderExpression,
        Expression<Func<TProvider, TModel>> convertFromProviderExpression,
        Func<object?, object?> convertToDataverseWebApi,
        ConverterMappingHints? mappingHints = null) : base(convertToProviderExpression, 
                                                           convertFromProviderExpression, 
                                                           mappingHints)
    {
        ConvertToDataverseWebApi = convertToDataverseWebApi;
    }

    public Func<object?, object?> ConvertToDataverseWebApi { get; }
}