using System;

namespace DataAccess.AzureStorage.Blob
{
    public interface IBlobDetails
    {
        string? Path { get; set; }
        string? AbsoluteUri { get; set; }
        long ContentLength { get; set; }
        string? ContentType { get; set; }
        DateTimeOffset LastModified { get; set; }
    }
}
