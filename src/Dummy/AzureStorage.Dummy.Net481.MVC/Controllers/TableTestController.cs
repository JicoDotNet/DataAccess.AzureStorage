using AzureStorage.Dummy.Net481.MVC.Models;
using System;
using System.Collections.Generic;
using System.Web.Mvc;
using DataAccess.AzureStorage.Table;
using System.Text.RegularExpressions;
using System.Linq;

namespace AzureStorage.Dummy.Net481.MVC.Controllers
{
    public class TableTestController : Controller
    {
        string ConnectionString = Environment.GetEnvironmentVariable("AzureStorageConnection");

        public ActionResult Add()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Add(TableTestModels models)
        {
            AzureTableAccess tableAccess = new AzureTableAccess("AmarTable", ConnectionString);
            tableAccess.InsertEntity(models);
            return RedirectToAction("Add");
        }

        public ActionResult Show()
        {
            AzureTableAccess tableAccess = new AzureTableAccess("AmarTable", ConnectionString);
            List<TableTestModels> models = tableAccess.RetrieveEntities<TableTestModels>();
            return View(models);
        }

        public ActionResult Details(string id)
        {
            AzureTableAccess tableAccess = new AzureTableAccess("AmarTable", ConnectionString);
            List<TableTestModels> models = tableAccess.RetrieveEntities<TableTestModels>("RowKey eq '" + id + "'");
            return View("Show", models);
        }

        public ActionResult Delete(string id)
        {
            AzureTableAccess tableAccess = new AzureTableAccess("AmarTable", ConnectionString);
            TableTestModels model = tableAccess.RetrieveEntity<TableTestModels>("RowKey eq '" + id + "'");
            tableAccess.DeleteEntity(model);
            return RedirectToAction("Show");
        }
        public ActionResult Update(string id)
        {
            AzureTableAccess tableAccess = new AzureTableAccess("AmarTable", ConnectionString);
            TableTestModels model = tableAccess.RetrieveEntity<TableTestModels>("RowKey eq '" + id + "'");
            model.Name = model.Name + Guid.NewGuid().ToString();
            tableAccess.UpdateEntity(model);
            return RedirectToAction("Show");
        }
        public ActionResult Replace(string id)
        {
            AzureTableAccess tableAccess = new AzureTableAccess("AmarTable", ConnectionString);
            TableTestModels model = tableAccess.RetrieveEntity<TableTestModels>("RowKey eq '" + id + "'");
            model.Age = null;
            tableAccess.ReplaceEntity(model);
            return RedirectToAction("Show");
        }
        public ActionResult Merge(string id)
        {
            AzureTableAccess tableAccess = new AzureTableAccess("AmarTable", ConnectionString);
            TableTestModels model = tableAccess.RetrieveEntity<TableTestModels>("RowKey eq '" + id + "'");
            model.Name = model.Name + Guid.NewGuid().ToString();
            model.Age = null;
            tableAccess.MergeEntity(model);
            return RedirectToAction("Show");
        }

        public ActionResult CustomMaster()
        {
            AzureTableAccess tableAccess = new AzureTableAccess("AmarTable", ConnectionString);
            List<CustomPropertyMaster> models = tableAccess.RetrieveEntities<CustomPropertyMaster>("PartitionKey eq 'AmarPropertyMaster'");
            return View(models);
        }
        public ActionResult CustomMasterAdd()
        {
            AzureTableAccess tableAccess = new AzureTableAccess("AmarTable", ConnectionString);
            string Rk = Regex.Replace( Guid.NewGuid().ToString(), @"[^a-zA-Z0-9]", "");
            Random random = new Random();
            Array values = Enum.GetValues(typeof(EdmType));
            EdmType edmType = (EdmType)values.GetValue(random.Next(values.Length));
            int rnd = random.Next(1111, 9999);
            CustomPropertyMaster customPropertyMaster = new CustomPropertyMaster()
            {
                PartitionKey = "AmarPropertyMaster",
                RowKey = Rk,
                LabelName = "Label & Name " + rnd + " #:",
                DataType = edmType.ToString(),
                DefaultValue = (rnd % 2) == 0 ? "Default" + rnd : null,
                IsRequired = (rnd % 2) == 0 ? true : false
            };
            customPropertyMaster.ColumnName =
                 Regex.Replace(customPropertyMaster.LabelName, @"[^a-zA-Z0-9]", "") + "_" + customPropertyMaster.RowKey;

            tableAccess.InsertEntity(customPropertyMaster);

            return RedirectToAction("CustomMaster");
        }

