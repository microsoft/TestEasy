using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace TestEasy.Core.Helpers
{
    /// <summary>
    ///     Helper API for FTP scenarios
    /// </summary>
    public class FtpHelper
    {   
        /// <summary>
        ///     Authorize credentials: user and password
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="password"></param>
        /// <param name="retries"></param>
        /// <returns></returns>
        public static FtpHelper Authorize(string userName, string password, int retries = 0)
        {
            return new FtpHelper(userName, password, retries);
        }

        private string UserName { get; set; }
        private string Password { get; set; }
        private int Retries { get; set; }
        public FtpWebResponse LastResponse { get; private set; }
        
        internal FtpHelper(string userName, string password, int retries)
        {
            UserName = userName;
            Password = password;
            Retries = retries;
        }

        /// <summary>
        ///     Upload file
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="destinationPath"></param>
        /// <returns></returns>
        public bool UploadFile(string sourcePath, string destinationPath)
        {
            var flag = RetryHelper.RetryUntil(() => Upload(sourcePath, destinationPath), Retries);
            TestEasyLog.Instance.Info(flag ? "FTP upload succeeded." : "FTP upload failed.");

            return flag;
        }

        /// <summary>
        ///     Upload directory
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="siteRoot"></param>
        /// <param name="siteRootRelativePath"></param>
        /// <returns></returns>
        public bool UploadDir(string sourcePath, string siteRoot, string siteRootRelativePath = "")
        {
            var destinationPath = siteRoot.Trim('/');
            if (!string.IsNullOrEmpty(siteRootRelativePath))
            {                
                // create directories one by one in siteRootRelativePath
                var subDirectories = siteRootRelativePath.Trim(new [] {'/', ' '}).Split(new[] {'/'});
                foreach (var dir in subDirectories)
                {
                    destinationPath = destinationPath + "/" + dir;

                    if (!RetryHelper.RetryUntil(() => CreateRemoteDirectory(destinationPath), Retries))
                    {
                        // don't throw here since it may fail to create existing directories 
                        var message = string.Format("Failed to create a remote directory '{0}' after '{1}' retries.",
                                                    destinationPath,
                                                    Retries);
                        TestEasyLog.Instance.Failure(message);
                        throw new Exception(message);
                    }                    
                }
            }

            IEnumerable<string> failedUploads;
            var flag = RecursiveUpload(sourcePath, destinationPath, out failedUploads);

            if (flag)
            {
                TestEasyLog.Instance.Info("FTP upload succeeded for directory and all contents.");
            }
            else
            {
                var stringBuilder = new StringBuilder();
                stringBuilder.AppendLine("FTP upload failed for one or more files:");
                foreach (var current in failedUploads)
                {
                    stringBuilder.AppendLine(string.Format("  - {0}", current));
                }

                TestEasyLog.Instance.Info(stringBuilder.ToString());
            }

            return flag;
        }

        /// <summary>
        ///     Upload file content
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="fileContent"></param>
        /// <param name="destinationPath"></param>
        public void CreateAndUploadFile(string fileName, string fileContent, string destinationPath)
        {
            var directoryInfo = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
            var sourcePath = Path.Combine(directoryInfo.FullName, fileName);
            
            File.WriteAllText(sourcePath, fileContent);
            TestEasyLog.Instance.Info(RetryHelper.RetryUntil(() => Upload(sourcePath, destinationPath), Retries) ? "FTP upload succeeded." : "FTP upload failed.");

            try
            {
               Directory.Delete(directoryInfo.FullName, true);
            }
            catch (Exception e)
            {
                TestEasyLog.Instance.Failure(string.Format("Failed to delete directory '{0}'. Message: '{1}'", directoryInfo.FullName, e.Message));
            }
        }

        /// <summary>
        ///     Rename FTP resource
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="destinationPath"></param>
        /// <returns></returns>
        public bool Rename(string sourcePath, string destinationPath)
        {
            var ftpRequest = GetFtpWebRequest(sourcePath);
            ftpRequest.Method = WebRequestMethods.Ftp.Rename;
            ftpRequest.RenameTo = destinationPath;

            return RetryHelper.RetryUntil(() => ExecuteSimpleFtpWebRequest(ftpRequest), Retries);
        }

        /// <summary>
        ///     Createa directory via FTP
        /// </summary>
        /// <param name="targetPath"></param>
        /// <returns></returns>
        public bool CreateDir(string targetPath)
        {
            var flag = RetryHelper.RetryUntil(() => CreateRemoteDirectory(targetPath), Retries);
            TestEasyLog.Instance.Info(flag ? "FTP create directory succeeded." : "FTP create directory failed.");

            return flag;
        }

        /// <summary>
        ///     Delete directory via FTP
        /// </summary>
        /// <param name="targetPath"></param>
        /// <returns></returns>
        public bool DeleteDir(string targetPath)
        {
            var ftpRequest = GetFtpWebRequest(targetPath);
            ftpRequest.Method = WebRequestMethods.Ftp.RemoveDirectory;

            return RetryHelper.RetryUntil(() => ExecuteSimpleFtpWebRequest(ftpRequest), Retries);
        }

        /// <summary>
        ///     Delete file via FTP
        /// </summary>
        /// <param name="targetPath"></param>
        /// <returns></returns>
        public bool DeleteFile(string targetPath)
        {
            var ftpRequest = GetFtpWebRequest(targetPath);
            ftpRequest.Method = WebRequestMethods.Ftp.DeleteFile;

            return RetryHelper.RetryUntil(() => ExecuteSimpleFtpWebRequest(ftpRequest), Retries);
        }

        /// <summary>
        ///     Download file via FTP
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="destinationPath"></param>
        /// <returns></returns>
        public bool DownloadFile(string sourcePath, string destinationPath)
        {
            var flag = RetryHelper.RetryUntil(() => Download(sourcePath, destinationPath), Retries);

            TestEasyLog.Instance.Info(flag ? "FTP download succeeded." : "FTP download failed.");

            return flag;
        }

        /// <summary>
        ///     Get file size at FTP location
        /// </summary>
        /// <param name="targetPath"></param>
        /// <returns></returns>
        public long GetFileSize(string targetPath)
        {
            long size;
            var result = TryGetRemoteFileSize(targetPath, out size);
            TestEasyLog.Instance.Info(result ? "FTP get file size succeeded." : "FTP get file size failed.");

            return size;
        }

        /// <summary>
        ///     Get date and time of last modification for FTP resource
        /// </summary>
        /// <param name="targetPath"></param>
        /// <param name="lastModified"></param>
        /// <returns></returns>
        public bool GetDateModified(string targetPath, out DateTime lastModified)
        {
            var flag = TryGetDateLastModified(targetPath, out lastModified);
            TestEasyLog.Instance.Info(flag ? "FTP get date modified succeeded." : "FTP get date modified failed.");

            return flag;
        }

        /// <summary>
        ///     Get list of directories under FTP path
        /// </summary>
        /// <param name="targetPath"></param>
        /// <param name="dirList"></param>
        /// <returns></returns>
        public bool GetDirList(string targetPath, out string[] dirList)
        {
            var flag = TryGetRemoteDirectoryListing(targetPath, out dirList);
            TestEasyLog.Instance.Info(flag ? "FTP directory listing succeeded." : "FTP directory listing failed.");

            return flag;
        }

        /// <summary>
        ///     Get detailed list of directories under FTP path
        /// </summary>
        /// <param name="targetPath"></param>
        /// <param name="dirList"></param>
        /// <returns></returns>
        public bool GetDetailedDirList(string targetPath, out List<string[]> dirList)
        {
            var flag = TryGetDetailedRemoteDirectoryListing(targetPath, out dirList);
            TestEasyLog.Instance.Info(flag ? "FTP detailed directory listing succeeded." : "FTP directory listing failed.");

            return flag;
        }

        /// <summary>
        ///     Download FTP directory
        /// </summary>
        /// <param name="sourcePath"></param>
        /// <param name="destinationPath"></param>
        /// <returns></returns>
        public bool DownloadDir(string sourcePath, string destinationPath)
        {
            IEnumerable<string> failedFiles;
            var flag = RecursiveDownload(sourcePath, destinationPath, out failedFiles);

            if (flag)
            {
                TestEasyLog.Instance.Info("FTP download succeeded for directory and all contents.");
            }
            else
            {
                var stringBuilder = new StringBuilder();
                stringBuilder.AppendLine("FTP download failed for one or more files:");
                foreach (var fileName in failedFiles)
                {
                    stringBuilder.AppendLine(string.Format("  - {0}", fileName));
                }
                TestEasyLog.Instance.Info(stringBuilder.ToString());
            }

            return flag;
        }

        #region Private methods 

        private bool TryGetDateLastModified(string targetPath, out DateTime lastModified)
        {
            var retries = Retries;
            do
            {
                lastModified = GetDateLastModified(targetPath);
            }
            while (lastModified == default(DateTime) && retries-- > 0);

            return lastModified != default(DateTime);
        }

        private bool TryGetRemoteDirectoryListing(string targetPath, out string[] list)
        {
            var retries = Retries;
            bool remoteDirectoryListing;
            do
            {
                remoteDirectoryListing = GetRemoteDirectoryListing(targetPath, out list);
            }
            while (!remoteDirectoryListing && retries-- > 0);

            return remoteDirectoryListing;
        }

        private bool TryGetDetailedRemoteDirectoryListing(string targetPath, out List<string[]> list)
        {
            var retries = Retries;
            bool detailedRemoteDirectoryListing;
            do
            {
                detailedRemoteDirectoryListing = GetDetailedRemoteDirectoryListing(targetPath, out list);
            }
            while (!detailedRemoteDirectoryListing && retries-- > 0);

            return detailedRemoteDirectoryListing;
        }

        private bool TryGetRemoteFileSize(string targetPath, out long size)
        {
            var retries = Retries;
            do
            {
                size = GetRemoteFileSize(targetPath);
            }
            while (size < 0 && retries-- > 0);

            return size >= 0;
        }

        private bool Upload(string sourcePath, string destinationPath, int offsetByte = 0)
        {
            var ftpWebRequest = GetFtpWebRequest(destinationPath);
            ftpWebRequest.Method = WebRequestMethods.Ftp.UploadFile;
            ftpWebRequest.ContentOffset = offsetByte;

            LogStepFtpRequest(ftpWebRequest.RequestUri, string.Format("{0} {1}, {2}", ftpWebRequest.Method, sourcePath, destinationPath));
            
            var fileInfo = new FileInfo(sourcePath);
            var fileStream = fileInfo.OpenRead();
            Stream stream = null;

            var result = false;
            try
            {
                stream = ftpWebRequest.GetRequestStream();
                fileStream.Seek(offsetByte, SeekOrigin.Begin);
                result = TransferData(fileStream, stream);
            }
            catch (InvalidOperationException ex)
            {
                TestEasyLog.Instance.Failure(string.Format("Exception while getting FTP request stream: '{0}'", ex.Message));
            }
            finally
            {
                if (stream != null)
                {
                    stream.Close();
                }
            }

            try
            {
                LastResponse = (FtpWebResponse)ftpWebRequest.GetResponse();
            }
            catch (WebException ex2)
            {
                TestEasyLog.Instance.Failure(string.Format("Exception while getting FTP response: '{0}'", ex2.Message));
                LastResponse = (FtpWebResponse)ex2.Response;
            }
            catch (InvalidOperationException ex3)
            {
                TestEasyLog.Instance.Failure(string.Format("Exception while getting FTP response: '{0}'", ex3.Message));
            }
            finally
            {
                if (LastResponse != null)
                {
                    LastResponse.Close();
                }
            }

            return result;
        }

        private bool Download(string sourcePath, string destinationPath, int offsetByte = 0)
        {
            var ftpWebRequest = GetFtpWebRequest(sourcePath);
            ftpWebRequest.Method = WebRequestMethods.Ftp.DownloadFile;
            ftpWebRequest.ContentOffset = offsetByte;

            LogStepFtpRequest(ftpWebRequest.RequestUri, string.Format("{0} {1}, {2}", ftpWebRequest.Method, sourcePath, destinationPath));

            var result = false;
            var fileInfo = new FileInfo(destinationPath);
            using (var fileStream = fileInfo.OpenWrite())
            {
                try
                {
                    LastResponse = (FtpWebResponse) ftpWebRequest.GetResponse();
                    var responseStream = LastResponse.GetResponseStream();
                    fileStream.Seek(offsetByte, SeekOrigin.Begin);
                    result = TransferData(responseStream, fileStream);
                }
                catch (WebException ex)
                {
                    TestEasyLog.Instance.Failure(string.Format("Exception while getting FTP response: '{0}'", ex.Message));
                    LastResponse = (FtpWebResponse) ex.Response;
                }
                catch (InvalidOperationException ex2)
                {
                    TestEasyLog.Instance.Failure(string.Format("Exception while getting FTP response: '{0}'",
                                                               ex2.Message));
                }
                finally
                {
                    if (LastResponse != null)
                    {
                        LastResponse.Close();
                    }
                }
            }

            return result;
        }

        private bool TransferData(Stream source, Stream destination)
        {
            bool result;

            try
            {
                const int fileTransferCap = 4096;
                var buffer = new byte[fileTransferCap];
                var total = 0;
                for (var bytesRead = source.Read(buffer, 0, 4096); bytesRead != 0; bytesRead = source.Read(buffer, 0, 4096))
                {
                    total += bytesRead;
                    destination.Write(buffer, 0, bytesRead);
                }

                TestEasyLog.Instance.Info(string.Format("Ending FTP data transfer after '{0}' bytes.", total.ToString(CultureInfo.InvariantCulture)));

                result = true;
            }
            catch (Exception ex)
            {
                TestEasyLog.Instance.Failure(string.Format("Exception while transferring file using FTP protocol: '{0}'", ex.Message));
                result = false;
            }

            return result;
        }

        private long GetRemoteFileSize(string targetPath)
        {
            long size = -1;
            var ftpWebRequest = GetFtpWebRequest(targetPath);
            ftpWebRequest.Method = WebRequestMethods.Ftp.GetFileSize;

            LogStepFtpRequest(ftpWebRequest.RequestUri, string.Format("{0} {1}", ftpWebRequest.Method, targetPath));

            try
            {
                LastResponse = (FtpWebResponse)ftpWebRequest.GetResponse();
                size = LastResponse.ContentLength;
            }
            catch (WebException ex)
            {
                TestEasyLog.Instance.Failure(string.Format("Exception while getting FTP response: '{0}'", ex.Message));
                LastResponse = (FtpWebResponse)ex.Response;
            }
            catch (InvalidOperationException ex2)
            {
                TestEasyLog.Instance.Failure(string.Format("Exception while getting FTP response: '{0}'", ex2.Message));
            }
            finally
            {
                if (LastResponse != null)
                {
                    LastResponse.Close();
                }
            }

            return size;
        }

        private DateTime GetDateLastModified(string targetPath)
        {
            var lastModified = default(DateTime);
            var ftpWebRequest = GetFtpWebRequest(targetPath);
            ftpWebRequest.Method = WebRequestMethods.Ftp.GetDateTimestamp;
            
            LogStepFtpRequest(ftpWebRequest.RequestUri, string.Format("{0} {1}", ftpWebRequest.Method, targetPath));

            try
            {
                LastResponse = (FtpWebResponse)ftpWebRequest.GetResponse();
                lastModified = LastResponse.LastModified;
            }
            catch (WebException ex)
            {
                TestEasyLog.Instance.Failure(string.Format("Exception while getting FTP response: '{0}'", ex.Message));
                LastResponse = (FtpWebResponse)ex.Response;
            }
            catch (InvalidOperationException ex2)
            {
                TestEasyLog.Instance.Failure(string.Format("Exception while getting FTP response: '{0}'", ex2.Message));
            }
            finally
            {
                if (LastResponse != null)
                {
                    LastResponse.Close();
                }
            }

            return lastModified;
        }

        private bool GetRemoteDirectoryListing(string targetPath, out string[] list)
        {
            var ftpWebRequest = GetFtpWebRequest(targetPath);
            ftpWebRequest.Method = WebRequestMethods.Ftp.ListDirectory;
            
            LogStepFtpRequest(ftpWebRequest.RequestUri, string.Format("{0} {1}, {2}", ftpWebRequest.Method, targetPath, targetPath));

            var stringFromResponseStream = GetStringFromResponseStream(ftpWebRequest);
            list = (stringFromResponseStream != null) 
                ? stringFromResponseStream.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries) 
                : null;

            return stringFromResponseStream != null;
        }

        private bool GetDetailedRemoteDirectoryListing(string targetPath, out List<string[]> list)
        {
            var ftpWebRequest = GetFtpWebRequest(targetPath);
            ftpWebRequest.Method = WebRequestMethods.Ftp.ListDirectoryDetails;
            
            LogStepFtpRequest(ftpWebRequest.RequestUri, string.Format("{0} {1}", ftpWebRequest.Method, targetPath));

            list = null;
            var stringFromResponseStream = GetStringFromResponseStream(ftpWebRequest);

            if (!string.IsNullOrEmpty(stringFromResponseStream))
            {
                var responseLines = stringFromResponseStream.Split(new[] {Environment.NewLine}, StringSplitOptions.RemoveEmptyEntries);
                list = new List<string[]>();
                foreach (var line in responseLines)
                {
                    // TODO this is a quick hack to get one of the response formats - see how to make it more generic
                    // supported now result output from directory listing is:
                    //      06-25-09  02:41PM            144700153 image34.gif
                    // however it may also be : 
                    //      -rw-r--r--    1 ftp      ftp        659450 Jun 15 05:07 TEST.TXT

                    var sourceArray = line.Split(new[] {" "}, StringSplitOptions.RemoveEmptyEntries);
                    var tempArray = new string[4];
                    Array.Copy(sourceArray, tempArray, 3);
                    tempArray[3] = line.Substring(39);
                    list.Add(tempArray);
                }
            }

            return stringFromResponseStream != null;
        }

        private bool CreateRemoteDirectory(string targetPath)
        {
            var ftpWebRequest = GetFtpWebRequest(targetPath);
            ftpWebRequest.Method = WebRequestMethods.Ftp.MakeDirectory;

            return ExecuteSimpleFtpWebRequest(ftpWebRequest);
        }

        private string GetStringFromResponseStream(FtpWebRequest ftpRequest)
        {
            string result = null;

            var memoryStream = new MemoryStream();
            StreamReader streamReader = null;

            try
            {
                LastResponse = (FtpWebResponse)ftpRequest.GetResponse();
                var responseStream = LastResponse.GetResponseStream();

                if (TransferData(responseStream, memoryStream))
                {
                    streamReader = new StreamReader(memoryStream);
                    memoryStream.Position = 0L;
                    result = streamReader.ReadToEnd();
                }
            }
            catch (WebException ex)
            {
                TestEasyLog.Instance.Info(string.Format("Exception while getting response for '{0}'. Message: '{1}'", ftpRequest.RequestUri, ex.Message));
                LastResponse = (FtpWebResponse)ex.Response;
            }
            catch (InvalidOperationException ex2)
            {
                TestEasyLog.Instance.Info(string.Format("Exception while getting response for '{0}'. Message: '{1}'", ftpRequest.RequestUri, ex2.Message));
            }
            finally
            {
                if (LastResponse != null)
                {
                    LastResponse.Close();
                }

                memoryStream.Close();

                if (streamReader != null)
                {
                    streamReader.Close();
                }
            }

            return result;
        }

        private bool RecursiveUpload(string sourcePath, string destinationPath, out IEnumerable<string> failedUploads)
        {
            var tempFailedUploads = new List<string>();
            string[] tempDirListing;
            if (!RetryHelper.RetryUntil(() => GetDirList(destinationPath, out tempDirListing), Retries))
            {
                // if directory does not exist
                if (!RetryHelper.RetryUntil(() => CreateRemoteDirectory(destinationPath), Retries))
                {
                    // don't throw here since it may fail to create existing directories 
                    var message = string.Format("Failed to create a remote directory '{0}' after '{1}' retries.",
                                                destinationPath,
                                                Retries);
                    TestEasyLog.Instance.Failure(message);
                    throw new Exception(message);
                }
            }

            var success = true;
            var directories = Directory.GetDirectories(sourcePath).ToList();
            var files = Directory.GetFiles(sourcePath).ToList();

            foreach (var dir in directories)
            {
                IEnumerable<string> failedUploadsRecursive;
                var flag = RecursiveUpload(dir, Path.Combine(destinationPath, new DirectoryInfo(dir).Name), out failedUploadsRecursive);
                
                tempFailedUploads.AddRange(failedUploadsRecursive);
                success &= flag;
            }

            foreach(var file in files)
            {
                if (string.IsNullOrEmpty(file)) continue;

                var tempFile = file;
                if (!RetryHelper.RetryUntil(() => Upload(tempFile, Path.Combine(destinationPath, Path.GetFileName(tempFile) ?? "")), Retries))
                {
                    success = false;
                    tempFailedUploads.Add(file);
                }
            }

            failedUploads = tempFailedUploads;

            return success;
        }

        private bool RecursiveDownload(string sourcePath, string destinationPath, out  IEnumerable<string> failedDownloads)
        {
            var tempFailedDownloads = new List<string>();
            Directory.CreateDirectory(destinationPath);
            var directories = new List<string>();
            var files = new List<string>();
            List<string[]> listing;

            var success = TryGetDetailedRemoteDirectoryListing(sourcePath, out listing);
            if (!success)
            {
                TestEasyLog.Instance.Warning(string.Format("Failed to get remote directory listing for '{0}' after '{1}' retries.", sourcePath, Retries));
            }

            foreach (var listingItem in listing)
            {
                // listings come in the following format: 
                //      08-10-11  12:02PM       <DIR>          Version2
                //      06-25-09  02:41PM            144700153 image34.gif
                if (listingItem[2].Equals("<DIR>", StringComparison.InvariantCultureIgnoreCase))
                {
                    directories.Add(listingItem[3]);
                }
                else
                {
                    files.Add(listingItem[3]);
                }
            }

            foreach (var dir in directories)
            {
                IEnumerable<string> failedDownloadsRecursive;
                var flag = RecursiveDownload(Path.Combine(sourcePath, dir),
                                              Path.Combine(destinationPath, dir), out failedDownloadsRecursive);

                tempFailedDownloads.AddRange(failedDownloadsRecursive);
                success &= flag;
            }

            foreach(var file in files)
            {
                if (string.IsNullOrEmpty(file)) continue;

                var path = Path.Combine(sourcePath, file);
                
                var tempFile = file;
                if (!RetryHelper.RetryUntil(() => Download(path, Path.Combine(destinationPath, Path.GetFileName(tempFile) ?? "")), Retries))
                {
                    success = false;
                    tempFailedDownloads.Add(file);
                }
            }

            failedDownloads = tempFailedDownloads;
            return success;
        }

        private bool ExecuteSimpleFtpWebRequest(FtpWebRequest ftpRequest)
        {
            bool result = false;
            try
            {
                LogStepFtpRequest(ftpRequest.RequestUri, ftpRequest.Method);
                LastResponse = (FtpWebResponse)ftpRequest.GetResponse();
                TestEasyLog.Instance.Info(string.Format("FTP request completed: {0} - {1}", LastResponse.StatusCode, LastResponse.StatusDescription));
                result = true;
            }
            catch (WebException ex)
            {
                TestEasyLog.Instance.Failure(string.Format("Exception while getting response: '{0}'", ex.Message));
                LastResponse = (FtpWebResponse)ex.Response;
            }
            catch (Exception ex2)
            {
                TestEasyLog.Instance.Failure(string.Format("Exception while getting response: '{0}'", ex2.Message));
            }
            finally
            {
                if (LastResponse != null)
                {
                    LastResponse.Close();
                }
            }

            return result;
        }

        private FtpWebRequest GetFtpWebRequest(string targetPath)
        {
            TestEasyLog.Instance.Info(string.Format("Initializing Ftp request for uri '{0}'", targetPath));

            var uri = new Uri(targetPath);
            if (uri.Scheme != Uri.UriSchemeFtp)
            {
                throw new InvalidOperationException(
                    string.Format("Invalid FTP Uri: '{0}'. FTP Uri should resemble 'ftp://[SERVER_IP_OR_NAME]/[SITE_ROOT_RELATIVE_TARGET_PATH]", targetPath));
            }

            var ftpWebRequest = (FtpWebRequest)WebRequest.Create(uri);
            ftpWebRequest.Credentials = new NetworkCredential(UserName, Password);
            ftpWebRequest.UsePassive = true;
            ftpWebRequest.UseBinary = true;
            ftpWebRequest.KeepAlive = false;
            ftpWebRequest.Proxy = null;

            return ftpWebRequest;
        }

        private void LogStepFtpRequest(Uri ftpUri, string action)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("- FTP request -");
            stringBuilder.Append(" - Action: ");
            stringBuilder.AppendLine(action);
            stringBuilder.Append(" - Uri: ");
            stringBuilder.AppendLine(ftpUri.ToString());
            stringBuilder.Append(" - Username: ");
            stringBuilder.Append(UserName);

            TestEasyLog.Instance.Info(stringBuilder.ToString());
        }

        #endregion
    }
}
