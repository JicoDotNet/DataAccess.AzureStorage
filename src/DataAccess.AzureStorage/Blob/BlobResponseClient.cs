using Azure;
using System;

namespace DataAccess.AzureStorage.Blob
{
    /// <inheritdoc cref="IBlobResponseClient"/>
    public class BlobResponseClient : IBlobResponseClient
    {
        /// <inheritdoc/>
        public Uri Uri { get; set; }

        /// <inheritdoc/>
        public string ContainerName { get; set; }

        /// <inheritdoc/>
        public string AbsolutePath { get; set; }

        /// <inheritdoc/>
        public string AccountName { get; set; }

        /// <inheritdoc/>
        public string Path { get; set; }

        /// <inheritdoc/>
        public ETag ETag { get; set; }
    }
}