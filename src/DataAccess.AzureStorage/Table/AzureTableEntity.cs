using Azure;
using System;

namespace DataAccess.AzureStorage.Table
{
    public abstract class AzureTableEntity : IAzureTableEntity
    {
        public AzureTableEntity(string partitionKey, string rowKey) : this()
        {
            PartitionKey = partitionKey;
            RowKey = rowKey;
        }
        public AzureTableEntity() { }
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
