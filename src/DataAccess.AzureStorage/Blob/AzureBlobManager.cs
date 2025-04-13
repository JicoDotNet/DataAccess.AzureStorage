using Azure.Storage.Blobs;
using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DataAccess.AzureStorage.Blob
{
    public abstract class AzureBlobManager : AzureManager, IDisposable
    {
        public string ContainerName { get; protected set; }
        private protected BlobServiceClient serviceClient { get; private set; }

        private protected BlobContainerClient _blobContainerClient;
        private protected AzureBlobManager(string connectionString) : base(connectionString)
        {
            try
            {
                serviceClient = new BlobServiceClient(AzureStorageConnectionString);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private protected async Task CreateContainerAsync()
        {
            try
            {
                await _blobContainerClient.CreateIfNotExistsAsync();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        protected bool IsValidContainerName(string containerName)
        {
            if (string.IsNullOrWhiteSpace(containerName))
                throw new ArgumentException("Container name cannot be null or empty.");

            if (containerName.Length < 3 || containerName.Length > 63)
                throw new ArgumentException("Container name must be between 3 and 63 characters.");

            if (!Regex.IsMatch(containerName, @"^[a-z0-9]([a-z0-9\-]*[a-z0-9])?$"))
                throw new ArgumentException("Container name must match with (^[a-z0-9]([a-z0-9\\-]*[a-z0-9])?$).");

            if (containerName.Contains("--"))
                throw new ArgumentException("Container name must not be contains of '--'.");

            if (!char.IsLetter(containerName[0]))
                throw new ArgumentException("Container name must start with a letter.");

            return true;
        }

        public void Dispose()
        {
            GC.Collect();
        }

        ~AzureBlobManager()
        {
            GC.Collect();
        }
    }
}
