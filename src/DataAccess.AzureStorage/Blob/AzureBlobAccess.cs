using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace DataAccess.AzureStorage.Blob
{
    public sealed class AzureBlobAccess : AzureBlobManager
    {
        private BlobClient _blobClient;

        public AzureBlobAccess(string connectionString)
            : base(connectionString)
        {
        }

        public AzureBlobAccess(string containerName, string connectionString)
            : base(connectionString)
        {
            SetContainer(containerName);
        }
        public void SetContainer(string containerName)
        {
            if (IsValidContainerName(containerName))
            {
                ContainerName = containerName;
                _blobContainerClient = serviceClient.GetBlobContainerClient(containerName);
                _ = CreateContainerAsync();
            }           
        }

        public IBlobResponseClient Upload(IBlobRequestClient blobRequestToUpload)
        {
            try
            {
                if (blobRequestToUpload == null)
                {
                    throw new ArgumentNullException(nameof(blobRequestToUpload), "blobRequestToUpload should not be null.");
                }
                string Path = string.Empty;
                string fullPath = string.Empty;
                Path += DirectoriesPath(blobRequestToUpload.directories);
                if (!string.IsNullOrEmpty(Path))
                {
                    fullPath = $"{Path}/{SanitizedName(blobRequestToUpload.FileName)}";
                }
                else
                {
                    fullPath = SanitizedName(blobRequestToUpload.FileName);
                }
                _blobClient = _blobContainerClient.GetBlobClient(fullPath);
                _blobClient.Upload(blobRequestToUpload.FileStream, new BlobHttpHeaders { ContentType = blobRequestToUpload.ContentType });

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

        public List<IBlobDetails> BlobDetails()
        {
            return BlobDetails(null);
        }

        public List<IBlobDetails> BlobDetails(string[] directories)
        {
            try
            {
                List<IBlobDetails> blobDetailsList = new List<IBlobDetails>();

                string prefix = directories != null && directories.Length > 0 ? $"{DirectoriesPath(directories)}/" : "/";

                foreach (var blobItem in _blobContainerClient.GetBlobs(prefix: prefix))
                {
                    _blobClient = _blobContainerClient.GetBlobClient(blobItem.Name);

                    BlobProperties properties = _blobClient.GetProperties();

                    IBlobDetails blobDetails = new BlobDetails
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
                _blobClient = _blobContainerClient.GetBlobClient(blobName);
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
                _blobClient = _blobContainerClient.GetBlobClient(blobName);
                using (MemoryStream ms = new MemoryStream())
                {
                    _blobClient.DownloadTo(ms);
                    return ms.ToArray();
                }
            }
            catch (Exception ex)
            {
                throw ex;
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
