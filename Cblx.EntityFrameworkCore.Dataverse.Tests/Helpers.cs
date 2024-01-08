using Azure.Identity;
using Cblx.EntityFrameworkCore.Dataverse.Tests.Data.Default;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace Cblx.EntityFrameworkCore.Dataverse.Tests;

internal static class Helpers
{
    private static Lazy<IServiceProvider> _testEnv = new(() =>
    {
        var services = new ServiceCollection();
        var credential = new DefaultAzureCredential();
        var config = new ConfigurationBuilder()
            .AddAzureKeyVault(new Uri("https://efcoredataversetests.vault.azure.net/"), credential)
            .Build();
        services.AddDbContext<DefaultTestContext>(options =>
        {
            options.UseDataverse(dataverse =>
                dataverse
                    .ClientId(config["ClientId"]!)
                    .ClientSecret(config["ClientSecret"]!)
                    .ResourceUrl(config["ResourceUrl"]!)
                    .Authority(config["Authority"]!)
            );
        });
        return services.BuildServiceProvider();
    });

    public static DefaultTestContext GetDefaultContext()
    {
        return _testEnv.Value.GetRequiredService<DefaultTestContext>();
    }
}
