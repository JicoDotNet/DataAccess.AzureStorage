using Azure.Data.Tables;
using System;
using System.Linq;

namespace DataAccess.AzureStorage.Table
{
    public abstract class AzureTableManager : AzureManager, IDisposable
    {
        public string TableName { get; protected set; }
        private protected TableServiceClient serviceClient { get; private set; }

        private protected TableClient _tableClient;
        private protected AzureTableManager(string connectionString) : base(connectionString)
        {
            try
            {
                serviceClient = new TableServiceClient(AzureStorageConnectionString);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private protected void CreateTable()
        {
            try
            {
                _tableClient.CreateIfNotExists();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        protected bool IsValidTableName(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Table name cannot be null or empty.");

            if (tableName.Length < 3 || tableName.Length > 63)
                throw new ArgumentException("Table name must be between 3 and 63 characters.");

            if (!char.IsLetter(tableName[0]))
                throw new ArgumentException("Table name must start with a letter.");

            if (!tableName.All(char.IsLetterOrDigit))
                throw new ArgumentException("Table name can only contain alphanumeric characters.");

            if (string.Equals(tableName, "tables", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("'tables' is a reserved name.");
            return true;
        }

        public void Dispose()
        {
            GC.Collect();
        }

        ~AzureTableManager()
        {
            GC.Collect();
        }
    }
}
