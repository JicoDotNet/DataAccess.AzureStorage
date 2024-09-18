using System;

namespace DataAccess.AzureStorage
{
    public abstract class AzureManager
    {
        private protected string AzureStorageConnectionString { get; private set; }
        public AzureManager(string _azureStorageConnectionString) {
            if (string.IsNullOrEmpty(_azureStorageConnectionString.ToString()))
            {
                throw new ArgumentNullException(nameof(AzureStorageConnectionString), "Connection String value can't be empty");
            }
            AzureStorageConnectionString = _azureStorageConnectionString;
        }
    }
}
