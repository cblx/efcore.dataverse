namespace Cblx.EntityFrameworkCore.Dataverse;

internal interface IDataverseValueConverter
{
    internal Func<object?, object?> ConvertToDataverseWebApi { get; }
}
