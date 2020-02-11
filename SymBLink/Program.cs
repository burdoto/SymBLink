using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace SymBLink {
    public static class Program {
        public const string AppId = "org.comroid.symblink";

        public static readonly DirectoryInfo DataDir;
        public static readonly DirectoryInfo TmpDir;

        static Program() {
            Console.WriteLine("[SymBLink] Program PreInitialization");
            DataDir = new DirectoryInfo(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) +
                Path.DirectorySeparatorChar + AppId);
            TmpDir = new DirectoryInfo(
                Path.GetTempPath() + Path.DirectorySeparatorChar + AppId);

            Application.ApplicationExit += (sender, args) => {
                App.Instance.Dispose();
                TmpDir.Delete(true);
            };
            Console.WriteLine("[SymBLink] Program PreInitialization complete");
        }

        [STAThread]
        public static void Main() {
            Console.WriteLine("[SymBLink] Starting up...");
            App.Instance.ReInitialize(true);

            Console.WriteLine("[SymBLink] Running Application...");
            Application.Run(App.Instance);
        }
    }

    public class App : ApplicationContext {
        public static readonly App Instance;
        public static readonly Icon VanityIcon;

        private readonly Container _components = new Container();
        public readonly ActivityCompanion Activity;

        public readonly Settings Settings;
        public readonly NotifyIcon TrayIcon;
        public readonly ContextMenu TrayMenu;

        private Ts4FileService _fds;
        private bool _loaded;

        static App() {
            Console.WriteLine("[SymBLink] App PreInitialization");
            Instance = new App();
            VanityIcon = new Icon("Resources/icon-green.ico");
            Console.WriteLine("[SymBLink] App PreInitialization complete");
        }

        private App() {
            Console.WriteLine("[SymBLink] Initializing App...");
            if (!Program.DataDir.Exists)
                Program.DataDir.Create();
            if (!Settings.File.Exists)
                File.WriteAllText(Settings.File.FullName, "{}", Encoding.UTF8);

            Settings = JsonConvert.DeserializeObject<Settings>(
                File.ReadAllText(Settings.File.FullName, Encoding.UTF8));
            Activity = new ActivityCompanion();
            TrayIcon = new NotifyIcon(_components);
            TrayMenu = new ContextMenu();

            _components.Add(TrayIcon, "trayIcon");
            _components.Add(TrayMenu, "trayMenu");
            Console.WriteLine("[SymBLink] Initializing App complete!");
        }

        private void Load() {
            Console.WriteLine("[SymBLink] Loading UI...");

            TrayIcon.Text = ToString();
            TrayIcon.Visible = true;

            var configure = new MenuItem("Configure...", (sender, args) => Settings.Configurator());
            configure.DefaultItem = true;
            TrayIcon.DoubleClick += (sender, args) => configure.PerformClick();

            MenuItem[] menuItems = {
                configure,
                new MenuItem("Why does the Icon look so bad...?", new[] {
                    new MenuItem {
                        Text =
                            "Because Windows requires me to replace the white with your TaskBar color.",
                        Enabled = false
                    },
                    new MenuItem {
                        Text = "This is retarded and absolutely overkill for such an Application.",
                        Enabled = false
                    }
                }),
                new MenuItem("Exit", (sender, args) => Application.Exit())
            };

            for (var i = 0; i < menuItems.Length; i++) {
                var it = menuItems[i];

                it.Visible = true;
                it.Index = i;
            }

            TrayMenu.MenuItems.AddRange(menuItems);

            TrayIcon.ContextMenu = TrayMenu;

            _loaded = true;

            Console.WriteLine("[SymBLink] UI Initialized!");
        }

        public void ReInitialize(bool reload) {
            Console.WriteLine($"[SymBLink] Reinitializing. Reloading? {!_loaded || reload}");

            Activity.LoadLevel = ActivityCompanion.Load.High;

            if (!_loaded || reload) Load();

            _fds?.Dispose();
            _fds = new Ts4FileService(this);

            Activity.LoadLevel = ActivityCompanion.Load.Idle;

            Console.WriteLine("[SymBLink] Reinitialization Complete.");
        }

        public override string ToString() {
            return $@"SymBLink v{Application.ProductVersion}";
        }

        protected override void Dispose(bool disposing) {
            Console.WriteLine("[SymBLink] App is being disposed!");

            Settings.Dispose();

            _components.Dispose();
        }
    }
}