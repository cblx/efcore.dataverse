namespace Cblx.EntityFrameworkCore.Dataverse;

public enum DataverseEventId
{
    CreatingBatchRequest = 83_001,
    SendingBatchRequest = 83_002,
    BatchRequestSucceeded = 83_003,
    BatchRequestFailed = 83_004,
    CreatingBatchRequestMessageContentItem = 83_101
}
