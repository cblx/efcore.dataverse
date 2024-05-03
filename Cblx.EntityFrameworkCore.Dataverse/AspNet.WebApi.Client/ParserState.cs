namespace Cblx.EntityFrameworkCore.Dataverse;

internal enum ParserState
{
    /// <summary>
    /// Need more data
    /// </summary>
    NeedMoreData = 0,

    /// <summary>
    /// Parsing completed (final)
    /// </summary>
    Done,

    /// <summary>
    /// Bad data format (final)
    /// </summary>
    Invalid,

    /// <summary>
    /// Data exceeds the allowed size (final)
    /// </summary>
    DataTooBig,
}