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
using System.Reflection;
using System.Diagnostics;

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
            string assemblyVersion = Assembly.GetExecutingAssembly().GetName().Version.ToString();
            //string assemblyVersion = Assembly.LoadFile('your assembly file').GetName().Version.ToString();
            string fileVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion;
            //string productVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).ProductVersion;

            Console.WriteLine("File Name Normalizer " + assemblyVersion);

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
                Console.WriteLine("  /formc        Perform Form C normalization. Default operation.");
                Console.WriteLine("  /formd        Perform Form D normalization. Reverse for Form C.");
                Console.WriteLine("  /d            Looks for file names that would be considered the same in a case-insensitive file systems.");
                Console.WriteLine("  /p=<pattern>  Set search pattern for files, eg. *.txt");
                Console.WriteLine("  /e            Show errors only.");
                Console.WriteLine("  /hex          Show hex codes. Experimental. Ugly.");
                Console.WriteLine("");
                Console.WriteLine("Note:           Without /rename option incorrect names are only displayed.");

                Console.ReadLine();
                return; // No paths -> end program
            }

            // Process valid paths

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
                    Console.WriteLine("Processing a single file is not supported in the current version.");
                    continue;
                    //// Path is a file
                    //string normalizedPath = path;
                    //NormalizeIfNeeded(ref normalizedPath, ref counter, isDir: false);
                } else if (path != null) {
                    // Invalid path. Shouldn't occur since argument parser skips invalid paths.
                    Console.WriteLine("*** Error: Invalid path {0:s}", path);
                }
            }
            //if (_optionRename) {
            //    Console.WriteLine($"*** {old_counter} files normalized and/or renamed.");
            //} else {
            //    Console.WriteLine($"*** {old_counter} files needs normalization or renaming.");
            //}

            Console.Write(counter.ToString());
            Console.ReadLine();
            //Console.WriteLine("Done.");
        }

        /// <summary>
        /// Read directory contents, process files/directories and optionally recurse subdirectories
        /// </summary>
        /// <param name="sourcePath">Path to the directory to be processed</param>
        static void HandleDirectory(string sourcePath, ref OpCounter counter)
        {

            // Read rirectory contents
            List<string> subDirs = FileOp.GetSubDirectories(sourcePath);
            List<string> files = FileOp.GetFiles(sourcePath, _optionSearchPattern);
            List<string> directoryContents = FileOp.GetFilesAndDirectories(sourcePath, _optionSearchPattern);
            int numberOfSubDirs = subDirs.Count();
            int numberOfFiles = subDirs.Count();
            subDirs = null;

            // Handle all files in directory

            for (int i = 0; i < directoryContents.Count(); i++) {
                string path = directoryContents[i];
                bool isDir = i < numberOfSubDirs;
                bool didNormalize = false;
                if (isDir && Path.GetDirectoryName(path) == ".fcpcache") {
                    // skip
                } else {
                    if (FileOp.FileOrDirectoryExists(path)) {
                        string resultPath = path;
                        didNormalize = NormalizeIfNeeded(ref resultPath, ref counter, directoryContents, isDir: isDir);
                        directoryContents[i] = resultPath;
                    } else
                        Console.WriteLine("*** Error: Cannot Access {1:s}: {0:s}", path, isDir ? "Directory" : "File");
                    if (didNormalize && _optionHexDump)
                        PrintHexFileName(Path.GetFileName(path)); // For debugging
                }
            }

            // create a list of directory contents in files first order
            // 
            List<string> filesFirstDirContents = new List<string>(directoryContents);
            for (int i = numberOfSubDirs; i < directoryContents.Count(); i++) {
                filesFirstDirContents.Add(directoryContents[i]);
            }
            for (int i = 0; i < numberOfSubDirs; i++) {
                filesFirstDirContents.Add(directoryContents[i]);
            }

            if (_optionDuplicates) {
                string prefix;
                string suffix = "";
                for (int j = 0; j < filesFirstDirContents.Count(); j++) {
                    bool isDir = j >= numberOfFiles;
                    prefix = isDir ? "Dir: " : "File:";

                    string path = filesFirstDirContents[j];
                    if (HasCaseInsensitiveDuplicate(path, filesFirstDirContents)) {
                        string newPath = CreateUniqueNameForDuplicate(path, filesFirstDirContents, isDir: isDir);
                        if (isDir) {
                            counter.DirsWithDuplicateNames++;
                        } else {
                            counter.FilesWithDuplicateNames++;
                        }
                        if (_optionRename) {
                            if (FileOp.Rename(path, newPath)) {
                                directoryContents[j] = newPath;
                                Console.WriteLine("{0:s}{3:s} => {1:s} *** Duplicate", path, Path.GetFileName(newPath), prefix, suffix);
                                if (isDir) {
                                    counter.DirsWithDuplicateNamesRenamed++;
                                } else {
                                    counter.FilesWithDuplicateNamesRenamed++;
                                }

                            } else {
                                Console.WriteLine("*** Error: Cannot Rename {1:s}: {0:s}", path, isDir ? "Directory" : "File");
                                if (isDir) {
                                    counter.DirsWithDuplicateNamesFailed++;
                                } else {
                                    counter.FilesWithDuplicateNamesFailed++;
                                }
                            }
                        } else {
                            directoryContents[j] = newPath;
                            Console.WriteLine("{0:s}{3:s} => {1:s} *** Duplicate", path, Path.GetFileName(newPath), prefix, suffix);
                        }
                    }
                }
            }


            // Free memory before a recursion takes place
            subDirs = null;
            files = null;
            directoryContents = null;
            filesFirstDirContents = null;

            if (_optionRecurse) {
                // reread subdirectories because some may have changed
                List<string> subDirectories = FileOp.GetSubDirectories(sourcePath);

                // recurse
                foreach (string subDirectory in subDirectories) {
                    string dirName = Path.GetDirectoryName(subDirectory);

                    // Recurse if recursion flag set
                    if (dirName != ".fcpcache") // .fcpcache SPECIAL CASE for Valve Media Company!!!
                    {
                        if (FileOp.DirectoryExists(subDirectory)) {
                            if (!FileOp.IsSymbolicDir(subDirectory))
                                HandleDirectory(subDirectory, ref counter); // -> Recurse subdirectories
                            else
                                Console.WriteLine("*** SymLink - not following: {0:s}", subDirectory);
                        } else {
                            Console.WriteLine("*** Error: Cannot Access Directory (After Normalization): {0:s}", subDirectory);
                        }
                    } else {
                        Console.WriteLine("*** .fcpcache - skipped");
                    }
                }
            }


            return;
        }

        private static string CreateUniqueNameForDuplicate(string path, List<string> dirContents, bool isDir = false)
        {
            string originalPath = path.Substring(0, path.Count() - Path.GetFileName(path).Count());
            string originalFilename = Path.GetFileName(path);
            string extension = Path.GetExtension(path);
            string baseName = Path.GetFileNameWithoutExtension(path);
            string testPath = path;
            int i = 1;
            while (FileOp.NameExists(testPath, dirContents)) {
                string suffix;
                if (isDir) {
                    suffix = " [Duplicate Directoryname]";
                } else {
                    suffix = " [Duplicate Filename]";
                }

                if (i != 1) {
                    suffix = $" [Duplicate Filename ({i})]";
                    if (isDir) {
                        suffix = $" [Duplicate Directoryname ({i})]";
                    } else {
                        suffix = $" [Duplicate Filename ({i})";
                    }
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

        static bool HasCaseInsensitiveDuplicate(string comparePath, List<string> dirContents)
        {
            string compareName = Path.GetFileName(comparePath);

            foreach (string path in dirContents) {
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
        static bool NormalizeIfNeeded(ref string path, NormalizationForm form, ref OpCounter counter, List<string> dirContents, bool isDir = false)
        {

            string fileName = Path.GetFileName(path);
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

                if (FileOp.NameExists(normalizedPath, dirContents)) {
                    normalizedPath = CreateUniqueNameForDuplicate(normalizedPath, dirContents, isDir);
                    normalizedFileName = Path.GetFileName(normalizedPath);
                    Console.WriteLine("{0:s}{3:s} => {1:s} *** Duplicate", path, normalizedFileName, prefix, suffix);
                    if (isDir) {
                        counter.DirsNeedNormalizeProducedDuplicate++;
                    } else {
                        counter.FilesNeedNormalizeProducedDuplicate++;
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


            /// Print length warnings, not logical place for them here -> refactor
            /// 
            if ((isDir && path.Length >= FileOp.MAX_DIR_PATH_LENGTH) ||
                (!isDir && path.Length >= FileOp.MAX_FILE_PATH_LENGTH)
                ) {
                Console.WriteLine("*** Warning: Path too long ({0:g}): {1:s} ", path.Length, path);
                counter.TooLongPaths++;
            }
            if (!isDir) {
                string basename = Path.GetFileNameWithoutExtension(path);
                string extension = Path.GetExtension(path);
                if (basename.Trim() != basename || extension.Trim() != extension) {
                    Console.WriteLine("*** Warning: File name with leading or trailing spaces: \"{0:s}\" ", fileName);
                    if (isDir) {
                        counter.DirsWithSpaces++;
                    } else {
                        counter.FilesWithSpaces++;
                    }
                }
            } else {
                if (fileName.Trim() != fileName) {
                    Console.WriteLine("*** Warning: Directory name with leading or trailing spaces: \"{0:s}\" ", fileName);
                    if (isDir) {
                        counter.DirsWithSpaces++;
                    } else {
                        counter.FilesWithSpaces++;
                    }
                }
            }
            // Actual Renaming
            if (normalizationNeeded && _optionRename) {
                bool succeded = FileOp.Rename(path, normalizedPath);
                if (succeded) {
                    succeded = FileOp.FileOrDirectoryExists(normalizedPath);
                }
                wasRenamed = succeded;
                if (succeded) {
                    if (isDir) {
                        counter.DirsNeedNormalizeRenamed++;
                    } else {
                        counter.FilesNeedNormalizeRenamed++;
                    }
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
            if (wasRenamed || !_optionRename)
                path = normalizedPath;

            return _optionRename ? wasRenamed : normalizationNeeded;

        }

        /// <summary>
        /// Check if file or directory name needs normalization, and normalize if normalize option is set
        /// </summary>
        /// <param name="path">Path to be examined</param>
        /// <param name="isDir">Boolean flag that tells us wheter the path is a directory or a file</param>
        /// <returns>Return true if normalization is needed</returns>
        static bool NormalizeIfNeeded(ref string path, ref OpCounter counter, List<string> dirContents, bool isDir = false)
        {
            return NormalizeIfNeeded(ref path, _optionNormalizationForm, ref counter, dirContents, isDir);
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
