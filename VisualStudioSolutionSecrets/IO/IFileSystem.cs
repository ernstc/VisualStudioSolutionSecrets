using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VisualStudioSolutionSecrets.IO
{
    public interface IFileSystem
    {
        DirectoryInfo CreateDirectory(string path);
        bool FileExists(string path);
        FileStream FileOpenRead(string path);
        string[] FileReadAllLines(string path);
        string FileReadAllText(string path);
        void FileWriteAllText(string path, string contents);
        string GetCurrentDirectory();
        FileInfo GetFileInfo(string fileName);
        string[] GetFiles(string path, string searchPattern, SearchOption searchOption);
        string PathCombine(string path1, string path2);
        string PathCombine(string path1, string path2, string path3);
        string PathCombine(params string[] paths);
    }
}
