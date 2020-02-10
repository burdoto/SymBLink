using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;
using Button = System.Windows.Controls.Button;
using Control = System.Windows.Forms.Control;
using Label = System.Windows.Forms.Label;
using Orientation = System.Windows.Controls.Orientation;
using TextBox = System.Windows.Controls.TextBox;

namespace SymBLink {
    public class ActivityCompanion {
        private static readonly Dictionary<Load, Icon> iconCache;
        private Load _loadLevel = Load.Idle;

        static ActivityCompanion() {
            iconCache = new Dictionary<Load, Icon>();
            iconCache.Add(Load.Idle, App.VanityIcon);
            iconCache.Add(Load.Low, new Icon("Resources/icon-yellow.ico"));
            iconCache.Add(Load.High, new Icon("Resources/icon-red.ico"));
        }

        public enum Load {
            Idle,
            Low,
            High
        }

        public Load LoadLevel {
            set {
                Icon buf;
                iconCache.TryGetValue(value, out buf);
                App.Instance.TrayIcon.Icon = buf;
                
                _loadLevel = value;
            }
            get => _loadLevel;
        }
    }

    public class Settings : IDisposable {
        public static readonly FileInfo File =
            new FileInfo(Program.DataDir.FullName + Path.DirectorySeparatorChar + "config.json");

        private ConfiguratorForm _form; 

        [JsonProperty]
        public int Version { get; set; } = 1;
        [JsonProperty]
        public string DownloadDir { get; set; } = TryFind(AutoFindProperty.DownloadDir);
        [JsonProperty] 
        public string SimsDir { get; set; } = TryFind(AutoFindProperty.SimsDir);

        public void Dispose() {
            _form?.Components.Dispose();
            
            throw new NotImplementedException();
        }

        private static T TryFind<T>(GatherableProperty<T> property) {
            return property.Provider.Invoke();
        }

        private class AutoFindProperty {
            public static readonly GatherableProperty<string> DownloadDir =
                new GatherableProperty<string>(() => {
                    var suggestedPath =
                        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                        + Path.DirectorySeparatorChar + ".."
                        + Path.DirectorySeparatorChar + "Downloads";

                    return System.IO.File.Exists(suggestedPath) ? suggestedPath : null;
                });

            public static readonly GatherableProperty<string> SimsDir =
                new GatherableProperty<string>(() => {
                    var eaDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                                + Path.DirectorySeparatorChar + "Electronic Arts";

                    return eaDir + Path.DirectorySeparatorChar +
                           new DirectoryInfo(eaDir).EnumerateDirectories("*Sims 4*")
                               .First()?.Name ?? "The Sims 4";
                });
        }

        private sealed class GatherableProperty<T> : AutoFindProperty {
            internal readonly Func<T> Provider;

            public GatherableProperty(Func<T> provider) {
                Provider = provider;
            }
        }

        public void Configurator() {
            if (_form != null) {
                _form.Visibility = Visibility.Visible;
                _form.Topmost = true;
                
                return;
            }

            _form = new ConfiguratorForm(this);
            _form.Closed += (sender, args) => _form = null;

            _form.Activate();
        }

        public sealed class ConfiguratorForm : Window {
            private readonly Settings _settings;

            public ConfiguratorForm(Settings settings) {
                _settings = settings;
                Title = App.Instance.ToString();
                Icon = BitmapFrame.Create(new Uri("Resources/icon-green.ico", UriKind.Relative));

                var mainPanel = new StackPanel();
                mainPanel.Orientation = Orientation.Vertical;
                
                
                var downloadDirPanel = new StackPanel();
                downloadDirPanel.Orientation = Orientation.Horizontal;
                mainPanel.Children.Add(downloadDirPanel);
                
                var downloadDirSelected = new TextBox();
                downloadDirSelected.Text = _settings.DownloadDir;
                downloadDirSelected.IsEnabled = false;
                
                var downloadDirChange = new Button();
                downloadDirChange.Content = "...";
                downloadDirChange.Click += (sender, args) => _settings.DownloadDir = SelectDir();
                
                
                var simsDirPanel = new StackPanel();
                simsDirPanel.Orientation = Orientation.Horizontal;
                mainPanel.Children.Add(simsDirPanel);

                var selectDLLabel = new Label();
                selectDLLabel.Text = "Select your Downloads directory";
                selectDLLabel.Visible = true;
                Components.Add(selectDLLabel, "selectDL-label");
                var selectDLDir = new FolderBrowserDialog();
                selectDLDir.RootFolder = Environment.SpecialFolder.MyDocuments;
                Components.Add(selectDLDir, "selectDL");

                var selectTSLabel = new Label();
                selectTSLabel.Text = "Select your Documents/.../Sims directory";
                selectTSLabel.Visible = true;
                Components.Add(selectTSLabel, "selectTS-label");
                var selectTSDir = new FolderBrowserDialog();
                selectTSDir.RootFolder = Environment.SpecialFolder.MyDocuments;
                Components.Add(selectDLDir, "selectTS");

                Control[] controls = {
                    selectDLLabel, selectDLDir,
                    selectTSLabel, selectTSDir
                };
                Controls.AddRange();
            }

            private string SelectDir() {
                var browserDialog = new FolderBrowserDialog();
                browserDialog.RootFolder = Environment.SpecialFolder.MyDocuments;
                browserDialog.ShowDialog(this)
            }
        }
    }
}