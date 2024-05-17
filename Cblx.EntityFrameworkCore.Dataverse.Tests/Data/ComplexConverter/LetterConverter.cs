using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Cblx.EntityFrameworkCore.Dataverse.Tests.Data.ComplexConverter;

public sealed class LetterConverter() : ValueConverter<IEnumerable<Letter>, string>(
    value => ListToString(value),
    value => StringToList(value)) 
{
    private static string ListToString(IEnumerable<Letter> enums)
    {
        return string.Join("",enums);
    }

    private static IEnumerable<Letter> StringToList(string value)
    {
        return string.IsNullOrEmpty(value) ? [] : value.Select(c => Enum.Parse<Letter>(c.ToString()));
    }
}