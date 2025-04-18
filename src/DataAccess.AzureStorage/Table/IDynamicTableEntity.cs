using Azure.Data.Tables;
using System.Collections.Generic;

namespace DataAccess.AzureStorage.Table
{
    public interface IDynamicTableEntity : ITableEntity
    {
        IDictionary<string, object> Properties { get; }
        void Set(IDictionary<string, object> properties);
    }
}
