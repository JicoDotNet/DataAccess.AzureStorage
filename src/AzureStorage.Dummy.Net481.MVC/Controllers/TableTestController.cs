using AzureStorage.Dummy.Net481.MVC.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.ModelBinding;
using System.Web.Mvc;
using DataAccess.AzureStorage.Table;
using System.Web.Configuration;

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

            return View();
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
    }
}