using Azure;
using System;

namespace DataAccess.AzureStorage.Blob
{
    public class BlobDetails : IBlobDetails
    {
        public string Path { get; set; }
        public string AbsoluteUri { get; set; }
        public long ContentLength { get; set; }
        public string ContentType { get; set; }
        public DateTimeOffset LastModified { get; set; }
        public ETag ETag { get; set; }
    }
}
