using Azure.Data.Tables;
using System;
using System.Linq;

namespace DataAccess.AzureStorage.Table
{
    /// <summary>
    /// Base class for table-scoped Azure Storage access. Owns the
    /// TableServiceClient/TableClient lifecycle and table-name validation/creation.
    ///
    /// This class holds no unmanaged resources (TableServiceClient/TableClient are
    /// lightweight HTTP-based clients, not IDisposable), so it intentionally does
    /// NOT implement IDisposable or a finalizer. Forcing GC.Collect() on every
    /// dispose/finalize (as the previous version did) is expensive and provides
    /// no benefit since there is nothing here to actually release.
    /// </summary>
    public abstract class AzureTableManager : AzureStorageManager
    {
        public string TableName { get; private protected set; }
        private protected TableServiceClient ServiceClient { get; }
        private protected TableClient TableClientInstance { get; private set; }
        private readonly object _tableClientLock = new object();

        private protected AzureTableManager(string connectionString) : base(connectionString)
        {
            try
            {
                ServiceClient = new TableServiceClient(AzureStorageConnectionString);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to create a TableServiceClient from the supplied connection string.", ex);
            }
        }

        /// <summary>
        /// Points this manager at the given table, validating the name and creating
        /// the table if it doesn't already exist. Safe to call repeatedly — if the
        /// table name hasn't actually changed, no network call is made.
        /// Thread-safe: concurrent callers won't race on TableClientInstance.
        /// </summary>
        private protected void SetTable(string tableName)
        {
            ValidateTableName(tableName);
            lock (_tableClientLock)
            {
                if (TableClientInstance != null && string.Equals(TableName, tableName, StringComparison.Ordinal))
                {
                    return;
                }                
                try
                {
                    TableClient client = ServiceClient.GetTableClient(tableName);
                    client.CreateIfNotExists();
                    TableName = tableName;
                    TableClientInstance = client;
                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException($"Failed to create or verify table '{tableName}'.", ex);
                }                
            }
        }

        /// <summary>
        /// Guards every CRUD operation against being called before a table has
        /// been selected, replacing what used to be an unhelpful NullReferenceException.
        /// </summary>
        private protected void EnsureTableReady()
        {
            if (TableClientInstance == null)
            {
                throw new InvalidOperationException(
                    "No table has been selected. Call SetTableName(...) or use the constructor " +
                    "overload that accepts a table name before performing operations.");
            }
        }

        private static void ValidateTableName(string tableName)
        {
            int minTableNameLength = 3;
            int maxTableNameLength = 63;

            if (string.IsNullOrWhiteSpace(tableName))
                throw new ArgumentException("Table name cannot be null or empty.", nameof(tableName));

            if (tableName.Length < minTableNameLength || tableName.Length > maxTableNameLength)
                throw new ArgumentException(
                    $"Table name must be between {minTableNameLength} and {maxTableNameLength} characters.",
                    nameof(tableName));

            if (!char.IsLetter(tableName[0]))
                throw new ArgumentException("Table name must start with a letter.", nameof(tableName));

            if (!tableName.All(char.IsLetterOrDigit))
                throw new ArgumentException("Table name can only contain alphanumeric characters.", nameof(tableName));

            if (string.Equals(tableName, "tables", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("'tables' is a reserved name.", nameof(tableName));
        }
    }
}