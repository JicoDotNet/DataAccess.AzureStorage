using System.Collections.Generic;

namespace DataAccess.AzureStorage.Blob
{
    public interface IAzureBlobAccess
    {
        string ContainerName { get; }
        void SetContainer(string containerName);
        IBlobResponseClient Upload(IBlobRequestClient blobRequestToUpload);
        List<IBlobDetails> BlobDetails();
        List<IBlobDetails> BlobDetails(string[] directories);
        void Delete(string blobUrl);
        (byte[] fileContent, string contentType) DownloadFile(string blobUrl);
    }
}
