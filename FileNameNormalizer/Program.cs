// File Name Normalizer
// Kati Haapamäki 2016-2017

// ToDo: parse . and .. from path
// Varoitus jos renamettu nimi aiheuttaa long pathin
// case sensivite mode ei ehkä toimi duplikaattien kanssa?
// mitä tapahtuu jos filename päättyy pisteeseen ext=""
// raporttiin fixatut



using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

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
        private static bool _optionCaseInsensitive = true;
        private static bool _optionFixDuplicates = false;
        private static bool _optionProcessFiles = true;
        private static bool _optionProcessDirs = true;
        private static bool _optionNormalize = false;
        private static bool _optionMacAware = true;
        private static bool _optionDumpLongPaths = false;
        private static bool _optionShowHelp = false;
        private static bool _optionFixIllegalChars = false;
        private static List<string> _tooLongPaths;
        private static TrimOptions _optionTrimOptions = TrimOptions.None;

        private static NormalizationForm _defaultNormalizationForm = NormalizationForm.FormC;
        private static NormalizationForm _optionNormalizationForm = _defaultNormalizationForm;

        private static List<string> _macPackages = new List<string> { ".fcpcache", ".app", ".framework", ".kext", ".plugin", ".docset", ".xpc", ".qlgenerator", ".component",
            ".mdimporter", ".bundle", ".lproj", ".nib", ".xib", ".download", ".rtfd", ".fcarch", ".pkg", ".dmg"
        };

        //private static List<int> illegalCharCodes = new List<int> { 0xF029, 0x2DC, 0xF020, 0xF021, 0xF022, 0xF023, 0xF024, 0xF025, 0xF026, 0xF027, 0xF028 };
        private static char[] dotTrimChars = { ' ', '.', '\xF029' };
        private static List<char> illegalChars = new List<char> { '\x2DC', '\xF020', '\xF021', '\xF022', '\xF023', '\xF024', '\xF025', '\xF026', '\xF027',
                                                                    '<', '>', '|', '\\', '/', '\"', '*', '?', '~', ':', '\xF028', '\xF029',};


        // not currently in use
        // useful stuff keep along with this project for future use
        const string acceptableCharacters = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZÁÀÂÄÃÅÇÉÈÊËÍÌÎÏÑÓÒÔÖÕÚÙÛÜŸabcdefghijklmnopqrstuvwxyzáàâäãåçéèêëíìîïñóòôöõúùûüÿ_ !@#£$€%&()[]{}'+-.,;§½";
        const char replacementCharacter = '_';

        private enum TrimOptions
        {
            FileBaseLeft = 0b00000001,
            FileBaseRight = 0b00000010,
            FileBase = 0b00000011,
            FileExtLeft = 0b00000100,
            FileExtRight = 0b00001000,
            FileExt = 0b00001100,
            DirLeft = 0b00010000,
            DirRight = 0b00100000,
            Dir = 0b00110000,
            None = 0
        }

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

            _optionTrimOptions = TrimOptions.None;

            // Parse arguments. Sets options and extracts valid paths.
            string[] paths = ParseArguments(args);

            // Print help, if no valid path given
            if (args.Length == 0 || _optionShowHelp) {
                Console.WriteLine("Usage:");
                Console.WriteLine("  fnamenorm <options> <path> [<path2>] [<path3>]\n");
                Console.WriteLine("Options:");
                Console.WriteLine("  /r            Recurses subdirectories");
                Console.WriteLine("  /nc           Performs Form C normalization. Normal operation.");
                Console.WriteLine("  /nd           Performs Form D normalization. Reverse for Form C.");
                //Console.WriteLine("  /nonorm       Bypass all normalization.");
                Console.WriteLine("  /b,/dup       Renames file and folder names that would have a duplicate name in a case-insensitive file system.");
                Console.WriteLine("                This is effective only when scanning case sensitive file system.");
                Console.WriteLine("  /t            Trims illegal folder names with trailing spaces. Same as option /t=dirright");
                Console.WriteLine("  /t=all        Trims all file and folder names with leading and trailing spaces.");
                Console.WriteLine("  /t=opt1,opt2  Specific trim instructions: base, ext, dir, baseleft, baseright, extleft, extright, dirleft, dirright.");
                Console.WriteLine("  /d            Processes folder names only");
                Console.WriteLine("  /f            Processes filenames only");
                Console.WriteLine("  /p=<pattern>  Search pattern for files, eg. *.txt");
                Console.WriteLine("  /rename       Does actual renaming instead of displaying info about what should be done.");
                Console.WriteLine("  /v            Verbose mode. Print out all files and folders in a tree");
                Console.WriteLine("  /l            Detailed report about long paths.");
                //Console.WriteLine("  /e            Show errors only.");
                Console.WriteLine("  /hex          Shows hex codes for file and folder names");
                Console.WriteLine("");
                Console.WriteLine("Note:           Without /rename option only dry run is performed without actual renaming.");

#if DEBUG
                Console.ReadLine();
#else

#endif
                return; // No paths -> end program
            }

            // Process valid paths
            if (paths.Count() > 0) {

                OpCounter counter = new OpCounter();
                _tooLongPaths = new List<string>(500);

                foreach (string sourcePath in paths) {
                    // What happens here:
                    // For scanning the given path, we first get it's parent and scan it
                    // This is because then the file/folder in the given path will also get normalized
                    // and it will be compared with it's siblings.
                    // By giving optional argument singletonPath in HandleDirectory method, it will then
                    // avoids processing the siblings in that parent directory
                    string path = sourcePath;
                    string pathWithoutSep = FileOp.PathWithoutPathSeparator(path);
                    string pathroot = FileOp.GetPathRoot(path);
                    if (path != FileOp.GetPathRoot(path))
                        path = pathWithoutSep;

                    if (FileOp.DirectoryExists(path)) {
                        //if (!IsMacPackage(FileOp.GetFileName(path, isDir: true)) && !FileOp.IsSymbolicDir(path)) {
                        if (!FileOp.IsSymbolicDir(path)) {
                            // Path is a dictory
                            if (_optionRename)
                                Console.WriteLine("*** Processing {0:s}", path);
                            else
                                Console.WriteLine("*** Checking {0:s}", path);

                            string parentPath = FileOp.GetDirectoryPath(path, true);

                            if (parentPath == path || parentPath == null) {
                                // start scanning from given path that is root and cannot have a parent path
                                HandleDirectory(path, ref counter, false);
                            } else {
                                // start form parent path but process only given source item
                                // comparison is made to siblings though
                                HandleDirectory(parentPath, ref counter, noLongPathWarnings: false, singletonPath: path);
                            }
                        } else {
                            counter.SkippedDirectories++;
                        }
                    } else if (FileOp.FileOrDirectoryExists(path) && _optionProcessFiles) {
                        // Scan single file or a package
                        string parentPath = FileOp.GetDirectoryPath(path, false);
                        HandleDirectory(parentPath, ref counter, noLongPathWarnings: false, singletonPath: path);

                    } else if (!FileOp.FileOrDirectoryExists(path)) {
                        // Invalid path. Shouldn't occur since argument parser skips invalid paths.
                        Console.WriteLine("*** Error: Invalid path {0:s}", path);
                    }
                }

                // Output the list of long paths
                if (_optionDumpLongPaths)
                    _tooLongPaths.ForEach(x => Console.WriteLine("Long Path: " + x));

                // Output the report
                Console.Write(counter.ToString());
            }

