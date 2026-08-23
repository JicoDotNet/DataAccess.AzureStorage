using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DataAccess.AzureStorage.Blob
{
    public interface IAzureBlobAccess
    {
        string ContainerName { get; }
        void SetContainer(string containerName);
        IBlobResponseClient Upload(IBlobRequestClient blobRequestToUpload, bool overwrite = true);
        Task<IBlobResponseClient> UploadAsync(IBlobRequestClient blobRequestToUpload, bool overwrite = true, CancellationToken cancellationToken = default);

        List<IBlobDetails> BlobDetails();
        List<IBlobDetails> BlobDetails(string[] directories);
        Task<List<IBlobDetails>> BlobDetailsAsync(CancellationToken cancellationToken = default);
        Task<List<IBlobDetails>> BlobDetailsAsync(string[] directories, CancellationToken cancellationToken = default);

        bool Exists(string blobName);
        Task<bool> ExistsAsync(string blobName, CancellationToken cancellationToken = default);

        bool Delete(string blobName);
        Task<bool> DeleteAsync(string blobName, CancellationToken cancellationToken = default);

        bool DeleteByUrl(string blobUrl);
        Task<bool> DeleteByUrlAsync(string blobUrl, CancellationToken cancellationToken = default);

        (byte[] fileContent, string contentType) DownloadFile(string blobName);
        Task<(byte[] fileContent, string contentType)> DownloadFileAsync(string blobName, CancellationToken cancellationToken = default);

        (byte[] fileContent, string contentType) DownloadFileByUrl(string blobUrl);
        Task<(byte[] fileContent, string contentType)> DownloadFileByUrlAsync(string blobUrl, CancellationToken cancellationToken = default);
    }
}
