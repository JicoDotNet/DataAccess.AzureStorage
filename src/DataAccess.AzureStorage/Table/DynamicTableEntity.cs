using System;
using System.Collections.Generic;
using System.Linq;

namespace DataAccess.AzureStorage.Table
{
    /// <summary>
    /// Default implementation of <see cref="IDynamicTableEntity"/>.
    /// See the interface for the contract around Properties vs. system fields.
    /// </summary>
    public sealed class DynamicTableEntity : AzureTableEntity, IDynamicTableEntity
    {
        private static readonly IReadOnlyDictionary<EdmType, Type[]> EdmTypeToClrTypes =
            new Dictionary<EdmType, Type[]>
            {
                [EdmType.String] = new[] { typeof(string) },
                [EdmType.Binary] = new[] { typeof(byte[]) },
                [EdmType.Boolean] = new[] { typeof(bool) },
                [EdmType.DateTime] = new[] { typeof(DateTime), typeof(DateTimeOffset) },
                [EdmType.Double] = new[] { typeof(double) },
                [EdmType.Guid] = new[] { typeof(Guid) },
                [EdmType.Int32] = new[] { typeof(int) },
                [EdmType.Int64] = new[] { typeof(long) }
            };

        private static readonly HashSet<Type> SupportedClrTypes =
            new HashSet<Type>(EdmTypeToClrTypes.Values.SelectMany(types => types));

        public DynamicTableEntity()
        {
            Properties = new Dictionary<string, object>();
        }

        /// <inheritdoc />
        public IDictionary<string, object> Properties { get; private set; }

        /// <inheritdoc />
        public void Set(IDictionary<string, object> properties)
        {
            if (properties == null) throw new ArgumentNullException(nameof(properties));

            var filtered = new Dictionary<string, object>();

            foreach (var property in properties)
            {
                if (IsSystemKey(property.Key))
                    continue;

                if (IsValidType(property.Value))
                {
                    filtered[property.Key] = property.Value;
                }
            }

            Properties = filtered;
        }

        /// <inheritdoc />
        public IDictionary<string, object> ToDictionary()
        {
            var result = new Dictionary<string, object>(Properties)
            {
                [nameof(PartitionKey)] = PartitionKey,
                [nameof(RowKey)] = RowKey,
                [nameof(Timestamp)] = Timestamp,
                [nameof(ETag)] = ETag
            };

            return result;
        }

        /// <summary>
        /// Populates this entity from a raw Azure.Data.Tables.TableEntity query
        /// result. Internal — used when materializing DynamicTableEntity results
        /// inside AzureTableAccess.
        /// </summary>
        internal void SetEntity(Azure.Data.Tables.TableEntity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            PartitionKey = entity.PartitionKey;
            RowKey = entity.RowKey;
            Timestamp = entity.Timestamp;
            ETag = entity.ETag;

            var properties = new Dictionary<string, object>();

            foreach (var property in entity)
            {
                if (IsSystemKey(property.Key) || property.Key == "odata.etag")
                    continue;

                if (IsValidType(property.Value))
                {
                    properties[property.Key] = property.Value;
                }
            }
            Properties = properties;
        }

        private static bool IsSystemKey(string key) =>
            key == nameof(PartitionKey) || key == nameof(RowKey) ||
            key == nameof(Timestamp) || key == nameof(ETag);

        private static bool IsValidType(object value)
        {
            if (value == null) return false;
            return SupportedClrTypes.Contains(value.GetType());
        }
    }
}