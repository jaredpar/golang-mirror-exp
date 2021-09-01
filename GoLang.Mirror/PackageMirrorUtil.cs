using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using System;
using System.Collections.Generic;
using System.IO;
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

        public async Task MirrorPackageVersionAsync(string packageName, string version, bool overwrite = false)
        {
            var path = @$"{packageName}/@v/list";
            var blobClient = ContainerClient.GetBlobClient(path);
            await blobClient.CreateIfNotExistsAsync();

            var leaseClient = blobClient.GetBlobLeaseClient();
            var lease = await leaseClient.AcquireAsync(TimeSpan.FromSeconds(30));
            try
            {
                var versions = await GetMirroredPackageVersionsAsync(packageName);
                if (!overwrite && versions.Contains(version))
                {
                    return;
                }

                await MirrorPackageContentAsync(packageName, version);
                versions.Add(version);
                versions.Sort(StringComparer.Ordinal);

                var bytes = Encoding.UTF8.GetBytes(string.Join('\n', versions));
                var uploadStream = new MemoryStream(bytes);
                var uploadOptions = new BlobUploadOptions()
                {
                    Conditions = new BlobRequestConditions()
                    {
                        LeaseId = lease.Value.LeaseId,
                    }
                };
                await blobClient.UploadAsync(uploadStream, uploadOptions);
            }
            finally
            {
                await leaseClient.ReleaseAsync();
            }
        }

        public async Task<List<string>> GetMirroredPackageVersionsAsync(string packageName)
        {
            var path = @$"{packageName}/@v/list";
            var blobClient = ContainerClient.GetBlobClient(path);
            if (!await blobClient.ExistsAsync())
            {
                return new List<string>();
            }

            var list = new List<string>();
            var stream = await blobClient.OpenReadAsync();
            var reader = new StreamReader(stream, Encoding.UTF8);
            while (reader.ReadLine() is { } line)
            {
                list.Add(line);
            }
            return list;
        }

        public async Task<bool> IsPackageMirroredAsync(string packageName, string version)
        {
            var versions = await GetMirroredPackageVersionsAsync(packageName);
            return versions.Contains(version);
        }

        /// <summary>
        /// This will mirror the contents of a package (mod, zip and info file). This will overwrite the contents
        /// if they already exist
        /// </summary>
        public async Task MirrorPackageContentAsync(string packageName, string version, bool overwrite = false)
        {
            await MirrorOne($"{packageName}/@v/{version}.info");
            await MirrorOne($"{packageName}/@v/{version}.mod");
            await MirrorOne($"{packageName}/@v/{version}.zip");
            async Task MirrorOne(string path)
            {
                var response = await HttpClient.GetAsync(GetGoProxyUri(path));
                var stream = await response.Content.ReadAsStreamAsync();
                var blobClient = ContainerClient.GetBlobClient(path);
                await blobClient.UploadAsync(stream, overwrite: true);
            }
        }

        private static Uri GetGoProxyUri(string path)
        {
            var uriBuilder = new UriBuilder(Constants.GoProxyUri);
            uriBuilder.Path = path;
            return uriBuilder.Uri;
        }
    }
}
