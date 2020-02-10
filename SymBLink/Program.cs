using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace SymBLink {
    public static class Program {
        public static readonly DirectoryInfo DataDir = new DirectoryInfo(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) +
            Path.DirectorySeparatorChar + "org.comroid.symblink");

        [STAThread]
        public static void Main() {
            App.Instance.Load();

            Application.Run(App.Instance);
        }
    }

    public class App : ApplicationContext {
        public static readonly App Instance = new App();
        public static readonly Icon VanityIcon = new Icon("Resources/icon-green.ico");

        private readonly Container _components = new Container();
        public readonly ActivityCompanion Activity;

        public readonly Settings Settings;
        public readonly NotifyIcon TrayIcon;
        public readonly ContextMenu TrayMenu;

        private App() {
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
        }

        public void Load() {
            Activity.LoadLevel = ActivityCompanion.Load.High;

            TrayIcon.Text = ToString();
            TrayIcon.Visible = true;

            var configure = new MenuItem("Configure...", (sender, args) => Settings.Configurator());
            configure.DefaultItem = true;
            TrayIcon.DoubleClick += (sender, args) => configure.PerformClick();
            
            MenuItem[] menuItems = {
                configure,
                new MenuItem("Exit", (sender, args) => Application.Exit())
            };

            for (var i = 0; i < menuItems.Length; i++) {
                var it = menuItems[i];

                it.Visible = true;
                it.Index = i;
            }

            TrayMenu.MenuItems.AddRange(menuItems);

            TrayIcon.ContextMenu = TrayMenu;
        }

        public override string ToString() {
            return $@"SymBLink v{Application.ProductVersion}";
        }

        protected override void Dispose(bool disposing) {
            _components.Dispose();
        }
    }
}