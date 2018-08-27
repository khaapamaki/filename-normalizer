// File Name Normalizer
// Kati Haapamäki 2016-2017

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
    public static class Program
    {
        // User options that are set by cmd line options
        private static bool _optionRecurse = false;
        private static bool _optionRename = false;
        private static bool _optionPrintEveryFile = false;
        private static string _optionSearchPattern = "*";
        private static bool _optionPrintHexCodes = false;
        private static bool _optionCaseInsensitive = true;
        private static bool _optionFixDuplicates = false;
        private static bool _optionProcessFiles = true;
        private static bool _optionProcessDirs = true;
        private static bool _optionNormalize = false;
        private static bool _optionMacAware = true;
        private static bool _optionPrintLongPaths = false;
        private static bool _optionShowHelp = false;
        private static bool _optionFixIllegalChars = false;

        private static bool
            _optionOnlyPrintParentFolder = false; // to prevent displaying individual files or folders to be fixed

        private static List<string> _longPaths; // list of long paths to show later
        private static TrimOptions _optionTrimOptions = TrimOptions.None;

        private static NormalizationForm _defaultNormalizationForm = NormalizationForm.FormC;
        private static NormalizationForm _optionNormalizationForm = _defaultNormalizationForm;

        private static readonly List<string> MacPackages = new List<string> {
            ".fcpcache",
            ".app",
            ".framework",
            ".kext",
            ".plugin",
            ".docset",
            ".xpc",
            ".qlgenerator",
            ".component",
            ".mdimporter",
            ".bundle",
            ".lproj",
            ".nib",
            ".xib",
            ".download",
            ".rtfd",
            ".fcarch",
            ".pkg",
            ".dmg"
        };

        //private static List<int> illegalCharCodes = new List<int> { 0xF029, 0x2DC, 0xF020, 0xF021, 0xF022, 0xF023, 0xF024, 0xF025, 0xF026, 0xF027, 0xF028 };
        private static readonly char[] DotTrimChars = {' ', '.', '\xF029'};

        private static readonly List<char> IllegalChars = new List<char> {
            '\xF020',
            '\xF021',
            '\xF022',
            '\xF023',
            '\xF024',
            '\xF025',
            '\xF026',
            '\xF027',
            '<',
            '>',
            '|',
            '\\',
            '/',
            '\"',
            '*',
            '?',
            ':',
            '\xF028',
            '\xF029',
        };

        // not currently in use
        // useful stuff keep along with this project for future use
        const string AcceptableCharacters = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZÁÀÂÄÃÅÇÉÈÊËÍÌÎÏÑÓÒÔÖÕÚÙÛÜŸ" +
                                            "abcdefghijklmnopqrstuvwxyzáàâäãåçéèêëíìîïñóòôöõúùûüÿ_ !@#£$€%&()[]{}'+-.,;§½";

        const char ReplacementCharacter = '_';

        [Flags]
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
        private static void Main(string[] args)
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
                Console.WriteLine("  /nc           Performs Form C normalization. Normal operation");
                Console.WriteLine("  /nd           Performs Form D normalization. Reverse for Form C");
                Console.WriteLine(@"  /i            Replaces illegal characters < > / \ | : * with underscore");
                Console.WriteLine(
                    "  /b,/dup       Renames file and folder names that would have a duplicate name in a case-insensitive file system");
                Console.WriteLine("                This is effective only when scanning case sensitive file system");
                Console.WriteLine(
                    "  /t            Trims illegal folder names with trailing spaces. Same as option /t=dirright");
                Console.WriteLine("  /t=all        Trims all file and folder names with leading and trailing space.");
                Console.WriteLine(
                    "  /t=opt1,opt2  Specific trim instructions: base, ext, dir, baseleft, baseright, extleft, extright, dirleft, dirright");
                Console.WriteLine("  /d            Processes folder names only");
                Console.WriteLine("  /f            Processes filenames only");
                Console.WriteLine("  /a            Sets options /nc /b /t /i at once.");
                Console.WriteLine("  /p=<pattern>  Search pattern for files, eg. *.txt");
                Console.WriteLine(
                    "  /rename       Does actual renaming instead of displaying info about what should be done");
                Console.WriteLine("  /v            Verbose mode. Print out all files and folders in a tree");
                Console.WriteLine(
                    "  /o            Only show folders that includes items to be fixed hiding items itself");
                Console.WriteLine("  /l            Detailed report about long paths");
                Console.WriteLine("  /hex          Shows hex codes for file and folder names");
                Console.WriteLine("");
                Console.WriteLine(
                    "Note:           Without /rename option only dry run is performed without actual renaming");

#if DEBUG
                Console.ReadLine();
#endif
                return; // No valid paths given -> end program
            }

            // Process valid paths
            if (!paths.Any()) return;
            
            var counter = new OpCounter();
            _longPaths = new List<string>(500);

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
                        Console.WriteLine(_optionRename ? $"*** Processing {path}" : $"*** Checking {path}");

                        string parentPath = FileOp.GetDirectoryPath(path, true);

                        if (parentPath == path || parentPath == null) {
                            // start scanning from given path that is root and cannot have a parent path
                            HandleDirectory(path, ref counter, false);
                        } else {
                            // start form parent path but process only given source item
                            // comparison is made to siblings though
                            HandleDirectory(parentPath, ref counter, noLongPathWarnings: false,
                                singleItemPath: path);
                        }
                    } else {
                        // skipping symbolic link
                        counter.SkippedDirectories++;
                    }
                } else if (FileOp.FileOrDirectoryExists(path) && _optionProcessFiles) {
                    // Scan single file or a package
                    string parentPath = FileOp.GetDirectoryPath(path, false);
                    HandleDirectory(parentPath, ref counter, noLongPathWarnings: false, singleItemPath: path);
                } else if (!FileOp.FileOrDirectoryExists(path)) {
                    // Invalid path. Shouldn't occur since argument parser skips invalid paths.
                    Console.WriteLine($"*** Error: Invalid path {path}");
                }
            }

            // Output the list of long paths
            if (_optionPrintLongPaths)
                _longPaths.ForEach(x => Console.WriteLine("Long Path: " + x));

            // Output the report
            Console.Write(counter.ToString());

