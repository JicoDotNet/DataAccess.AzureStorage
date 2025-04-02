using DataAccess.AzureStorage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace AzureStorage.Dummy.Net481.MVC.Models
{
    public class TableTestModels : TableEntity
    {
        public int? Age { get; set; }
        public string Name { get; set; }
    }
}