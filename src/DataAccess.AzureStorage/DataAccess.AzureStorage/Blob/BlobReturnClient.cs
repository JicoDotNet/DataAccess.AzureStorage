using Azure.Storage.Blobs.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataAccess.AzureStorage.Blob
{
    public class BlobResponseClient
    {
        public Uri Uri { get; set; }
        public string ContainerName { get; set; }
        public string AbsolutePath { get; set; }
        public string AccountName { get; set; }
        public string Path { get; set; }
    }
    public class BlobRequestClient : IBlobRequestClient
    {
        public BlobRequestClient(Stream fileStream, string fileNameWithExtension)
        {
            if(fileStream == null)
            {
                throw new ArgumentNullException(nameof(fileStream), "file Stream should not be null.");
            }
            if(string.IsNullOrEmpty( fileNameWithExtension ))
            {
                throw new ArgumentNullException(nameof (fileNameWithExtension), "file Name should not be null or empty.");
            }
            FileStream = fileStream;
            FileName = fileNameWithExtension;
        }

        public Stream FileStream { get; private set; }
        public string FileName { get; private set; }
        public string ContentType { get; set; }
        public string[] directories { get; set; }
    }

    public interface IBlobRequestClient
    {
        Stream FileStream { get; }
        string FileName { get; }
        string ContentType { get; set; }
        string[] directories { get; set; }
    }

    public class BlobDetails
    {
        public string? Path { get; set; }
        public string? AbsoluteUri { get; set; }
        public long ContentLength { get; set; }
        public string? ContentType { get; set; }
        public DateTimeOffset LastModified { get; set; }
        
    }
}
