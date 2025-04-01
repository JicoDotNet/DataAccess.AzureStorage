using Azure.Data.Tables;
using System.Collections.Generic;

namespace DataAccess.AzureStorage.Table
{
    public interface IDynamicTableEntity : ITableEntity
    {
        Dictionary<string, object> Properties { get; set; }
        void ReadEntity(IDictionary<string, object> properties);
        IDictionary<string, object> WriteEntity();
    }
}
