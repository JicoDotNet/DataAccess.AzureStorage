using Azure.Data.Tables;
using Azure.Storage.Blobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.AzureStorage.Blob
{
    public abstract class AzureBlobManager : AzureManager, IDisposable
    {
        private protected BlobServiceClient serviceClient { get; private set; }

        private protected BlobContainerClient blobContainerClient;
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
                await blobContainerClient.CreateIfNotExistsAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
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
