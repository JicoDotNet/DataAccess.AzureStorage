using Azure;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DataAccess.AzureStorage.Table
{
    public interface IAzureTableAccess
    {
        string TableName { get; }
        void SetTableName(string tableName);

        T InsertEntity<T>(T entity) where T : AzureTableEntity;
        Task<T> InsertEntityAsync<T>(T entity, CancellationToken cancellationToken = default) where T : AzureTableEntity;

        T UpsertEntity<T>(T entity) where T : AzureTableEntity;
        Task<T> UpsertEntityAsync<T>(T entity, CancellationToken cancellationToken = default) where T : AzureTableEntity;

        T ReplaceEntity<T>(T entity, ETag? ifMatch = null) where T : AzureTableEntity;
        Task<T> ReplaceEntityAsync<T>(T entity, ETag? ifMatch = null, CancellationToken cancellationToken = default) where T : AzureTableEntity;

        T MergeEntity<T>(T entity, ETag? ifMatch = null) where T : AzureTableEntity;
        Task<T> MergeEntityAsync<T>(T entity, ETag? ifMatch = null, CancellationToken cancellationToken = default) where T : AzureTableEntity;

        bool DeleteEntity<T>(T entity, ETag? ifMatch = null) where T : AzureTableEntity;
        Task<bool> DeleteEntityAsync<T>(T entity, ETag? ifMatch = null, CancellationToken cancellationToken = default) where T : AzureTableEntity;
        bool DeleteEntity(string partitionKey, string rowKey, ETag? ifMatch = null);
        Task<bool> DeleteEntityAsync(string partitionKey, string rowKey, ETag? ifMatch = null, CancellationToken cancellationToken = default);

        List<T> RetrieveEntities<T>(string filter = null) where T : AzureTableEntity;
        Task<List<T>> RetrieveEntitiesAsync<T>(string filter = null, CancellationToken cancellationToken = default) where T : AzureTableEntity;
        T RetrieveEntity<T>(string filter) where T : AzureTableEntity;
        Task<T> RetrieveEntityAsync<T>(string filter, CancellationToken cancellationToken = default) where T : AzureTableEntity;

        DynamicTableEntity InsertEntity(DynamicTableEntity entity);
        Task<DynamicTableEntity> InsertEntityAsync(DynamicTableEntity entity, CancellationToken cancellationToken = default);
        List<DynamicTableEntity> RetrieveEntities(string filter);
        Task<List<DynamicTableEntity>> RetrieveEntitiesAsync(string filter, CancellationToken cancellationToken = default);
        DynamicTableEntity RetrieveEntity(string filter);
        Task<DynamicTableEntity> RetrieveEntityAsync(string filter, CancellationToken cancellationToken = default);
        DynamicTableEntity UpsertEntity(DynamicTableEntity entity);
        Task<DynamicTableEntity> UpsertEntityAsync(DynamicTableEntity entity, CancellationToken cancellationToken = default);

        DynamicTableEntity ReplaceEntity(DynamicTableEntity entity, ETag? ifMatch = null);
        Task<DynamicTableEntity> ReplaceEntityAsync(DynamicTableEntity entity, ETag? ifMatch = null, CancellationToken cancellationToken = default);

        DynamicTableEntity MergeEntity(DynamicTableEntity entity, ETag? ifMatch = null);
        Task<DynamicTableEntity> MergeEntityAsync(DynamicTableEntity entity, ETag? ifMatch = null, CancellationToken cancellationToken = default);

        bool DeleteEntity(DynamicTableEntity entity, ETag? ifMatch = null);
        Task<bool> DeleteEntityAsync(DynamicTableEntity entity, ETag? ifMatch = null, CancellationToken cancellationToken = default);
    }
}