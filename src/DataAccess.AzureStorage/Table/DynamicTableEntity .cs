using Azure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataAccess.AzureStorage.Table
{
    public abstract class DynamicTableEntity : TableEntity, IDynamicTableEntity
    {
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
        public void ReadEntity(IDictionary<string, object> properties)
        {
            PartitionKey = properties[nameof(PartitionKey)].ToString();
            RowKey = properties[nameof(RowKey)].ToString();
            Timestamp = properties.ContainsKey(nameof(Timestamp)) ? (DateTimeOffset?)properties[nameof(Timestamp)] : null;
            ETag = properties.ContainsKey(nameof(ETag)) ? (ETag)properties[nameof(ETag)] : default;

            foreach (KeyValuePair<string, object> property in properties)
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

            foreach (KeyValuePair<string, object> property in Properties)
            {
                if (IsValidType(property.Value))
                {
                    entity[property.Key] = property.Value;
                }
            }
            return entity;
        }
        private bool IsValidType(object value)
        {
            return Enum.GetValues(typeof(EdmType))
                .Cast<EdmType>()
                .Select(GetClrType)
                .Any(type => type.IsInstanceOfType(value));
        }

        private Type GetClrType(EdmType dataType)
        {
            return dataType switch
            {
                EdmType.String => typeof(string),
                EdmType.Binary => typeof(byte[]),
                EdmType.Boolean => typeof(bool),
                EdmType.DateTime => typeof(DateTime),
                EdmType.Double => typeof(double),
                EdmType.Int32 => typeof(int),
                EdmType.Int64 => typeof(long),
                _ => throw new ArgumentOutOfRangeException(nameof(dataType), "Unsupported data type")
            };
        }
    }
}
