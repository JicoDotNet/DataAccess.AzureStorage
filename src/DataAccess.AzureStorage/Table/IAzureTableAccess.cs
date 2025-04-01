using System.Collections.Generic;

namespace DataAccess.AzureStorage.Table
{
    public interface IAzureTableAccess
    {
        string TableName { get; }
        void SetTableName(string tableName);
        T InsertEntity<T>(T entity) where T : TableEntity;
        DynamicTableEntity InsertEntity<T>(DynamicTableEntity entity) where T : IDynamicTableEntity;
        T ReplaceEntity<T>(T entity) where T : TableEntity;
        T UpdateEntity<T>(T entity) where T : TableEntity;
        T MergeEntity<T>(T entity) where T : TableEntity;
        bool DeleteEntity<T>(T entity) where T : TableEntity;
        bool DeleteEntity(string partitionKey, string rowKey);        
        List<T> RetrieveEntities<T>(string query) where T : TableEntity;
        T RetrieveEntity<T>(string query) where T : TableEntity;
        DynamicTableEntity RetrieveEntity(string query);
    }
}
