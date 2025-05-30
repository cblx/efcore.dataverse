﻿//using System.Diagnostics.CodeAnalysis;
//using System.Diagnostics.Contracts;
//using System.Net.Http.Headers;
//using System.Net;
//using System.Text;
//using System.Globalization;


//namespace Cblx.EntityFrameworkCore.Dataverse;
///// <summary>
///// Copied from: https://github.com/aspnet/AspNetWebStack/blob/d3c4055b361d3644df467c52b43b80576652180c/src/System.Net.Http.Formatting/HttpMessageContent.cs#L19
///// Derived <see cref="HttpContent"/> class which can encapsulate an <see cref="HttpResponseMessage"/>
///// or an <see cref="HttpRequestMessage"/> as an entity with media type "application/http".
///// </summary>
//internal class HttpMessageContent : HttpContent
//{
//    private const string SP = " ";
//    private const string ColonSP = ": ";
//    private const string CRLF = "\r\n";
//    private const string CommaSeparator = ", ";

//    private const int DefaultHeaderAllocation = 2 * 1024;

//    private const string DefaultMediaType = "application/http";

//    private const string MsgTypeParameter = "msgtype";
//    private const string DefaultRequestMsgType = "request";
//    private const string DefaultResponseMsgType = "response";

//    private const string DefaultRequestMediaType = DefaultMediaType + "; " + MsgTypeParameter + "=" + DefaultRequestMsgType;
//    private const string DefaultResponseMediaType = DefaultMediaType + "; " + MsgTypeParameter + "=" + DefaultResponseMsgType;

//    // Set of header fields that only support single values such as Set-Cookie.
//    private static readonly HashSet<string> _singleValueHeaderFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
//        {
//            "Cookie",
//            "Set-Cookie",
//            "X-Powered-By",
//        };

//    // Set of header fields that should get serialized as space-separated values such as User-Agent.
//    private static readonly HashSet<string> _spaceSeparatedValueHeaderFields = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
//        {
//            "User-Agent",
//        };

//    private bool _contentConsumed;
//    private Lazy<Task<Stream>> _streamTask;

//    /// <summary>
//    /// Initializes a new instance of the <see cref="HttpMessageContent"/> class encapsulating an
//    /// <see cref="HttpRequestMessage"/>.
//    /// </summary>
//    /// <param name="httpRequest">The <see cref="HttpResponseMessage"/> instance to encapsulate.</param>
//    public HttpMessageContent(HttpRequestMessage httpRequest)
//    {
//        if (httpRequest == null)
//        {
//            throw new ArgumentNullException("httpRequest");
//        }

//        HttpRequestMessage = httpRequest;
//        Headers.ContentType = new MediaTypeHeaderValue(DefaultMediaType);
//        Headers.ContentType.Parameters.Add(new NameValueHeaderValue(MsgTypeParameter, DefaultRequestMsgType));

//        InitializeStreamTask();
//    }

//    /// <summary>
//    /// Initializes a new instance of the <see cref="HttpMessageContent"/> class encapsulating an
//    /// <see cref="HttpResponseMessage"/>.
//    /// </summary>
//    /// <param name="httpResponse">The <see cref="HttpResponseMessage"/> instance to encapsulate.</param>
//    public HttpMessageContent(HttpResponseMessage httpResponse)
//    {
//        if (httpResponse == null)
//        {
//            throw new ArgumentNullException("httpRequest");
//        }

//        HttpResponseMessage = httpResponse;
//        Headers.ContentType = new MediaTypeHeaderValue(DefaultMediaType);
//        Headers.ContentType.Parameters.Add(new NameValueHeaderValue(MsgTypeParameter, DefaultResponseMsgType));

//        InitializeStreamTask();
//    }

//    private HttpContent Content
//    {
//        get { return HttpRequestMessage != null ? HttpRequestMessage.Content : HttpResponseMessage.Content; }
//    }

//    /// <summary>
//    /// Gets the HTTP request message.
//    /// </summary>
//    public HttpRequestMessage HttpRequestMessage { get; private set; }

//    /// <summary>
//    /// Gets the HTTP response message.
//    /// </summary>
//    public HttpResponseMessage HttpResponseMessage { get; private set; }

//    private void InitializeStreamTask()
//    {
//        _streamTask = new Lazy<Task<Stream>>(() => Content == null ? null : Content.ReadAsStreamAsync());
//    }

