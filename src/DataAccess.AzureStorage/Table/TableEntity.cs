using Azure;
using Azure.Data.Tables;
using System;

namespace DataAccess.AzureStorage.Table
{
    public abstract class TableEntity : ITableEntity
    {
        public TableEntity(string partitionKey, string rowKey) : this()
        {
            PartitionKey = partitionKey;
            RowKey = rowKey;
        }
        public TableEntity() { }
        public string PartitionKey { get; set; }
        public string RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }
}
