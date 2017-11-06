using System.Text;

namespace FileNameNormalizer
{
    class OpCounter
    {
        public int FilesNeedNormalize { get; set; } = 0;
        public int DirsNeedNormalize { get; set; } = 0;
        public int FilesWithDuplicateNames { get; set; } = 0;
        public int DirsWithDuplicateNames { get; set; } = 0;
        public int FilesNeedTrim { get; set; } = 0;
        public int DirsNeedTrim { get; set; } = 0;
        public int FilesWithIllegalChars { get; set; } = 0;
        public int DirsWithIllegalChars { get; set; } = 0;

        public int FilesNeedNormalizeRenamed { get; set; } = 0;
        public int DirsNeedNormalizeRenamed { get; set; } = 0;
        public int FilesWithDuplicateNamesRenamed { get; set; } = 0;
        public int DirsWithDuplicateNamesRenamed { get; set; } = 0;
        public int FilesNeedTrimRenamed { get; set; } = 0;
        public int DirsNeedTrimRenamed { get; set; } = 0;
        public int FilesWithIllegalCharsRenamed { get; set; } = 0;
        public int DirsWithIllegalCharsRenamed { get; set; } = 0;
        public int FilesNeedNormalizeFailed { get; set; } = 0;
        public int DirsNeedNormalizeFailed { get; set; } = 0;
        public int FilesWithDuplicateNamesFailed { get; set; } = 0;
        public int DirsWithDuplicateNamesFailed { get; set; } = 0;
        public int FilesNeedTrimFailed { get; set; } = 0;
        public int DirsNeedTrimFailed { get; set; } = 0;
        public int FilesWithIllegalCharsFailed { get; set; } = 0;
        public int DirsWithIllegalCharsFailed { get; set; } = 0;
        public int FilesNeedNormalizeProducedDuplicate { get; set; } = 0;
        public int DirsNeedNormalizeProducedDuplicate { get; set; } = 0;
        public int FilesNeedTrimProducedDuplicate { get; set; } = 0;
        public int DirsNeedTrimProducedDuplicate { get; set; } = 0;
        public int FilesWithIllegalCharsProducedDuplicate { get; set; } = 0;
        public int DirsWithIllegalCharsProducedDuplicate { get; set; } = 0;

        public int FilesWithDuplicateNamesCreated { get; set; } = 0;
        public int DirsWithDuplicateNamesCreated { get; set; } = 0;

        public int TooLongFilePaths { get; set; } = 0;
        public int TooLongDirPaths { get; set; } = 0;
        public int SkippedDirectories { get; set; } = 0;
        public int IOErrors { get; set; } = 0;
        public int UnaccesableDirs { get; set; } = 0;