#if DEBUG
            Console.ReadLine();
#else

#endif
        }

        /// <summary>
        /// Reads directory contents, processes files/directories and optionally recurses subdirectories
        /// </summary>
        /// <param name="sourcePath">Path to the directory to be processed</param>
        static void HandleDirectory(string sourcePath, ref OpCounter counter, bool noLongPathWarnings = false, string singletonPath = null)
        {
            // Read directory contents
            bool canAccess = FileOp.GetFilesAndDirectories(sourcePath, _optionSearchPattern,
                out int numberOfFiles, false, out List<string> directoryContents);
            bool pathShown = false;
            bool longPathFound = false;

            if (!canAccess) {
                counter.UnaccesableDirs++;
                Console.WriteLine("*** Cannot access directory: {0:s}", sourcePath);
                return;
            }

            for (int pos = 0; pos < directoryContents.Count(); pos++) {

                bool isDir = pos >= numberOfFiles;
                string path = directoryContents[pos];
                if (singletonPath != null && !FileOp.AreSame(singletonPath, path, _optionCaseInsensitive))
                    continue;

                bool isPackage = false;

                string fileName = FileOp.GetFileName(path, isDir);
                string pathWithoutFileName = path.Substring(0, path.Length - fileName.Length);

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

                if ((_optionProcessDirs && isDir && !isPackage) || (_optionProcessFiles && (!isDir || isPackage))) {

                    string newPath = GetUniqueName(
                        path,
                        directoryContents,
                        isDir,
                        isPackage,
                        _optionNormalize,
                        _optionTrimOptions,
                        _optionCaseInsensitive,
                        _optionFixDuplicates,
                        out bool needNormalization,
                        out bool needTrim,
                        out bool createsDuplicate,
                        out bool needsRename,
                        out bool genuineDuplicate,
                        out bool didFixIllegal,
                        skipIndex: pos);

                    string prefix = GetReportingPrefix(isDir, needNormalization, didFixIllegal, genuineDuplicate, needTrim);

                    if (needsRename && !needNormalization && !genuineDuplicate && !needTrim && !didFixIllegal)
                        throw new Exception("Gathering statistics failed.");

                    if (!needsRename && _optionShowEveryFile) {
                        if (!pathShown) {
                            Console.WriteLine("* " + sourcePath);
                            pathShown = true;
                        }
                        string fName = FileOp.GetFileName(path, isDir);
                        Console.WriteLine($"    {prefix:s} \"{fName:s}\"");
                        // Hex Dump
                        if (_optionHexDump) {
                            PrintHexFileName(FileOp.GetFileName(path, isDir));
                        }

                    }

                    /// Rename
                    /// 
                    if (needsRename) {
                        if (!pathShown) {
                            Console.WriteLine("* " + sourcePath);
                            pathShown = true;
                        }
                        directoryContents[pos] = newPath;
                        string fName = FileOp.GetFileName(path, isDir);
                        string newFName = FileOp.GetFileName(newPath, isDir);
                        Console.WriteLine($"    {prefix:s} \"{fName:s}\"  ==>  \"{newFName:s}\"");
                        // Hex Dump
                        if (_optionHexDump) {
                            PrintHexFileName(FileOp.GetFileName(path, isDir));
                        }

                        bool renameFailed = false;

                        /// Rename part for filename fixing (not normalization)
                        /// 
                        if (_optionRename) {
                            if (FileOp.Rename(path, newPath, isDir)) {
                                if (needNormalization) {
                                    if (isDir)
                                        counter.DirsNeedNormalizeRenamed++;
                                    else
                                        counter.FilesNeedNormalizeRenamed++;
                                }

                                if (genuineDuplicate) {
                                    if (isDir)
                                        counter.DirsWithDuplicateNamesRenamed++;
                                    else
                                        counter.FilesWithDuplicateNamesRenamed++;
                                }
                                if (needTrim) {
                                    if (isDir)
                                        counter.DirsNeedTrimRenamed++;
                                    else
                                        counter.FilesNeedTrimRenamed++;
                                }

                            } else {

                                renameFailed = true;
                                if (isDir) {
                                    Console.WriteLine("*** Error: Cannot rename directory: {0:s}", path);
                                } else {
                                    Console.WriteLine("*** Error: Cannot rename file: {0:s}", path);
                                }
                                if (needNormalization) {
                                    if (isDir)
                                        counter.DirsNeedNormalizeFailed++;
                                    else
                                        counter.FilesNeedNormalizeFailed++;
                                }
                                if (genuineDuplicate) {
                                    if (isDir)
                                        counter.DirsWithDuplicateNamesFailed++;
                                    else
                                        counter.FilesWithDuplicateNamesFailed++;
                                }
                                if (needTrim) {
                                    if (isDir)
                                        counter.DirsNeedTrimFailed++;
                                    else
                                        counter.FilesNeedTrimFailed++;
                                }
                            }
                        }

                        if (needNormalization) {
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
                        }

                        if (genuineDuplicate) {
                            if (isDir) {
                                counter.DirsWithDuplicateNames++;
                            } else {
                                counter.FilesWithDuplicateNames++;
                            }
                        }
                        if (needTrim) {
                            if (isDir) {
                                counter.DirsNeedTrim++;
                            } else {
                                counter.FilesNeedTrim++;
                            }
                            if (createsDuplicate && !renameFailed) {
                                if (isDir) {
                                    counter.DirsNeedTrimProducedDuplicate++;
                                } else {
                                    counter.FilesNeedTrimProducedDuplicate++;
                                }
                            }
                        }
                    }

                }
            }

            /// Free memory before a recursion takes place

            directoryContents = null;
            //directoryContentsDirsFirst = null;

            if (longPathFound) {
                counter.TooLongDirPaths++;
                _tooLongPaths.Add(sourcePath);
            }

            /// Recursion part
            /// 
            if (_optionRecurse || singletonPath != null) {
                // reread subdirectories because some may have changed
                if (!FileOp.GetSubDirectories(sourcePath, out List<string> subDirectories))
                    counter.IOErrors++;

                foreach (string path in subDirectories) {
                    if (singletonPath != null && !FileOp.AreSame(singletonPath, path, _optionCaseInsensitive))
                        continue;

                    string dirName = FileOp.GetFileName(path, isDir: true);


                    bool tooLongPath = path.Length >= FileOp.MAX_FILE_PATH_LENGTH;
                    if (tooLongPath & !noLongPathWarnings) {
                        //Console.WriteLine("*** Warning: Path too long for DIRECTORY ({0:g}): {1:s} ", subDirectory.Length, subDirectory);
                        //Console.WriteLine("*** Subsequent warnings in this path are supressed.");
                        counter.TooLongDirPaths++;
                        if (_optionDumpLongPaths)
                            _tooLongPaths.Add(path);
                    }

                    // Recurse if recursion flag set
                    if (!IsMacPackage(dirName)) {
                        if (FileOp.DirectoryExists(path)) {
                            if (!FileOp.IsSymbolicDir(path))
                                HandleDirectory(path, ref counter, noLongPathWarnings: tooLongPath || noLongPathWarnings); // -> Recurse subdirectories
                            else {
                                Console.WriteLine("*** SymLink - not following: {0:s}", path);
                                counter.SkippedDirectories++;
                            }
                        } else {
                            Console.WriteLine("*** Error: Cannot Access Directory (After renaming): {0:s}", path);
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

        static string GetReportingPrefix(bool isDir, bool normalize, bool illegals, bool duplicate, bool trim)
        {
            string prefix = isDir ? "DIR:   " : "File:  ";

            StringBuilder sb = new StringBuilder(4);
            if (normalize)
                sb.Append("N");
            if (illegals)
                sb.Append("I");
            if (duplicate)
                sb.Append("D");
            if (trim)
                sb.Append("T");
            for (int i = sb.Length; i <= 4; i++) {
                sb.Append(" ");
            }

            prefix += sb.ToString();
            prefix += " ";

            //if (normalize && trim && duplicate) {
            //    prefix += "NORM+S+D  ";
            //} else {
            //    if (normalize && duplicate)
            //        prefix += "NORM+DUPL ";
            //    else if (normalize && trim)
            //        prefix += "NORM+SPCS ";

            //    else if (trim && duplicate)
            //        prefix += "SPCS+DUPL ";
            //    else if (trim)
            //        prefix += "SPACES    ";
            //    else if (duplicate)
            //        prefix += "DUPLICATE ";
            //    else if (normalize)
            //        prefix += "NORMALIZE ";
            //    else
            //        prefix += "noop       ";
            //}
            return prefix;
        }

        /// <summary>
        /// Does all the stuff and produces a new unique path for a file/folder
        /// </summary>
        /// <param name="path"></param>
        /// <param name="dirContents"></param>
        /// <param name="isDir"></param>
        /// <param name="isPackage"></param>
        /// <param name="normalize"></param>
        /// <param name="trimOptions"></param>
        /// <param name="caseInsensitive"></param>
        /// <param name="fixDuplicates"></param>
        /// <param name="didNormalize"></param>
        /// <param name="didTrim"></param>
        /// <param name="createdDuplicate"></param>
        /// <param name="needRename"></param>
        /// <param name="genuineDuplicate"></param>
        /// <param name="skipIndex"></param>
        /// <returns></returns>
        private static string GetUniqueName(
            string path,
            List<string> dirContents,
            bool isDir,
            bool isPackage,
            bool normalize,
            TrimOptions trimOptions,
            bool caseInsensitive,
            bool fixDuplicates,
            out bool didNormalize,
            out bool didTrim,
            out bool createdDuplicate,
            out bool needRename,
            out bool genuineDuplicate,
            out bool didFixIllegal,
            int skipIndex = -1)
        {
            string newPath = path;
            string fileName = FileOp.GetFileName(path, isDir);
            didNormalize = false;
            didTrim = false;
            createdDuplicate = false;
            needRename = false;
            genuineDuplicate = false;
            didFixIllegal = false;

            /// Normalize
            /// 
            if (normalize) {
                bool needNormalization = !fileName.IsNormalized(_optionNormalizationForm);
                if (needNormalization)
                    didNormalize = NormalizeFileNameInPath(ref newPath, _optionNormalizationForm, isDir);
            }
            fileName = FileOp.GetFileName(newPath, isDir);

            /// Illegal Characters
            /// 
            if (_optionFixIllegalChars) {
                didFixIllegal = FixIllegalChars(ref newPath, isDir);
            }

            /// Trim
            /// 
            if (trimOptions != TrimOptions.None) {
                didTrim = Trim(ref newPath, trimOptions, (isDir && !isPackage));
            }

            /// Get new name
            /// 
            fileName = FileOp.GetFileName(newPath, isDir);
            string pathWihtoutLastComponent = newPath.Substring(0, newPath.Count() - fileName.Count());
            pathWihtoutLastComponent = FileOp.PathWithoutPathSeparator(pathWihtoutLastComponent);
            string extension = FileOp.GetExtension(newPath, isDir);
            string baseName = FileOp.GetFileNameWithoutExtension(newPath, isDir);

            string newBase = baseName;
            string dot = "";
            if (extension.StartsWith(".")) {
                extension = extension.Substring(1);
                dot = ".";
            }

            /// Add suffix if file/folder already exists
            /// 

            if (didNormalize || didTrim || fixDuplicates) {
                int i = 1;
                while (FileOp.NameExists(newPath, dirContents, caseInsensitive, out bool genuineDup, skipIndex, path)) {
                    if (!genuineDup)
                        createdDuplicate = true;
                    else
                        genuineDuplicate = true;

                    string suffix = " [Duplicate Name]";
                    if (i != 1) {
                        suffix = $" [Duplicate Name ({i})]";
                    }

                    if (baseName != "") {
                        if (!isDir || isPackage) {
                            // normal file or package type folder
                            newPath = pathWihtoutLastComponent + @"\" + newBase + suffix + dot + extension;
                        } else {
                            // folder
                            newPath = pathWihtoutLastComponent + @"\" + newBase + dot + extension + suffix;
                        }
                    } else {
                        // .dotfile
                        newPath = pathWihtoutLastComponent + @"\" + newBase + dot + extension + suffix;
                    }
                    i++;
                }
            }

            if (newPath != path)
                needRename = true;

            return newPath;
        }

        private static bool FixIllegalChars(ref string path, bool isDir)
        {
            bool didFix = false;
            string newPath = path;
            string fileName = FileOp.GetFileName(path, isDir);
            string pathWihtoutLastComponent = path.Substring(0, path.Count() - fileName.Count());
            pathWihtoutLastComponent = FileOp.PathWithoutPathSeparator(pathWihtoutLastComponent);
            StringBuilder sb = new StringBuilder(fileName.Length);
            for (int i = 0; i < fileName.Length; i++) {
                char c = fileName[i];
                if (illegalChars.Contains(c)) {
                    didFix = true;
                    sb.Append(replacementCharacter);
                } else {
                    sb.Append(fileName[i]);
                }
            }
            string newFileName = sb.ToString();
            newPath = pathWihtoutLastComponent + @"\" + newFileName;
            return didFix;
        }

        /// <summary>
        /// Trim
        /// </summary>
        /// <param name="path"></param>
        /// <param name="options"></param>
        /// <param name="isDir"></param>
        /// <returns></returns>
        private static bool Trim(ref string path, TrimOptions options, bool isDir)
        {
            string newPath = path;
            string fileName = FileOp.GetFileName(path, isDir);
            string pathWihtoutLastComponent = path.Substring(0, path.Count() - fileName.Count());
            pathWihtoutLastComponent = FileOp.PathWithoutPathSeparator(pathWihtoutLastComponent);
            string extension = FileOp.GetExtension(path, isDir);
            string baseName = FileOp.GetFileNameWithoutExtension(path, isDir);

            string newBase = baseName;
            string dot = "";
            if (extension.StartsWith(".")) {
                extension = extension.Substring(1);
                dot = ".";
            }

            string newExt = extension;
            string newFolderName = fileName;

            if (!isDir) {
                if (IsBinaryMatch(options, TrimOptions.FileBase)) {
                    newBase = baseName.Trim();
                } else {
                    if ((options & TrimOptions.FileBaseLeft) != 0) {
                        newBase = baseName.TrimStart();
                    }
                    if ((options & TrimOptions.FileBaseRight) != 0) {
                        newBase = baseName.TrimEnd();
                    }

                }
                if (IsBinaryMatch(options, TrimOptions.FileExt)) {
                    newExt = extension.Trim();
                } else {
                    if ((options & TrimOptions.FileExtLeft) != 0) {
                        newExt = extension.TrimStart();
                    }
                    if ((options & TrimOptions.FileExtRight) != 0) {
                        newExt = extension.TrimEnd();
                    }
                }
                if (baseName != "" && newBase == "")
                    newBase = "[No Name]";

                string newFileName;
                if (newExt != "")
                    newFileName = newBase + dot + newExt;
                else
                    newFileName = newBase;

                if (newFileName == "")
                    newFileName = "[No Name]";

                newPath = pathWihtoutLastComponent + @"\" + newFileName;

            } else {
                if (IsBinaryMatch(options, TrimOptions.Dir)) {
                    newFolderName = fileName.TrimEnd(dotTrimChars);
                    newFolderName = newFolderName.TrimStart();
                } else {
                    if ((options & TrimOptions.DirLeft) != 0) {
                        newFolderName = fileName.TrimStart();
                    }
                    if ((options & TrimOptions.DirRight) != 0) {
                        newFolderName = fileName.TrimEnd(dotTrimChars);
                    }
                }
                if (newFolderName == "")
                    newFolderName = "[No Name]";
                newPath = pathWihtoutLastComponent + @"\" + newFolderName;
            }
            bool didTrim = newPath != path;
            path = newPath;
            return didTrim;

        }

        private static bool IsBinaryMatch(TrimOptions options, TrimOptions requirements)
        {
            return (options & requirements) == requirements;
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
        static bool NormalizeFileNameInPath(ref string path, NormalizationForm form, bool isDir)
        {
            string fileName = FileOp.GetFileName(path, isDir);
            string pathWithoutFileName = path.Substring(0, path.Length - fileName.Length);
            string normalizedPath = path;
            string normalizedFileName = fileName;

            pathWithoutFileName = FileOp.PathWithoutPathSeparator(pathWithoutFileName);

            normalizedFileName = fileName.Normalize(form);
            normalizedPath = pathWithoutFileName + @"\" + normalizedFileName;
            bool didNormalize = (path != normalizedPath);
            path = normalizedPath;
            return didNormalize;
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
                if (lcaseArg == "/nd" && _optionNormalize == false) {
                    _optionNormalizationForm = NormalizationForm.FormD;
                    _optionNormalize = true;
                }
                // option /formc = use Form C normalization (normal operation)
                if (lcaseArg == "/nc") {
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
                // case sensitive operation in case sensitive systems
                if (lcaseArg == "/case") {
                    _optionCaseInsensitive = false;
                }
                // option check trailing and leading spaces
                if (lcaseArg == "/t") {
                    _optionTrimOptions = ParseTrimOptions("dirright");
                }

                // option directories only
                if (lcaseArg == "/d") {
                    _optionProcessFiles = false;
                }

                // option directories only
                if (lcaseArg == "/l") {
                    _optionDumpLongPaths = true;
                }
                // option directories only
                if (lcaseArg == "/i") {
                    _optionFixIllegalChars = true;
                }
                // option to handle case insensitive duplicates
                if (lcaseArg == "/dup" || lcaseArg == "/b") {
                    _optionFixDuplicates = true;
                }
                // bypass normalization
                //if (lcaseArg == "/nonorm") {
                //    _optionNormalize = false;
                //}
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

                // Trim options
                if (lcaseArg.StartsWith("/t=")) {
                    string trimStr = lcaseArg.Substring(3);
                    _optionTrimOptions = _optionTrimOptions | ParseTrimOptions(trimStr);
                }

                // Get source paths
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

        private static TrimOptions ParseTrimOptions(string trimStr)
        {
            TrimOptions result = TrimOptions.None;
            string[] args = trimStr.ToLower().Split(',');
            foreach (string arg in args) {

                if (arg == "baseleft" || arg == "bl")
                    result = result | TrimOptions.FileBaseLeft;
                if (arg == "baseright" || arg == "br")
                    result = result | TrimOptions.FileBaseRight;
                if (arg == "base" || arg == "b")
                    result = result | TrimOptions.FileBaseLeft | TrimOptions.FileBaseRight;
                if (arg == "extleft" || arg == "el")
                    result = result | TrimOptions.FileExtLeft;
                if (arg == "extright" || arg == "er")
                    result = result | TrimOptions.FileExtRight;
                if (arg == "ext" || arg == "e")
                    result = result | TrimOptions.FileExtLeft | TrimOptions.FileExtRight;

                if (arg == "dirleft" || arg == "dl")
                    result = result | TrimOptions.DirLeft;
                if (arg == "dirright" || arg == "dr")
                    result = result | TrimOptions.DirRight;
                if (arg == "dir" || arg == "d")
                    result = result | TrimOptions.DirLeft | TrimOptions.DirRight;

                if (arg == "all") {
                    result = result | TrimOptions.FileBaseLeft | TrimOptions.FileBaseRight
                        | TrimOptions.FileExtLeft | TrimOptions.FileExtRight
                        | TrimOptions.DirLeft | TrimOptions.DirRight;
                }
            }
            return result;
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

        /// <summary>
        /// Testing
        /// </summary>
        private static void PrintIllegalChars()
        {
            foreach (char c in illegalChars) {

                Console.WriteLine($"{(int)c:X}({c:s}) ");
            }
        }
    }
}