//    /// <summary>
//    /// Validates whether the content contains an HTTP Request or an HTTP Response.
//    /// </summary>
//    /// <param name="content">The content to validate.</param>
//    /// <param name="isRequest">if set to <c>true</c> if the content is either an HTTP Request or an HTTP Response.</param>
//    /// <param name="throwOnError">Indicates whether validation failure should result in an <see cref="Exception"/> or not.</param>
//    /// <returns><c>true</c> if content is either an HTTP Request or an HTTP Response</returns>
//    internal static bool ValidateHttpMessageContent(HttpContent content, bool isRequest, bool throwOnError)
//    {
//        if (content == null)
//        {
//            throw new ArgumentNullException("httpRequest");
//        }

//        MediaTypeHeaderValue contentType = content.Headers.ContentType;
//        if (contentType != null)
//        {
//            if (!contentType.MediaType.Equals(DefaultMediaType, StringComparison.OrdinalIgnoreCase))
//            {
//                if (throwOnError)
//                {
//                    throw Error.Argument("content", Properties.Resources.HttpMessageInvalidMediaType, FormattingUtilities.HttpContentType.Name,
//                                  isRequest ? DefaultRequestMediaType : DefaultResponseMediaType);
//                }
//                else
//                {
//                    return false;
//                }
//            }

//            foreach (NameValueHeaderValue parameter in contentType.Parameters)
//            {
//                if (parameter.Name.Equals(MsgTypeParameter, StringComparison.OrdinalIgnoreCase))
//                {
//                    string msgType = FormattingUtilities.UnquoteToken(parameter.Value);
//                    if (!msgType.Equals(isRequest ? DefaultRequestMsgType : DefaultResponseMsgType, StringComparison.OrdinalIgnoreCase))
//                    {
//                        if (throwOnError)
//                        {
//                            throw Error.Argument("content", Properties.Resources.HttpMessageInvalidMediaType, FormattingUtilities.HttpContentType.Name, isRequest ? DefaultRequestMediaType : DefaultResponseMediaType);
//                        }
//                        else
//                        {
//                            return false;
//                        }
//                    }

//                    return true;
//                }
//            }
//        }

//        if (throwOnError)
//        {
//            throw Error.Argument("content", Properties.Resources.HttpMessageInvalidMediaType, FormattingUtilities.HttpContentType.Name, isRequest ? DefaultRequestMediaType : DefaultResponseMediaType);
//        }
//        else
//        {
//            return false;
//        }
//    }

//    /// <summary>
//    /// Asynchronously serializes the object's content to the given <paramref name="stream"/>.
//    /// </summary>
//    /// <param name="stream">The <see cref="Stream"/> to which to write.</param>
//    /// <param name="context">The associated <see cref="TransportContext"/>.</param>
//    /// <returns>A <see cref="Task"/> instance that is asynchronously serializing the object's content.</returns>
//    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext context)
//    {
//        if (stream == null)
//        {
//            throw Error.ArgumentNull("stream");
//        }

//        byte[] header = SerializeHeader();
//        await stream.WriteAsync(header, 0, header.Length);

//        if (Content != null)
//        {
//            Stream readStream = await _streamTask.Value;
//            ValidateStreamForReading(readStream);
//            await Content.CopyToAsync(stream);
//        }
//    }

//    /// <summary>
//    /// Computes the length of the stream if possible.
//    /// </summary>
//    /// <param name="length">The computed length of the stream.</param>
//    /// <returns><c>true</c> if the length has been computed; otherwise <c>false</c>.</returns>
//    [SuppressMessage("StyleCop.CSharp.ReadabilityRules", "SA1108:BlockStatementsMustNotContainEmbeddedComments",
//        Justification = "The code is more readable with such comments")]
//    protected override bool TryComputeLength(out long length)
//    {
//        // We have four states we could be in:
//        //   0. We have content and it knows its ContentLength.
//        //   1. We have content, but the task is still running or finished without success
//        //   2. We have content, the task has finished successfully, and the stream came back as a null or non-seekable
//        //   3. We have content, the task has finished successfully, and the stream is seekable, so we know its length
//        //   4. We don't have content (streamTask.Value == null)
//        //
//        // For #1 and #2, we return false.
//        // For #3, we return true & the size of our headers + the content length
//        // For #4, we return true & the size of our headers

//        length = 0;

//        if (Content?.Headers.ContentLength is not null)
//        {
//            length = (long)Content.Headers.ContentLength; // Case #0
//        }
//        else if (_streamTask.Value is not null)
//        {
//            Stream readStream;
//            if (!_streamTask.Value.TryGetResult(out readStream) // Case #1
//                || readStream == null || !readStream.CanSeek) // Case #2
//            {
//                length = -1;
//                return false;
//            }

