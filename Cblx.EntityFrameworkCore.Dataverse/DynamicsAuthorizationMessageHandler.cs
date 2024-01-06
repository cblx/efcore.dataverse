using Microsoft.Identity.Client;
using System.Net.Http.Headers;

namespace Cblx.EntityFrameworkCore.Dataverse;

public class DynamicsAuthorizationMessageHandler(DataverseOptionsExtension extension) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var app = ConfidentialClientApplicationBuilder.Create(extension.ClientId)
            .WithClientSecret(extension.ClientSecret)
            .WithAuthority(extension.Authority)
            .Build();
        // Estudar sobre token caches. Ainda não entendi => https://learn.microsoft.com/pt-br/entra/msal/dotnet/how-to/token-cache-serialization?tabs=aspnetcore
        var authResult = await app.AcquireTokenForClient([$"{extension.ResourceUrl}.default"]).ExecuteAsync(cancellationToken);
        var authorizationHeader = AuthenticationHeaderValue.Parse($"Bearer {authResult.AccessToken}");
        request.Headers.Authorization = authorizationHeader;
        return await base.SendAsync(request, cancellationToken);
    }
}