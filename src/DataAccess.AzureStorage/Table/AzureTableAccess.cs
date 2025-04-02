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
            CreateTable();
        }

        public void SetTableName(string tableName)
        {
            TableName = tableName;
            tableClient = serviceClient.GetTableClient(tableName);
            CreateTable();
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

        public T UpdateEntity<T>(T entity) where T : TableEntity
        {
            try
            {
                return ReplaceEntity(entity);
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

        public bool DeleteEntity<T>(T entity) where T : TableEntity
        {
            try
            {
                tableClient.DeleteEntity(entity);
                return true;
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

        public List<T> RetrieveEntities<T>(string query = null) where T : TableEntity
        {
            List<T> entities = new List<T>();
            try
            {                
                if (string.IsNullOrEmpty(query))
                {
                    foreach (T entity in tableClient.Query<T>())
                    {
                        entities.Add(entity);
                    }
                }
                else
                {                   
                    foreach (T entity in tableClient.Query<T>(query))
                    {
                        entities.Add(entity);
                    }
                }                
            }
            catch (Exception ex)
            {
                throw ex;
            }

            return entities;
        }

        public T RetrieveEntity<T>(string query) where T : TableEntity
        {
            List<T> tEntities = new List<T>();

            try
            {
                return RetrieveEntities<T>(query).FirstOrDefault();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public DynamicTableEntity InsertEntity<T>(DynamicTableEntity entity) where T : IDynamicTableEntity
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
        public DynamicTableEntity RetrieveEntity(string query)
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
