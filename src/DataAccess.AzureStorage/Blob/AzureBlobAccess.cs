using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using DataAccess.AzureStorage.Table;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.AzureStorage.Blob
{
    public sealed class AzureBlobAccess : AzureBlobManager
    {
        public string ContainerName { get; private set; }

        private BlobClient _blobClient;

        public AzureBlobAccess(string connectionString)
            : base(connectionString)
        {
        }

        public AzureBlobAccess(string containerName, string connectionString)
            : base(connectionString)
        {
            ContainerName = containerName;
            blobContainerClient = serviceClient.GetBlobContainerClient(containerName);
            _ = CreateContainerAsync();
        }
        public void SetContainer(string containerName)
        {
            ContainerName = containerName;
            blobContainerClient = serviceClient.GetBlobContainerClient(containerName);
            _ = CreateContainerAsync();
        }

        public BlobResponseClient Upload(BlobRequestClient blobUpload)
        {
            try
            {
                if (blobUpload == null)
                {
                    throw new ArgumentNullException(nameof(blobUpload), "blobUpload should not be null.");
                }
                string Path = string.Empty;
                string fullPath = string.Empty;
                Path += DirectoriesPath(blobUpload.directories);
                if (!string.IsNullOrEmpty(Path))
                {
                    fullPath = $"{Path}/{SanitizedName(blobUpload.FileName)}";
                }
                else
                {
                    fullPath = SanitizedName(blobUpload.FileName);
                }
                _blobClient = blobContainerClient.GetBlobClient(fullPath);
                _blobClient.Upload(blobUpload.FileStream, new BlobHttpHeaders { ContentType = blobUpload.ContentType });

                return new BlobResponseClient
                {
                    Uri = _blobClient.Uri,
                    AccountName = _blobClient.AccountName,
                    AbsolutePath = _blobClient.Uri.AbsolutePath,
                    ContainerName = _blobClient.BlobContainerName,
                    Path = _blobClient.Uri.ToString(),
                };
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public List<BlobDetails> BlobDetails()
        {
            return BlobDetails(null);
        }

        public List<BlobDetails> BlobDetails(string[] directories)
        {
            try
            {
                List<BlobDetails> blobDetailsList = new List<BlobDetails>();

                string prefix = directories != null && directories.Length > 0 ? $"{DirectoriesPath(directories)}/" : "/";

                foreach (var blobItem in blobContainerClient.GetBlobs(prefix: prefix))
                {
                    _blobClient = blobContainerClient.GetBlobClient(blobItem.Name);

                    BlobProperties properties = _blobClient.GetProperties();

                    BlobDetails blobDetails = new BlobDetails
                    {
                        Path = _blobClient.Uri.ToString(),
                        ContentLength = properties.ContentLength,
                        ContentType = properties.ContentType,
                        LastModified = properties.LastModified,
                        AbsoluteUri = _blobClient.Uri.AbsoluteUri,
                    };

                    blobDetailsList.Add(blobDetails);
                }
                return blobDetailsList;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public void Delete(string blobUrl)
        {
            try
            {
                Uri blobUri = new Uri(blobUrl);
                string containerName = blobUri.Segments[1].TrimEnd('/');
                string blobName = string.Join("", blobUri.Segments, 2, blobUri.Segments.Length - 2);

                SetContainer(containerName);
                _blobClient = blobContainerClient.GetBlobClient(blobName);
                _blobClient.DeleteIfExists();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public byte[] DownloadFile(string blobUrl)
        {
            try
            {
                Uri blobUri = new Uri(blobUrl);
                string containerName = blobUri.Segments[1].TrimEnd('/');
                string blobName = string.Join("", blobUri.Segments, 2, blobUri.Segments.Length - 2);
                SetContainer(containerName);
                _blobClient = blobContainerClient.GetBlobClient(blobName);
                using (MemoryStream ms = new MemoryStream())
                {
                    _blobClient.DownloadTo(ms);
                    return ms.ToArray();
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        private string? DirectoriesPath(string[] directory)
        {
            if (directory != null && directory.Length > 0)
            {
                var paths = "";
                foreach (string directoryPath in directory)
                {
                    string sanitizeDirectoryPath = SanitizedName(directoryPath);
                    if (!string.IsNullOrEmpty(sanitizeDirectoryPath))
                    {
                        paths += sanitizeDirectoryPath + "/";
                    }                    
                }
                paths = (paths.Length > 1 || paths.Contains("/")) ? paths.Remove(paths.Length - 1) : paths;
                return string.IsNullOrEmpty(paths) ? null : paths;
            }
            return null;
        }

        private string SanitizedName(string directoryOrFileName)
        {
            var invalidChars = new string[] { "\\", "?", "#", "[", "]" };
            foreach (var invalidChar in invalidChars)
            {
                directoryOrFileName = directoryOrFileName.Replace(invalidChar, "");
            }
            directoryOrFileName = directoryOrFileName.Trim('/');

            return directoryOrFileName;
        }
    }
}
