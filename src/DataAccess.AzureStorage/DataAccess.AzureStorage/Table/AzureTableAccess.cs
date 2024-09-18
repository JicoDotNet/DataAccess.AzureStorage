using Azure;
using Azure.Data.Tables;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DataAccess.AzureStorage.Table
{
    public sealed class AzureTableAccess : AzureTableManager, IAzureTableAccess 
    {
        public string TableName { get; private set; }
        public AzureTableAccess(string tableName, string connectionString)
            : base(connectionString)
        {
            TableName = tableName;
            tableClient = serviceClient.GetTableClient(tableName);
            _ = CreateTableAsync();
        }

        public void SetTableName(string tableName)
        {
            TableName = tableName;
            tableClient = serviceClient.GetTableClient(tableName);
            _ = CreateTableAsync();
        }

        public T InsertEntity<T>(T entity) where T : TableEntity
        {
            try
            {
                tableClient.UpsertEntity(entity, TableUpdateMode.Merge);
                return entity;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public DynamicTableEntity InsertEntity<T>(DynamicTableEntity entity) where T : ITableEntity
        {
            try
            {
                tableClient.UpsertEntity(entity, TableUpdateMode.Merge);
                return entity;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public T ReplaceEntity<T>(T entity) where T : TableEntity
        {
            try
            {
                tableClient.UpdateEntity(entity, ETag.All, TableUpdateMode.Replace);
                return entity;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public T MergeEntity<T>(T entity) where T : TableEntity
        {
            try
            {                
                tableClient.UpdateEntity(entity, ETag.All, TableUpdateMode.Merge);
                return entity;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public T DeleteEntity<T>(T entity) where T : TableEntity
        {
            try
            {
                tableClient.DeleteEntity(entity);
                return entity;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public bool DeleteEntity(string partitionKey, string rowKey)
        {
            try
            {
                tableClient.DeleteEntity(partitionKey, rowKey);
                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public List<T> QueryEntities<T>(string query) where T : TableEntity
        {
            List<T> tEntities = new List<T>();

            try
            {
                Pageable<T> queryResults = tableClient.Query<T>(query, 5000);

                foreach (var entity in queryResults)
                {
                    tEntities.Add(entity);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return tEntities;
        }

        public T QueryEntity<T>(string query) where T : TableEntity
        {
            List<T> tEntities = new List<T>();

            try
            {
                return tableClient.Query<T>(filter: query).FirstOrDefault();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

       public DynamicTableEntity QueryEntity(string query)
        {
            try
            {
                return tableClient.Query<DynamicTableEntity>(filter: query).FirstOrDefault();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
    }
}
