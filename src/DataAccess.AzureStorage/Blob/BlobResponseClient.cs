using System;

namespace DataAccess.AzureStorage.Blob
{
    public class BlobResponseClient : IBlobResponseClient
    {
        public Uri Uri { get; set; }
        public string ContainerName { get; set; }
        public string AbsolutePath { get; set; }
        public string AccountName { get; set; }
        public string Path { get; set; }
    }
}