//            length = readStream.Length; // Case #3
//        }

//        // We serialize header to a StringBuilder so that we can determine the length
//        // following the pattern for HttpContent to try and determine the message length.
//        // The perf overhead is no larger than for the other HttpContent implementations.
//        byte[] header = SerializeHeader();
//        length += header.Length;
//        return true;
//    }

//    /// <summary>
//    /// Releases unmanaged and - optionally - managed resources
//    /// </summary>
//    /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
//    protected override void Dispose(bool disposing)
//    {
//        if (disposing)
//        {
//            if (HttpRequestMessage != null)
//            {
//                HttpRequestMessage.Dispose();
//                HttpRequestMessage = null;
//            }

//            if (HttpResponseMessage != null)
//            {
//                HttpResponseMessage.Dispose();
//                HttpResponseMessage = null;
//            }
//        }

//        base.Dispose(disposing);
//    }

//    /// <summary>
//    /// Serializes the HTTP request line.
//    /// </summary>
//    /// <param name="message">Where to write the request line.</param>
//    /// <param name="httpRequest">The HTTP request.</param>
//    private static void SerializeRequestLine(StringBuilder message, HttpRequestMessage httpRequest)
//    {
//        Contract.Assert(message != null, "message cannot be null");
//        message.Append(httpRequest.Method + SP);
//        message.Append(httpRequest.RequestUri.PathAndQuery + SP);
//        message.Append(FormattingUtilities.HttpVersionToken + "/" + (httpRequest.Version != null ? httpRequest.Version.ToString(2) : "1.1") + CRLF);

//        // Only insert host header if not already present.
//        if (httpRequest.Headers.Host == null)
//        {
//            message.Append(FormattingUtilities.HttpHostHeader + ColonSP + httpRequest.RequestUri.Authority + CRLF);
//        }
//    }

//    /// <summary>
//    /// Serializes the HTTP status line.
//    /// </summary>
//    /// <param name="message">Where to write the status line.</param>
//    /// <param name="httpResponse">The HTTP response.</param>
//    private static void SerializeStatusLine(StringBuilder message, HttpResponseMessage httpResponse)
//    {
//        Contract.Assert(message != null, "message cannot be null");
//        message.Append(FormattingUtilities.HttpVersionToken + "/" + (httpResponse.Version != null ? httpResponse.Version.ToString(2) : "1.1") + SP);
//        message.Append((int)httpResponse.StatusCode + SP);
//        message.Append(httpResponse.ReasonPhrase + CRLF);
//    }

//    /// <summary>
//    /// Serializes the header fields.
//    /// </summary>
//    /// <param name="message">Where to write the status line.</param>
//    /// <param name="headers">The headers to write.</param>
//    private static void SerializeHeaderFields(StringBuilder message, HttpHeaders headers)
//    {
//        Contract.Assert(message != null, "message cannot be null");
//        if (headers != null)
//        {
//            foreach (KeyValuePair<string, IEnumerable<string>> header in headers)
//            {
//                if (_singleValueHeaderFields.Contains(header.Key))
//                {
//                    foreach (string value in header.Value)
//                    {
//                        message.Append(header.Key + ColonSP + value + CRLF);
//                    }
//                }
//                else if (_spaceSeparatedValueHeaderFields.Contains(header.Key))
//                {
//                    message.Append(header.Key + ColonSP + String.Join(SP, header.Value) + CRLF);
//                }
//                else
//                {
//                    message.Append(header.Key + ColonSP + String.Join(CommaSeparator, header.Value) + CRLF);
//                }
//            }
//        }
//    }

//    private byte[] SerializeHeader()
//    {
//        StringBuilder message = new StringBuilder(DefaultHeaderAllocation);
//        HttpHeaders headers = null;
//        HttpContent content = null;
//        if (HttpRequestMessage != null)
//        {
//            SerializeRequestLine(message, HttpRequestMessage);
//            headers = HttpRequestMessage.Headers;
//            content = HttpRequestMessage.Content;
//        }
//        else
//        {
//            SerializeStatusLine(message, HttpResponseMessage);
//            headers = HttpResponseMessage.Headers;
//            content = HttpResponseMessage.Content;
//        }

//        SerializeHeaderFields(message, headers);
//        if (content != null)
//        {
//            SerializeHeaderFields(message, content.Headers);
//        }

//        message.Append(CRLF);
//        return Encoding.UTF8.GetBytes(message.ToString());
//    }

