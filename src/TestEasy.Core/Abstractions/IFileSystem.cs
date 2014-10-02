using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml.Linq;

namespace TestEasy.Core.Abstractions
{
    public interface IFileSystem
    {
        bool DirectoryExists(string directoryPath);
        void DirectoryCreate(string path);
        void DirectoryDelete(string directoryPath, bool recursive);
        string[] DirectoryGetSubDirs(string directoryPath);
        string[] DirectoryGetSubDirs(string directoryPath, string searchPattern, SearchOption searchOption);
        string[] DirectoryGetFiles(string directoryPath);
        string[] DirectoryGetFiles(string directoryPath, string searchPattern, SearchOption searchOption);
        void DirectoryCopy(string sourcePath, string destinationPath);
        void DirectorySetAttribute(string dirpath, FileAttributes attributes);

        bool FileExists(string filePath);
        void FileCopy(string sourceFileName, string destFileName, bool overwrite);
        Stream FileOpenRead(string filename);
        string FileReadAllText(string fileName);
        void FileWrite(string filename, string content, Encoding encoding);
        void FileWrite(string fileName, string content);
        void FileDelete(string fileName);
        void FileMove(string sourceFileName, string destFileName);
        Version FileGetVersion(string fileName);
        DateTime FileGetLastWriteTime(string fileName);
        string GetExecutingAssemblyDirectory();
        string GetTempPath();

        Assembly GetExecutingAssembly();
        Assembly GetCallingAssembly();
        Assembly LoadAssemblyFromFile(string path);
        XDocument LoadXDocumentFromFile(string path);
        void StoreXDocumentToFile(XDocument doc, string path);
    }
}
