using Azure;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataAccess.AzureStorage.Table
{
    public sealed class DynamicTableEntity : TableEntity, IDynamicTableEntity
    {
        public DynamicTableEntity()
        {
            Properties = new Dictionary<string, object>();
        }

        public IDictionary<string, object> Properties { get; private set; }
        public void Set(IDictionary<string, object> properties)
        {
            Properties = new Dictionary<string, object>
            {
                [nameof(PartitionKey)] = PartitionKey,
                [nameof(RowKey)] = RowKey,
                [nameof(Timestamp)] = Timestamp,
                [nameof(ETag)] = ETag
            };

            foreach (KeyValuePair<string, object> property in properties)
            {
                if (IsValidType(property.Value))
                {
                    Properties[property.Key] = property.Value;
                }
            }
        }
        internal void SetEntity(Azure.Data.Tables.TableEntity entity)
        {
            PartitionKey = entity[nameof(PartitionKey)].ToString();
            RowKey = entity[nameof(RowKey)].ToString();
            Timestamp = entity.ContainsKey(nameof(Timestamp)) ? (DateTimeOffset?)entity[nameof(Timestamp)] : null;
            ETag = entity.ContainsKey(nameof(ETag)) ? (ETag)entity[nameof(ETag)] : default;

            foreach (KeyValuePair<string, object> property in entity)
            {
                if (property.Key != nameof(PartitionKey) &&
                    property.Key != nameof(RowKey) &&
                    property.Key != nameof(Timestamp) &&
                    property.Key != nameof(ETag) &&
                    property.Key != "odata.etag")
                {
                    if (IsValidType(property.Value))
                    {
                        Properties[property.Key] = property.Value;
                    }                    
                }
            }
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
                EdmType.Guid => typeof(Guid),
                _ => throw new ArgumentOutOfRangeException(nameof(dataType), "Unsupported data type")
            };
        }
    }
}
