using System;
using System.IO;
using Microsoft.WindowsAzure.Storage.Blob;

namespace TestEasy.Azure
{
    /// <summary>
    ///     Storage blob extensions
    /// </summary>
    public static class BlobExtensions
    {
        /// <summary>
        ///     Upload file to blob
        /// </summary>
        /// <param name="blob"></param>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static Uri UploadFile(this CloudBlockBlob blob, string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException(string.Format("File '{0}' does not exist.", filePath));
            }

            using (var fileStream = File.OpenRead(filePath))
            {
                blob.UploadFromStream(fileStream);
            }

            return blob.Uri;
        }
    }
}
