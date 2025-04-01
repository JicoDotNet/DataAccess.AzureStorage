using Azure;
using System;
using System.Collections.Generic;

namespace DataAccess.AzureStorage.Table
{
    public abstract class DynamicTableEntity : TableEntity, IDynamicTableEntity
    {
        // Dictionary to hold dynamic properties
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();

        // Implementation of the interface methods
        public void ReadEntity(IDictionary<string, object> properties)
        {
            PartitionKey = properties[nameof(PartitionKey)].ToString();
            RowKey = properties[nameof(RowKey)].ToString();
            Timestamp = properties.ContainsKey(nameof(Timestamp)) ? (DateTimeOffset?)properties[nameof(Timestamp)] : null;
            ETag = properties.ContainsKey(nameof(ETag)) ? (ETag)properties[nameof(ETag)] : default;

            // Load dynamic properties
            foreach (var property in properties)
            {
                if (property.Key != nameof(PartitionKey) &&
                    property.Key != nameof(RowKey) &&
                    property.Key != nameof(Timestamp) &&
                    property.Key != nameof(ETag))
                {
                    Properties[property.Key] = property.Value;
                }
            }
        }

        public IDictionary<string, object> WriteEntity()
        {
            Dictionary<string, object> entity = new Dictionary<string, object>
            {
                [nameof(PartitionKey)] = PartitionKey,
                [nameof(RowKey)] = RowKey,
                [nameof(Timestamp)] = Timestamp,
                [nameof(ETag)] = ETag
            };

            // Add dynamic properties
            foreach (var property in Properties)
            {
                entity[property.Key] = property.Value;
            }

            return entity;
        }
    }
}
