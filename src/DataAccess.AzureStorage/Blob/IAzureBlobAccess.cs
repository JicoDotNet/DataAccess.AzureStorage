using Azure;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace DataAccess.AzureStorage.Blob
{
    public interface IAzureBlobAccess
    {
        /// <summary>
        /// The name of the container currently selected on this instance, or
        /// null if <see cref="SetContainer"/> hasn't been called yet.
        /// Example value: "customer-photos".
        /// </summary>
        string ContainerName { get; }

        /// <summary>
        /// Selects (and creates, if it doesn't already exist) the container this
        /// instance operates against. Safe to call repeatedly — if the name
        /// hasn't changed since the last call, no network request is made.
        /// </summary>
        /// <param name="containerName">
        /// The container name, e.g. "customer-photos". Must be 3–63 characters,
        /// lowercase letters/numbers/single-hyphens only, and start and end with
        /// a letter or number (Azure's own container naming rule). Casing is
        /// normalized to lowercase automatically, so "Customer-Photos" is
        /// accepted and stored as "customer-photos".
        /// </param>
        void SetContainer(string containerName);

        /// <summary>
        /// Uploads a file to the currently selected container, at a path built
        /// from the request's <c>Directories</c> and <c>FileName</c>.
        /// </summary>
        /// <param name="blobRequestToUpload">
        /// The file to upload, e.g. a <c>BlobRequestClient</c> built from a
        /// <c>FileStream</c> with <c>FileName = "invoice-1042.pdf"</c>,
        /// <c>ContentType = "application/pdf"</c>, and
        /// <c>Directories = new[] { "invoices", "2026" }</c> (which uploads to
        /// blob path "invoices/2026/invoice-1042.pdf").
        /// </param>
        /// <param name="overwrite">
        /// <c>true</c> (default) replaces an existing blob at that path with no
        /// error, e.g. <c>overwrite: true</c> for "save or update this report".
        /// <c>false</c> fails instead if a blob already exists at that path,
        /// e.g. <c>overwrite: false</c> for "only upload if this file doesn't
        /// exist yet".
        /// </param>
        /// <returns>Details of the uploaded blob (URI, container, ETag, etc.).</returns>
        IBlobResponseClient Upload(IBlobRequestClient blobRequestToUpload, bool overwrite = true);

        /// <summary>
        /// Async version of <see cref="Upload"/>. See that method for parameter details.
        /// </summary>
        /// <param name="blobRequestToUpload">
        /// The file to upload, e.g. <c>FileName = "invoice-1042.pdf"</c>,
        /// <c>Directories = new[] { "invoices", "2026" }</c>.
        /// </param>
        /// <param name="overwrite">
        /// <c>true</c> (default) to replace an existing blob; <c>false</c> to
        /// fail if one already exists at that path.
        /// </param>
        /// <param name="cancellationToken">
        /// Token to cancel the upload mid-flight, e.g. tied to an ASP.NET
        /// Core request's <c>HttpContext.RequestAborted</c>.
        /// </param>
        Task<IBlobResponseClient> UploadAsync(IBlobRequestClient blobRequestToUpload, bool overwrite = true, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lists details for every blob in the currently selected container.
        /// </summary>
        /// <returns>One <see cref="IBlobDetails"/> entry per blob in the container.</returns>
        List<IBlobDetails> BlobDetails();

        /// <summary>
        /// Lists details for blobs under a specific virtual directory path within
        /// the currently selected container.
        /// </summary>
        /// <param name="directories">
        /// The virtual folder path to filter by, e.g.
        /// <c>new[] { "invoices", "2026" }</c> to list only blobs under
        /// "invoices/2026/". Pass <c>null</c> (or use the no-argument overload)
        /// to list the whole container.
        /// </param>
        List<IBlobDetails> BlobDetails(string[] directories);

        /// <summary>
        /// Async version of <see cref="ListBlobDetails()"/> — lists every blob in
        /// the currently selected container.
        /// </summary>
        /// <param name="cancellationToken">
        /// Token to cancel the listing operation, e.g.
        /// <c>cancellationToken: cts.Token</c>.
        /// </param>
        Task<List<IBlobDetails>> BlobDetailsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Async version of <see cref="ListBlobDetails(string[])"/>.
        /// </summary>
        /// <param name="directories">
        /// The virtual folder path to filter by, e.g.
        /// <c>new[] { "invoices", "2026" }</c>. Pass <c>null</c> for the whole container.
        /// </param>
        /// <param name="cancellationToken">Token to cancel the listing operation.</param>
        Task<List<IBlobDetails>> BlobDetailsAsync(string[] directories, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks whether a blob exists at the given path within the currently
        /// selected container.
        /// </summary>
        /// <param name="blobName">
        /// The blob's path relative to the selected container, e.g.
        /// "invoices/2026/invoice-1042.pdf".
        /// </param>
        /// <returns><c>true</c> if the blob exists, otherwise <c>false</c>.</returns>
        bool Exists(string blobName);

        /// <summary>
        /// Async version of <see cref="Exists"/>.
        /// </summary>
        /// <param name="blobName">
        /// The blob's path relative to the selected container, e.g.
        /// "invoices/2026/invoice-1042.pdf".
        /// </param>
        /// <param name="cancellationToken">Token to cancel the check.</param>
        Task<bool> ExistsAsync(string blobName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a blob from the currently selected container, if it exists.
        /// </summary>
        /// <param name="blobName">
        /// The blob's path relative to the selected container, e.g.
        /// "invoices/2026/invoice-1042.pdf".
        /// </param>
        /// <returns><c>true</c> if a blob was actually deleted, <c>false</c> if none existed at that path.</returns>
        bool Delete(string blobName);

        /// <summary>
        /// Async version of <see cref="Delete"/>.
        /// </summary>
        /// <param name="blobName">
        /// The blob's path relative to the selected container, e.g.
        /// "invoices/2026/invoice-1042.pdf".
        /// </param>
        /// <param name="cancellationToken">Token to cancel the delete.</param>
        Task<bool> DeleteAsync(string blobName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a blob identified by its full absolute URL, which may belong
        /// to a different container than the one currently selected on this
        /// instance. Does not change <see cref="ContainerName"/> and does not
        /// create the target container if it's missing.
        /// </summary>
        /// <param name="blobUrl">
        /// The full blob URL, e.g.
        /// "https://mystorageacct.blob.core.windows.net/customer-photos/invoices/2026/invoice-1042.pdf".
        /// </param>
        /// <returns><c>true</c> if a blob was actually deleted, <c>false</c> if none existed at that URL.</returns>
        bool DeleteByUrl(string blobUrl);

        /// <summary>
        /// Async version of <see cref="DeleteByUrl"/>.
        /// </summary>
        /// <param name="blobUrl">
        /// The full blob URL, e.g.
        /// "https://mystorageacct.blob.core.windows.net/customer-photos/invoices/2026/invoice-1042.pdf".
        /// </param>
        /// <param name="cancellationToken">Token to cancel the delete.</param>
        Task<bool> DeleteByUrlAsync(string blobUrl, CancellationToken cancellationToken = default);

        /// <summary>
        /// Downloads a blob's full content into memory, from the currently
        /// selected container.
        /// </summary>
        /// <param name="blobName">
        /// The blob's path relative to the selected container, e.g.
        /// "invoices/2026/invoice-1042.pdf".
        /// </param>
        /// <returns>
        /// A tuple of the raw file bytes and the blob's stored content type,
        /// e.g. <c>(fileContent: byte[45000], contentType: "application/pdf")</c>.
        /// </returns>
        (byte[] fileContent, string contentType) DownloadFile(string blobName);

        /// <summary>
        /// Async version of <see cref="DownloadFile"/>.
        /// </summary>
        /// <param name="blobName">
        /// The blob's path relative to the selected container, e.g.
        /// "invoices/2026/invoice-1042.pdf".
        /// </param>
        /// <param name="cancellationToken">Token to cancel the download.</param>
        Task<(byte[] fileContent, string contentType)> DownloadFileAsync(string blobName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Downloads a blob identified by its full absolute URL, which may
        /// belong to a different container than the one currently selected.
        /// Does not change <see cref="ContainerName"/>.
        /// </summary>
        /// <param name="blobUrl">
        /// The full blob URL, e.g.
        /// "https://mystorageacct.blob.core.windows.net/customer-photos/invoices/2026/invoice-1042.pdf".
        /// </param>
        (byte[] fileContent, string contentType) DownloadFileByUrl(string blobUrl);

        /// <summary>
        /// Async version of <see cref="DownloadFileByUrl"/>.
        /// </summary>
        /// <param name="blobUrl">
        /// The full blob URL, e.g.
        /// "https://mystorageacct.blob.core.windows.net/customer-photos/invoices/2026/invoice-1042.pdf".
        /// </param>
        /// <param name="cancellationToken">Token to cancel the download.</param>
        Task<(byte[] fileContent, string contentType)> DownloadFileByUrlAsync(string blobUrl, CancellationToken cancellationToken = default);
    }
}