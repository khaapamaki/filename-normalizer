### Filename Normalizer

Filename Normalizer is a tiny command line tool that fixes filenames that use wrong unicode normalization. 
This issue may happen on fileservers that are used improperly with different operating systems.

Note: Normal operation is to normalize to Form C when using Windows operating system.

```
Usage:
  fnamenorm <options> <path> [<path2>] [<path3>]
Options:
  /r            Recurses subdirectories
  /nc           Performs Form C normalization. Normal operation
  /nd           Performs Form D normalization. Reverse for Form C
  /i            Replaces illegal characters < > / \ | : * with underscore
  /b,/dup       Renames file and folder names that would have a duplicate name in a case-insensitive file system
                This is effective only when scanning case sensitive file system
  /t            Trims illegal folder names with trailing spaces. Same as option /t=dirright
  /t=all        Trims all file and folder names with leading and trailing space.
  /t=opt1,opt2  Specific trim instructions: base, ext, dir, baseleft, baseright, extleft, extright, dirleft, dirright
  /d            Processes folder names only
  /f            Processes filenames only
  /a            Sets options /nc /b /t /i at once.
  /p=<pattern>  Search pattern for files, eg. *.txt
  /rename       Does actual renaming instead of displaying info about what should be done
  /v            Verbose mode. Print out all files and folders in a tree
  /o            Only show folders that includes items to be fixed hiding items itself
  /l            Detailed report about long paths
  /hex          Shows hex codes for file and folder names

Note:           Without /rename option only dry run is performed without actual renaming

```
