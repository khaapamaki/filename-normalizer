// File Name Normalizer
// Kati Haapamäki 2016-2017

// ToDo: parse . and .. from path
// Varoitut jos renamettu nimi aiheuttaa long pathin
// Testaa missä kohtaa .fcpcache feilaa ja koita catchata sen yli ??
// WHITE SPACE HEAD OR TRAIL, rename with option
// -"- näytä palku errorissa
// älä varoita pitkästä polusta kuin kerran (ei rekursiossa)



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
        private static bool _optionCaseInsensitive = false;
        private static bool _optionProcessFiles = true;
        private static bool _optionProcessDirs = true;
        private static bool _optionFixSpacesAll = false;
        private static bool _optionFixSpacesMandatory = false;
        private static bool _optionNormalize = true;
        private static bool _optionMacAware = true;
        private static List<string> _tooLongPaths;

        private static NormalizationForm _defaultNormalizationForm = NormalizationForm.FormC;
        private static NormalizationForm _optionNormalizationForm = _defaultNormalizationForm;

        private static List<string> _macPackages = new List<string> { ".fcpcache", ".app", ".framework", ".kext", ".plugin", ".docset", ".xpc", ".qlgenerator", ".component",
            ".mdimporter", ".bundle", ".lproj", ".nib", ".xib", ".download", ".rtfd", ".fcarch", ".pkg", ".dmg"
        };

        // not currently in use
        // useful stuff keep along with this project for future use
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

            Console.WriteLine("File and Folder Name Normalizer " + assemblyVersion + " beta");

            // Parse arguments. Sets options and extracts valid paths.
            string[] paths = ParseArguments(args);

            // Print help, if no valid path given
            if (paths.Length == 0) {
                Console.WriteLine("Usage:");
                Console.WriteLine("  fnamenorm <options> <path> [<path2>] [<path3>]\n");
                Console.WriteLine("Options:");
                Console.WriteLine("  /r            Recurses subdirectories");
                Console.WriteLine("  /rename       Normalizes and renames file and directory names when needed.");
                //Console.WriteLine("  /v            Verbose mode. Print out all files and folders in a tree");
                Console.WriteLine("  /formc        Performs Form C normalization. Default operation.");
                Console.WriteLine("  /formd        Performs Form D normalization. Reverse for Form C.");
                Console.WriteLine("  /c            Remames file and folder names that would be considered the same in a case-insensitive file systems.");
                Console.WriteLine("  /s            Fixes illegal folder names with trailing spaces.");
                Console.WriteLine("  /spaces       Fixes all file and folder names with leading and trailing spaces.");
                Console.WriteLine("  /nonorm       Bypass normalization.");
                //Console.WriteLine("  /d            Processes folder names only");
                //Console.WriteLine("  /f            Processes filenames only");
                Console.WriteLine("  /p=<pattern>  Search pattern for files, eg. *.txt");
                //Console.WriteLine("  /e            Show errors only.");
                Console.WriteLine("  /hex          Shows hex codes for files needing normalization");
                Console.WriteLine("");
                Console.WriteLine("Note:           Without /rename option only dry run is performed without actual renaming.");

#if DEBUG
                Console.ReadLine();
#else

#endif
                return; // No paths -> end program
            }

            // Process valid paths

            OpCounter counter = new OpCounter();
            _tooLongPaths = new List<string>(500);

            foreach (string sourcePath in paths) {
                string path = sourcePath;
                if (sourcePath.EndsWith(@"\")) {
                    //path = sourcePath.Substring(0, sourcePath.Length - 1);
                }
                if (FileOp.DirectoryExists(path)) {
                    if (!IsMacPackage(FileOp.GetFileName(path, isDir: true)) && !FileOp.IsSymbolicDir(path)) {
                        // Path is a dictory
                        if (_optionRename)
                            Console.WriteLine("*** Processing {0:s}", path);
                        else
                            Console.WriteLine("*** Checking {0:s}", path);
                        HandleDirectory(path, ref counter);
                    } else {
                        counter.SkippedDirectories++;
                    }
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

            _tooLongPaths.ForEach(x => Console.WriteLine("Long Path: " + x));
            Console.Write(counter.ToString());

#if DEBUG
            Console.ReadLine();
#else

#endif
        }

        /// <summary>
        /// Reads directory contents, processes files/directories and optionally recurses subdirectories
        /// </summary>
        /// <param name="sourcePath">Path to the directory to be processed</param>
        static void HandleDirectory(string sourcePath, ref OpCounter counter, bool noLongPathWarnings = false)
        {
            // Read directory contents
            bool canAccess = FileOp.GetFilesAndDirectories(sourcePath, _optionSearchPattern,
                out int numberOfFiles, false, out List<string> directoryContentsFilesFirst);
            bool pathShown = false;
            bool longPathFound = false;

            if (!canAccess) {
                counter.UnaccesableDirs++;
                Console.WriteLine("*** Cannot access directory: {0:s}", sourcePath);
                return;
            }

            for (int pos = 0; pos < directoryContentsFilesFirst.Count(); pos++) {

                bool isDir = pos >= numberOfFiles;
                string path = directoryContentsFilesFirst[pos];
                bool isPackage = false;
                string fileName = FileOp.GetFileName(path, isDir);
                string pathWithoutFileName = path.Substring(0, path.Length - fileName.Length);
                //string pathWithoutFileName = sourcePath; //FileOp.GetDirectoryPath(path, isDir); // SEPARATOR?
                bool needsRename = false;
                bool normalize = false;
                bool fixDuplicates = false;

                // long path detection
                if (noLongPathWarnings == false && !isDir) {
                    if (path.Length >= FileOp.MAX_DIR_PATH_LENGTH) {
                        longPathFound = true;
                    }
                }

                // macOS package detection. (folder names will be renamed like file names)
                if (isDir) {
                    if (IsMacPackage(FileOp.GetFileName(path, true))) {
                        isPackage = true;
                    }
                }

                if (_optionNormalize) {
                    normalize = !fileName.IsNormalized(_optionNormalizationForm);
                }

                if (_optionCaseInsensitive) {
                    fixDuplicates = HasCaseInsensitiveDuplicate(path, directoryContentsFilesFirst, isDir, startIndex: 0);
                }

                bool fixSpaces = false;
                if (_optionFixSpacesMandatory || _optionFixSpacesAll) {
                    fixSpaces = HasLeadingOrTrailingSpaces(path, isDir, _optionFixSpacesAll);
                }

                string prefix = GetReportingPrefix(isDir, normalize, fixDuplicates, fixSpaces);

                string newPath = path;

                bool createsDuplicate = false;

                if (fixDuplicates || fixSpaces || normalize) {
                    if (normalize) {
                        newPath = Normalize(newPath, _optionNormalizationForm, isDir);
                    }

                    newPath = GetUniqueName(newPath,
                        directoryContentsFilesFirst,
                        isDir, isPackage,
                        out createsDuplicate,
                        caseInsensitive: _optionCaseInsensitive,
                        removeSpaces: fixSpaces,
                        removeSpacesFull: _optionFixSpacesAll,

                        skipIndex: pos);
                    needsRename = true;
                }

                if (needsRename) {
                    if (!pathShown) {
                        Console.WriteLine("* " + sourcePath);
                        pathShown = true;
                    }
                    directoryContentsFilesFirst[pos] = newPath;
                    string fName = FileOp.GetFileName(path, isDir);
                    string newFName = FileOp.GetFileName(newPath, isDir);
                    Console.WriteLine($"    {prefix:s} \"{fName:s}\"  ==>  \"{newFName:s}\"");

                    bool renameFailed = false;

                    /// Rename part for filename fixing (not normalization)
                    /// 
                    if (_optionRename) {
                        if (FileOp.Rename(path, newPath, isDir)) {
                            if (normalize) {
                                if (isDir)
                                    counter.DirsNeedNormalizeRenamed++;
                                else
                                    counter.FilesNeedNormalizeRenamed++;
                            }

                            if (fixDuplicates) {
                                if (isDir)
                                    counter.DirsWithDuplicateNamesRenamed++;
                                else
                                    counter.FilesWithDuplicateNamesRenamed++;
                            }
                            if (fixSpaces) {
                                if (isDir)
                                    counter.DirsWithSpacesRenamed++;
                                else
                                    counter.FilesWithSpacesRenamed++;
                            }

                        } else {

                            renameFailed = true;
                            if (isDir) {
                                Console.WriteLine("*** Error: Cannot rename directory: {0:s}", path);
                            } else {
                                Console.WriteLine("*** Error: Cannot rename file: {0:s}", path);
                            }
                            if (normalize) {
                                if (isDir)
                                    counter.DirsNeedNormalizeFailed++;
                                else
                                    counter.FilesNeedNormalizeFailed++;
                            }
                            if (fixDuplicates) {
                                if (isDir)
                                    counter.DirsWithDuplicateNamesFailed++;
                                else
                                    counter.FilesWithDuplicateNamesFailed++;
                            }
                            if (fixSpaces) {
                                if (isDir)
                                    counter.DirsWithSpacesFailed++;
                                else
                                    counter.FilesWithSpacesFailed++;
                            }
                        }
                    }

                    if (normalize) {
                        if (isDir) {
                            counter.DirsNeedNormalize++;
                        } else {
                            counter.FilesNeedNormalize++;
                        }
                        if (createsDuplicate && !renameFailed) {
                            if (isDir) {
                                counter.DirsNeedNormalizeProducedDuplicate++;
                            } else {
                                counter.FilesNeedNormalizeProducedDuplicate++;
                            }
                        }
                        if (_optionHexDump) {
                            PrintHexFileName(FileOp.GetFileName(path, isDir));
                        }
                    }

                    if (fixDuplicates) {
                        if (isDir) {
                            counter.DirsWithDuplicateNames++;
                        } else {
                            counter.FilesWithDuplicateNames++;
                        }
                    }
                    if (fixSpaces) {
                        if (isDir) {
                            counter.DirsWithSpaces++;
                        } else {
                            counter.FilesWithSpaces++;
                        }
                        if (createsDuplicate && !renameFailed) {
                            if (isDir) {
                                counter.DirsWithSpacesProducedDuplicate++;
                            } else {
                                counter.FilesWithSpacesProducedDuplicate++;
                            }
                        }
                    }

                }
            }

            /// Free memory before a recursion takes place

            directoryContentsFilesFirst = null;
            //directoryContentsDirsFirst = null;

            if (longPathFound) {
                counter.TooLongDirPaths++;
                _tooLongPaths.Add(sourcePath);
            }

            /// Recursion part
            /// 
            if (_optionRecurse) {
                // reread subdirectories because some may have changed
                if (!FileOp.GetSubDirectories(sourcePath, out List<string> subDirectories))
                    counter.IOErrors++;

                foreach (string subDirectory in subDirectories) {
                    string dirName = FileOp.GetFileName(subDirectory, isDir: true);
                    bool tooLongPath = subDirectory.Length >= FileOp.MAX_FILE_PATH_LENGTH;
                    if (tooLongPath) {
                        //Console.WriteLine("*** Warning: Path too long for DIRECTORY ({0:g}): {1:s} ", subDirectory.Length, subDirectory);
                        //Console.WriteLine("*** Subsequent warnings in this path are supressed.");
                        counter.TooLongDirPaths++;
                        _tooLongPaths.Add(subDirectory);
                    }

                    // Recurse if recursion flag set
                    if (!IsMacPackage(dirName)) {
                        if (FileOp.DirectoryExists(subDirectory)) {
                            if (!FileOp.IsSymbolicDir(subDirectory))
                                HandleDirectory(subDirectory, ref counter, noLongPathWarnings: tooLongPath || noLongPathWarnings); // -> Recurse subdirectories
                            else {
                                Console.WriteLine("*** SymLink - not following: {0:s}", subDirectory);
                                counter.SkippedDirectories++;
                            }
                        } else {
                            Console.WriteLine("*** Error: Cannot Access Directory (After renaming): {0:s}", subDirectory);
                            counter.IOErrors++;
                        }
                    } else {
                        //Console.WriteLine($"*** skipped {subDirectory:s}");
                        counter.SkippedDirectories++;
                    }
                }
            }

            return;
        }


        static string GetReportingPrefix(bool isDir, bool normalize, bool fixDuplicates, bool fixSpaces)
        {
            string prefix = isDir ? "DIR:   " : "File:  ";

            if (normalize && fixSpaces && fixDuplicates) {
                prefix += "NORM+S+D  ";
            } else {
                if (normalize && fixDuplicates)
                    prefix += "NORM+DUPL ";
                else if (normalize && fixSpaces)
                    prefix += "NORM+SPCS ";

                else if (fixSpaces && fixDuplicates)
                    prefix += "SPCS+DUPL ";
                else if (fixSpaces)
                    prefix += "SPACES    ";
                else if (fixDuplicates)
                    prefix += "DUPLICATE ";
                else if (normalize)
                    prefix += "NORMALIZE ";
            }
            return prefix;
        }

        /// <summary>
        /// Adds 'Duplicate' suffix to the filename if needed and remove unwanted spaces (if asked)
        /// </summary>
        /// <param name="path"></param>
        /// <param name="dirContents"></param>
        /// <param name="isDir"></param>
        /// <returns></returns>
        private static string GetUniqueName(string path, List<string> dirContents, bool isDir, bool isPackage, out bool duplicate, bool caseInsensitive, bool removeSpaces, bool removeSpacesFull, int skipIndex = -1)
        {
            string fileName = FileOp.GetFileName(path, isDir);
            string pathWihtoutLastComponent = path.Substring(0, path.Count() - fileName.Count());
            string extension = FileOp.GetExtension(path, isDir);
            string baseName = FileOp.GetFileNameWithoutExtension(path, isDir);
            string testPath = path;
            string newBase = baseName;
            string dot = "";
            if (extension.StartsWith(".")) {
                extension = extension.Substring(1);
                dot = ".";
            }

            string newExt = extension;
            string newFolderName = fileName;

            pathWihtoutLastComponent = FileOp.PathWithoutPathSeparator(pathWihtoutLastComponent);

            if (removeSpaces) {
                if (!isDir) {
                    if (removeSpacesFull) {
                        newBase = baseName.Trim();
                        newExt = extension.Trim();
                        testPath = pathWihtoutLastComponent + @"\" + newBase + dot + newExt;
                    }
                    //else {
                    //    newBase = baseName.TrimStart();
                    //    newExt = extension.Trim();
                    //    testPath = pathWihtoutLastComponent + @"\" + newBase + dot + newExt;
                    //}
                } else {
                    if (removeSpacesFull) {
                        newFolderName = fileName.Trim();
                        testPath = pathWihtoutLastComponent + @"\" + newFolderName;
                    } else {
                        newFolderName = fileName.TrimEnd();
                        testPath = pathWihtoutLastComponent + @"\" + newFolderName;
                    }
                }
            }

            duplicate = false;
            int i = 1;
            while (FileOp.NameExists(testPath, dirContents, caseInsensitive, skipIndex)) {
                duplicate = true;
                string suffix;
                if (isDir) {
                    suffix = " [Duplicate Foldername]";
                } else {
                    suffix = " [Duplicate Filename]";
                }

                if (i != 1) {
                    if (isDir) {
                        suffix = $" [Duplicate Foldername ({i})]";
                    } else {
                        suffix = $" [Duplicate Filename ({i})]";
                    }
                }

                if (baseName != "") {
                    if (!isDir || isPackage) {
                        testPath = pathWihtoutLastComponent + @"\" + newBase + suffix + dot + newExt;
                    } else {
                        testPath = pathWihtoutLastComponent + @"\" + newBase + dot + newExt + suffix;
                    }
                } else {
                    testPath = pathWihtoutLastComponent + @"\" + newBase + dot + newExt + suffix;
                }
                i++;
            }
            return testPath;
        }

        /// <summary>
        /// Checks is file/folder has a duplicate name when compared case insensitively
        /// </summary>
        /// <param name="comparePath"></param>
        /// <param name="dirContents"></param>
        /// <returns></returns>
        static bool HasCaseInsensitiveDuplicate(string comparePath, List<string> dirContents, bool isDir, int startIndex = 0)
        {
            string compareName = FileOp.GetFileName(comparePath, isDir);

            for (int i = startIndex; i < dirContents.Count(); i++) {
                //foreach (string path in dirContents) {
                string filename = FileOp.GetFileName(dirContents[i], isDir);
                if (compareName.ToLower() == filename.ToLower() && compareName != filename)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="isDir"></param>
        /// <returns></returns>
        static bool HasLeadingOrTrailingSpaces(string path, bool isDir, bool full = false)
        {
            string basename = FileOp.GetFileNameWithoutExtension(path, isDir);
            string extension = FileOp.GetExtension(path, isDir);
            string dot = "";
            if (extension.StartsWith(".")) {
                extension = extension.Substring(1);
                dot = ".";
            }
            string fName = FileOp.GetFileName(path, isDir);
            if (!isDir) {
                if (full) {
                    if (basename.Trim() != basename || extension.Trim() != extension)
                        return true;
                    else
                        return false;
                } else {
                    return false;
                    //if (basename.TrimStart() != basename || extension.Trim() != extension)
                    //    return true;
                    //else
                    //    return false;
                }
            } else {
                if (full) {
                    if (fName.Trim() != fName)
                        return true;
                    else
                        return false;
                } else {
                    if (fName.TrimEnd() != fName)
                        return true;
                    else
                        return false;
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
        static string Normalize(string path, NormalizationForm form, bool isDir)
        {
            string fileName = FileOp.GetFileName(path, isDir);
            string pathWithoutFileName = path.Substring(0, path.Length - fileName.Length);
            string normalizedPath = path;
            string normalizedFileName = fileName;
            if (pathWithoutFileName.EndsWith(@"\"))
                pathWithoutFileName.Substring(0, pathWithoutFileName.Length - 1);

            normalizedFileName = fileName.Normalize(form);
            normalizedPath = pathWithoutFileName + @"\" + normalizedFileName;
            return normalizedPath;
        }

        [Obsolete]
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
            string fileName = FileOp.GetFileName(path, isDir);
            string pathWithoutFileName = path.Substring(0, path.Length - fileName.Length);
            bool normalizationNeeded = false;
            bool wasRenamed = false;
            string normalizedPath = path;
            string normalizedFileName = fileName;

            string prefix;
            if (isDir) prefix = "DIR:  "; else prefix = "File: ";
            prefix += "Normalize ";

            string suffix = "";
            if (isDir) suffix = @"\";

            // Do we need to normalize?
            if (!fileName.IsNormalized(form)) {
                normalizedFileName = fileName.Normalize(form);
                normalizedPath = pathWithoutFileName + normalizedFileName;

                if (FileOp.NameExists(normalizedPath, dirContents, caseInsensitive: _optionCaseInsensitive)) {
                    normalizedPath = GetUniqueName(normalizedPath,
                        dirContents,
                        isDir, false,
                        out bool dupl,
                        caseInsensitive: _optionCaseInsensitive,
                        removeSpaces: false,
                        removeSpacesFull: _optionFixSpacesAll);

                    normalizedFileName = FileOp.GetFileName(normalizedPath, isDir);
                    Console.WriteLine("{2:s}{0:s}{3:s} => {1:s}", path, normalizedFileName, prefix, suffix);
                    if (isDir) {
                        counter.DirsNeedNormalizeProducedDuplicate++;
                    } else {
                        counter.FilesNeedNormalizeProducedDuplicate++;
                    }
                } else {
                    // Show the normalization that will/would be made
                    if (!_optionPrintErrorsOnly)
                        Console.WriteLine("{2:s}{0:s}{3:s} => {1:s}", path, normalizedFileName, prefix, suffix);
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
                    Console.WriteLine("{1:s}{0:s}{2:s} ", path, prefix, suffix);
                normalizationNeeded = false;
            }


            /// Actual Renaming
            /// 
            if (normalizationNeeded && _optionRename) {
                bool succeded = FileOp.Rename(path, normalizedPath, isDir);
                if (succeded) {
                    if (isDir)
                        succeded = FileOp.DirectoryExists(normalizedPath);
                    else
                        succeded = FileOp.FileExists(normalizedPath);

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

        [Obsolete]
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

        static bool IsMacPackage(string dirName)
        {
            if (_optionMacAware) {
                foreach (string suffix in _macPackages) {
                    if (dirName.EndsWith(suffix))
                        return true;
                }
            }
            return false;
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
                // option /rename causes actual renaming
                if (lcaseArg == "/rename") {
                    _optionRename = true;
                }
                // option /formd = use Form D normalization instead of default Form C
                if (lcaseArg == "/formd") {
                    _optionNormalizationForm = NormalizationForm.FormD;
                    _optionNormalize = true;
                }
                // option /formc = use Form C normalization (default)
                if (lcaseArg == "/formc") {
                    _optionNormalizationForm = NormalizationForm.FormC;
                    _optionNormalize = true;
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
                // option /c handle case insensitive duplicates
                if (lcaseArg == "/c") {
                    _optionCaseInsensitive = true;
                }
                // option check trailing and leading spaces
                if (lcaseArg == "/s") {
                    _optionFixSpacesMandatory = true;
                }
                if (lcaseArg == "/spaces") {
                    _optionFixSpacesAll = true;
                }
                // option directories only
                if (lcaseArg == "/d") {
                    _optionProcessFiles = false;
                }
                if (lcaseArg == "/nonorm") {
                    _optionNormalize = false;
                }
                // option files only
                if (lcaseArg == "/f") {
                    if (_optionProcessFiles == false) {
                        // user has set both files and directories options at the same time
                        _optionProcessDirs = true;
                        _optionProcessFiles = true;
                    } else {
                        // normal behabior
                        _optionProcessDirs = false;
                    }
                }

                if (!arg.StartsWith("/")) {
                    string path = arg;
                    if (arg.EndsWith(@"\")) {
                        //path = path.Substring(0, path.Length - 1);
                        if (FileOp.DirectoryExists(path))
                            validPaths.Add(path);
                        else
                            Console.WriteLine("*** Error: Invalid path {0:s}", arg);
                    } else {
                        if (FileOp.FileOrDirectoryExists(arg))
                            validPaths.Add(arg);
                        else
                            Console.WriteLine("*** Error: Invalid path {0:s}", arg);
                    }
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
            char[] chars = fname.ToCharArray();
            for (int i = 0; i < fname.Length; i++) {
                Console.Write($"{(int)chars[i]:X}({chars[i]:s}) ");
            }
            Console.WriteLine($" [{fname.Length}]");
        }
    }
}
