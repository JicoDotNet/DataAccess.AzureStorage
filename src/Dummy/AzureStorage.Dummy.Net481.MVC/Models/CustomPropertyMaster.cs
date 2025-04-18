using DataAccess.AzureStorage.Table;

namespace AzureStorage.Dummy.Net481.MVC.Models
{
    public class CustomPropertyMaster : TableEntity
    {
        public string LabelName { get; set; }

        public string ColumnName { get; set; }

        public string DataType { get; set; }

        public string DefaultValue { get; set; }

        public bool IsRequired { get; set; }
    }
}