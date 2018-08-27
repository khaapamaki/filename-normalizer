// Long Path Aware File Operations
// Uses Pri.LongPath library by Peter Richman

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace FileNameNormalizer
{
    /// <summary>
    /// A class for file operations. All operations are able to handle extended length
    /// paths.
    /// </summary>
    /// <remarks>
    /// Uses Pri.LongPath library by Peter Richman
    /// </remarks>
    public static class FileOp
    {
        public const int MaxDirPathLength = 248;
        public const int MaxFilePathLength = 260;

        /// <summary>
        /// Get Files Array (path strings)
        /// </summary>
        /// <param name="path"></param>
        /// <param name="pattern"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool GetFiles(string path, string pattern, out List<string> result)
        {
            List<string> fileList = new List<string>(100);
            result = fileList;
            try {
                IEnumerable filesInDirectory;
                if (path.Length < MaxDirPathLength) {
                    // Normal Version
                    filesInDirectory = System.IO.Directory.EnumerateFiles(path, pattern, SearchOption.TopDirectoryOnly);
                    foreach (string file in filesInDirectory)
                        fileList.Add(file);
                } else {
                    // Long path version
                    filesInDirectory =
                        Pri.LongPath.Directory.EnumerateFiles(path, pattern, SearchOption.TopDirectoryOnly);
                    foreach (string file in filesInDirectory)
                        fileList.Add(file);
                }
            } catch {
                Console.WriteLine($"*** Error: Cannot Open Directory: {path}");
                return false;
            }

            result = new List<string>(fileList); //.ToArray();
            return true;
        }

        /// <summary>
        /// Get SubDirectory Array
        /// </summary>
        /// <param name="directoryItem"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool GetSubDirectories(string directoryItem, out List<string> result)
        {
            List<string> dirList = new List<string>(20);
            result = dirList;
            try {
                IEnumerable subDirectories;
                if (directoryItem.Length < MaxDirPathLength) {
                    // Normal Version
                    subDirectories =
                        System.IO.Directory.EnumerateDirectories(directoryItem, "*", SearchOption.TopDirectoryOnly);
                    foreach (string dir in subDirectories)
                        dirList.Add(dir);
                } else {
                    // Long path version
                    subDirectories =
                        Pri.LongPath.Directory.EnumerateDirectories(directoryItem, "*", SearchOption.TopDirectoryOnly);
                    foreach (string dir in subDirectories)
                        dirList.Add(dir);
                }
            } catch {
                //Console.WriteLine("*** Error: Cannot Open Directory: {0:s}", directoryItem);
                return false;
            }

            result = new List<string>(dirList); //.ToArray();
            return true;
        }

        /// <summary>
        /// Get direcotry contents with files or folder first
        /// </summary>
        /// <param name="path"></param>
        /// <param name="pattern"></param>
        /// <param name="dividePoint"></param>
        /// <param name="directoriesFirst"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public static bool GetFilesAndDirectories(string path, string pattern, out int dividePoint,
            bool directoriesFirst, out List<string> result)
        {
            bool succeeded1 = GetFiles(path, pattern, out List<string> files);
            bool succeeded2 = GetSubDirectories(path, out List<string> dirs);
            if (succeeded1 == false || succeeded2 == false) {
                dividePoint = 0;
                result = new List<string>(0);
                return false;
            }

            List<string> contents = new List<string>(files.Count() + dirs.Count());
            if (directoriesFirst) {
                dirs.ForEach(x => contents.Add(x));
                files.ForEach(x => contents.Add(x));
                dividePoint = dirs.Count();
            } else {
                files.ForEach(x => contents.Add(x));
                dirs.ForEach(x => contents.Add(x));
                dividePoint = files.Count();
            }

            result = contents;
            return true;
        }

        /// <summary>
        /// Rename File or Directory
        /// </summary>
        /// <param name="path"></param>
        /// <param name="newPath"></param>
        /// <param name="isDir"></param>
        /// <returns></returns>
        public static bool Rename(string path, string newPath, bool isDir = false)
        {
            if (isDir) {
                try {
                    if (path.Length < MaxDirPathLength && newPath.Length < MaxDirPathLength)
                        Directory.Move(path, newPath);
                    else
                        Pri.LongPath.Directory.Move(path, newPath);
                } catch {
                    return false;
                }

                return true;
            }

            try {
                if (path.Length < MaxFilePathLength && newPath.Length < MaxFilePathLength)
                    File.Move(path, newPath);
                else
                    Pri.LongPath.File.Move(path, newPath);
            } catch {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Check if given path exists
        /// </summary>
        /// <param name="name">The path to be examined</param>
        /// <returns>Returns true if path exists</returns>
        /// 
        public static bool FileOrDirectoryExists(string name)
        {
            return (DirectoryExists(name) || FileExists(name));
        }

        /// <summary>
        /// Test if a directory exists
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool DirectoryExists(string path)
        {
            if (path == null)
                return false;

            return path.Length < MaxDirPathLength ? Directory.Exists(path) : Pri.LongPath.Directory.Exists(path);
        }

        /// <summary>
        /// Test if a file exists
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static bool FileExists(string path)
        {
            if (path == null)
                return false;

            if (path.Length < MaxFilePathLength)
                return System.IO.File.Exists(path);
            else
                return Pri.LongPath.File.Exists(path);
        }

        /// <summary>
        /// Test if path is a symbolic link
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool IsSymbolicFile(string path)
        {
            if (path.Length < MaxFilePathLength) {
                var pathInfo = new FileInfo(path);
                return pathInfo.Attributes.HasFlag(FileAttributes.ReparsePoint);
            } else {
                var pathInfo = new Pri.LongPath.FileInfo(path);
                return pathInfo.Attributes.HasFlag(FileAttributes.ReparsePoint);
            }
        }

        /// <summary>
        /// Test if path is a symbolic link
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool IsSymbolicDir(string path)
        {
            if (path.Length < MaxFilePathLength) {
                System.IO.DirectoryInfo pathInfo = new System.IO.DirectoryInfo(path);
                return pathInfo.Attributes.HasFlag(FileAttributes.ReparsePoint);
            } else {
                Pri.LongPath.DirectoryInfo pathInfo = new Pri.LongPath.DirectoryInfo(path);
                return pathInfo.Attributes.HasFlag(FileAttributes.ReparsePoint);
            }
        }

        /// <summary>
        /// Compares too paths case-sensitive or case-insensitvely and returns true if they are same
        /// NEED MORE: Implementation is pretty simple. Should take care of drive letters.
        /// </summary>
        /// <param name="path1"></param>
        /// <param name="path2"></param>
        /// <param name="caseInsensitive"></param>
        /// <returns></returns>
        public static bool AreSame(string path1, string path2, bool caseInsensitive = true)
        {
            if (caseInsensitive) {
                path1 = path1.ToLower();
                path2 = path2.ToLower();
            }

            return path1 == path2;
        }

        /// <summary>
        /// Tests if file or folder exists in the list of directory items
        /// </summary>
        /// <param name="path"></param>
        /// <param name="dirContents"></param>
        /// <param name="caseInsensitive"></param>
        /// <param name="genuineDuplicate"></param>
        /// <param name="skipIndex"></param>
        /// <param name="originalPath"></param>
        /// <returns></returns>
        public static bool NameExists(string path,
            List<string> dirContents,
            bool caseInsensitive,
            out bool genuineDuplicate,
            int skipIndex = -1,
            string originalPath = null
        )
        {
            genuineDuplicate = false;

            if (dirContents == null)
                return false;

            for (int i = 0; i < dirContents.Count(); i++) {
                if (i == skipIndex)
                    continue;

                if (AreSame(path, dirContents[i], caseInsensitive)) {
                    if (AreSame(originalPath, dirContents[i], true)) {
                        genuineDuplicate = true;
                    }
                    
                    return true;
                }
            }

            return false;
        }


        /// <summary>
        /// Gets parent path to file or directory
        /// </summary>
        /// <param name="path"></param>
        /// <param name="isDir"></param>
        /// <returns></returns>
        public static string GetDirectoryPath(string path, bool isDir)
        {
            if (isDir) {
                return path.Length < MaxDirPathLength ? Path.GetDirectoryName(path) : Pri.LongPath.Path.GetDirectoryName(path);
            } else {
                return path.Length < MaxFilePathLength ? Path.GetDirectoryName(path) : Pri.LongPath.Path.GetDirectoryName(path);
            }
        }

        /// <summary>
        /// Get file's or folder's name
        /// </summary>
        /// <param name="path"></param>
        /// <param name="isDir"></param>
        /// <returns></returns>
        public static string GetFileName(string path, bool isDir)
        {
            if (isDir) {
                return path.Length < MaxDirPathLength ? Path.GetFileName(path) : Pri.LongPath.Path.GetFileName(path);
            } else {
                return path.Length < MaxFilePathLength ? Path.GetFileName(path) : Pri.LongPath.Path.GetFileName(path);
            }
        }

        /// <summary>
        /// Get file's or folder's name without extension
        /// </summary>
        /// <param name="path"></param>
        /// <param name="isDir"></param>
        /// <returns></returns>
        public static string GetFileNameWithoutExtension(string path, bool isDir)
        {
            if (isDir) {
                return path.Length < MaxDirPathLength
                    ? Path.GetFileNameWithoutExtension(path)
                    : Pri.LongPath.Path.GetFileNameWithoutExtension(path);
            } else {
                return path.Length < MaxFilePathLength
                    ? Path.GetFileNameWithoutExtension(path)
                    : Pri.LongPath.Path.GetFileNameWithoutExtension(path);
            }
        }

        /// <summary>
        /// Get the extension of a file or a folder
        /// </summary>
        /// <param name="path"></param>
        /// <param name="isDir"></param>
        /// <returns></returns>
        public static string GetExtension(string path, bool isDir)
        {
            if (isDir) {
                return path.Length < MaxDirPathLength
                    ? Path.GetExtension(path)
                    : Pri.LongPath.Path.GetExtension(path);
            } else {
                return path.Length < MaxFilePathLength ? Path.GetExtension(path) : Pri.LongPath.Path.GetExtension(path);
            }
        }

        /// <summary>
        /// Get Drive letter
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetPathRoot(string path)
        {
            return path.Length < MaxDirPathLength ? Path.GetPathRoot(path) : Pri.LongPath.Path.GetPathRoot(path);
        }

        /// <summary>
        /// Removes \ from the end of the path
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string PathWithoutPathSeparator(string path)
        {
            if (path.EndsWith(@"\"))
                path = path.Substring(0, path.Length - 1);
            return path;
        }
    }
}