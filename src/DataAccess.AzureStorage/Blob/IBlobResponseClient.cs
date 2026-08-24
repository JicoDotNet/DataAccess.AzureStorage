using Azure;
using System;

namespace DataAccess.AzureStorage.Blob
{
    public  interface IBlobResponseClient
    {
        Uri Uri { get; set; }
        string ContainerName { get; set; }
        string AbsolutePath { get; set; }
        string AccountName { get; set; }
        string Path { get; set; }
        ETag ETag { get; set; }
    }
}
