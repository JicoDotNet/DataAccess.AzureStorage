using Azure.Data.Tables;
using System;
using System.Threading.Tasks;

namespace DataAccess.AzureStorage.Table
{
    public abstract class AzureTableManager : AzureManager, IDisposable
    {
        private protected TableServiceClient serviceClient { get; private set; }

        private protected TableClient tableClient;
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

        private protected void CreateTableIfNot()
        {
            try
            {
                tableClient.CreateIfNotExists();
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

        ~AzureTableManager()
        {
            GC.Collect();
        }
    }
}
