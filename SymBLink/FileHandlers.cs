using System;
using System.Collections.Generic;
using System.Diagnostics;
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

            if (!_app.Settings.Valid)
                Console.Error.WriteLine("[SymBLink:TS4] Settings are invalid! Please fix them using the Configurator.");

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
            Console.WriteLine(
                $"[SymBLink:TS4] Trying to handle FileEvent; [scope={e.ChangeType},path={e.FullPath}]");

            var modId = e.Name.Substring(0, e.Name.LastIndexOf('.'));
            var modTmpDir = TmpDir.CreateSubdirectory(modId);
            DirectoryInfo deflateDir = null, composeDir = null, modsDir = null;

            try {
                _app.Activity.LoadLevel = ActivityCompanion.Load.Low;
                var targetFile = new FileInfo(e.FullPath);

                try {
                    Console.Write(
                        $@"[SymBLink:TS4:{modId}] Checking file {targetFile.FullName}... ");

                    if (!ExtWhitelist.Any(ext =>
                            targetFile.Extension.Equals(ext, StringComparison.OrdinalIgnoreCase))
                        && ExtBlacklist.Any(ext =>
                            targetFile.Extension.Equals(ext, StringComparison.OrdinalIgnoreCase))) {
                        Console.Write("INVALID\n");

                        _app.Activity.LoadLevel = ActivityCompanion.Load.Idle;
                        return;
                    }

                    Console.Write("OK!\n");
                }
                catch {
                    Console.Write("FAIL\n");
                    throw;
                }

                try {
                    Console.Write($@"[SymBLink:TS4:{modId}] Preparing Paths... ");
                    deflateDir = modTmpDir.CreateSubdirectory("deflate");
                    composeDir = modTmpDir.CreateSubdirectory("compose");
                    modsDir = new DirectoryInfo(_app.Settings.SimsDir + Sep + "Mods" + Sep + modId);
                    Console.Write("OK!\n");
                }
                catch {
                    Console.Write("FAIL\n");
                    throw;
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
                catch {
                    Console.Write("FAIL\n");
                    throw;
                }

                try {
                    Console.Write($@"[SymBLink:TS4:{modId}] Copying files... ");
                    foreach (var asset in assets)
                        MoveHelper.Move(asset, composeDir);

                    // if (!modsDir.Exists)
                    //     modsDir.Create();

                    MoveHelper.Move(composeDir, modsDir);

                    Console.Write("OK!\n");
                }
                catch {
                    Console.Write("FAIL\n");
                    throw;
                }

                Console.WriteLine(
                    $@"[SymBLink:TS4:{modId}] Successfully extracted mod {modId} to {modsDir.FullName}");
            }
            catch (Exception any) {
                Console.WriteLine(
                    $"[SymBLink:TS4:{modId}] Could not extract mod: {any.GetType()} - {any.Message}\nException Information:\n{e}");

                if (modsDir?.Exists ?? false)
                    modsDir.Delete(true);
            }
            finally {
                Console.WriteLine($@"[SymBLink:TS4:{modId}] Cleaning up...");

                if (modTmpDir.Exists)
                    modTmpDir.Delete(true);
            }

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
        public enum FsType {
            Dir,
            File
        }

        public static void Move(FileSystemInfo source, FileSystemInfo target) {
            Debug.Write("\n" +
                        $"[SymBLink:Move] Moving {source.GetType()} to {target.GetType()}:\n" +
                        $"\t-\t{source.FullName}\n" +
                        $"\t-\t{target.FullName}\n" +
                        "...");

            var mount = MountRelation(source, target);

            if (Is(FsType.Dir, source)) // source is dir
                MoveDir(mount, source as DirectoryInfo, target);
            else // source is file
                MoveFile(mount, source as FileInfo, target);
        }

        private static void MoveFile(Mount mount, FileInfo source, FileSystemInfo target) {
            source.MoveTo(DirName(target, source.Name));
        }

        private static void MoveDir(Mount mount, DirectoryInfo source, FileSystemInfo target) {
            var destDirName = DirName(target);

            switch (mount) {
                case Mount.Same:
                    source.MoveTo(destDirName);
                    break;
                case Mount.Different:
                    foreach (var file in source.GetFiles()) {
                        var tempPath = Path.Combine(destDirName, file.Name);
                        MoveFile(mount, file, GetFSI(tempPath));
                    }

                    foreach (var sub in source.GetDirectories()) {
                        var tempPath = Path.Combine(destDirName, sub.Name);
                        MoveDir(mount, sub, GetFSI(tempPath));
                    }

                    source.Delete(true);

                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(mount), mount, null);
            }
        }

        private static FileSystemInfo GetFSI(string path) {
            return Directory.Exists(path) || path.EndsWith(Path.DirectorySeparatorChar.ToString())
                ? (FileSystemInfo) new DirectoryInfo(path)
                : new FileInfo(path);
        }

        private static string DirName(FileSystemInfo fsi, string sourceName = null) {
            return (fsi as FileInfo)?.DirectoryName ?? fsi.FullName +
                   (sourceName == null ? "" : Path.DirectorySeparatorChar + sourceName);
        }

        public static bool Is(FsType type, FileSystemInfo path) {
            switch (type) {
                case FsType.Dir:
                    return path is DirectoryInfo;
                case FsType.File:
                    return path is FileInfo;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }

        private static Mount MountRelation(FileSystemInfo path1, FileSystemInfo path2) {
            return Path.GetPathRoot(path1.FullName).Equals(Path.GetPathRoot(path2.FullName))
                ? Mount.Same
                : Mount.Different;
        }

        // as shared on https://docs.microsoft.com/en-us/dotnet/standard/io/how-to-copy-directories
        private static void DirectoryCopy(string sourceDirName, string destDirName,
            bool copySubDirs) {
        }

        private enum Mount {
            Same,
            Different
        }
    }
}