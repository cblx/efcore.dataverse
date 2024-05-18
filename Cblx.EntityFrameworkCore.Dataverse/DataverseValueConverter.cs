using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Linq.Expressions;

namespace Cblx.EntityFrameworkCore.Dataverse;

public class DataverseValueConverter<TModel, TProvider, TDataverseWebApi> : ValueConverter<TModel, TProvider>, IDataverseValueConverter
{
    public DataverseValueConverter(
        Expression<Func<TModel, TProvider>> convertToProviderExpression,
        Expression<Func<TProvider, TModel>> convertFromProviderExpression,
        Func<TModel?, TDataverseWebApi?> convertToDataverseWebApi,
        ConverterMappingHints? mappingHints = null) : base(convertToProviderExpression, 
                                                           convertFromProviderExpression, 
                                                           mappingHints)
    {
        ConvertToDataverseWebApi = from => convertToDataverseWebApi((TModel?)from);
    }

    public Func<object?, object?> ConvertToDataverseWebApi { get; }
}