// File Name Normalizer
// v0.3.15
// Kati Haapamäki 2016

// ToDo: parse . and .. from path
// Testaa missä kohtaa .fcpcache feilaa ja koita catchata sen yli.


using System;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FileNameNormalizer
{
    /// <summary>
    /// A class for running programs as command line application
    /// </summary>
    class Program
    {
        // User options that are set by cmd line options
        private static bool _optionRecurse = false;
        private static bool _optionRename = false;
        private static bool _optionShowEveryFile = false;
        private static bool _optionPrintErrorsOnly = false;
        private static string _optionSearchPattern = "*";
        private static bool _optionHexDump = false;

        private static NormalizationForm _defaultNormalizationForm = NormalizationForm.FormC;
        private static NormalizationForm _optionNormalizationForm = _defaultNormalizationForm;

        // not currently in use
        const string acceptableCharacters = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZÁÀÂÄÃÅÇÉÈÊËÍÌÎÏÑÓÒÔÖÕÚÙÛÜŸabcdefghijklmnopqrstuvwxyzáàâäãåçéèêëíìîïñóòôöõúùûüÿ_ !@#£$€%&()[]{}'+-.,;§½";
        const string replacementCharacter = "_";

        /// <summary>
        /// Reads options and paths from command line arguments, and process all accessible paths
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            Console.WriteLine("File Name Normalizer 0.3.15");

            // Parse arguments. Sets options and extracts valid paths.
            string[] paths = ParseArguments(args);

            // Print help, if no valid path given
            if (paths.Length == 0)
            {
                Console.WriteLine("Usage:");
                Console.WriteLine("  fnamenorm <options> <path> [<path2>] [<path3>]\n");
                Console.WriteLine("Options:");
                Console.WriteLine("  /r            Recurse subdirectories");
                Console.WriteLine("  /normalize    Normalize and rename incorrect file and directory names.");
                Console.WriteLine("                Without this option performs dry run and incorrect names are only printed out.");
                Console.WriteLine("  /v            Verbose mode. Print out all scanned items");
                Console.WriteLine("  /formd        Perform Form D normalization instead of default Form C");
                Console.WriteLine("  /p=<pattern>  Set search pattern for files, eg. *.txt");
                Console.WriteLine("  /e            Show only errors");

                return; // No paths -> end program
            }

            // Process valid paths
            foreach (string path in paths)
            {
                if (FileOp.DirectoryExists(path))
                {
                    // Path is a dictory
                    if (_optionRename)
                        Console.WriteLine("*** Processing {0:s}", path);
                    else
                        Console.WriteLine("*** Checking {0:s}", path);
                    HandleDirectory(path);
                }
                else if (FileOp.FileExists(path))
                {
                    // Path is a file
                    NormalizeIfNeeded(path, isDir: false);
                }
                else if (path != null)
                {
                    // Invalid path. Shouldn't occur since argument parser skips invalid paths.
                    Console.WriteLine("*** Error: Invalid Path {0:s}", path);
                }
            }
            Console.WriteLine("Done.");
        }

        /// <summary>
        /// Read directory contents, process files/directories and optionally recurse subdirectories
        /// </summary>
        /// <param name="directoryItem">Path to the directory to be processed</param>
        static void HandleDirectory(string directoryItem)
        {
            // Read rirectory contents
            string[] files = FileOp.GetFiles(directoryItem, _optionSearchPattern);
            string[] subDirectories = FileOp.GetSubDirectories(directoryItem);

            // Handle all files in directory
            foreach (string path in files)
            {
                if (FileOp.FileExists(path))
                    NormalizeIfNeeded(path, isDir: false);
                else
                    Console.WriteLine("*** Error: Cannot Access File: {0:s}", path);
                if (_optionHexDump)
                    printHexFileName(FileOp.ExtractLastComponent(path)); // For debugging
            }

            // Free some memory before going into sub directories to leave more space in stack
            files = null;

            // Handle all subdirectories and recurse if recurse option is set 
            foreach (string path in subDirectories)
            {
                string fileName = FileOp.ExtractLastComponent(path);

                if (FileOp.DirectoryExists(path))
                {
                    string pathAfterNormalization = path;
                    pathAfterNormalization = NormalizeIfNeeded(path, isDir: true);

                    // .fcpcache SPECIAL CASE for nasty Final Cut Pro X symlinks
                    if (fileName == ".fcpcache")
                        Console.WriteLine("*** .fcpcache - skipped");

                    // Recurse if recursion flag set
                    if (_optionRecurse && fileName != ".fcpcache") // .fcpcache SPECIAL CASE for Valve Media Company!!!
                    {
                        if (FileOp.DirectoryExists(pathAfterNormalization))
                        {
                            if (!FileOp.IsSymbolicDir(pathAfterNormalization))
                                HandleDirectory(pathAfterNormalization); // -> Recurse subdirectories
                            else
                                Console.WriteLine("*** SymLink - not following: {0:s}", pathAfterNormalization);
                        }
                        else
                        {
                            Console.WriteLine("*** Error: Cannot Access Directory (After Normalization): {0:s}", pathAfterNormalization);
                        }
                    }
                }
                else
                {
                    Console.WriteLine("*** Error: Cannot Access Directory: {0:s}", path);
                    if (_optionHexDump)
                        printHexFileName(fileName); // For debugging
                }
            }
        }

        /// <summary>
        /// Check if file or directory name needs normalization, and normalize if normalize
        /// option is set
        /// </summary>
        /// <param name="path">Path to be examined</param>
        /// <param name="isDir">Boolean flag that tells us wheter the path is a directory or
        /// a file</param>
        /// <param name="form">Form C (default), Form D</param>
        /// <returns>
        /// Return true if normalization is needed
        /// </returns>
        static string NormalizeIfNeeded(string path, bool isDir, NormalizationForm form)
        {
            char[] delimiterChars = { '\\' };
            string[] pathComponents = path.Split(delimiterChars);
            string fileName = pathComponents.Last();
            string pathWithoutFileName = path.Substring(0, path.Length - fileName.Length);
            bool normalizationNeeded = false;
            bool wasRenamed = false;
            string normalizedPath = path;
            string normalizedFileName = fileName;

            string prefix;
            if (isDir) prefix = "Dir: "; else prefix = "File:";
            string suffix = "";
            if (isDir) suffix = @"\";

            // Do we need to normalize?
            if (!fileName.IsNormalized(form))
            {
                normalizedFileName = fileName.Normalize(form);
                normalizedPath = pathWithoutFileName + normalizedFileName;

                // Handle duplicate file/dir names
                if (FileOp.FileOrDirectoryExists(normalizedPath))
                {
                    if (isDir)
                    {
                        while (FileOp.DirectoryExists(normalizedPath))
                        {
                            normalizedPath += " (Duplicate Name)";
                            normalizedFileName += " (Duplicate Name)";
                        }
                        Console.WriteLine("{0:s}{3:s} => {1:s} *** Duplicate", path, normalizedFileName, prefix, suffix);
                    }
                    else
                    {
                        // File already exists with the same name? Add suffix to the new filename
                        while (FileOp.FileExists(normalizedPath))
                        {
                            normalizedPath += " (Duplicate Name)";
                            normalizedFileName += " (Duplicate Name)";
                        }
                        Console.WriteLine("{0:s}{3:s} => {1:s} *** Duplicate", path, normalizedFileName, prefix, suffix);
                    }
                }
                else
                {
                    // Show the normalization that will/would be made
                    if (!_optionPrintErrorsOnly)
                        Console.WriteLine("{0:s}{3:s} => {1:s}", path, normalizedFileName, prefix, suffix);
                }
                normalizationNeeded = true;
            }
            else
            // Normalization not needed
            {
                // In verbose mode, this will show correct files too
                if (_optionShowEveryFile && !_optionPrintErrorsOnly)
                    Console.WriteLine("{0:s}{2:s} ", path, prefix, suffix);
                normalizationNeeded = false;
            }

            // Print warnings, not logical place for them here -> refactor
            //if (isDir && path.Length >= MAX_DIR_PATH_LENGTH)
            //    Console.WriteLine("*** Warning: Path too long ({0:g}): {1:s} ", path.Length, path);

            //if (!isDir && path.Length >= MAX_FILE_PATH_LENGTH)
            //    Console.WriteLine("*** Warning: Path too long ({0:g}): {1:s} ", path.Length, path);

            if (fileName.EndsWith(" "))
                Console.WriteLine("*** Warning: Name ends with space: \"{0:s}\" ", fileName);

            // Actual Renaming
            if (normalizationNeeded && _optionRename)
            {
                FileOp.Rename(path, normalizedPath);

                if (FileOp.FileOrDirectoryExists(normalizedPath))
                {
                    wasRenamed = true;
                    if (!_optionPrintErrorsOnly)
                        if (isDir)
                            Console.WriteLine("Renaming Directory Succeeded");
                        else
                            Console.WriteLine("Renaming Succeeded");
                }
                else
                {
                    if (isDir)
                        Console.WriteLine("*** Error: Renaming Directory Failed");
                    else
                        Console.WriteLine("*** Error: Renaming Failed");
                }

            }
            if (wasRenamed)
                return normalizedPath;
            else
                return path;
        }

        /// <summary>
        /// Check if file or directory name needs normalization, and normalize if normalize option is set
        /// </summary>
        /// <param name="path">Path to be examined</param>
        /// <param name="isDir">Boolean flag that tells us wheter the path is a directory or a file</param>
        /// <returns>Return true if normalization is needed</returns>
        static string NormalizeIfNeeded(string path, bool isDir)
        {
            return NormalizeIfNeeded(path, isDir, _optionNormalizationForm);
        }

        /// <summary>
        /// Parses command line arguments
        /// Sets options and reads accessible paths from argument array.
        /// </summary>
        /// <param name="args">Argument array as it is provided to Main</param>
        /// <returns>Array of accessible paths</returns>
        internal static string[] ParseArguments(string[] args)
        {
            List<string> validPaths = new List<string>(5);

            foreach (string arg in args)
            {
                string lcaseArg = arg.ToLower();
                // option /r = recurse
                if (lcaseArg == "/r")
                {
                    _optionRecurse = true;
                }
                // option /normalize = normalize
                if (lcaseArg == "/normalize")
                {
                    _optionRename = true;
                }
                // option /formd = use Form D normalization instead of default Form C
                if (lcaseArg == "/formd")
                {
                    _optionNormalizationForm = NormalizationForm.FormD;
                }
                // option /formc = use Form C normalization (default)
                if (lcaseArg == "/formc")
                {
                    _optionNormalizationForm = NormalizationForm.FormC;
                }
                // option /v = verbose, show all processed files and folders
                if (lcaseArg == "/v")
                {
                    _optionShowEveryFile = true;
                }
                // option /e, print errors only
                if (lcaseArg == "/e")
                {
                    _optionPrintErrorsOnly = true;
                }
                // option /p=<pattern>, search pattern for files
                if (lcaseArg.StartsWith("/p="))
                {
                    _optionSearchPattern = arg.Substring(3);
                }
                if (lcaseArg.StartsWith("/hex"))
                {
                    _optionHexDump = true;
                }
                // Collect all valid paths
                if (FileOp.FileOrDirectoryExists(arg))
                    validPaths.Add(arg);

                else if (!arg.StartsWith("/"))
                {
                    Console.WriteLine("*** Error: Invalid path {0:s}", arg);
                }
            }
            return validPaths.ToArray();
        }

        /// <summary>
        /// Print Name in Hexadecimal. For Debuging.
        /// </summary>
        /// <param name="fname"></param>
        internal static void printHexFileName(string fname)
        {
            Console.WriteLine(fname.Length);
            char[] chars = fname.ToCharArray();
            for (int i = 0; i < fname.Length; i++)
            {
                Console.Write("{0:s}={1:x} ", chars[i], (int)chars[i]);
            }
            Console.WriteLine("");
        }
    }
}
