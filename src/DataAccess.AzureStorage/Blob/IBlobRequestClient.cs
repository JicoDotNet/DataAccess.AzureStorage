using System.IO;

namespace DataAccess.AzureStorage.Blob
{
    public interface IBlobRequestClient
    {
        Stream FileStream { get; }
        string FileName { get; }
        string ContentType { get; set; }
        string[] directories { get; set; }
    }
}
