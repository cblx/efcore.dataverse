//using Azure.Identity;
//using Microsoft.Extensions.Configuration;
//namespace Cblx.EntityFrameworkCore.Dataverse.Tests;

//internal static class Helpers
//{
//    public static DbContextOptionsBuilder<T> UseTestDataverse<T>(this DbContextOptionsBuilder<T> builder)
//        where T : DataverseDbContext
//    {
//        var credential = new DefaultAzureCredential();
//        var config = new ConfigurationBuilder()
//            .AddAzureKeyVault(new Uri("https://efcoredataversetests.vault.azure.net/"), credential)
//            .Build();
//        return (builder.UseDataverse(dataverse =>
//                   dataverse
//                    .ClientId(config["ClientId"]!)
//                    .ClientSecret(config["ClientSecret"]!)
//                    .ResourceUrl(config["ResourceUrl"]!)
//                    .Authority(config["Authority"]!)
//                    .CommandTimeout(100)
//                    .HttpRequestTimeout(TimeSpan.FromSeconds(100))
//        ) as DbContextOptionsBuilder<T>)!;
//    }

//    public static DbContextOptionsBuilder<T> UseTestDataverse2<T>(this DbContextOptionsBuilder<T> builder)
//       where T : DataverseDbContext
//    {
//        var credential = new DefaultAzureCredential();
//        var config = new ConfigurationBuilder()
//            .AddAzureKeyVault(new Uri("https://efcoredataversetests.vault.azure.net/"), credential)
//            .Build();
//        return (builder.UseDataverse(dataverse =>
//                   dataverse
//                    .ClientId(config["ClientId"]!)
//                    .ClientSecret(config["ClientSecret"]!)
//                    .ResourceUrl(config["ResourceUrl"]! + 2)
//                    .Authority(config["Authority"]!)
//                    .CommandTimeout(100)
//                    .HttpRequestTimeout(TimeSpan.FromSeconds(100))
//        ) as DbContextOptionsBuilder<T>)!;
//    }
//}