#if DEBUG
            Console.ReadLine();
#endif
        }

        /// <summary>
        /// Reads directory contents, processes files/directories and optionally recurses subdirectories
        /// </summary>
        /// <param name="sourcePath">Path to the directory to be processed</param>
        /// <param name="counter"></param>
        /// <param name="noLongPathWarnings"></param>
        /// <param name="singleItemPath"></param>
        private static void HandleDirectory(string sourcePath, ref OpCounter counter, bool noLongPathWarnings = false,
            string singleItemPath = null)
        {
            // Read directory contents
            bool canAccess = FileOp.GetFilesAndDirectories(sourcePath, _optionSearchPattern,
                out int numberOfFiles, false, out List<string> directoryContents);
            bool pathShown = false;
            bool longPathFound = false;

            if (!canAccess) {
                counter.UnaccesableDirs++;
                Console.WriteLine($"*** Cannot access directory: {sourcePath}");
                return;
            }

            for (int pos = 0; pos < directoryContents.Count(); pos++) {
                bool isDir = pos >= numberOfFiles;
                string path = directoryContents[pos];
                if (singleItemPath != null && !FileOp.AreSame(singleItemPath, path, _optionCaseInsensitive))
                    continue;

                bool isPackage = false;

                string fileName = FileOp.GetFileName(path, isDir);
                string pathWithoutFileName = path.Substring(0, path.Length - fileName.Length);

                // long path detection
                if (!noLongPathWarnings && !isDir) {
                    if (path.Length >= FileOp.MaxFilePathLength)
                        longPathFound = true;
                }

                if (!noLongPathWarnings && isDir) {
                    if (path.Length >= FileOp.MaxDirPathLength)
                        longPathFound = true;
                }

                // macOS package detection. (folder names will be renamed like file names)
                if (isDir) {
                    if (IsMacPackage(FileOp.GetFileName(path, true)))
                        isPackage = true;
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
                        out bool caseDuplicate,
                        out bool needFixIllegal,
                        skipIndex: pos);

                    string prefix = GetReportingPrefix(isDir, needNormalization, needFixIllegal, caseDuplicate,
                        needTrim);

                    if (needsRename && !needNormalization && !caseDuplicate && !needTrim && !needFixIllegal)
                        throw new Exception("Gathering statistics failed.");

                    if (!needsRename && _optionPrintEveryFile) {
                        if (!pathShown) {
                            Console.WriteLine("* " + sourcePath);
                            pathShown = true;
                        }

                        string fName = FileOp.GetFileName(path, isDir);
                        if (!_optionOnlyPrintParentFolder)
                            Console.WriteLine($"    {prefix} \"{fName}\"");

                        // Hex Dump
                        if (_optionPrintHexCodes)
                            PrintHexFileName(FileOp.GetFileName(path, isDir));
                    }

                    // Rename
                    if (needsRename) {
                        if (!pathShown) {
                            Console.WriteLine("* " + sourcePath);
                            pathShown = true;
                        }

                        directoryContents[pos] = newPath;
                        string fName = FileOp.GetFileName(path, isDir);
                        string newFName = FileOp.GetFileName(newPath, isDir);
                        if (!_optionOnlyPrintParentFolder)
                            Console.WriteLine($"    {prefix} \"{fName}\"  ==>  \"{newFName}\"");

                        // Hex Dump
                        if (_optionPrintHexCodes)
                            PrintHexFileName(FileOp.GetFileName(path, isDir));

                        bool renameFailed = false;

                        // Rename part for filename fixing (not normalization)
                        if (_optionRename) {
                            if (FileOp.Rename(path, newPath, isDir)) {
                                if (needNormalization) {
                                    if (isDir)
                                        counter.DirsNeedNormalizeRenamed++;
                                    else
                                        counter.FilesNeedNormalizeRenamed++;
                                }

                                if (needFixIllegal) {
                                    if (isDir)
                                        counter.DirsWithIllegalCharsRenamed++;
                                    else
                                        counter.FilesWithIllegalCharsRenamed++;
                                }

                                if (caseDuplicate) {
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
                                Console.WriteLine(isDir
                                    ? $"*** Error: Cannot rename directory: {path}"
                                    : $"*** Error: Cannot rename file: {path}");

                                if (needNormalization) {
                                    if (isDir)
                                        counter.DirsNeedNormalizeFailed++;
                                    else
                                        counter.FilesNeedNormalizeFailed++;
                                }

                                if (needFixIllegal) {
                                    if (isDir)
                                        counter.DirsWithIllegalCharsFailed++;
                                    else
                                        counter.FilesWithIllegalCharsFailed++;
                                }

                                if (caseDuplicate) {
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
                            if (isDir)
                                counter.DirsNeedNormalize++;
                            else
                                counter.FilesNeedNormalize++;

                            if (createsDuplicate && !renameFailed) {
                                if (isDir)
                                    counter.DirsNeedNormalizeProducedDuplicate++;
                                else
                                    counter.FilesNeedNormalizeProducedDuplicate++;
                            }
                        }

                        if (needFixIllegal) {
                            if (isDir)
                                counter.DirsWithIllegalChars++;
                            else
                                counter.FilesWithIllegalChars++;

                            if (createsDuplicate && !renameFailed) {
                                if (isDir)
                                    counter.DirsWithIllegalCharsProducedDuplicate++;
                                else
                                    counter.FilesWithIllegalCharsProducedDuplicate++;
                            }
                        }

                        if (caseDuplicate) {
                            if (isDir)
                                counter.DirsWithDuplicateNames++;
                            else
                                counter.FilesWithDuplicateNames++;
                        }

                        if (needTrim) {
                            if (isDir)
                                counter.DirsNeedTrim++;
                            else
                                counter.FilesNeedTrim++;

                            if (createsDuplicate && !renameFailed) {
                                if (isDir)
                                    counter.DirsNeedTrimProducedDuplicate++;
                                else
                                    counter.FilesNeedTrimProducedDuplicate++;
                            }
                        }
                    }
                }
            }

            // Free memory before a recursion takes place
            directoryContents = null;

            //directoryContentsDirsFirst = null;

            if (longPathFound && !noLongPathWarnings) {
                counter.TooLongDirPaths++;
                if (_optionPrintLongPaths)
                    _longPaths.Add(sourcePath);
            }

            // Recursion part
            if (!_optionRecurse && singleItemPath == null) return;
            
            // reread subdirectories because some may have changed
            if (!FileOp.GetSubDirectories(sourcePath, out List<string> subDirectories))
                counter.IOErrors++;

            foreach (string path in subDirectories) {
                if (singleItemPath != null && !FileOp.AreSame(singleItemPath, path, _optionCaseInsensitive))
                    continue;

                string dirName = FileOp.GetFileName(path, isDir: true);

                // Recurse if recursion flag set
                if (!IsMacPackage(dirName)) {
                    if (FileOp.DirectoryExists(path)) {
                        if (!FileOp.IsSymbolicDir(path))
                            HandleDirectory(path, ref counter,
                                noLongPathWarnings: longPathFound ||
                                                    noLongPathWarnings); // -> Recurse subdirectories
                        else {
                            Console.WriteLine($"*** SymLink - not following: {path}");
                            counter.SkippedDirectories++;
                        }
                    } else {
                        Console.WriteLine($"*** Error: Cannot Access Directory (After renaming): {path}");
                        counter.IOErrors++;
                    }
                } else {
                    //Console.WriteLine($"*** skipped {subDirectory:s}");
                    counter.SkippedDirectories++;
                }
            }

        }

        private static string GetReportingPrefix(bool isDir, bool normalize, bool illegals, bool duplicate, bool trim)
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
        /// <param name="caseDuplicate"></param>
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
            out bool caseDuplicate,
            out bool didFixIllegal,
            int skipIndex = -1)
        {
            string newPath = path;
            string fileName = FileOp.GetFileName(path, isDir);
            didNormalize = false;
            didTrim = false;
            createdDuplicate = false;
            needRename = false;
            caseDuplicate = false;
            didFixIllegal = false;

            // Normalize
            if (normalize) {
                bool needNormalization = !fileName.IsNormalized(_optionNormalizationForm);
                if (needNormalization)
                    didNormalize = NormalizeFileNameInPath(ref newPath, _optionNormalizationForm, isDir);
            }

            fileName = FileOp.GetFileName(newPath, isDir);

            // Illegal Characters
            if (_optionFixIllegalChars) {
                didFixIllegal = FixIllegalChars(ref newPath, isDir);
            }

            // Trim
            if (trimOptions != TrimOptions.None) {
                didTrim = Trim(ref newPath, trimOptions, (isDir && !isPackage));
            }

            // Get new name
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

            // Add suffix if file/folder already exists
            if (didNormalize || didTrim || fixDuplicates) {
                int i = 1;
                while (FileOp.NameExists(newPath, dirContents, caseInsensitive, out bool caseDupl, skipIndex, path)) {
                    if (!caseDupl)
                        createdDuplicate = true;
                    else
                        caseDuplicate = true;

                    string suffix = " [Duplicate Name]";
                    if (i != 1)
                        suffix = $" [Duplicate Name ({i})]";

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
                if (IllegalChars.Contains(c)) {
                    didFix = true;
                    sb.Append(ReplacementCharacter);
                } else {
                    sb.Append(fileName[i]);
                }
            }

            string newFileName = sb.ToString();
            if (didFix)
                path = pathWihtoutLastComponent + @"\" + newFileName;
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
            string newPath;
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
                if (IsBinaryMatch(options, TrimOptions.FileBase))
                    newBase = baseName.Trim();
                else {
                    if ((options & TrimOptions.FileBaseLeft) != 0)
                        newBase = baseName.TrimStart();

                    if ((options & TrimOptions.FileBaseRight) != 0)
                        newBase = baseName.TrimEnd();
                }

                if (IsBinaryMatch(options, TrimOptions.FileExt))
                    newExt = extension.Trim();
                else {
                    if ((options & TrimOptions.FileExtLeft) != 0)
                        newExt = extension.TrimStart();

                    if ((options & TrimOptions.FileExtRight) != 0)
                        newExt = extension.TrimEnd();
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
                    newFolderName = fileName.TrimEnd(DotTrimChars);
                    newFolderName = newFolderName.TrimStart();
                } else {
                    if ((options & TrimOptions.DirLeft) != 0)
                        newFolderName = fileName.TrimStart();

                    if ((options & TrimOptions.DirRight) != 0)
                        newFolderName = fileName.TrimEnd(DotTrimChars);
                }

                if (newFolderName == "")
                    newFolderName = "[No Name]";

                newPath = pathWihtoutLastComponent + @"\" + newFolderName;
            }

            bool didTrim = newPath != path;
            path = newPath;
            return didTrim;
        }

        private static bool IsBinaryMatch(TrimOptions options, TrimOptions requirements) =>
            (options & requirements) == requirements;

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
            var fileName = FileOp.GetFileName(path, isDir);
            var pathWithoutFileName = path.Substring(0, path.Length - fileName.Length);

            pathWithoutFileName = FileOp.PathWithoutPathSeparator(pathWithoutFileName);

            var normalizedFileName = fileName.Normalize(form);
            var normalizedPath = pathWithoutFileName + @"\" + normalizedFileName;
            bool didNormalize = (path != normalizedPath);
            path = normalizedPath;
            return didNormalize;
        }

        static bool IsMacPackage(string dirName)
        {
            if (_optionMacAware) {
                foreach (var suffix in MacPackages) {
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
        private static string[] ParseArguments(string[] args)
        {
            List<string> validPaths = new List<string>(5);

            foreach (var arg in args) {
                var lcaseArg = arg.ToLower();
                // option /r = recurse
                if (lcaseArg == "/r")
                    _optionRecurse = true;

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
                if (lcaseArg == "/v")
                    _optionPrintEveryFile = true;

                // option /e, print errors only
                if (lcaseArg == "/a") {
                    _optionNormalizationForm = NormalizationForm.FormC;
                    _optionNormalize = true;
                    _optionTrimOptions = ParseTrimOptions("dirright");
                    _optionFixIllegalChars = true;
                    _optionFixDuplicates = true;
                }

                // option /p=<pattern>, search pattern for files
                if (lcaseArg.StartsWith("/p=")) {
                    _optionSearchPattern = arg.Substring(3);
                }

                if (lcaseArg == "/hex") {
                    _optionPrintHexCodes = true;
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

                // option only show parent folders
                if (lcaseArg == "/o") {
                    _optionOnlyPrintParentFolder = true;
                }

                // option list all long path branches
                if (lcaseArg == "/l") {
                    _optionPrintLongPaths = true;
                }

                // option directories only
                if (lcaseArg == "/i") {
                    _optionFixIllegalChars = true;
                }

                // option to handle case insensitive duplicates
                if (lcaseArg == "/dup" || lcaseArg == "/b") {
                    _optionFixDuplicates = true;
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
                            Console.WriteLine($"*** Error: Invalid path {arg}");
                    } else {
                        if (FileOp.FileOrDirectoryExists(arg))
                            validPaths.Add(arg);
                        else
                            Console.WriteLine($"*** Error: Invalid path {arg}");
                    }
                }
            }

            return validPaths.ToArray();
        }

        private static TrimOptions ParseTrimOptions(string trimStr)
        {
            TrimOptions result = TrimOptions.None;
            string[] args = trimStr.ToLower().Split(',');
            foreach (var arg in args) {
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

                if (arg == "all" || arg == "a") {
                    result = result | TrimOptions.FileBase
                                    | TrimOptions.FileExt
                                    | TrimOptions.Dir;
                }
            }

            return result;
        }

        /// <summary>
        /// Print Name in Hexadecimal. For Debuging.
        /// </summary>
        /// <param name="fname"></param>
        private static void PrintHexFileName(string fname)
        {
            char[] chars = fname.ToCharArray();
            for (int i = 0; i < fname.Length; i++) {
                Console.Write($"{(int) chars[i]:X}({chars[i]}) ");
            }

            Console.WriteLine($" [{fname.Length}]");
        }

        /// <summary>
        /// Testing
        /// </summary>
        private static void PrintIllegalChars()
        {
            foreach (char c in IllegalChars) {
                Console.WriteLine($"{(int) c:X}({c}) ");
            }
        }
    }
}