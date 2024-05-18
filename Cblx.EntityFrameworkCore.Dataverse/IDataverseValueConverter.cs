namespace Cblx.EntityFrameworkCore.Dataverse;

internal interface IDataverseValueConverter
{
    Func<object?, object?> ConvertToDataverseWebApi { get; }
}
