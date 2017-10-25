﻿// Long Path Aware File Operations
// Uses Pri.LongPath library by Peter Richman

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using Pri.LongPath;

/// <summary>
/// FileNameNormalizer
/// </summary>
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
        private const int MAX_DIR_PATH_LENGTH = 248;
        private const int MAX_FILE_PATH_LENGTH = 260;

        /// <summary>
        /// Get Files Array (path strings)
        /// </summary>
        /// <param name="path"></param>
        /// <param name="pattern"></param>
        /// <returns></returns>
        /// 
        public static string[] GetFiles(string path, string pattern)
        {
            List<string> fileList = new List<string>(100);
            IEnumerable filesInDirectory;

            try {
                if (path.Length < MAX_DIR_PATH_LENGTH) {
                    // Normal Version
                    filesInDirectory = System.IO.Directory.EnumerateFiles(path, pattern, SearchOption.TopDirectoryOnly);
                    foreach (string file in filesInDirectory)
                        fileList.Add(file);
                } else {
                    // Long path version
                    filesInDirectory = Pri.LongPath.Directory.EnumerateFiles(path, pattern, SearchOption.TopDirectoryOnly);
                    foreach (string file in filesInDirectory)
                        fileList.Add(file);
                }
            } catch {
                Console.WriteLine("*** Error: Cannot Open Directory: {0:s}", path);
            }

            return fileList.ToArray();
        }

        /// <summary>
        /// Get SubDirectory Array
        /// </summary>
        /// <param name="directoryItem"></param>
        /// <returns></returns>
        public static string[] GetSubDirectories(string directoryItem)
        {
            IEnumerable subDirectories;
            List<string> dirList = new List<string>(20);

            try {
                if (directoryItem.Length < MAX_DIR_PATH_LENGTH) {
                    // Normal Version
                    subDirectories = System.IO.Directory.EnumerateDirectories(directoryItem, "*", SearchOption.TopDirectoryOnly);
                    foreach (string dir in subDirectories)
                        dirList.Add(dir);
                } else {
                    // Long path version
                    subDirectories = Pri.LongPath.Directory.EnumerateDirectories(directoryItem, "*", SearchOption.TopDirectoryOnly);
                    foreach (string dir in subDirectories)
                        dirList.Add(dir);
                }
            } catch {
                Console.WriteLine("*** Error: Cannot Open Directory: {0:s}", directoryItem);
            }
            return dirList.ToArray();
        }

        /// <summary>
        /// Rename File or Directory
        /// </summary>
        /// <param name="path"></param>
        /// <param name="newPath"></param>
        public static bool Rename(string path, string newPath)
        {
            try {
                if (path.Length < MAX_FILE_PATH_LENGTH)
                    System.IO.File.Move(path, newPath);
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

            if (path.Length < MAX_DIR_PATH_LENGTH)
                return System.IO.Directory.Exists(path);
            else
                return Pri.LongPath.Directory.Exists(path);
        }

        /// <summary>
        /// Test if a file exists
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool FileExists(string path)
        {
            if (path == null)
                return false;

            if (path.Length < MAX_FILE_PATH_LENGTH)
                return System.IO.File.Exists(path);
            else
                return Pri.LongPath.File.Exists(path);
        }

        /// <summary>
        /// Enumerates all files and directories. Not currently in use. Code from C# Cook Book
        /// Modified to use Pri.LongPath library
        /// </summary>
        /// <param name="dir"></param>
        /// <returns></returns>
        public static IEnumerable<Pri.LongPath.FileSystemInfo> GetAllFilesAndDirectories(string dir)
        {
            if (string.IsNullOrWhiteSpace(dir))
                throw new ArgumentException(nameof(dir));

            Pri.LongPath.DirectoryInfo dirInfo = new Pri.LongPath.DirectoryInfo(dir);
            Stack<Pri.LongPath.FileSystemInfo> stack = new Stack<Pri.LongPath.FileSystemInfo>();
            stack.Push(dirInfo);
            while (dirInfo != null || stack.Count > 0) {
                Pri.LongPath.FileSystemInfo fileSystemInfo = stack.Pop();
                if (fileSystemInfo is Pri.LongPath.DirectoryInfo subDirectoryInfo) {
                    yield return subDirectoryInfo;
                    foreach (Pri.LongPath.FileSystemInfo fsi in subDirectoryInfo.GetFileSystemInfos())
                        stack.Push(fsi);
                    dirInfo = subDirectoryInfo;
                } else {
                    yield return fileSystemInfo;
                    dirInfo = null;
                }
            }
        }

        /// <summary>
        /// Testing. Just displays all file items
        /// </summary>
        /// <param name="dir"></param>
        public static void DisplayAllFilesAndDirectories(string dir)
        {
            if (string.IsNullOrWhiteSpace(dir))
                throw new ArgumentException(nameof(dir));

            var strings = (from fileSystemInfo in GetAllFilesAndDirectories(dir)
                           select fileSystemInfo.ToString()).ToArray();

            Array.ForEach(strings, s => { Console.WriteLine(s); });

        }

        /// <summary>
        /// Test if path is a symbolic link
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool IsSymbolicFile(string path)
        {
            if (path.Length < MAX_FILE_PATH_LENGTH) {
                System.IO.FileInfo pathInfo = new System.IO.FileInfo(path);
                return pathInfo.Attributes.HasFlag(FileAttributes.ReparsePoint);
            } else {
                Pri.LongPath.FileInfo pathInfo = new Pri.LongPath.FileInfo(path);
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
            if (path.Length < MAX_FILE_PATH_LENGTH) {
                System.IO.DirectoryInfo pathInfo = new System.IO.DirectoryInfo(path);
                return pathInfo.Attributes.HasFlag(FileAttributes.ReparsePoint);
            } else {
                Pri.LongPath.DirectoryInfo pathInfo = new Pri.LongPath.DirectoryInfo(path);
                return pathInfo.Attributes.HasFlag(FileAttributes.ReparsePoint);
            }
        }

        /// <summary>
        /// Get the path without the last component
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string ExtractLastComponent(string path)
        {
            char[] delimiterChars = { '\\' };
            if (path.EndsWith(@"\"))
                path = path.Substring(0, path.Length - 1);
            string[] pathComponents = path.Split(delimiterChars);
            return pathComponents.Last();
        }
    }
}
