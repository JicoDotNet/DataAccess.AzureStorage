using DataAccess.AzureStorage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace AzureStorage.Dummy.Net481.MVC.Controllers
{
    public class BlobTestController : Controller
    {
        string ConnectionString = Environment.GetEnvironmentVariable("AzureStorageConnection");
        public ActionResult Add()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Add(string FolderName)
        {
            var uploadedFile = Request.Files["AddFile"];
            if (uploadedFile != null && uploadedFile.ContentLength > 0)
            {

                Stream fileStream = uploadedFile.InputStream;
                IBlobRequestClient blobRequestClient = new BlobRequestClient(fileStream, "file123"+ Path.GetExtension(uploadedFile.FileName));
                blobRequestClient.Directories = new string[] { "Folder1", FolderName };
                blobRequestClient.ContentType= uploadedFile.ContentType;

                AzureBlobAccess azureBlobAccess = new AzureBlobAccess("AmarBlob", ConnectionString);
                IBlobResponseClient blobResponseClient = azureBlobAccess.Upload(blobRequestClient);
            }
            return RedirectToAction("Show");
        }

        public ActionResult Show()
        {
            AzureBlobAccess azureBlobAccess = new AzureBlobAccess("AmarBlob", ConnectionString);
            List<IBlobDetails> blobs = azureBlobAccess.BlobDetails(new string[] { "Folder1", "CC" });
            return View(blobs);
        }

        public ActionResult Download (string path)
        {
            AzureBlobAccess azureBlobAccess = new AzureBlobAccess("AmarBlob", ConnectionString);
            var result = azureBlobAccess.DownloadFile(path);
            string fileName = Path.GetFileName(path);
            return File(result.fileContent, result.contentType, fileName);
        }
    }
}