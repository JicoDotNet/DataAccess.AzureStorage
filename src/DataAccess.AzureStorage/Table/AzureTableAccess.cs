using Azure;
using Azure.Data.Tables;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace DataAccess.AzureStorage.Table
{
    public sealed class AzureTableAccess : AzureTableManager, IAzureTableAccess
    {
        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> DateTimePropertyCache = new ConcurrentDictionary<Type, PropertyInfo[]>();
        public AzureTableAccess(string connectionString) : base(connectionString){ }

        public AzureTableAccess(string tableName, string connectionString) : base(connectionString)
        {
            SetTableName(tableName);
        }

        public void SetTableName(string tableName) => SetTable(tableName);

        #region AzureTableEntity
        public T InsertEntity<T>(T entity) where T : AzureTableEntity
        {
            EnsureTableReady();
            ValidateEntity(entity);
            try
            {
                TableClientInstance.AddEntity(ToSdkEntity(entity));
                return entity;
            }
            catch(RequestFailedException rex)
            {
                throw rex;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Insert failed for PartitionKey='{entity.PartitionKey}', RowKey='{entity.RowKey}'.", ex);
            }
        }

        public async Task<T> InsertEntityAsync<T>(T entity, CancellationToken cancellationToken = default) where T : AzureTableEntity
        {
            EnsureTableReady();
            ValidateEntity(entity);
            try
            {
                await TableClientInstance.AddEntityAsync(ToSdkEntity(entity), cancellationToken).ConfigureAwait(false);
                return entity;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Insert failed for PartitionKey='{entity.PartitionKey}', RowKey='{entity.RowKey}'.", ex);
            }
        }

        public T UpsertEntity<T>(T entity) where T : AzureTableEntity
        {
            EnsureTableReady();
            ValidateEntity(entity);
            try
            {
                TableClientInstance.UpsertEntity(ToSdkEntity(entity), TableUpdateMode.Merge);
                return entity;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Upsert failed for PartitionKey='{entity.PartitionKey}', RowKey='{entity.RowKey}'.", ex);
            }
        }

        public async Task<T> UpsertEntityAsync<T>(T entity, CancellationToken cancellationToken = default) where T : AzureTableEntity
        {
            EnsureTableReady();
            ValidateEntity(entity);
            try
            {
                await TableClientInstance.UpsertEntityAsync(ToSdkEntity(entity), TableUpdateMode.Merge, cancellationToken).ConfigureAwait(false);
                return entity;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Upsert failed for PartitionKey='{entity.PartitionKey}', RowKey='{entity.RowKey}'.", ex);
            }
        }

        public T ReplaceEntity<T>(T entity, ETag? ifMatch = null) where T : AzureTableEntity
        {
            EnsureTableReady();
            ValidateEntity(entity);
            try
            {
                TableClientInstance.UpdateEntity(ToSdkEntity(entity), ResolveIfMatch(ifMatch, entity.ETag), TableUpdateMode.Replace);
                return entity;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Replace failed for PartitionKey='{entity.PartitionKey}', RowKey='{entity.RowKey}'.", ex);
            }
        }

        public async Task<T> ReplaceEntityAsync<T>(T entity, ETag? ifMatch = null, CancellationToken cancellationToken = default) where T : AzureTableEntity
        {
            EnsureTableReady();
            ValidateEntity(entity);
            try
            {
                await TableClientInstance.UpdateEntityAsync(ToSdkEntity(entity), ResolveIfMatch(ifMatch, entity.ETag), TableUpdateMode.Replace, cancellationToken).ConfigureAwait(false);
                return entity;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Replace failed for PartitionKey='{entity.PartitionKey}', RowKey='{entity.RowKey}'.", ex);
            }
        }

        public T MergeEntity<T>(T entity, ETag? ifMatch = null) where T : AzureTableEntity
        {
            EnsureTableReady();
            ValidateEntity(entity);
            try
            {
                TableClientInstance.UpdateEntity(ToSdkEntity(entity), ResolveIfMatch(ifMatch, entity.ETag), TableUpdateMode.Merge);
                return entity;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Merge failed for PartitionKey='{entity.PartitionKey}', RowKey='{entity.RowKey}'.", ex);
            }
        }

        public async Task<T> MergeEntityAsync<T>(T entity, ETag? ifMatch = null, CancellationToken cancellationToken = default) where T : AzureTableEntity
        {
            EnsureTableReady();
            ValidateEntity(entity);
            try
            {
                await TableClientInstance.UpdateEntityAsync(ToSdkEntity(entity), ResolveIfMatch(ifMatch, entity.ETag), TableUpdateMode.Merge, cancellationToken).ConfigureAwait(false);
                return entity;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Merge failed for PartitionKey='{entity.PartitionKey}', RowKey='{entity.RowKey}'.", ex);
            }
        }

        public bool DeleteEntity<T>(T entity, ETag? ifMatch = null) where T : AzureTableEntity
        {
            EnsureTableReady();
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            return DeleteEntity(entity.PartitionKey, entity.RowKey, ifMatch ?? ResolveIfMatch(null, entity.ETag));
        }

        public async Task<bool> DeleteEntityAsync<T>(T entity, ETag? ifMatch = null, CancellationToken cancellationToken = default) where T : AzureTableEntity
        {
            EnsureTableReady();
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            return await DeleteEntityAsync(entity.PartitionKey, entity.RowKey, ifMatch ?? ResolveIfMatch(null, entity.ETag), cancellationToken)
                .ConfigureAwait(false);
        }

        public bool DeleteEntity(string partitionKey, string rowKey, ETag? ifMatch = null)
        {
            EnsureTableReady();
            try
            {
                TableClientInstance.DeleteEntity(partitionKey, rowKey, ifMatch ?? ETag.All);
                return true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Delete failed for PartitionKey='{partitionKey}', RowKey='{rowKey}'.", ex);
            }
        }

        public async Task<bool> DeleteEntityAsync(string partitionKey, string rowKey, ETag? ifMatch = null, CancellationToken cancellationToken = default)
        {
            EnsureTableReady();
            try
            {
                await TableClientInstance.DeleteEntityAsync(partitionKey, rowKey, ifMatch ?? ETag.All, cancellationToken).ConfigureAwait(false);
                return true;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Delete failed for PartitionKey='{partitionKey}', RowKey='{rowKey}'.", ex);
            }
        }        

        public List<T> RetrieveEntities<T>(string filter = null) where T : AzureTableEntity
        {
            EnsureTableReady();
            try
            {                
                Pageable<T> entities = string.IsNullOrEmpty(filter)
                    ? TableClientInstance.Query<T>()
                    : TableClientInstance.Query<T>(filter);

                return entities.ToList();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Query failed for table '{TableName}'.", ex);
            }
        }

        public async Task<List<T>> RetrieveEntitiesAsync<T>(string filter = null, CancellationToken cancellationToken = default) where T : AzureTableEntity
        {
            EnsureTableReady();
            try
            {
                List<T> results = new List<T>();
                AsyncPageable<T> entities = string.IsNullOrEmpty(filter)
                    ? TableClientInstance.QueryAsync<T>(cancellationToken: cancellationToken)
                    : TableClientInstance.QueryAsync<T>(filter, cancellationToken: cancellationToken);

                await foreach (T entity in entities.ConfigureAwait(false))
                {
                    results.Add(entity);
                }

                return results;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Query failed for table '{TableName}'.", ex);
            }
        }

        public T RetrieveEntity<T>(string filter) where T : AzureTableEntity
            => RetrieveEntities<T>(filter).FirstOrDefault();

        public async Task<T> RetrieveEntityAsync<T>(string filter, CancellationToken cancellationToken = default) where T : AzureTableEntity
            => (await RetrieveEntitiesAsync<T>(filter, cancellationToken).ConfigureAwait(false)).FirstOrDefault();
        #endregion

        #region DynamicTableEntity variants
        public DynamicTableEntity InsertEntity(DynamicTableEntity entity)
        {
            EnsureTableReady();
            ValidateEntity(entity);
            try
            {
                TableEntity sdkEntity = ToSdkEntity(entity);
                TableClientInstance.AddEntity(sdkEntity);
                return entity;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Insert failed for PartitionKey='{entity.PartitionKey}', RowKey='{entity.RowKey}'.", ex);
            }
        }

        public async Task<DynamicTableEntity> InsertEntityAsync(DynamicTableEntity entity, CancellationToken cancellationToken = default)
        {
            EnsureTableReady();
            ValidateEntity(entity);
            try
            {
                TableEntity sdkEntity = ToSdkEntity(entity);
                await TableClientInstance.AddEntityAsync(sdkEntity, cancellationToken).ConfigureAwait(false);
                return entity;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Insert failed for PartitionKey='{entity.PartitionKey}', RowKey='{entity.RowKey}'.", ex);
            }
        }

        public DynamicTableEntity UpsertEntity(DynamicTableEntity entity)
        {
            EnsureTableReady();
            ValidateEntity(entity);
            try
            {
                TableEntity sdkEntity = ToSdkEntity(entity);
                TableClientInstance.UpsertEntity(sdkEntity, TableUpdateMode.Merge);
                return entity;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Upsert failed for PartitionKey='{entity.PartitionKey}', RowKey='{entity.RowKey}'.", ex);
            }
        }

        public async Task<DynamicTableEntity> UpsertEntityAsync(DynamicTableEntity entity, CancellationToken cancellationToken = default)
        {
            EnsureTableReady();
            ValidateEntity(entity);
            try
            {
                TableEntity sdkEntity = ToSdkEntity(entity);
                await TableClientInstance.UpsertEntityAsync(sdkEntity, TableUpdateMode.Merge, cancellationToken).ConfigureAwait(false);
                return entity;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Upsert failed for PartitionKey='{entity.PartitionKey}', RowKey='{entity.RowKey}'.", ex);
            }
        }

        public DynamicTableEntity ReplaceEntity(DynamicTableEntity entity, ETag? ifMatch = null)
        {
            EnsureTableReady();
            ValidateEntity(entity);
            try
            {
                TableEntity sdkEntity = ToSdkEntity(entity);
                TableClientInstance.UpdateEntity(sdkEntity, ResolveIfMatch(ifMatch, entity.ETag), TableUpdateMode.Replace);
                return entity;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Replace failed for PartitionKey='{entity.PartitionKey}', RowKey='{entity.RowKey}'.", ex);
            }
        }

        public async Task<DynamicTableEntity> ReplaceEntityAsync(DynamicTableEntity entity, ETag? ifMatch = null, CancellationToken cancellationToken = default)
        {
            EnsureTableReady();
            ValidateEntity(entity);
            try
            {
                TableEntity sdkEntity = ToSdkEntity(entity);
                await TableClientInstance.UpdateEntityAsync(sdkEntity, ResolveIfMatch(ifMatch, entity.ETag), TableUpdateMode.Replace, cancellationToken).ConfigureAwait(false);
                return entity;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Replace failed for PartitionKey='{entity.PartitionKey}', RowKey='{entity.RowKey}'.", ex);
            }
        }

        public DynamicTableEntity MergeEntity(DynamicTableEntity entity, ETag? ifMatch = null)
        {
            EnsureTableReady();
            ValidateEntity(entity);
            try
            {
                TableEntity sdkEntity = ToSdkEntity(entity);
                TableClientInstance.UpdateEntity(sdkEntity, ResolveIfMatch(ifMatch, entity.ETag), TableUpdateMode.Merge);
                return entity;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Merge failed for PartitionKey='{entity.PartitionKey}', RowKey='{entity.RowKey}'.", ex);
            }
        }

        public async Task<DynamicTableEntity> MergeEntityAsync(DynamicTableEntity entity, ETag? ifMatch = null, CancellationToken cancellationToken = default)
        {
            EnsureTableReady();
            ValidateEntity(entity);
            try
            {
                TableEntity sdkEntity = ToSdkEntity(entity);
                await TableClientInstance.UpdateEntityAsync(sdkEntity, ResolveIfMatch(ifMatch, entity.ETag), TableUpdateMode.Merge, cancellationToken).ConfigureAwait(false);
                return entity;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Merge failed for PartitionKey='{entity.PartitionKey}', RowKey='{entity.RowKey}'.", ex);
            }
        }

        public List<DynamicTableEntity> RetrieveEntities(string filter)
        {
            EnsureTableReady();
            try
            {
                List<TableEntity> entities = TableClientInstance.Query<TableEntity>(filter: filter).ToList();
                if (entities.Count == 0) return new List<DynamicTableEntity>();

                return entities.Select(ToDynamicEntity).ToList();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Query failed for table '{TableName}'.", ex);
            }
        }

        public async Task<List<DynamicTableEntity>> RetrieveEntitiesAsync(string filter, CancellationToken cancellationToken = default)
        {
            EnsureTableReady();
            try
            {
                List<DynamicTableEntity> results = new List<DynamicTableEntity>();
                await foreach (var entity in TableClientInstance.QueryAsync<TableEntity>(filter: filter, cancellationToken: cancellationToken).ConfigureAwait(false))
                {
                    results.Add(ToDynamicEntity(entity));
                }
                return results;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Query failed for table '{TableName}'.", ex);
            }
        }

        public DynamicTableEntity RetrieveEntity(string filter)
            => RetrieveEntities(filter).FirstOrDefault();

        public async Task<DynamicTableEntity> RetrieveEntityAsync(string filter, CancellationToken cancellationToken = default)
            => (await RetrieveEntitiesAsync(filter, cancellationToken).ConfigureAwait(false)).FirstOrDefault();
        public bool DeleteEntity(DynamicTableEntity entity, ETag? ifMatch = null)
            => DeleteEntity<DynamicTableEntity>(entity, ifMatch);

        public Task<bool> DeleteEntityAsync(DynamicTableEntity entity, ETag? ifMatch = null, CancellationToken cancellationToken = default)
            => DeleteEntityAsync<DynamicTableEntity>(entity, ifMatch, cancellationToken);

        private static TableEntity ToSdkEntity(DynamicTableEntity entity)
        {
            TableEntity tableEntity = new TableEntity(entity.PartitionKey, entity.RowKey);
            foreach (var prop in entity.Properties)
            {
                tableEntity[prop.Key] = prop.Value;
            }
            return tableEntity;
        }

        private static DynamicTableEntity ToDynamicEntity(TableEntity source)
        {
            var dynamicEntity = new DynamicTableEntity();
            dynamicEntity.SetEntity(source);
            return dynamicEntity;
        }
        #endregion

        #region Internal helpers
        private static void ValidateEntity<T>(T entity) where T : class, IAzureTableEntity
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            if (string.IsNullOrEmpty(entity.PartitionKey))
                throw new ArgumentException("Entity.PartitionKey must not be null or empty.", nameof(entity));

            if (string.IsNullOrEmpty(entity.RowKey))
                throw new ArgumentException("Entity.RowKey must not be null or empty.", nameof(entity));
            NormalizeDateTimeKinds(entity);
        }

        /// <summary>
        /// Picks the ETag to send as a precondition: an explicit override wins;
        /// otherwise the entity's own tracked ETag is used for optimistic
        /// concurrency; falls back to ETag.All (unconditional) only if the
        /// entity was never loaded with a real ETag.
        /// </summary>
        private static ETag ResolveIfMatch(ETag? ifMatch, ETag entityETag)
        {
            if (ifMatch.HasValue) return ifMatch.Value;
            return entityETag.Equals(default(ETag)) ? ETag.All : entityETag;
        }
        #endregion

        #region Validate DateTime
        /// <summary>
        /// Scans every public DateTime/DateTime? property on the entity and, if
        /// its Kind is Unspecified, normalizes it according to
        /// _unspecifiedDateTimeHandling — so callers throughout the existing
        /// application don't need to be updated to call DateTime.SpecifyKind
        /// themselves. Property lists are cached per type to avoid repeated
        /// reflection cost on every call.
        /// </summary>
        private static void NormalizeDateTimeKinds<T>(T entity) where T : class, IAzureTableEntity
        {
            PropertyInfo[] properties = DateTimePropertyCache.GetOrAdd(typeof(T), FindDateTimeProperties);
            if (properties.Length == 0) return;
            foreach (PropertyInfo property in properties)
            {
                object rawValue = property.GetValue(entity);
                if (rawValue == null) continue;
                DateTime value = property.PropertyType == typeof(DateTime?) ? ((DateTime?)rawValue).Value : (DateTime)rawValue;
                if (value.Kind != DateTimeKind.Unspecified) continue;
                DateTime normalized = DateTime.SpecifyKind(value, DateTimeKind.Utc);
                property.SetValue(entity, property.PropertyType == typeof(DateTime?) ? (DateTime?)normalized : normalized);
            }
        }

        private static PropertyInfo[] FindDateTimeProperties(Type type)
        {
            return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => (p.PropertyType == typeof(DateTime) || p.PropertyType == typeof(DateTime?)) && p.CanRead && p.CanWrite)
                .ToArray();
        }
        #endregion

        #region Validate Property
        private static readonly ConcurrentDictionary<Type, (PropertyInfo[] Supported, PropertyInfo[] Skipped)> EntityPropertyMapCache = new ConcurrentDictionary<Type, (PropertyInfo[], PropertyInfo[])>();

        /// <summary>
        /// Returns the names of properties on T that Azure Table Storage cannot
        /// represent and which are therefore silently excluded from every write
        /// operation (Insert/Upsert/Replace/Merge). These properties are NOT
        /// persisted — after a round trip through Retrieve, they'll hold their
        /// type's default value, not whatever was set before the write.
        /// </summary>
        public static IReadOnlyList<string> GetUnsupportedProperties<T>() where T : IAzureTableEntity
        {
            var (_, skipped) = EntityPropertyMapCache.GetOrAdd(typeof(T), BuildPropertyMap);
            return skipped.Select(p => p.Name).ToList();
        }

        private static (PropertyInfo[] Supported, PropertyInfo[] Skipped) BuildPropertyMap(Type type)
        {
            var allProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && p.CanWrite && !IsSystemProperty(p.Name))
                .ToArray();

            var supported = allProperties.Where(p => EdmTypeMap.IsSupportedClrType(p.PropertyType)).ToArray();
            var skipped = allProperties.Except(supported).ToArray();

            return (supported, skipped);
        }

        private static bool IsSystemProperty(string name) => 
                name == nameof(ITableEntity.PartitionKey) || name == nameof(ITableEntity.RowKey) ||
                name == nameof(ITableEntity.Timestamp) || name == nameof(ITableEntity.ETag);

        /// <summary>
        /// Builds the actual wire-format entity for T, including only properties
        /// whose CLR type Table Storage supports. Properties like a List&lt;string&gt;
        /// (e.g. sCredential.Permissions) are silently omitted here — this is what
        /// stops them from ever reaching Azure and triggering a 400 InvalidInput.
        /// </summary>
        private static TableEntity ToSdkEntity<T>(T entity) where T : IAzureTableEntity
        {
            var (supported, _) = EntityPropertyMapCache.GetOrAdd(typeof(T), BuildPropertyMap);

            TableEntity sdkEntity = new TableEntity(entity.PartitionKey, entity.RowKey);
            foreach (var property in supported)
            {
                sdkEntity[property.Name] = property.GetValue(entity);
            }
            return sdkEntity;
        }
        #endregion
    }
}