using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Windows.Forms;

namespace SymBLink.Old {
    public class Ts4Mover {
        private static readonly DirectoryInfo TmpDir = new DirectoryInfo(
            Path.GetTempPath() + Path.DirectorySeparatorChar + "org.comroid.symblink.ts4mover");

        private readonly DirectoryInfo _modsDir;
        private readonly FileSystemWatcher _watcher;

        internal Ts4Mover(string downloadsDir, string modsDir) {
            Application.ApplicationExit += (sender, args) => TmpDir.Delete(true);

            _modsDir = new DirectoryInfo(modsDir);
            _watcher = new FileSystemWatcher(downloadsDir);

            _watcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName;

            _watcher.Created += HandleFile;
            _watcher.Renamed += HandleFile;

            _watcher.EnableRaisingEvents = true;

            Console.WriteLine("Ts4Mover Ready!");
        }

        private void HandleFile(object sender, FileSystemEventArgs e) {
            var target = new FileInfo(e.FullPath);
            Console.WriteLine(
                $@"Processing file {e.FullPath}; ext={target.Extension},sco={e.ChangeType}...");
            var itTmpDir = TmpDir.FullName + Path.DirectorySeparatorChar +
                           e.Name.Substring(0, e.Name.LastIndexOf('.'));

            if (!(target.Extension.Equals(".zip", StringComparison.OrdinalIgnoreCase)
                  || target.Extension.Equals(".rar", StringComparison.OrdinalIgnoreCase)
                  || target.Extension.Equals(".package", StringComparison.OrdinalIgnoreCase)
                  || target.Extension.Equals(".ts4script", StringComparison.OrdinalIgnoreCase))) {
                Console.WriteLine($@"[{e.Name}] File is not an archive or mod file, aborted.");
                return;
            }

            if (IsFileLocked(target)) {
                Console.WriteLine($@"[{e.Name}]File is currently in use! Skipping for now.");
                return;
            }

            Console.WriteLine($@"[{e.Name}] Extracting to {itTmpDir}...");

            ZipFile.ExtractToDirectory(target.FullName, itTmpDir);
            var unpacked = new DirectoryInfo(itTmpDir);

            if (!unpacked.Exists) {
                Console.WriteLine($@"[{e.Name}] Unpacked Directory does not exist. Skipping.");
                return;
            }

            var modAssets = IterateAssets(unpacked);

            Console.WriteLine($@"[{e.Name}] Scanned Files; {modAssets.Count} assets found");

            if (modAssets.Count > 0) {
                foreach (var modAsset in modAssets)
                    if (modAsset.Directory?.Equals(unpacked) ?? false)
                        Console.WriteLine(
                            $@"[{e.Name}] Moving {modAsset.Name} to working dir; method={MoveHelper.Move(modAsset, unpacked)}");

                Console.WriteLine(
                    $@"[{e.Name}] Moving {unpacked.FullName} to {_modsDir.FullName}; method={MoveHelper.Move(unpacked, _modsDir)}");
            }

            Console.WriteLine($@"[{e.Name}] Finished.");
        }

        private List<FileInfo> IterateAssets(DirectoryInfo directoryInfo) {
            var yields = new List<FileInfo>();

            foreach (var asset in directoryInfo.EnumerateFiles())
                if (asset.Extension.Equals(".ts4script", StringComparison.OrdinalIgnoreCase)
                    || asset.Extension.Equals(".package", StringComparison.OrdinalIgnoreCase))
                    yields.Add(asset);

            foreach (var assetDir in directoryInfo.EnumerateDirectories())
                yields.AddRange(IterateAssets(assetDir));

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
}