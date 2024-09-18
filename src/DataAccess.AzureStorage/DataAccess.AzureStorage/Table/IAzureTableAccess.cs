using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.AzureStorage.Table
{
    public interface IAzureTableAccess
    {
        string TableName { get; }
        void SetTableName(string tableName);
        T InsertEntity<T>(T entity) where T : TableEntity;
        T ReplaceEntity<T>(T entity) where T : TableEntity;
        T MergeEntity<T>(T entity) where T : TableEntity;
        T DeleteEntity<T>(T entity) where T : TableEntity;
        bool DeleteEntity(string partitionKey, string rowKey);
        List<T> QueryEntities<T>(string query) where T : TableEntity;
        T QueryEntity<T>(string query) where T : TableEntity;
    }
}
