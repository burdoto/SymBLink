using System;
using System.IO;

namespace SymBLink.Old {
    public static class LoggerUtil {
    }

    public static class MoveHelper {
        public static Method Move(FileSystemInfo source, FileSystemInfo target) {
            if (Path.GetPathRoot(source.FullName).Equals(Path.GetPathRoot(target.FullName),
                StringComparison.OrdinalIgnoreCase)) {
                if (source is FileInfo fil)
                    fil.MoveTo(target.FullName);
                else if (source is DirectoryInfo dir)
                    dir.MoveTo(target.FullName);

                return Method.Move;
            }

            // different volumes
            if (source is FileInfo) {
                File.Copy(source.FullName, target.FullName, true);
                File.Delete(source.FullName);
            }
            else if (source is DirectoryInfo) {
                DirectoryCopy(source.FullName + Path.DirectorySeparatorChar + "..", target.FullName, true);
                Directory.Delete(source.FullName, true);
            }
            else {
                throw new OperationCanceledException("source is of unknown type!");
            }

            return Method.CopyDestroy;
        }

        // as shared on https://docs.microsoft.com/en-us/dotnet/standard/io/how-to-copy-directories
        private static void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
        {
            // Get the subdirectories for the specified directory.
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);

            if (!dir.Exists)
            {
                throw new DirectoryNotFoundException(
                    "Source directory does not exist or could not be found: "
                    + sourceDirName);
            }

            DirectoryInfo[] dirs = dir.GetDirectories();
            // If the destination directory doesn't exist, create it.
            if (!Directory.Exists(destDirName))
            {
                Directory.CreateDirectory(destDirName);
            }
        
            // Get the files in the directory and copy them to the new location.
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string temppath = Path.Combine(destDirName, file.Name);
                file.CopyTo(temppath, false);
            }

            // If copying subdirectories, copy them and their contents to new location.
            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string temppath = Path.Combine(destDirName, subdir.Name);
                    DirectoryCopy(subdir.FullName, temppath, copySubDirs);
                }
            }
        }
    }

    public enum Method {
        Move,
        CopyDestroy
    }
}