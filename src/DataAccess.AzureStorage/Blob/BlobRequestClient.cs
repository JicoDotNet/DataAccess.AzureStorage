using System;
using System.IO;

namespace DataAccess.AzureStorage.Blob
{
    public class BlobRequestClient : IBlobRequestClient
    {
        public BlobRequestClient(Stream fileStream, string fileNameWithExtension)
        {
            if (fileStream == null)
                throw new ArgumentNullException(nameof(fileStream), "File stream must not be null.");

            if (fileNameWithExtension == null)
                throw new ArgumentNullException(nameof(fileNameWithExtension), "File name must not be null.");

            if (string.IsNullOrWhiteSpace(fileNameWithExtension))
                throw new ArgumentException("File name must not be empty.", nameof(fileNameWithExtension));

            FileStream = fileStream;
            FileName = fileNameWithExtension;
            Directories = Array.Empty<string>();
        }

        public Stream FileStream { get; private set; }
        public string FileName { get; private set; }
        public string ContentType { get; set; }
        public string[] Directories { get; set; }
    }
}