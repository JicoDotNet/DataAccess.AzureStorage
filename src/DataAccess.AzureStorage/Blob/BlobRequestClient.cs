using System;
using System.IO;

namespace DataAccess.AzureStorage.Blob
{
    public class BlobRequestClient : IBlobRequestClient
    {
        public BlobRequestClient(Stream fileStream, string fileNameWithExtension)
        {
            if (fileStream == null)
            {
                throw new ArgumentNullException(nameof(fileStream), "file Stream should not be null.");
            }
            if (string.IsNullOrEmpty(fileNameWithExtension))
            {
                throw new ArgumentNullException(nameof(fileNameWithExtension), "file Name should not be null or empty.");
            }
            FileStream = fileStream;
            FileName = fileNameWithExtension;
        }

        public Stream FileStream { get; private set; }
        public string FileName { get; private set; }
        public string ContentType { get; set; }
        public string[] Directories { get; set; }
    }
}