        public override string ToString()
        {
            StringBuilder s = new StringBuilder(1000);

            if (FilesNeedNormalize > 0) {
                if (IsPlural(FilesNeedNormalize))
                    s.AppendLine($"{FilesNeedNormalize} filenames need normalization");
                else
                    s.AppendLine($"1 filename needs normalization");

                if (FilesNeedNormalizeRenamed > 0 || FilesNeedNormalizeFailed > 0) {
                    if (FilesNeedNormalizeRenamed > 0) {
                        if (IsPlural(FilesNeedNormalizeRenamed))
                            s.AppendLine($"   of which {FilesNeedNormalizeRenamed} filenames were normalized succesfully");
                        else
                            s.AppendLine("    of which 1 filename was normalized succesfully");
                    }
                    if (FilesNeedNormalizeFailed > 0) {
                        if (IsPlural(FilesNeedNormalizeFailed))
                            s.AppendLine($"   with {FilesNeedNormalizeFailed} filenames failed to be normalized succesfully");
                        else
                            s.AppendLine($"   with 1 filename failed to be normalized succesfully");
                    }
                    if (FilesNeedNormalizeProducedDuplicate > 0) {
                        if (IsPlural(FilesNeedNormalizeProducedDuplicate))
                            s.AppendLine($"   {FilesNeedNormalizeProducedDuplicate} files were renamed with 'Duplicate' suffixes");
                        else
                            s.AppendLine($"   1 file was renamed with a 'Duplicate' suffix");
                    }
                } else {
                    if (FilesNeedNormalizeProducedDuplicate > 0) {
                        if (IsPlural(FilesNeedNormalizeProducedDuplicate))
                            s.AppendLine($"   of which {FilesNeedNormalizeProducedDuplicate} files would get duplicate names and need to be renamed with 'Duplicate' suffixes");
                        else
                            s.AppendLine($"   of which 1 file would get a duplicate name and need to be renamed a 'Duplicate' suffix");
                    }
                }
            }

            if (DirsNeedNormalize > 0) {
                if (IsPlural(DirsNeedNormalize))
                    s.AppendLine($"{DirsNeedNormalize} foldernames need normalization");
                else
                    s.AppendLine($"1 foldername needs normalization");

                if (DirsNeedNormalizeRenamed > 0 || DirsNeedNormalizeFailed > 0) {
                    if (DirsNeedNormalizeRenamed > 0) {
                        if (IsPlural(DirsNeedNormalizeRenamed))
                            s.AppendLine($"   of which {DirsNeedNormalizeRenamed} foldernames were normalized succesfully");
                        else
                            s.AppendLine($"   of which 1 foldername was normalized succesfully");
                    }
                    if (DirsNeedNormalizeFailed > 0) {
                        if (IsPlural(DirsNeedNormalizeFailed))
                            s.AppendLine($"   with {DirsNeedNormalizeFailed} foldernames failed to be normalized");
                        else
                            s.AppendLine($"   with 1 foldername failed to be normalized");
                    }
                    if (DirsNeedNormalizeProducedDuplicate > 0) {
                        if (IsPlural(DirsNeedNormalizeProducedDuplicate))
                            s.AppendLine($"   {DirsNeedNormalizeProducedDuplicate} folders were renamed with 'Duplicate' suffixes");
                        else
                            s.AppendLine($"   1 folder was renamed with a 'Duplicate' suffix");
                    }
                } else {
                    if (DirsNeedNormalizeProducedDuplicate > 0) {
                        if (IsPlural(DirsNeedNormalizeProducedDuplicate))
                            s.AppendLine($"   of which {DirsNeedNormalizeProducedDuplicate} folders would get duplicate names and need to be renamed with 'Duplicate' suffixes");
                        else
                            s.AppendLine($"   of which 1 folder would get a duplicate name and needs to be renamed with a 'Duplicate' suffix");
                    }
                }
            }



            // INSERT REPORT FOR ILLEGALS FIX



            if (FilesWithDuplicateNames > 0) {
                if (IsPlural(FilesWithDuplicateNames))
                    s.AppendLine($"{FilesWithDuplicateNames} files have duplicate names in case sensitive domain");
                else
                    s.AppendLine($"1 file has a duplicate name in case sensitive domain");

                if (FilesWithDuplicateNamesRenamed > 0) {
                    if (IsPlural(FilesWithDuplicateNamesRenamed))
                        s.AppendLine($"   of which {FilesWithDuplicateNamesRenamed} files were renamed with 'Duplicate' suffixes");
                    else
                        s.AppendLine($"   of which 1 file was renamed with a 'Duplicate' suffix");
                }
                if (FilesWithDuplicateNamesFailed > 0) {
                    if (IsPlural(FilesWithDuplicateNamesFailed))
                        s.AppendLine($"   with {FilesWithDuplicateNamesFailed} files failed to be renamed");
                    else
                        s.AppendLine($"   with 1 file failed to be renamed");
                }
            }

            if (DirsWithDuplicateNames > 0) {
                if (IsPlural(DirsWithDuplicateNames))
                    s.AppendLine($"{DirsWithDuplicateNames} folders have duplicate names in case sensitive domain");
                else
                    s.AppendLine($"1 folder has a duplicate name in case sensitive domain");
                if (DirsWithDuplicateNamesRenamed > 0) {
                    if (IsPlural(DirsWithDuplicateNamesRenamed))
                        s.AppendLine($"   of which {DirsWithDuplicateNamesRenamed} folders were renamed with 'Duplicate' suffixes");
                    else
                        s.AppendLine($"   of which 1 folder was renamed with a 'Duplicate' suffix");
                }
                if (DirsWithDuplicateNamesFailed > 0) {
                    if (IsPlural(DirsWithDuplicateNamesFailed))
                        s.AppendLine($"   with {DirsWithDuplicateNamesFailed} failed to be renamed");
                    else
                        s.AppendLine($"   with 1 folder failed to be renamed");
                }
            }

            if (FilesNeedTrim > 0) {
                if (IsPlural(FilesNeedTrim))
                    s.AppendLine($"{FilesNeedTrim} filenames that have leading or trailing spaces");
                else
                    s.AppendLine($"1 filename that has leading or trailing spaces");
                if (FilesNeedTrimRenamed > 0 || FilesNeedTrimFailed > 0) {
                    if (FilesNeedTrimRenamed > 0) {
                        if (IsPlural(FilesNeedTrimRenamed))
                            s.AppendLine($"   of which {FilesNeedTrimRenamed} filernames were fixed");
                        else
                            s.AppendLine($"   of which 1 filesname were fixed");
                    }
                    if (FilesNeedTrimFailed > 0) {
                        if (IsPlural(FilesNeedTrimFailed))
                            s.AppendLine($"   with {FilesNeedTrimFailed} filenames failed to be fixed");
                        else
                            s.AppendLine($"   with 1 filename failed to be fixed");
                    }
                    if (FilesNeedTrimProducedDuplicate > 0) {
                        if (IsPlural(FilesNeedTrimProducedDuplicate))
                            s.AppendLine($"   {FilesNeedTrimProducedDuplicate} files were renamed with 'Duplicate' suffixes");
                        else
                            s.AppendLine($"   1 file was renamed with a 'Duplicate' suffix");
                    }
                } else {
                    if (FilesNeedTrimProducedDuplicate > 0) {
                        if (IsPlural(FilesNeedTrimProducedDuplicate))
                            s.AppendLine($"   of which {FilesNeedTrimProducedDuplicate} files would get duplicate names and need to be renamed with 'Duplicate' suffixes");
                        else
                            s.AppendLine($"   of which 1 files would get a duplicate name and needs to be renamed with a 'Duplicate' suffix");
                    }
                }
            }

            if (DirsNeedTrim > 0) {
                if (IsPlural(DirsNeedTrim))
                    s.AppendLine($"{DirsNeedTrim} foldernames that have leading or trailing spaces or trailing dots");
                else
                    s.AppendLine($"1 foldername that has leading or trailing spaces or trailing dots");
                if (DirsNeedTrimRenamed > 0 || DirsNeedTrimFailed > 0) {
                    if (DirsNeedTrimRenamed > 0) {
                        if (IsPlural(DirsNeedTrimRenamed))
                            s.AppendLine($"   of which {DirsNeedTrimRenamed} foldernames were fixed");
                        else
                            s.AppendLine($"   of which 1 foldername were fixed");
                    }
                    if (DirsNeedTrimFailed > 0) {
                        if (IsPlural(DirsNeedTrimFailed))
                            s.AppendLine($"   with {DirsNeedTrimFailed} foldernames failed to be fixed");
                        else
                            s.AppendLine($"   with 1 foldername failed to be fixed");
                    }
                    if (DirsNeedTrimProducedDuplicate > 0) {
                        if (IsPlural(DirsNeedTrimProducedDuplicate))
                            s.AppendLine($"   {DirsNeedTrimProducedDuplicate} folders were renamed with 'Duplicate' suffixes");
                        else
                            s.AppendLine($"   1 folders was renamed with a 'Duplicate' suffix");
                    }
                } else {
                    if (DirsNeedTrimProducedDuplicate > 0) {
                        if (IsPlural(DirsNeedTrimProducedDuplicate))
                            s.AppendLine($"   of which {DirsNeedTrimProducedDuplicate} folders would get duplicate names and need to be renamed with 'Duplicate' suffixes");
                        else
                            s.AppendLine($"   of which 1 folder would get a duplicate name and needs to be renamed with a 'Duplicate' suffix");
                    }
                }
            }

            //if (TooLongFilePaths > 0) {
            //    if (IsPlural(TooLongFilePaths))
            //        s.AppendLine($"{TooLongFilePaths} files have long path");
            //    else
            //        s.AppendLine($"1 file has long path");
            //}

            if (TooLongDirPaths > 0) {
                if (IsPlural(TooLongDirPaths))
                    s.AppendLine($"{TooLongDirPaths} long path branches found");
                else
                    s.AppendLine($"1 long path branch found");
            }

            if (s.ToString() == "") {
                s.AppendLine("Found nothing. Going home.");
            }

            if (SkippedDirectories > 0) {
                if (IsPlural(SkippedDirectories))
                    s.AppendLine($"{SkippedDirectories} macOS packages or symlinks skipped");
                else
                    s.AppendLine($"1 macOS package or symlink skipped");
            }

            if (UnaccesableDirs > 0) {
                if (IsPlural(UnaccesableDirs))
                    s.AppendLine($"{UnaccesableDirs} unaccessible folders");
                else
                    s.AppendLine($"1 unaccessible folder");
            }

            if (IOErrors > 0) {
                if (IsPlural(IOErrors))
                    s.AppendLine($"{IOErrors} I/O Errors");
                else
                    s.AppendLine($"1 I/O Error");
            }

            return s.ToString();
        }

        private bool IsPlural(int number)
        {
            return number == 1 ? false : true;
        }
    }


}
