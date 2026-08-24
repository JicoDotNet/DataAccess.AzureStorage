using Azure;
using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace DataAccess.AzureStorage.Table
{
    public sealed class AzureTableAccess : AzureTableManager, IAzureTableAccess
    {
        public AzureTableAccess(string connectionString) : base(connectionString){ }

        public AzureTableAccess(string tableName, string connectionString) : base(connectionString)
        {
            SetTableName(tableName);
        }

        public void SetTableName(string tableName) => SetTable(tableName);

        public T InsertEntity<T>(T entity) where T : TableEntity
        {
            EnsureTableReady();
            ValidateEntity(entity);
            try
            {
                TableClientInstance.AddEntity(entity);
                return entity;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Insert failed for PartitionKey='{entity.PartitionKey}', RowKey='{entity.RowKey}'.", ex);
            }
        }

        public async Task<T> InsertEntityAsync<T>(T entity, CancellationToken cancellationToken = default) where T : TableEntity
        {
            EnsureTableReady();
            ValidateEntity(entity);
            try
            {
                await TableClientInstance.AddEntityAsync(entity, cancellationToken).ConfigureAwait(false);
                return entity;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Insert failed for PartitionKey='{entity.PartitionKey}', RowKey='{entity.RowKey}'.", ex);
            }
        }

        public T UpsertEntity<T>(T entity) where T : TableEntity
        {
            EnsureTableReady();
            ValidateEntity(entity);
            try
            {
                TableClientInstance.UpsertEntity(entity, TableUpdateMode.Merge);
                return entity;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Upsert failed for PartitionKey='{entity.PartitionKey}', RowKey='{entity.RowKey}'.", ex);
            }
        }

        public async Task<T> UpsertEntityAsync<T>(T entity, CancellationToken cancellationToken = default) where T : TableEntity
        {
            EnsureTableReady();
            ValidateEntity(entity);
            try
            {
                await TableClientInstance.UpsertEntityAsync(entity, TableUpdateMode.Merge, cancellationToken).ConfigureAwait(false);
                return entity;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Upsert failed for PartitionKey='{entity.PartitionKey}', RowKey='{entity.RowKey}'.", ex);
            }
        }

        public T ReplaceEntity<T>(T entity, ETag? ifMatch = null) where T : TableEntity
        {
            EnsureTableReady();
            ValidateEntity(entity);
            try
            {
                TableClientInstance.UpdateEntity(entity, ResolveIfMatch(ifMatch, entity.ETag), TableUpdateMode.Replace);
                return entity;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Replace failed for PartitionKey='{entity.PartitionKey}', RowKey='{entity.RowKey}'.", ex);
            }
        }

        public async Task<T> ReplaceEntityAsync<T>(T entity, ETag? ifMatch = null, CancellationToken cancellationToken = default) where T : TableEntity
        {
            EnsureTableReady();
            ValidateEntity(entity);
            try
            {
                await TableClientInstance.UpdateEntityAsync(entity, ResolveIfMatch(ifMatch, entity.ETag), TableUpdateMode.Replace, cancellationToken).ConfigureAwait(false);
                return entity;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Replace failed for PartitionKey='{entity.PartitionKey}', RowKey='{entity.RowKey}'.", ex);
            }
        }

        public T MergeEntity<T>(T entity, ETag? ifMatch = null) where T : TableEntity
        {
            EnsureTableReady();
            ValidateEntity(entity);
            try
            {
                TableClientInstance.UpdateEntity(entity, ResolveIfMatch(ifMatch, entity.ETag), TableUpdateMode.Merge);
                return entity;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Merge failed for PartitionKey='{entity.PartitionKey}', RowKey='{entity.RowKey}'.", ex);
            }
        }

        public async Task<T> MergeEntityAsync<T>(T entity, ETag? ifMatch = null, CancellationToken cancellationToken = default) where T : TableEntity
        {
            EnsureTableReady();
            ValidateEntity(entity);
            try
            {
                await TableClientInstance.UpdateEntityAsync(entity, ResolveIfMatch(ifMatch, entity.ETag), TableUpdateMode.Merge, cancellationToken).ConfigureAwait(false);
                return entity;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Merge failed for PartitionKey='{entity.PartitionKey}', RowKey='{entity.RowKey}'.", ex);
            }
        }

        public bool DeleteEntity<T>(T entity, ETag? ifMatch = null) where T : TableEntity
        {
            EnsureTableReady();
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            return DeleteEntity(entity.PartitionKey, entity.RowKey, ifMatch ?? ResolveIfMatch(null, entity.ETag));
        }

        public async Task<bool> DeleteEntityAsync<T>(T entity, ETag? ifMatch = null, CancellationToken cancellationToken = default) where T : TableEntity
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

        public List<T> RetrieveEntities<T>(string filter = null) where T : TableEntity
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

        public async Task<List<T>> RetrieveEntitiesAsync<T>(string filter = null, CancellationToken cancellationToken = default) where T : TableEntity
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

        public T RetrieveEntity<T>(string filter) where T : TableEntity
            => RetrieveEntities<T>(filter).FirstOrDefault();

        public async Task<T> RetrieveEntityAsync<T>(string filter, CancellationToken cancellationToken = default) where T : TableEntity
            => (await RetrieveEntitiesAsync<T>(filter, cancellationToken).ConfigureAwait(false)).FirstOrDefault();


        // ---------------------------------------------------------------
        // DynamicTableEntity variants
        // ---------------------------------------------------------------
        public DynamicTableEntity InsertEntity(DynamicTableEntity entity)
        {
            EnsureTableReady();
            ValidateEntity(entity);
            try
            {
                var sdkEntity = ToSdkEntity(entity);
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
                var sdkEntity = ToSdkEntity(entity);
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
                var sdkEntity = ToSdkEntity(entity);
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
                var sdkEntity = ToSdkEntity(entity);
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
                var sdkEntity = ToSdkEntity(entity);
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
                var sdkEntity = ToSdkEntity(entity);
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
                var sdkEntity = ToSdkEntity(entity);
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
                var sdkEntity = ToSdkEntity(entity);
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
                var entities = TableClientInstance.Query<Azure.Data.Tables.TableEntity>(filter: filter).ToList();
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
                var results = new List<DynamicTableEntity>();
                await foreach (var entity in TableClientInstance.QueryAsync<Azure.Data.Tables.TableEntity>(filter: filter, cancellationToken: cancellationToken).ConfigureAwait(false))
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

        // ---------------------------------------------------------------
        // Internal helpers
        // ---------------------------------------------------------------

        private static void ValidateEntity<T>(T entity) where T : TableEntity
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            if (string.IsNullOrEmpty(entity.PartitionKey))
                throw new ArgumentException("Entity.PartitionKey must not be null or empty.", nameof(entity));

            if (string.IsNullOrEmpty(entity.RowKey))
                throw new ArgumentException("Entity.RowKey must not be null or empty.", nameof(entity));
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

        private static Azure.Data.Tables.TableEntity ToSdkEntity(DynamicTableEntity entity)
        {
            var tableEntity = new Azure.Data.Tables.TableEntity(entity.PartitionKey, entity.RowKey);
            foreach (var prop in entity.Properties)
            {
                tableEntity[prop.Key] = prop.Value;
            }
            return tableEntity;
        }

        private static DynamicTableEntity ToDynamicEntity(Azure.Data.Tables.TableEntity source)
        {
            var dynamicEntity = new DynamicTableEntity();
            dynamicEntity.SetEntity(source);
            return dynamicEntity;
        }
    }
}