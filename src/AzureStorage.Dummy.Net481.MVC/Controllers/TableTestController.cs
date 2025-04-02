using AzureStorage.Dummy.Net481.MVC.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.ModelBinding;
using System.Web.Mvc;
using DataAccess.AzureStorage.Table;
using System.Web.Configuration;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace AzureStorage.Dummy.Net481.MVC.Controllers
{
    public class TableTestController : Controller
    {
        public ActionResult Add()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Add(TableTestModels models)
        {
            AzureTableAccess tableAccess = new AzureTableAccess("AmarTable", WebConfigurationManager.ConnectionStrings["AzureStorageConnection"].ToString());
            tableAccess.InsertEntity(models);
            return RedirectToAction("Add");
        }

        public ActionResult Show()
        {
            AzureTableAccess tableAccess = new AzureTableAccess("AmarTable", WebConfigurationManager.ConnectionStrings["AzureStorageConnection"].ToString());
            List<TableTestModels> models = tableAccess.RetrieveEntities<TableTestModels>();
            return View(models);
        }

        public ActionResult Details(string id)
        {
            AzureTableAccess tableAccess = new AzureTableAccess("AmarTable", WebConfigurationManager.ConnectionStrings["AzureStorageConnection"].ToString());
            List<TableTestModels> models = tableAccess.RetrieveEntities<TableTestModels>("RowKey eq '" + id + "'");
            return View("Show", models);
        }

        public ActionResult Delete(string id)
        {
            AzureTableAccess tableAccess = new AzureTableAccess("AmarTable", WebConfigurationManager.ConnectionStrings["AzureStorageConnection"].ToString());
            TableTestModels model = tableAccess.RetrieveEntity<TableTestModels>("RowKey eq '" + id + "'");
            tableAccess.DeleteEntity(model);
            return RedirectToAction("Show");
        }
        public ActionResult Update(string id)
        {
            AzureTableAccess tableAccess = new AzureTableAccess("AmarTable", WebConfigurationManager.ConnectionStrings["AzureStorageConnection"].ToString());
            TableTestModels model = tableAccess.RetrieveEntity<TableTestModels>("RowKey eq '" + id + "'");
            model.Name = model.Name + Guid.NewGuid().ToString();
            tableAccess.UpdateEntity(model);
            return RedirectToAction("Show");
        }
        public ActionResult Replace(string id)
        {
            AzureTableAccess tableAccess = new AzureTableAccess("AmarTable", WebConfigurationManager.ConnectionStrings["AzureStorageConnection"].ToString());
            TableTestModels model = tableAccess.RetrieveEntity<TableTestModels>("RowKey eq '" + id + "'");
            model.Age = null;
            tableAccess.ReplaceEntity(model);
            return RedirectToAction("Show");
        }
        public ActionResult Merge(string id)
        {
            AzureTableAccess tableAccess = new AzureTableAccess("AmarTable", WebConfigurationManager.ConnectionStrings["AzureStorageConnection"].ToString());
            TableTestModels model = tableAccess.RetrieveEntity<TableTestModels>("RowKey eq '" + id + "'");
            model.Name = model.Name + Guid.NewGuid().ToString();
            model.Age = null;
            tableAccess.MergeEntity(model);
            return RedirectToAction("Show");
        }

        public ActionResult CustomMaster()
        {
            AzureTableAccess tableAccess = new AzureTableAccess("AmarTable", WebConfigurationManager.ConnectionStrings["AzureStorageConnection"].ToString());
            List<CustomPropertyMaster> models = tableAccess.RetrieveEntities<CustomPropertyMaster>("PartitionKey eq 'AmarProperty'");
            return View(models);
        }
        public ActionResult CustomMasterAdd()
        {
            AzureTableAccess tableAccess = new AzureTableAccess("AmarTable", WebConfigurationManager.ConnectionStrings["AzureStorageConnection"].ToString());
            string Rk = Guid.NewGuid().ToString();
            Random random = new Random();
            Array values = Enum.GetValues(typeof(EdmType));
            EdmType edmType = (EdmType)values.GetValue(random.Next(values.Length));
            int rnd = random.Next(1111, 9999);
            CustomPropertyMaster customPropertyMaster = new CustomPropertyMaster()
            {
                PartitionKey = "AmarProperty",
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
            AzureTableAccess tableAccess = new AzureTableAccess("AmarTable", WebConfigurationManager.ConnectionStrings["AzureStorageConnection"].ToString());
            List<CustomPropertyMaster> models = tableAccess.RetrieveEntities<CustomPropertyMaster>("PartitionKey eq 'AmarProperty'");
            return View(models);
        }
    }
}