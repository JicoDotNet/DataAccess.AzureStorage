using Azure;
using System;

namespace DataAccess.AzureStorage.Blob
{
    /// <summary>
    /// Details of a blob returned after a successful upload — everything you'd
    /// need to reference, link to, or conditionally operate on that exact blob
    /// afterward.
    /// </summary>
    public interface IBlobResponseClient
    {
        /// <summary>
        /// The full URI of the uploaded blob, including scheme, account, container,
        /// and blob path. Example value:
        /// "https://mystorageacct.blob.core.windows.net/customer-data/invoices/2026/invoice-1042.pdf".
        /// </summary>
        Uri Uri { get; set; }

        /// <summary>
        /// The container the blob was uploaded into. Example value: 
        /// "customer-data".
        /// </summary>
        string ContainerName { get; set; }

        /// <summary>
        /// Just the path portion of <see cref="Uri"/> — no scheme or host. Example value: 
        /// "/customer-data/invoices/2026/invoice-1042.pdf".
        /// </summary>
        string AbsolutePath { get; set; }

        /// <summary>
        /// The storage account name, parsed from the connection string. Example value:
        /// "mystorageacct".
        /// </summary>
        string AccountName { get; set; }

        /// <summary>
        /// The full blob URI as a string (equivalent to <c>Uri.ToString()</c>). Example value:
        /// "https://mystorageacct.blob.core.windows.net/customer-data/invoices/2026/invoice-1042.pdf".
        /// </summary>
        string Path { get; set; }

        /// <summary>
        /// The blob's ETag at the moment of upload. Pass this back in as <c>ifMatch</c>
        /// on a later <c>ReplaceEntity</c>/<c>Delete</c>-style call for optimistic
        /// concurrency. Example value: 
        /// a value equivalent to <c>new ETag("\"0x8DC1F2A3B4C5D6E\"")</c>.
        /// </summary>
        ETag ETag { get; set; }
    }
}
