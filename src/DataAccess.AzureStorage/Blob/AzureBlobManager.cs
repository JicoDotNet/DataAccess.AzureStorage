using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System;
using System.Text.RegularExpressions;

namespace DataAccess.AzureStorage.Blob
{
    /// <summary>
    /// Base class for container-scoped Azure Blob Storage access. Owns the
    /// BlobServiceClient/BlobContainerClient lifecycle and container-name
    /// validation/creation.
    ///
    /// Holds no unmanaged resources (BlobServiceClient/BlobContainerClient are
    /// lightweight HTTP-based clients, not IDisposable), so this class
    /// intentionally does NOT implement IDisposable or a finalizer.
    /// </summary>
    public abstract class AzureBlobManager : AzureStorageManager
    {
        /// <summary>
        /// The name of the container currently selected, or null if
        /// <see cref="SetContainerCore"/> hasn't been called yet.
        /// Example value: "customer-photos".
        /// </summary>
        public string ContainerName { get; private protected set; }
        /// <summary>
        /// The account-level client used to create/retrieve container clients.
        /// Constructed once from the connection string supplied to the constructor.
        /// </summary>
        private protected BlobServiceClient ServiceClient { get; }
        /// <summary>
        /// The client for the currently selected container (see <see cref="ContainerName"/>).
        /// Null until a container has been selected.
        /// </summary>
        private protected BlobContainerClient ContainerClient { get; private set; }
        private readonly object _containerClientLock = new object();

        /// <summary>
        /// Creates the underlying <see cref="BlobServiceClient"/> from the given
        /// connection string. Does not select a container yet — call
        /// <see cref="SetContainerCore"/> (via the derived class's
        /// <c>SetContainer</c>) before performing any operation.
        /// </summary>
        /// <param name="connectionString">
        /// The storage account connection string, e.g.
        /// "DefaultEndpointsProtocol=https;AccountName=mystorageacct;AccountKey=...;EndpointSuffix=core.windows.net",
        /// or "UseDevelopmentStorage=true" for the Azurite emulator.
        /// </param>
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

        /// <summary>
        /// Validates and selects the container this instance operates against,
        /// creating it (with blob-level public access) if it doesn't already
        /// exist. Safe to call repeatedly — if the container name hasn't
        /// actually changed since the last call, no network request is made.
        /// Thread-safe.
        /// </summary>
        /// <param name="containerName">
        /// The container name, e.g. "customer-photos". Must be 3–63 characters,
        /// lowercase letters/numbers/single-hyphens only, and start and end with
        /// a letter or number. Case is normalized to lowercase automatically —
        /// e.g. passing "Customer-Photos" selects/creates "customer-photos".
        /// </param>
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

        /// <summary>
        /// Returns a client for an arbitrary container by name, without
        /// changing this instance's currently selected container
        /// (<see cref="ContainerName"/>) and without creating the container if
        /// it doesn't exist. Used for cross-container operations, e.g. deleting
        /// a blob by URL that points at a container other than the one
        /// currently selected, so the operation never has the side effect of
        /// repointing this instance or silently creating a container.
        /// </summary>
        /// <param name="containerName">
        /// The container name to get a client for, e.g. "archived-invoices".
        /// Same naming rules as <see cref="SetContainerCore"/>.
        /// </param>
        private protected BlobContainerClient GetContainerClient(string containerName)
        {
            string normalizedContainerName = containerName?.ToLowerInvariant();
            ValidateContainerName(normalizedContainerName);
            return ServiceClient.GetBlobContainerClient(normalizedContainerName);
        }

        /// <summary>
        /// Throws if no container has been selected yet, so CRUD methods fail
        /// with a clear, actionable message instead of a NullReferenceException.
        /// </summary>
        private protected void EnsureContainerReady()
        {
            if (ContainerClient == null)
            {
                throw new InvalidOperationException(
                    "No container has been selected. Call SetContainer(...) or use the constructor " +
                    "overload that accepts a container name before performing operations.");
            }
        }

        /// <summary>
        /// Validates a container name against Azure's container naming rules.
        /// Throws <see cref="ArgumentException"/> (or <see cref="ArgumentException"/>
        /// for null/empty) if invalid; does not return a value on failure.
        /// </summary>
        /// <param name="containerName">
        /// The already-lowercased container name to validate, e.g. "customer-photos".
        /// A name like "Customer_Photos" (uppercase or underscore) would fail
        /// this check.
        /// </param>
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