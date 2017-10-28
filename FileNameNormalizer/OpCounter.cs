using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileNameNormalizer
{
    class OpCounter
    {
        public int FilesNeedNormalize { get; set; } = 0;
        public int DirsNeedNormalize { get; set; } = 0;
        public int FilesWithDuplicateNames { get; set; } = 0;
        public int DirsWithDuplicateNames { get; set; } = 0;
        public int FilesWithSpaces { get; set; } = 0;
        public int DirsWithSpaces { get; set; } = 0;
        public int FilesNeedNormalizeRenamed { get; set; } = 0;
        public int DirsNeedNormalizeRenamed { get; set; } = 0;
        public int FilesWithDuplicateNamesRenamed { get; set; } = 0;
        public int DirsWithDuplicateNamesRenamed { get; set; } = 0;
        public int FilesWithSpacesRenamed { get; set; } = 0;
        public int DirsWithSpacesRenamed { get; set; } = 0;
        public int FilesNeedNormalizeFailed { get; set; } = 0;
        public int DirsNeedNormalizeFailed { get; set; } = 0;
        public int FilesWithDuplicateNamesFailed { get; set; } = 0;
        public int DirsWithDuplicateNamesFailed { get; set; } = 0;
        public int FilesWithSpacesFailed { get; set; } = 0;
        public int DirsWithSpacesFailed { get; set; } = 0;
        public int FilesNeedNormalizeProducedDuplicate { get; set; } = 0;
        public int DirsNeedNormalizeProducedDuplicate { get; set; } = 0;
        public int FilesWithSpacesProducedDuplicate { get; set; } = 0;
        public int DirsWithSpacesProducedDuplicate { get; set; } = 0;

        public int FilesWithDuplicateNamesCreated { get; set; } = 0;
        public int DirsWithDuplicateNamesCreated { get; set; } = 0;

        public int TooLongPaths { get; set; } = 0;

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
                            s.AppendLine($"   {FilesNeedNormalizeProducedDuplicate} files were renamed with 'Duplicate' tags");
                        else
                            s.AppendLine($"   1 file was renamed with a 'Duplicate' tag");
                    }
                } else {
                    if (FilesNeedNormalizeProducedDuplicate > 0) {
                        if (IsPlural(FilesNeedNormalizeProducedDuplicate))
                            s.AppendLine($"   of which {FilesNeedNormalizeProducedDuplicate} files have duplicate names and need to be renamed");
                        else
                            s.AppendLine($"   of which 1 file has a duplicate name and needs to be renamed");
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
                            s.AppendLine($"   {DirsNeedNormalizeProducedDuplicate} folders were renamed with 'Duplicate' tags");
                        else
                            s.AppendLine($"   1 folder was renamed with a 'Duplicate' tag");
                    }
                } else {
                    if (DirsNeedNormalizeProducedDuplicate > 0) {
                        if (IsPlural(DirsNeedNormalizeProducedDuplicate))
                            s.AppendLine($"   of which {DirsNeedNormalizeProducedDuplicate} folders have duplicate names and need to be renamed");
                        else
                            s.AppendLine($"   of which 1 folder has a duplicate name and needs to be renamed");
                    }
                }
            }

            if (FilesWithDuplicateNames > 0) {
                if (IsPlural(FilesWithDuplicateNames))
                    s.AppendLine($"{FilesWithDuplicateNames} files have duplicate names in case sensitive domain");
                else
                    s.AppendLine($"1 file has a duplicate name in case sensitive domain");

                if (FilesWithDuplicateNamesRenamed > 0) {
                    if (IsPlural(FilesWithDuplicateNamesRenamed))
                        s.AppendLine($"   of which {FilesWithDuplicateNamesRenamed} files were renamed with 'Duplicate' tags");
                    else
                        s.AppendLine($"   of which 1 file was renamed with a 'Duplicate' tag");
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
                        s.AppendLine($"   of which {DirsWithDuplicateNamesRenamed} folders were renamed with 'Duplicate' tags");
                    else
                        s.AppendLine($"   of which 1 folder was renamed with a 'Duplicate' tag");
                }
                if (DirsWithDuplicateNamesFailed > 0) {
                    if (IsPlural(DirsWithDuplicateNamesFailed))
                        s.AppendLine($"   with {DirsWithDuplicateNamesFailed} failed to be renamed");
                    else
                        s.AppendLine($"   with 1 folder failed to be renamed");
                }

            }

            if (FilesWithSpaces > 0) {
                if (IsPlural(FilesWithSpaces))
                    s.AppendLine($"{FilesWithSpaces} filenames that have leading or trailing spaces. Not fixed");
                else
                    s.AppendLine($"1 filename that has leading or trailing spaces. Not fixed");
            }
            if (DirsWithSpaces > 0) {
                if (IsPlural(DirsWithSpaces))
                    s.AppendLine($"{DirsWithSpaces} foldernames that have leading or trailing spaces. Not fixed");
                else
                    s.AppendLine($"1 foldername that has leading or trailing spaces. Not fixed");
            }

            if (TooLongPaths > 0) {
                if (IsPlural(TooLongPaths))
                    s.AppendLine($"{TooLongPaths} files or folders have too long path. Not fixed");
                else
                    s.AppendLine($"{TooLongPaths} file or folder has too long path. Not fixed");
            }

            if (s.ToString() == "") {
                s.AppendLine("Found nothing. Going home.");
            }

            return s.ToString();
        }
        private bool IsPlural(int number)
        {
            return number == 1 ? false : true;
        }
    }


}
