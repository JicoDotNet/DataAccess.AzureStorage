using Azure.Data.Tables;
using System.Collections.Generic;

namespace DataAccess.AzureStorage.Table
{
    /// <summary>
    /// A table entity whose custom (non-system) properties are stored as a
    /// loosely-typed property bag, for scenarios where the row shape isn't
    /// known at compile time.
    ///
    /// PartitionKey, RowKey, Timestamp and ETag are NOT duplicated inside
    /// <see cref="Properties"/> — they already exist as strongly-typed members
    /// via <see cref="ITableEntity"/>. Use <see cref="Get"/> if you
    /// need one flat dictionary containing everything (system fields + custom
    /// properties), e.g. for logging or serialization.
    /// </summary>
    public interface IDynamicTableEntity : ITableEntity
    {
        /// <summary>
        /// This entity's custom properties only. System fields are intentionally
        /// excluded — read PartitionKey/RowKey/Timestamp/ETag directly off the entity.
        /// </summary>
        IDictionary<string, object> Properties { get; }

        /// <summary>
        /// Replaces the custom properties on this entity. A value is kept only if
        /// its CLR type is a supported Azure Table Storage EDM type; unsupported
        /// types and null values are skipped (Table Storage has no first-class
        /// "null property" — omit it instead of storing null).
        /// </summary>
        void Set(IDictionary<string, object> properties);

        /// <summary>
        /// Returns a single flat dictionary containing both the system fields
        /// (PartitionKey, RowKey, Timestamp, ETag) and all custom properties.
        /// </summary>
        IDictionary<string, object> Get();
    }
}