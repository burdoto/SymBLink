using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace SymBLink {
    public class Ts4FileService : IDisposable {
        public static readonly char Sep = Path.DirectorySeparatorChar;
        public static readonly DirectoryInfo TmpDir;

        private readonly App _app;
        // ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
        private readonly FileSystemWatcher _watcher;
        public readonly string[] ExtBlacklist = {".crdownload"};

        public readonly string[] ExtWhitelist = {".zip", ".rar", ".package", ".ts4script"};

        static Ts4FileService() {
            TmpDir = Program.TmpDir.CreateSubdirectory("ts4");
        }

        public Ts4FileService(App app) {
            Console.WriteLine("[SymBLink:TS4] Initializing Ts4FileService...");
            _app = app;

            if (!_app.Settings.Valid) {
                Console.WriteLine("[SymBLink:TS4] Settings are invalid!");
                throw new InvalidDataException("Invalid Settings!");
            }

            _watcher = new FileSystemWatcher(_app.Settings.DownloadDir) {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName
            };

            _watcher.Created += HandleFile;
            _watcher.Renamed += HandleFile;

            _watcher.EnableRaisingEvents = true;
            
            Console.WriteLine("[SymBLink:TS4] Ts4FileService Ready!");
        }

        public void Dispose() {
            _watcher.Dispose();
            
            Console.WriteLine("[SymBLink:TS4] Ts4FileService was disposed");
        }

        private void HandleFile(object sender, FileSystemEventArgs e) {
            Console.WriteLine($"[SymBLink:TS4] Trying to handle FileEvent; [scope={e.ChangeType},path={e.FullPath}]");
            
            _app.Activity.LoadLevel = ActivityCompanion.Load.Low;

            var modId = e.Name.Substring(0, e.Name.LastIndexOf('.'));
            var targetFile = new FileInfo(e.FullPath);
            DirectoryInfo deflateDir = null, composeDir = null, modsDir = null;

            try {
                Console.Write($@"[SymBLink:TS4:{modId}] Checking file {targetFile.FullName}... ");

                if (!ExtWhitelist.Any(ext =>
                        targetFile.Extension.Equals(ext, StringComparison.OrdinalIgnoreCase))
                    && ExtBlacklist.Any(ext => targetFile.Extension.Equals(ext, StringComparison.OrdinalIgnoreCase))) {
                    Console.Write("INVALID\n");

                    _app.Activity.LoadLevel = ActivityCompanion.Load.Idle;
                    return;
                }

                Console.Write("OK!\n");
            }
            catch (Exception any) {
                Console.Write($"FAIL: {any.GetType()} - {any.Message}\n");
            }

            try {
                Console.Write($@"[SymBLink:TS4:{modId}] Preparing Paths... ");
                deflateDir = TmpDir.CreateSubdirectory(modId + Sep + "deflate");
                composeDir = TmpDir.CreateSubdirectory(modId + Sep + "compose");
                modsDir = new DirectoryInfo(_app.Settings.SimsDir + Sep + "Mods" + Sep + modId);
                Console.Write("OK!\n");
            }
            catch (Exception any) {
                Console.Write($"FAIL: {any.GetType()} - {any.Message}\n");
            }

            // ReSharper disable once ExpressionIsAlwaysNull
            if ((deflateDir == null) | (composeDir == null) | (modsDir == null)) {
                _app.Activity.LoadLevel = ActivityCompanion.Load.High;
                throw new NullReferenceException(
                    $"\nUnexpected null reference; stack: [deflateDir={deflateDir},composeDir={composeDir},modsDir={modsDir}]");
            }

            var assets = new List<FileInfo>();

            try {
                Console.Write($@"[SymBLink:TS4:{modId}] Gathering Assets... ");
                switch (targetFile.Extension) {
                    case ".package":
                    case ".ts4script":
                        // handle file
                        assets.Add(targetFile);

                        break;
                    default:
                        // handle zip
                        if (IsFileLocked(targetFile)) {
                            Console.Write(
                                $"INVALID: File {targetFile.FullName} is locked! Skipping\n");
                            break;
                        }

                        ZipFile.ExtractToDirectory(targetFile.FullName, deflateDir.FullName);
                        IterateAssets(deflateDir, assets);

                        if (assets.Count == 0)
                            Console.Write("INVALID: No valid assets found! Skipping\n");

                        break;
                }

                Console.Write("OK!\n");
            }
            catch (Exception any) {
                Console.Write($"FAIL: {any.GetType()} - {any.Message}\n");
            }

            try {
                Console.Write($@"[SymBLink:TS4:{modId}] Copying files and cleaning up... ");
                foreach (var asset in assets)
                    asset.CopyTo(composeDir.FullName);

                deflateDir.Delete(true);

                if (!modsDir.Exists)
                    modsDir.Create();

                var moveMethod = MoveHelper.Move(composeDir, modsDir);

                composeDir.Delete(true);

                Console.Write($"OK; used method {moveMethod}\n");
            }
            catch (Exception any) {
                Console.Write($"FAIL: {any.GetType()} - {any.Message}\n");
            }

            Console.WriteLine(
                $@"[SymBLink:TS4:{modId}] Successfully extracted mod {modId} to {modsDir.FullName}");
            _app.Activity.LoadLevel = ActivityCompanion.Load.Idle;
        }

        private List<FileInfo> IterateAssets(DirectoryInfo directoryInfo, List<FileInfo> yields) {
            foreach (var asset in directoryInfo.EnumerateFiles())
                if (asset.Extension.Equals(".package", StringComparison.OrdinalIgnoreCase)
                    || asset.Extension.Equals(".ts4script", StringComparison.OrdinalIgnoreCase))
                    yields.Add(asset);

            foreach (var assetDir in directoryInfo.EnumerateDirectories())
                yields.AddRange(IterateAssets(assetDir, yields));

            return yields;
        }

        // as posted on https://stackoverflow.com/questions/876473/is-there-a-way-to-check-if-a-file-is-in-use
        internal static bool IsFileLocked(FileInfo file) {
            try {
                using (var stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None)) {
                    stream.Close();
                }
            }
            catch (IOException) {
                //the file is unavailable because it is:
                //still being written to
                //or being processed by another thread
                //or does not exist (has already been processed)
                return true;
            }

            //file is not locked
            return false;
        }
    }
    public static class MoveHelper {
        public static Method Move(FileSystemInfo source, FileSystemInfo target) {
            //todo Rework this bullshit
        }

        private static bool MatchRoots(string path1, string path2) {
            return Path.GetPathRoot(path1)?.Equals(Path.GetPathRoot(path2)) ?? false;
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
                    // ReSharper disable once ConditionIsAlwaysTrueOrFalse
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