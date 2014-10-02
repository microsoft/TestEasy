using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Diagnostics;
using System.Xml.Linq;

namespace TestEasy.Core.Abstractions
{
    /// <summary>
    ///     Abstraction to deal with file system
    /// </summary>
    public class FileSystem : IFileSystem
    {
        public bool DirectoryExists(string directoryPath)
        {
            return Directory.Exists(directoryPath);
        }

        public string[] DirectoryGetSubDirs(string directoryPath)
        {
            return Directory.GetDirectories(directoryPath);
        }

        public string[] DirectoryGetSubDirs(string directoryPath, string searchPattern, SearchOption searchOption)
        {
            return Directory.GetDirectories(directoryPath, searchPattern, searchOption);
        }

        public string[] DirectoryGetFiles(string directoryPath)
        {
            return Directory.GetFiles(directoryPath);
        }

        public string[] DirectoryGetFiles(string directoryPath, string searchPattern, SearchOption searchOption)
        {
            return Directory.GetFiles(directoryPath, searchPattern, searchOption);
        }

        public void DirectoryDelete(string directoryPath, bool recursive)
        {
            Directory.Delete(directoryPath, recursive);
        }

        public void DirectoryCreate(string path)
        {
            Directory.CreateDirectory(path);
        }

        public void DirectoryCopy(string sourcePath, string destinationPath)
        {
            if (!DirectoryExists(destinationPath))
            {
                DirectoryCreate(destinationPath);
            }

            foreach (string file in DirectoryGetFiles(sourcePath, "*.*", SearchOption.TopDirectoryOnly))
            {
                string fileName = Path.GetFileName(file) ?? "";
                FileCopy(file, Path.Combine(destinationPath, fileName), true);
            }

            foreach (string subDir in DirectoryGetSubDirs(sourcePath, "*.*", SearchOption.TopDirectoryOnly))
            {
                string subDirName = Path.GetFileName(subDir) ?? "";
                DirectoryCopy(subDir, Path.Combine(destinationPath, subDirName));
            }
        }

        public void DirectorySetAttribute(string dirpath, FileAttributes attributes)
        {
            DirectoryInfo dir = new DirectoryInfo(dirpath);
            dir.Attributes = attributes;

            foreach (FileInfo file in dir.GetFiles())
            {
                file.Attributes = FileAttributes.Normal;
            }

            foreach (DirectoryInfo subDir in dir.GetDirectories())
            {
                DirectorySetAttribute(subDir.FullName, attributes);
            }
        }

        public bool FileExists(string filePath)
        {
            return File.Exists(filePath);
        }

        public Stream FileOpenRead(string filename)
        {
            return File.OpenRead(filename);
        }

        public string FileReadAllText(string fileName)
        {
            return File.ReadAllText(fileName);
        }

        public void FileDelete(string fileName)
        {
            File.Delete(fileName);
        }

        public void FileMove(string sourceFileName, string destFileName)
        {
            File.Move(sourceFileName, destFileName);
        }

        public void FileCopy(string sourceFileName, string destFileName, bool overwrite)
        {
            File.Copy(sourceFileName, destFileName, overwrite);
        }

        public void FileWrite(string fileName, string content)
        {
            File.WriteAllText(fileName, content);
        }

        public void FileWrite(string filename, string content, Encoding encoding)
        {
            FileInfo fileInfo = new FileInfo(filename);
            if (!DirectoryExists(fileInfo.DirectoryName))
            {
                DirectoryCreate(fileInfo.DirectoryName);
            }

            FileStream fout = null;
            StreamWriter sw = StreamWriter.Null;

            try
            {
                fout = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                if (encoding == null)
                {
                    sw = new StreamWriter(fout);
                }
                else
                {
                    sw = new StreamWriter(fout, encoding);
                }
                sw.Write(content);
            }
            finally
            {
                sw.Close();
                if (fout != null)
                {
                    fout.Close();
                }

                System.Threading.Thread.Sleep(100);
                fileInfo.LastAccessTime = DateTime.Now;
                fileInfo.LastWriteTime = DateTime.Now;
            }
        }

        public Version FileGetVersion(string fileName)
        {
            FileVersionInfo fileVersion = FileVersionInfo.GetVersionInfo(fileName);

            return new Version(fileVersion.FileMajorPart,
                                fileVersion.FileMinorPart,
                                fileVersion.FileBuildPart,
                                fileVersion.FilePrivatePart);
        }

        public DateTime FileGetLastWriteTime(string fileName)
        {
            var fi = new FileInfo(fileName);
            return fi.LastWriteTime;
        }
        
        public string GetExecutingAssemblyDirectory()
        {
            return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
        }

        public string GetTempPath()
        {
            return Path.GetTempPath();
        }

        public Assembly GetExecutingAssembly()
        {
            return Assembly.GetExecutingAssembly();
        }

        public Assembly GetCallingAssembly()
        {
            return Assembly.GetCallingAssembly();
        }

        public Assembly LoadAssemblyFromFile(string path)
        {
            return Assembly.LoadFrom(path);
        }

        public XDocument LoadXDocumentFromFile(string path)
        {
            return XDocument.Load(path);
        }

        public void StoreXDocumentToFile(XDocument doc, string path)
        {
            doc.Save(path);
        }
    }
}
