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

        public override string ToString()
        {
            StringBuilder s = new StringBuilder(1000);

            if (FilesNeedNormalize > 0) {
                s.AppendLine($"{FilesNeedNormalize} files need normalization");

                if (FilesNeedNormalizeRenamed > 0 || FilesNeedNormalizeFailed > 0) {
                    if (FilesNeedNormalizeRenamed > 0) {
                        s.AppendLine($"   of which {FilesNeedNormalizeRenamed} were normalized");
                    }
                    if (FilesNeedNormalizeFailed > 0) {
                        s.AppendLine($"   with {FilesNeedNormalizeFailed} failed to be normalized");
                    }
                    if (FilesNeedNormalizeProducedDuplicate > 0) {
                        s.AppendLine($"   {FilesNeedNormalizeProducedDuplicate} files were renamed with 'Duplicate' tag");
                    }
                } else {
                    if (FilesNeedNormalizeProducedDuplicate > 0) {
                        s.AppendLine($"   of which {FilesNeedNormalizeProducedDuplicate} will produce an existing name");
                    }
                }
            }

            if (DirsNeedNormalize > 0) {
                s.AppendLine($"{DirsNeedNormalize} folders need normalization");

                if (DirsNeedNormalizeRenamed > 0 || DirsNeedNormalizeFailed > 0) {
                    if (DirsNeedNormalizeRenamed > 0) {
                        s.AppendLine($"   of which {DirsNeedNormalizeRenamed} were normalized");
                    }
                    if (DirsNeedNormalizeFailed > 0) {
                        s.AppendLine($"   with {DirsNeedNormalizeFailed} failed to be normalized");
                    }
                    if (DirsNeedNormalizeProducedDuplicate > 0) {
                        s.AppendLine($"   {DirsNeedNormalizeProducedDuplicate} folders were renamed with 'Duplicate' tag");
                    }
                } else {
                    if (DirsNeedNormalizeProducedDuplicate > 0) {
                        s.AppendLine($"   of which {DirsNeedNormalizeProducedDuplicate} will produce an existing name");
                    }
                }
            }

            //if (DirsNeedNormalize > 0) {
            //    s.Append($"{DirsNeedNormalize} folders need normalization");
            //    if (DirsNeedNormalizeProducedDuplicate > 0) {
            //        s.AppendLine();
            //        s.AppendLine($" of which {DirsNeedNormalizeProducedDuplicate} will produce an existing name");
            //    } else {
            //        s.AppendLine();
            //    }
            //}

            //if (FilesNeedNormalizeRenamed > 0 || FilesNeedNormalizeFailed > 0) {
            //    s.Append($"{FilesNeedNormalize} files need normalization");
            //    if (FilesNeedNormalizeRenamed > 0) {
            //        s.AppendLine();
            //        s.Append($"   of which {FilesNeedNormalizeRenamed} were normalized");
            //    }
            //    if (FilesNeedNormalizeFailed > 0) {
            //        s.AppendLine();
            //        s.Append($"   with {FilesNeedNormalizeFailed} failed to be normalized");
            //    }
            //    s.AppendLine();

            //    if (FilesNeedNormalizeProducedDuplicate > 0) {
            //        s.AppendLine($"    {FilesNeedNormalizeProducedDuplicate} files were renamed with 'Duplicate' tag");
            //    }
            //}

            //if (DirsNeedNormalizeRenamed > 0 || DirsNeedNormalizeFailed > 0) {
            //    s.AppendLine($"{DirsNeedNormalize} folders need normalization");
            //    if (DirsNeedNormalizeRenamed > 0) {
            //        s.AppendLine($"   of which {DirsNeedNormalizeRenamed} were renamed");
            //    }
            //    if (DirsNeedNormalizeFailed > 0) {
            //        s.AppendLine($"   with {DirsNeedNormalizeFailed} failed to be renamed");
            //    }

            //    if (DirsNeedNormalizeProducedDuplicate > 0) {
            //        s.AppendLine($"    {DirsNeedNormalizeProducedDuplicate} folders were renamed with 'Duplicate' tag");
            //    }
            //}

            if (FilesWithDuplicateNames > 0) {
                s.AppendLine($"{FilesWithDuplicateNames} files have a duplicate name in case sensitive domain");
                if (FilesWithDuplicateNamesRenamed > 0)
                    s.AppendLine($"   of which {FilesWithDuplicateNamesRenamed} were renamed with 'Duplicate' tag");
                if (FilesWithDuplicateNamesFailed > 0)
                    s.AppendLine($"   with {FilesWithDuplicateNamesFailed} failed to be renamed");
            }
            if (DirsWithDuplicateNames > 0) {
                s.AppendLine($"{DirsWithDuplicateNames} folders have a duplicate name in case sensitive domain");
                if (DirsWithDuplicateNamesRenamed > 0)
                    s.AppendLine($"   of which {DirsWithDuplicateNamesRenamed} were renamed with 'Duplicate' tag");
                if (DirsWithDuplicateNamesFailed > 0)
                    s.AppendLine($"   with {DirsWithDuplicateNamesFailed} failed to be renamed");
            }

            if (s.ToString() == "") {
                s.AppendLine("Found Nothing.");
            }

            return s.ToString();
        }


        private int numberOfMessages()
        {
            int msgCount = 0;




            return msgCount;
        }
    }


}
