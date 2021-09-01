using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using GoLang.Mirror;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.AzureKeyVault;
using System;
using System.Threading.Tasks;

namespace Scratch
{
    class Program
    {
        static async Task Main()
        {
            var configuration = CreateConfiguration();
            var connectionString = configuration[Constants.StorageConnectionStringSecreteName];
            var client = new BlobServiceClient(connectionString);
            var containerClient = client.GetBlobContainerClient("packages");
            await containerClient.CreateIfNotExistsAsync();
            var packageMirrorUtil = new PackageMirrorUtil(containerClient);
            await packageMirrorUtil.MirrorPackageVersionAsync("github.com/jaredpar/greetings", "v0.1.1");
            await packageMirrorUtil.MirrorPackageVersionAsync("github.com/jaredpar/greetings", "v0.1.1", overwrite: true);
            await packageMirrorUtil.MirrorPackageVersionAsync("github.com/jaredpar/greetings", "v0.1.1", overwrite: false);


        }



        internal static IConfiguration CreateConfiguration()
        {
            var config = new ConfigurationBuilder()
                .AddAzureKeyVault(
                    "https://golangexp2.vault.azure.net/",
                    new DefaultKeyVaultSecretManager())
                .Build();
            return config;
        }
    }
}