//    private void ValidateStreamForReading(Stream stream)
//    {
//        // Stream is null case should be an extreme, incredibly unlikely corner case. Every HttpContent from
//        // the framework (see dotnet/runtime or .NET Framework reference source) provides a non-null Stream
//        // in the ReadAsStreamAsync task's return value. Likely need a poorly-designed derived HttpContent
//        // to hit this. Mostly ignoring the fact this message doesn't make much sense for the case.
//        if (stream is null || !stream.CanRead)
//        {
//            throw Error.NotSupported(Properties.Resources.NotSupported_UnreadableStream);
//        }

//        // If the content needs to be written to a target stream a 2nd time, then the stream must support
//        // seeking (e.g. a FileStream), otherwise the stream can't be copied a second time to a target
//        // stream (e.g. a NetworkStream).
//        if (_contentConsumed)
//        {
//            if (stream.CanSeek)
//            {
//                stream.Position = 0;
//            }
//            else
//            {
//                throw Error.InvalidOperation(Properties.Resources.HttpMessageContentAlreadyRead,
//                              FormattingUtilities.HttpContentType.Name,
//                              HttpRequestMessage != null
//                                  ? FormattingUtilities.HttpRequestMessageType.Name
//                                  : FormattingUtilities.HttpResponseMessageType.Name);
//            }
//        }

//        _contentConsumed = true;
//    }
//}

//internal static class Error
//{
//    internal static NotSupportedException NotSupported(string messageFormat, params object[] messageArgs)
//    {
//        return new NotSupportedException(Error.Format(messageFormat, messageArgs));
//    }

//    internal static ArgumentOutOfRangeException ArgumentMustBeGreaterThanOrEqualTo(string parameterName, object actualValue, object minValue)
//    {
//        return new ArgumentOutOfRangeException(parameterName, actualValue, $"ArgumentMustBeGreaterThanOrEqualTo {minValue}");
//    }

//    internal static ArgumentOutOfRangeException ArgumentMustBeLessThanOrEqualTo(string parameterName, object actualValue, object maxValue)
//    {
//        return new ArgumentOutOfRangeException(parameterName, actualValue, $"ArgumentMustBeLessThanOrEqualTo {maxValue}");
//    }


//    public static Exception ArgumentNull(string parameterName)
//    {
//        return new ArgumentNullException(parameterName);
//    }

//    public static InvalidOperationException InvalidOperation(string messageFormat, params object[] args)
//    {
//        string message = String.Format(CultureInfo.CurrentCulture, messageFormat, args);
//        return new InvalidOperationException(message);
//    }

//    internal static InvalidOperationException InvalidOperation(Exception innerException, string messageFormat, params object[] messageArgs)
//    {
//        return new InvalidOperationException(Error.Format(messageFormat, messageArgs), innerException);
//    }

//    internal static string Format(string format, params object[] args)
//    {
//        return String.Format(CultureInfo.CurrentCulture, format, args);
//    }

//    internal static ArgumentException Argument(string parameterName, string messageFormat, params object[] messageArgs)
//    {
//        return new ArgumentException(Error.Format(messageFormat, messageArgs), parameterName);
//    }
//}

//file static class TaskHelpersExtensions
//{
//    /// <summary>
//    /// Cast Task to Task of object
//    /// </summary>
//    internal static async Task<object> CastToObject(this Task task)
//    {
//        await task;
//        return null;
//    }

//    /// <summary>
//    /// Cast Task of T to Task of object
//    /// </summary>
//    internal static async Task<object> CastToObject<T>(this Task<T> task)
//    {
//        return (object)await task;
//    }

//    /// <summary>
//    /// Throws the first faulting exception for a task which is faulted. It preserves the original stack trace when
//    /// throwing the exception. Note: It is the caller's responsibility not to pass incomplete tasks to this
//    /// method, because it does degenerate into a call to the equivalent of .Wait() on the task when it hasn't yet
//    /// completed.
//    /// </summary>
//    internal static void ThrowIfFaulted(this Task task)
//    {
//        task.GetAwaiter().GetResult();
//    }

//    /// <summary>
//    /// Attempts to get the result value for the given task. If the task ran to completion, then
//    /// it will return true and set the result value; otherwise, it will return false.
//    /// </summary>
//    [SuppressMessage("Microsoft.Web.FxCop", "MW1201", Justification = "The usages here are deemed safe, and provide the implementations that this rule relies upon.")]
//    internal static bool TryGetResult<TResult>(this Task<TResult> task, out TResult result)
//    {
//        if (task.Status == TaskStatus.RanToCompletion)
//        {
//            result = task.Result;
//            return true;
//        }

//        result = default(TResult);
//        return false;
//    }
//}
