using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Cblx.EntityFrameworkCore.Dataverse;

public class DataverseEventDefinition(ILoggingOptions loggingOptions, EventId eventId, LogLevel level, string eventIdCode) : EventDefinitionBase(
    loggingOptions,
    eventId,
    level, eventIdCode
    )
{
}
