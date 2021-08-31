using Azure.Storage.Blobs;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GoLang.Mirror
{
    public sealed class PackageMirrorUtil
    {
        public BlobContainerClient ContainerClient { get; }
        public HttpClient HttpClient { get; }

        public PackageMirrorUtil(BlobContainerClient containerClient, HttpClient? httpClient = null)
        {
            ContainerClient = containerClient;
            HttpClient = httpClient ?? new HttpClient();
        }

        public async Task MirrorPackageVersionAsync(string packageName, string version)
        {
            await MirrorOne($"{packageName}/@v/{version}.info");
            await MirrorOne($"{packageName}/@v/{version}.mod");
            await MirrorOne($"{packageName}/@v/{version}.zip");

            // HACK: list needs to be updated separately and handle contention
            await MirrorOne($"{packageName}/@v/list");

            async Task MirrorOne(string path)
            {
                var uriBuilder = new UriBuilder(Constants.GoProxyUri);
                uriBuilder.Path = path;
                var response = await HttpClient.GetAsync(uriBuilder.Uri);
                var stream = await response.Content.ReadAsStreamAsync();
                var blobClient = ContainerClient.GetBlobClient(path);
                await blobClient.UploadAsync(stream);
            }
        }
    }
}
