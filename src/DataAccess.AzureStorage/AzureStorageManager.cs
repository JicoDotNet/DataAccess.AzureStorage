using System;

namespace DataAccess.AzureStorage
{
    public abstract class AzureStorageManager
    {
        private protected string AzureStorageConnectionString { get; private set; }
        public AzureStorageManager(string azureStorageConnectionString) {
            if (string.IsNullOrEmpty(azureStorageConnectionString))
            {
                throw new ArgumentNullException(nameof(AzureStorageConnectionString), "Connection String value can't be empty");
            }
            AzureStorageConnectionString = azureStorageConnectionString;
        }
    }
}
