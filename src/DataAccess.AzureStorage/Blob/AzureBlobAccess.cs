using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataAccess.AzureStorage.Blob
{
    public sealed class AzureBlobAccess : AzureBlobManager, IAzureBlobAccess
    {
        public AzureBlobAccess(string connectionString) : base(connectionString){ }

        public AzureBlobAccess(string containerName, string connectionString) : base(connectionString)
        {
            SetContainer(containerName);
        }

        /// <inheritdoc/>
        public void SetContainer(string containerName) => SetContainerCore(containerName);

        #region Upload
        /// <inheritdoc/>
        public IBlobResponseClient Upload(IBlobRequestClient blobRequestToUpload, bool overwrite = true)
        {
            EnsureContainerReady();
            if (blobRequestToUpload == null) throw new ArgumentNullException(nameof(blobRequestToUpload));
            string blobPath = BuildBlobPath(blobRequestToUpload.Directories, blobRequestToUpload.FileName);
            var blobClient = ContainerClient.GetBlobClient(blobPath);
            try
            {
                var result = blobClient.Upload(blobRequestToUpload.FileStream, new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders { ContentType = blobRequestToUpload.ContentType },
                    Conditions = overwrite ? null : new BlobRequestConditions { IfNoneMatch = Azure.ETag.All }
                });
                return ToResponseClient(blobClient, result.Value.ETag);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Upload failed for blob '{blobPath}' in container '{ContainerName}'.", ex);
            }
        }

        /// <inheritdoc/>
        public async Task<IBlobResponseClient> UploadAsync(IBlobRequestClient blobRequestToUpload, bool overwrite = true, CancellationToken cancellationToken = default)
        {
            EnsureContainerReady();
            if (blobRequestToUpload == null) throw new ArgumentNullException(nameof(blobRequestToUpload));
            string blobPath = BuildBlobPath(blobRequestToUpload.Directories, blobRequestToUpload.FileName);
            BlobClient blobClient = ContainerClient.GetBlobClient(blobPath);

            try
            {
                var result = await blobClient.UploadAsync(blobRequestToUpload.FileStream, new BlobUploadOptions
                {
                    HttpHeaders = new BlobHttpHeaders { ContentType = blobRequestToUpload.ContentType },
                    Conditions = overwrite ? null : new BlobRequestConditions { IfNoneMatch = Azure.ETag.All }
                }, cancellationToken).ConfigureAwait(false);
                return ToResponseClient(blobClient, result.Value.ETag);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Upload failed for blob '{blobPath}' in container '{ContainerName}'.", ex);
            }
        }
        #endregion

        #region List
        /// <inheritdoc/>
        public List<IBlobDetails> BlobDetails() => BlobDetails(null);
        /// <inheritdoc/>
        public List<IBlobDetails> BlobDetails(string[] directories)
        {
            EnsureContainerReady();
            string prefix = BuildPrefix(directories);
            try
            {
                List<IBlobDetails> results = new List<IBlobDetails>();
                foreach (BlobItem blobItem in ContainerClient.GetBlobs(prefix: prefix))
                {
                    results.Add(ToBlobDetails(blobItem));
                }
                return results;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Listing blobs failed for container '{ContainerName}'.", ex);
            }
        }
        /// <inheritdoc/>
        public Task<List<IBlobDetails>> BlobDetailsAsync(CancellationToken cancellationToken = default)
            => BlobDetailsAsync(null, cancellationToken);
        /// <inheritdoc/>
        public async Task<List<IBlobDetails>> BlobDetailsAsync(string[] directories, CancellationToken cancellationToken = default)
        {
            EnsureContainerReady();
            string prefix = BuildPrefix(directories);
            try
            {
                List<IBlobDetails> results = new List<IBlobDetails>();
                await foreach (BlobItem blobItem in ContainerClient.GetBlobsAsync(prefix: prefix, cancellationToken: cancellationToken).ConfigureAwait(false))
                {
                    results.Add(ToBlobDetails(blobItem));
                }
                return results;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Listing blobs failed for container '{ContainerName}'.", ex);
            }
        }
        #endregion

        #region Exists
        /// <inheritdoc/>
        public bool Exists(string blobName)
        {
            EnsureContainerReady();
            string sanitized = RequireBlobName(blobName);
            try
            {
                return ContainerClient.GetBlobClient(sanitized).Exists();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Exists check failed for blob '{blobName}' in container '{ContainerName}'.", ex);
            }
        }
        /// <inheritdoc/>
        public async Task<bool> ExistsAsync(string blobName, CancellationToken cancellationToken = default)
        {
            EnsureContainerReady();
            string sanitized = RequireBlobName(blobName);

            try
            {
                var response = await ContainerClient.GetBlobClient(sanitized).ExistsAsync(cancellationToken).ConfigureAwait(false);
                return response.Value;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Exists check failed for blob '{blobName}' in container '{ContainerName}'.", ex);
            }
        }
        #endregion

        #region Delete — name-relative (current container)
        /// <inheritdoc/>
        public bool Delete(string blobName)
        {
            EnsureContainerReady();
            string sanitized = RequireBlobName(blobName);

            try
            {
                return ContainerClient.GetBlobClient(sanitized).DeleteIfExists();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Delete failed for blob '{blobName}' in container '{ContainerName}'.", ex);
            }
        }
        /// <inheritdoc/>
        public async Task<bool> DeleteAsync(string blobName, CancellationToken cancellationToken = default)
        {
            EnsureContainerReady();
            string sanitized = RequireBlobName(blobName);

            try
            {
                var response = await ContainerClient.GetBlobClient(sanitized).DeleteIfExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                return response.Value;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Delete failed for blob '{blobName}' in container '{ContainerName}'.", ex);
            }
        }
        #endregion

        #region Delete — by URL
        /// <inheritdoc/>
        public bool DeleteByUrl(string blobUrl)
        {
            var (containerName, blobName) = ParseBlobUrl(blobUrl);

            try
            {
                var containerClient = GetContainerClient(containerName);
                return containerClient.GetBlobClient(blobName).DeleteIfExists();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Delete failed for blob '{blobName}' in container '{containerName}'.", ex);
            }
        }
        /// <inheritdoc/>
        public async Task<bool> DeleteByUrlAsync(string blobUrl, CancellationToken cancellationToken = default)
        {
            var (containerName, blobName) = ParseBlobUrl(blobUrl);

            try
            {
                var containerClient = GetContainerClient(containerName);
                var response = await containerClient.GetBlobClient(blobName).DeleteIfExistsAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                return response.Value;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Delete failed for blob '{blobName}' in container '{containerName}'.", ex);
            }
        }
        #endregion

        #region Download — name-relative
        /// <inheritdoc/>
        public (byte[] fileContent, string contentType) DownloadFile(string blobName)
        {
            EnsureContainerReady();
            string sanitized = RequireBlobName(blobName);

            try
            {
                return DownloadCore(ContainerClient.GetBlobClient(sanitized));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Download failed for blob '{blobName}' in container '{ContainerName}'.", ex);
            }
        }
        /// <inheritdoc/>
        public async Task<(byte[] fileContent, string contentType)> DownloadFileAsync(string blobName, CancellationToken cancellationToken = default)
        {
            EnsureContainerReady();
            string sanitized = RequireBlobName(blobName);

            try
            {
                return await DownloadCoreAsync(ContainerClient.GetBlobClient(sanitized), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Download failed for blob '{blobName}' in container '{ContainerName}'.", ex);
            }
        }
        #endregion

        #region Download — by URL
        /// <inheritdoc/>
        public (byte[] fileContent, string contentType) DownloadFileByUrl(string blobUrl)
        {
            var (containerName, blobName) = ParseBlobUrl(blobUrl);

            try
            {
                var containerClient = GetContainerClient(containerName);
                return DownloadCore(containerClient.GetBlobClient(blobName));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Download failed for blob '{blobName}' in container '{containerName}'.", ex);
            }
        }
        /// <inheritdoc/>
        public async Task<(byte[] fileContent, string contentType)> DownloadFileByUrlAsync(string blobUrl, CancellationToken cancellationToken = default)
        {
            var (containerName, blobName) = ParseBlobUrl(blobUrl);

            try
            {
                var containerClient = GetContainerClient(containerName);
                return await DownloadCoreAsync(containerClient.GetBlobClient(blobName), cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Download failed for blob '{blobName}' in container '{containerName}'.", ex);
            }
        }
        #endregion

        #region Internal helpers
        private static (byte[] fileContent, string contentType) DownloadCore(BlobClient blobClient)
        {
            using (var ms = new MemoryStream())
            {
                blobClient.DownloadTo(ms);
                string contentType = blobClient.GetProperties().Value.ContentType;
                return (ms.ToArray(), contentType);
            }
        }

        private static async Task<(byte[] fileContent, string contentType)> DownloadCoreAsync(BlobClient blobClient, CancellationToken cancellationToken)
        {
            using (var ms = new MemoryStream())
            {
                await blobClient.DownloadToAsync(ms, cancellationToken).ConfigureAwait(false);
                var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
                return (ms.ToArray(), properties.Value.ContentType);
            }
        }

        private static (string containerName, string blobName) ParseBlobUrl(string blobUrl)
        {
            if (string.IsNullOrWhiteSpace(blobUrl))
                throw new ArgumentException("Blob URL must not be null or empty.", nameof(blobUrl));

            Uri uri;
            try
            {
                uri = new Uri(blobUrl);
            }
            catch (UriFormatException ex)
            {
                throw new ArgumentException($"'{blobUrl}' is not a valid absolute blob URL.", nameof(blobUrl), ex);
            }

            var builder = new BlobUriBuilder(uri);

            if (string.IsNullOrEmpty(builder.BlobContainerName) || string.IsNullOrEmpty(builder.BlobName))
            {
                throw new ArgumentException(
                    $"'{blobUrl}' does not appear to contain both a container name and a blob name.", nameof(blobUrl));
            }
            return (builder.BlobContainerName, builder.BlobName);
        }

        private static string RequireBlobName(string blobName)
        {
            if (string.IsNullOrWhiteSpace(blobName))
                throw new ArgumentException("Blob name must not be null or empty.", nameof(blobName));
            return SanitizedName(blobName);
        }

        private static IBlobDetails ToBlobDetails(BlobItem blobItem)
        {
            return new BlobDetails
            {
                Path = blobItem.Name,
                AbsoluteUri = null,
                ContentLength = blobItem.Properties.ContentLength ?? 0,
                ContentType = blobItem.Properties.ContentType,
                LastModified = blobItem.Properties.LastModified ?? default,
                ETag = blobItem.Properties.ETag ?? default
            };
        }

        private static IBlobResponseClient ToResponseClient(BlobClient blobClient, Azure.ETag eTag)
        {
            return new BlobResponseClient
            {
                Uri = blobClient.Uri,
                AccountName = blobClient.AccountName,
                AbsolutePath = blobClient.Uri.AbsolutePath,
                ContainerName = blobClient.BlobContainerName,
                Path = blobClient.Uri.ToString(),
                ETag = eTag
            };
        }

        private static string BuildPrefix(string[] directories)
        {
            string path = BuildDirectoryPath(directories);
            return string.IsNullOrEmpty(path) ? null : $"{path}/";
        }

        private static string BuildDirectoryPath(string[] directories)
        {
            if (directories == null || directories.Length == 0) return null;

            List<string> segments = directories
                .Select(SanitizedName)
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
            return segments.Count == 0 ? null : string.Join("/", segments);
        }

        private static string BuildBlobPath(string[] directories, string fileName)
        {
            string sanitizedFileName = SanitizedName(fileName);
            if (string.IsNullOrEmpty(sanitizedFileName))
                throw new ArgumentException("File name is empty or contains only invalid characters.", nameof(fileName));

            string directoryPath = BuildDirectoryPath(directories);
            string fullPath = string.IsNullOrEmpty(directoryPath) ? sanitizedFileName : $"{directoryPath}/{sanitizedFileName}";

            if (fullPath.Length > 1024)
                throw new ArgumentException("The resulting blob path exceeds Azure's 1024-character limit.", nameof(fileName));

            return fullPath;
        }

        private static string SanitizedName(string directoryOrFileName)
        {
            if (string.IsNullOrWhiteSpace(directoryOrFileName))
                return string.Empty;

            string[] invalidChars = new[] { "\\", "?", "#", "[", "]" };
            string sanitized = directoryOrFileName;
            foreach (var invalidChar in invalidChars)
            {
                sanitized = sanitized.Replace(invalidChar, string.Empty);
            }
            sanitized = sanitized.Trim('/');
            if (sanitized.Split('/').Any(segment => segment == ".."))
            {
                throw new ArgumentException($"'{directoryOrFileName}' contains a path-traversal segment ('..'), which is not allowed.");
            }
            return sanitized;
        }
        #endregion
    }
}