using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System;
using System.Text.RegularExpressions;

namespace DataAccess.AzureStorage.Blob
{
    public abstract class AzureBlobManager : AzureStorageManager
    {
        public string ContainerName { get; private protected set; }
        private protected BlobServiceClient ServiceClient { get; }
        private protected BlobContainerClient ContainerClient { get; private set; }
        private readonly object _containerClientLock = new object();

        private protected AzureBlobManager(string connectionString) : base(connectionString)
        {
            try
            {
                ServiceClient = new BlobServiceClient(AzureStorageConnectionString);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to create a BlobServiceClient from the supplied connection string.", ex);
            }
        }

        private protected void SetContainerCore(string containerName)
        {
            string normalizedContainerName = containerName?.ToLowerInvariant();
            ValidateContainerName(normalizedContainerName);
            lock (_containerClientLock)
            {
                if (ContainerClient != null && string.Equals(ContainerName, normalizedContainerName, StringComparison.Ordinal))
                {
                    return;
                }
                try
                {
                    BlobContainerClient client = ServiceClient.GetBlobContainerClient(normalizedContainerName);
                    client.CreateIfNotExists(publicAccessType: PublicAccessType.Blob);
                    ContainerName = normalizedContainerName;
                    ContainerClient = client;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to create or verify container '{normalizedContainerName}'.", ex);
                }
            }
        }

        private protected BlobContainerClient GetContainerClient(string containerName)
        {
            string normalizedContainerName = containerName?.ToLowerInvariant();
            ValidateContainerName(normalizedContainerName);
            return ServiceClient.GetBlobContainerClient(normalizedContainerName);
        }

        private protected void EnsureContainerReady()
        {
            if (ContainerClient == null)
            {
                throw new InvalidOperationException(
                    "No container has been selected. Call SetContainer(...) or use the constructor " +
                    "overload that accepts a container name before performing operations.");
            }
        }

        protected static void ValidateContainerName(string containerName)
        {
            int minContainerNameLength = 3;
            int maxContainerNameLength = 63;
            Regex ContainerNamePattern = new Regex(@"^[a-z0-9]([a-z0-9\-]*[a-z0-9])?$", RegexOptions.Compiled);

            if (string.IsNullOrWhiteSpace(containerName))
                throw new ArgumentException("Container name cannot be null or empty.", nameof(containerName));

            if (containerName.Length < minContainerNameLength || containerName.Length > maxContainerNameLength)
                throw new ArgumentException(
                    $"Container name must be between {minContainerNameLength} and {maxContainerNameLength} characters.",
                    nameof(containerName));

            if (!ContainerNamePattern.IsMatch(containerName))
                throw new ArgumentException(
                    "Container name must consist of lowercase letters, numbers, and single hyphens, " +
                    "and must start and end with a letter or number.", nameof(containerName));

            if (containerName.Contains("--"))
                throw new ArgumentException("Container name must not contain consecutive hyphens ('--').", nameof(containerName));
        }
    }
}