namespace Cblx.EntityFrameworkCore.Dataverse;

internal static class Properties
{
    internal static class Resources
    {
        public const string NotSupported_UnreadableStream = "The stream does not support reading.";
        public const string HttpMessageInvalidMediaType = "The content type '{0}' is invalid. The correct content type is '{1}'.";
        public const string HttpMessageContentAlreadyRead = "The '{0}' of the '{1}' has already been read.";
        public const string ReadAsMimeMultipartErrorWriting = "Error writing MIME multipart body part to output stream.";
        public const string ReadAsMimeMultipartStreamProviderException = "The stream provider of type '{0}' threw an exception.";
        public const string ReadAsMimeMultipartStreamProviderNull = "The stream provider of type '{0}' returned null. It must return a writable '{1}' instance.";
        public const string ReadAsMimeMultipartStreamProviderReadOnly = "The stream provider of type '{0}' returned a read-only stream. It must return a writable '{1}' instance.";
        public const string ReadAsMimeMultipartErrorReading = "Error reading MIME multipart body part.";
        public const string ReadAsMimeMultipartArgumentNoMultipart = "Invalid '{0}' instance provided. It does not have a content type header starting with '{1}'.";
        public const string ReadAsMimeMultipartArgumentNoContentType = "Invalid '{0}' instance provided. It does not have a content-type header value. '{0}' instances must have a content-type header starting with '{1}'.";
        public const string ReadAsMimeMultipartArgumentNoBoundary = "Invalid '{0}' instance provided. It does not have a '{1}' content-type header with a '{2}' parameter.";
        public const string ReadAsMimeMultipartHeaderParseError = "Error parsing MIME multipart body part header byte {0} of data segment {1}.";
        public const string ReadAsMimeMultipartParseError = "Error parsing MIME multipart message byte {0} of data segment {1}.";
        public const string ReadAsMimeMultipartUnexpectedTermination = "Unexpected end of MIME multipart stream. MIME multipart message is not complete.";
        public const string MimeMultipartParserBadBoundary = "MIME multipart boundary cannot end with an empty space.";
    }
}