        public ActionResult CustomProperty()
        {
            AzureTableAccess tableAccess = new AzureTableAccess("AmarTable", ConnectionString);
            List<CustomPropertyMaster> models = tableAccess.RetrieveEntities<CustomPropertyMaster>("PartitionKey eq 'AmarPropertyMaster'");
            return View(models);
        }

        [HttpPost]
        public ActionResult CustomPropertySet(FormCollection form)
        {
            AzureTableAccess tableAccess = new AzureTableAccess("AmarTable", ConnectionString);
            IDictionary<string, object> formDictionary =
                form.AllKeys.ToDictionary(key => key, value => (object)form[value]);

            List<CustomPropertyMaster> customProperties = tableAccess.RetrieveEntities<CustomPropertyMaster>("PartitionKey eq 'AmarPropertyMaster'");
            
            IDictionary<string, object> customPropertiesValue = new Dictionary<string, object>();

            foreach (CustomPropertyMaster dm in customProperties)
            {
                if (dm.DataType == EdmType.String.ToString())
                {
                    if (formDictionary[dm.ColumnName] != null
                                && !string.IsNullOrEmpty(formDictionary[dm.ColumnName]?.ToString()))
                    {
                        customPropertiesValue.Add(dm.ColumnName, formDictionary[dm.ColumnName].ToString());
                    }
                }
                if (dm.DataType == EdmType.Double.ToString())
                {
                    try
                    {
                        customPropertiesValue.Add(dm.ColumnName, Convert.ToDouble(formDictionary[dm.ColumnName]));
                    }
                    catch { }
                }
                if (dm.DataType == EdmType.Int32.ToString())
                {
                    try
                    {
                        customPropertiesValue.Add(dm.ColumnName, Convert.ToInt32(formDictionary[dm.ColumnName]));
                    }
                    catch { }
                }
                if (dm.DataType == EdmType.Int64.ToString())
                {
                    try
                    {
                        customPropertiesValue.Add(dm.ColumnName, Convert.ToInt64(formDictionary[dm.ColumnName]));
                    }
                    catch { }
                }
                if (dm.DataType == EdmType.DateTime.ToString())
                {
                    try
                    {
                        if (formDictionary[dm.ColumnName] != null
                            && !string.IsNullOrEmpty(formDictionary[dm.ColumnName]?.ToString()))
                        {
                            DateTime.TryParseExact(formDictionary[dm.ColumnName]?.ToString(),
                                                    "dd/MM/yyyy",
                                                    System.Globalization.CultureInfo.InvariantCulture,
                                                    System.Globalization.DateTimeStyles.None,
                                                    out DateTime PropDateValue);

                            PropDateValue = DateTime.SpecifyKind(PropDateValue, DateTimeKind.Utc);
                            customPropertiesValue.Add(dm.ColumnName, PropDateValue);
                        }
                    }
                    catch { }
                }
            }

            DynamicTableEntity dynamicProperty = new DynamicTableEntity();
            dynamicProperty.PartitionKey = "AmarCustomPropertyData";
            dynamicProperty.RowKey = Guid.NewGuid().ToString();
            dynamicProperty.Set(customPropertiesValue);

            tableAccess.InsertEntity(dynamicProperty);

            return RedirectToAction("CustomPropertyGet");
        }

        public ActionResult CustomPropertyGetAll()
        {
            AzureTableAccess tableAccess = new AzureTableAccess("AmarTable", ConnectionString);
            List<DynamicTableEntity> dynamicTableEntities  = tableAccess.RetrieveEntities("PartitionKey eq 'AmarCustomPropertyData'");
            return View(dynamicTableEntities);
        }
    }
}