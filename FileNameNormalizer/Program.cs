// File Name Normalizer
// v0.3.15
// Kati Haapamäki 2016-2017

// ToDo: parse . and .. from path
// Testaa missä kohtaa .fcpcache feilaa ja koita catchata sen yli.
// WHITE SPACE HEAD OR TRAIL!

using System;
using System.Text;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;

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
        private static bool _optionDuplicates = false;

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
            Console.WriteLine("File Name Normalizer 0.4.0");

            // Parse arguments. Sets options and extracts valid paths.
            string[] paths = ParseArguments(args);

            // Print help, if no valid path given
            if (paths.Length == 0) {
                Console.WriteLine("Usage:");
                Console.WriteLine("  fnamenorm <options> <path> [<path2>] [<path3>]\n");
                Console.WriteLine("Options:");
                Console.WriteLine("  /r            Recurse subdirectories");
                Console.WriteLine("  /rename       Normalize and rename incorrect file and directory names.");
                Console.WriteLine("  /v            Verbose mode. Print out all scanned items");
                Console.WriteLine("  /formc        Perform Form C normalization. Default.");
                Console.WriteLine("  /formd        Perform Form D normalization. Reverse for Form C.");
                Console.WriteLine("  /d            Looks for file names that would be considered the same in a case-insensitive file systems.");
                Console.WriteLine("  /p=<pattern>  Set search pattern for files, eg. *.txt");
                Console.WriteLine("  /e            Show errors only.");
                Console.WriteLine("  /hex          Show hex codes.");
                Console.WriteLine("");
                Console.WriteLine("Note:           Without /rename option incorrect names are only displayed.");

                return; // No paths -> end program
            }

            // Process valid paths
            int old_counter = 0;
            OpCounter counter = new OpCounter();

            foreach (string path in paths) {
                if (FileOp.DirectoryExists(path)) {
                    // Path is a dictory
                    if (_optionRename)
                        Console.WriteLine("*** Processing {0:s}", path);
                    else
                        Console.WriteLine("*** Checking {0:s}", path);
                    HandleDirectory(path, ref counter);
                } else if (FileOp.FileExists(path)) {
                    // Path is a file
                    string normalizedPath = path;
                    NormalizeIfNeeded(ref normalizedPath, ref counter, isDir: false);
                } else if (path != null) {
                    // Invalid path. Shouldn't occur since argument parser skips invalid paths.
                    Console.WriteLine("*** Error: Invalid Path {0:s}", path);
                }
            }
            //if (_optionRename) {
            //    Console.WriteLine($"*** {old_counter} files normalized and/or renamed.");
            //} else {
            //    Console.WriteLine($"*** {old_counter} files needs normalization or renaming.");
            //}

            Console.Write(counter.ToString());
            Console.WriteLine("Done.");
        }

        /// <summary>
        /// Read directory contents, process files/directories and optionally recurse subdirectories
        /// </summary>
        /// <param name="directoryItem">Path to the directory to be processed</param>
        static int HandleDirectory(string directoryItem, ref OpCounter counter)
        {
            int old_counter = 0;

            // Read rirectory contents
            string[] files = FileOp.GetFiles(directoryItem, _optionSearchPattern);
            List<string> normalizedFiles = new List<string>(files.Count());

            string[] subDirectories = FileOp.GetSubDirectories(directoryItem);

            // Ensin tsekattava diricat jotta saadaan normalized listalle

            // Handle all files in directory
            foreach (string path in files) {
                if (FileOp.FileExists(path)) {
                    string resultPath = path;
                    NormalizeIfNeeded(ref resultPath, ref counter, false, normalizedFiles);
                    normalizedFiles.Add(resultPath);
                } else
                    Console.WriteLine("*** Error: Cannot Access File: {0:s}", path);
                if (_optionHexDump)
                    PrintHexFileName(Path.GetFileName(path)); // For debugging
            }

            // INSERT DUPLICATE CHECK HERE <----------------------------------------

            if (_optionDuplicates) {
                string prefix = "File:";
                string suffix = "";
                for (int i = 0; i < normalizedFiles.Count(); i++) {
                    string path = normalizedFiles[i];
                    string filename = Path.GetFileName(path);
                    if (HasCaseInsensitiveDuplicate(path, normalizedFiles)) {
                        string newPath = CreateUniqueNameForDuplicate(path);
                        counter.FilesWithDuplicateNames++;
                        if (_optionRename) {
                            if (FileOp.Rename(path, newPath)) {
                                normalizedFiles[i] = newPath;
                                Console.WriteLine("{0:s}{3:s} => {1:s} *** Duplicate", path, Path.GetFileName(newPath), prefix, suffix);
                                counter.FilesWithDuplicateNamesRenamed++;
                            } else {
                                Console.WriteLine("*** Error: Cannot Rename File: {0:s}", path);
                                counter.FilesWithDuplicateNamesFailed++;
                            }
                        } else {
                            normalizedFiles[i] = newPath;
                            Console.WriteLine("{0:s}{3:s} => {1:s} *** Duplicate", path, Path.GetFileName(newPath), prefix, suffix);
                        }
                    }
                }
            }

            // Free some memory before going into sub directories to leave more space in stack
            files = null;

            // Handle all subdirectories and recurse if recurse option is set 
            foreach (string path in subDirectories) {
                string fileName = Path.GetFileName(path);

                if (FileOp.DirectoryExists(path)) {
                    string pathAfterNormalization = path;
                    old_counter += NormalizeIfNeeded(ref pathAfterNormalization, ref counter, isDir: true) ? 1 : 0;

                    // .fcpcache SPECIAL CASE for nasty Final Cut Pro X symlinks
                    if (fileName == ".fcpcache")
                        Console.WriteLine("*** .fcpcache - skipped");

                    // Recurse if recursion flag set
                    if (_optionRecurse && fileName != ".fcpcache") // .fcpcache SPECIAL CASE for Valve Media Company!!!
                    {
                        if (FileOp.DirectoryExists(pathAfterNormalization)) {
                            if (!FileOp.IsSymbolicDir(pathAfterNormalization))
                                HandleDirectory(pathAfterNormalization, ref counter); // -> Recurse subdirectories
                            else
                                Console.WriteLine("*** SymLink - not following: {0:s}", pathAfterNormalization);
                        } else {
                            Console.WriteLine("*** Error: Cannot Access Directory (After Normalization): {0:s}", pathAfterNormalization);
                        }
                    }
                } else {
                    Console.WriteLine("*** Error: Cannot Access Directory: {0:s}", path);

                    //if (_optionHexDump)
                    //    PrintHexFileName(fileName); // For debugging
                }
            }

            return old_counter;
        }

        private static string CreateUniqueNameForDuplicate(string path, bool isDir = false)
        {
            string originalPath = path.Substring(0, path.Count() - Path.GetFileName(path).Count());
            string originalFilename = Path.GetFileName(path);
            string extension = Path.GetExtension(path);
            string baseName = Path.GetFileNameWithoutExtension(path);
            string testPath = path;
            int i = 1;
            while (FileOp.FileOrDirectoryExists(testPath)) {
                string suffix = " [Duplicate File Name]";
                if (i != 1) {
                    suffix = $" [Duplicate File Name ({i})]";
                }
                if (baseName != "") {
                    if (!isDir) {
                        testPath = originalPath + baseName + suffix + extension;
                    } else {
                        testPath = originalPath + baseName + extension + suffix;
                    }
                } else {
                    testPath = originalPath + baseName + extension + suffix;
                }
                i++;
            }
            return testPath;
        }

        static bool HasCaseInsensitiveDuplicate(string comparePath, List<string> paths)
        {
            string compareName = Path.GetFileName(comparePath);

            foreach (string path in paths) {
                string filename = Path.GetFileName(path);
                if (compareName.ToLower() == filename.ToLower() && compareName != filename)
                    return true;
            }
            return false;
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
        static bool NormalizeIfNeeded(ref string path, NormalizationForm form, ref OpCounter counter, bool isDir, List<string> dirContents = null)
        {
            //testing

            //string test = CreateUniqueNameForDuplicate(path);
            //testing ends

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
            if (!fileName.IsNormalized(form)) {
                normalizedFileName = fileName.Normalize(form);
                normalizedPath = pathWithoutFileName + normalizedFileName;

                // Handle duplicate file/dir names

                // KORJAA. TARKISTA dirContentsista !!!!!!!!!!!!!!!!

                if (FileOp.FileOrDirectoryExists(normalizedPath)) {
                    normalizedPath = CreateUniqueNameForDuplicate(normalizedPath, isDir);
                    normalizedFileName = Path.GetFileName(normalizedPath);
                    Console.WriteLine("{0:s}{3:s} => {1:s} *** Duplicate", path, normalizedFileName, prefix, suffix);
                    if (isDir) {
                        counter.DirsWithDuplicateNamesCreated++;
                    } else {
                        counter.FilesWithDuplicateNamesCreated++;
                    }
                } else {
                    // Show the normalization that will/would be made
                    if (!_optionPrintErrorsOnly)
                        Console.WriteLine("{0:s}{3:s} => {1:s}", path, normalizedFileName, prefix, suffix);
                }
                normalizationNeeded = true;
                if (isDir) {
                    counter.DirsNeedNormalize++;
                } else {
                    counter.FilesNeedNormalize++;
                }
            } else {
                // Normalization not needed
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

            if (fileName.EndsWith(" ")) {
                Console.WriteLine("*** Warning: Name ends with space: \"{0:s}\" ", fileName);
                if (isDir) {
                    counter.DirsWithSpaces++;
                } else {
                    counter.FilesWithSpaces++;
                }
            }

            // Actual Renaming
            if (normalizationNeeded && _optionRename) {
                FileOp.Rename(path, normalizedPath);

                if (FileOp.FileOrDirectoryExists(normalizedPath)) {
                    wasRenamed = true;
                    if (isDir) {
                        counter.DirsNeedNormalizeRenamed++;
                    } else {
                        counter.FilesNeedNormalizeRenamed++;
                    }
                    //if (!_optionPrintErrorsOnly) {
                    //    if (isDir)
                    //        Console.WriteLine("Renaming Directory Succeeded");
                    //    else
                    //        Console.WriteLine("Renaming Succeeded");
                    //}
                } else {
                    if (isDir) {
                        Console.WriteLine("*** Error: Renaming Directory Failed");
                        counter.DirsNeedNormalizeFailed++;
                    } else {
                        Console.WriteLine("*** Error: Renaming Failed");
                        counter.FilesNeedNormalizeFailed++;
                    }
                }

            }
            if (wasRenamed)
                path = normalizedPath;

            return _optionRename ? wasRenamed : normalizationNeeded;

        }

        /// <summary>
        /// Check if file or directory name needs normalization, and normalize if normalize option is set
        /// </summary>
        /// <param name="path">Path to be examined</param>
        /// <param name="isDir">Boolean flag that tells us wheter the path is a directory or a file</param>
        /// <returns>Return true if normalization is needed</returns>
        static bool NormalizeIfNeeded(ref string path, ref OpCounter counter, bool isDir, List<string> dirContents = null)
        {
            return NormalizeIfNeeded(ref path, _optionNormalizationForm, ref counter, isDir, dirContents);
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

            foreach (string arg in args) {
                string lcaseArg = arg.ToLower();
                // option /r = recurse
                if (lcaseArg == "/r") {
                    _optionRecurse = true;
                }
                // option /normalize = normalize
                if (lcaseArg == "/rename") {
                    _optionRename = true;
                }
                // option /formd = use Form D normalization instead of default Form C
                if (lcaseArg == "/formd") {
                    _optionNormalizationForm = NormalizationForm.FormD;
                }
                // option /formc = use Form C normalization (default)
                if (lcaseArg == "/formc") {
                    _optionNormalizationForm = NormalizationForm.FormC;
                }
                // option /v = verbose, show all processed files and folders
                if (lcaseArg == "/v") {
                    _optionShowEveryFile = true;
                }
                // option /e, print errors only
                if (lcaseArg == "/e") {
                    _optionPrintErrorsOnly = true;
                }
                // option /p=<pattern>, search pattern for files
                if (lcaseArg.StartsWith("/p=")) {
                    _optionSearchPattern = arg.Substring(3);
                }
                if (lcaseArg == "/hex") {
                    _optionHexDump = true;
                }
                if (lcaseArg == "/d") {
                    _optionDuplicates = true;
                }
                // Collect all valid paths
                if (FileOp.FileOrDirectoryExists(arg))
                    validPaths.Add(arg);

                else if (!arg.StartsWith("/")) {
                    Console.WriteLine("*** Error: Invalid path {0:s}", arg);
                }
            }
            return validPaths.ToArray();
        }

        /// <summary>
        /// Print Name in Hexadecimal. For Debuging.
        /// </summary>
        /// <param name="fname"></param>
        internal static void PrintHexFileName(string fname)
        {
            Console.WriteLine(fname.Length);
            char[] chars = fname.ToCharArray();
            for (int i = 0; i < fname.Length; i++) {
                Console.Write("{0:s}={1:x} ", chars[i], (int)chars[i]);
            }
            Console.WriteLine("");
        }
    }
}
