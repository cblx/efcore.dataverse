using System.Net.Http.Headers;


namespace Cblx.EntityFrameworkCore.Dataverse;

public class MultipartMemoryStreamProvider : MultipartStreamProvider
{
    /// <summary>
    /// This <see cref="MultipartStreamProvider"/> implementation returns a <see cref="MemoryStream"/> instance.
    /// This facilitates deserialization or other manipulation of the contents in memory. 
    /// </summary>
    public override Stream GetStream(HttpContent parent, HttpContentHeaders headers)
    {
        if (parent == null)
        {
            throw Error.ArgumentNull("parent");
        }

        if (headers == null)
        {
            throw Error.ArgumentNull("headers");
        }

        return new MemoryStream();
    }
}