using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Cblx.EntityFrameworkCore.Dataverse.Tests.Data.WithDateOnlyConverter;
public sealed class DynamicDateOnlyConverter() : ValueConverter<DateOnly, DateTime>(
    value => value.ToDateTime(TimeOnly.MinValue),
    value => DateOnly.FromDateTime(value));