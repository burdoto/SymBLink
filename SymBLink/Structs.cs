using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using Newtonsoft.Json;
using Button = System.Windows.Controls.Button;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using Label = System.Windows.Controls.Label;
using TextBox = System.Windows.Controls.TextBox;

namespace SymBLink {
    public class ActivityCompanion {
        public enum Load {
            Idle,
            Low,
            High
        }

        private static readonly Dictionary<Load, Icon> iconCache;
        private Load _loadLevel = Load.Idle;

        static ActivityCompanion() {
            iconCache = new Dictionary<Load, Icon>();
            iconCache.Add(Load.Idle, App.VanityIcon);
            iconCache.Add(Load.Low, new Icon("Resources/icon-yellow.ico"));
            iconCache.Add(Load.High, new Icon("Resources/icon-red.ico"));
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

        private ConfiguratorForm _configurator;

        [JsonProperty] public int Version { get; set; } = 1;

        [JsonProperty]
        public string DownloadDir { get; set; } = TryFind(AutoFindProperty.DownloadDir);

        [JsonProperty] public string SimsDir { get; set; } = TryFind(AutoFindProperty.SimsDir);

        public void Dispose() {
            var data = JsonConvert.SerializeObject(this, Formatting.Indented);

            System.IO.File.WriteAllText(File.FullName, data);
        }

        private static T TryFind<T>(GatherableProperty<T> property) {
            return property.Provider.Invoke();
        }

        public void Configurator() {
            if (_configurator != null) {
                if (_configurator.Visibility == Visibility.Visible) {
                    _configurator.Visibility = Visibility.Collapsed;
                }
                else {
                    _configurator.Visibility = Visibility.Visible;
                    _configurator.Topmost = true;
                }

                return;
            }

            _configurator = new ConfiguratorForm(this);
            _configurator.Closed += (sender, args) => _configurator = null;
            _configurator.Visibility = Visibility.Visible;

            _configurator.Activate();
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

        public sealed class ConfiguratorForm : Window, IDisposable {
            public const int TextWidth = 420;
            public const int TextHeight = 22;
            public const int BtnWidth = 22;
            public const int BtnHeight = BtnWidth;

            private readonly Settings _settings;

            public ConfiguratorForm(Settings settings) {
                _settings = settings;
                Title = App.Instance + " - Configuration";
                Icon = BitmapFrame.Create(new Uri("Resources/icon-green.png", UriKind.Relative));
                SizeToContent = SizeToContent.WidthAndHeight;
                Left = Screen.PrimaryScreen.Bounds.Width * 0.6;
                Top = Screen.PrimaryScreen.Bounds.Height * 0.7;


                var mainPanel = new DockPanel {
                    Visibility = Visibility.Visible,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Top
                };
                DockPanel.SetDock(mainPanel, Dock.Top);


                var downloadDirPanel = new DockPanel {
                    Visibility = Visibility.Visible,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Top
                };
                DockPanel.SetDock(downloadDirPanel, Dock.Top);
                mainPanel.Children.Add(downloadDirPanel);

                var downloadDirLabel = new TextBlock {
                    Text = "Download Directory - Monitored Directory",
                    IsEnabled = false,
                    Visibility = Visibility.Visible,
                    Width = TextWidth + BtnWidth,
                    Height = TextHeight
                };
                DockPanel.SetDock(downloadDirLabel, Dock.Top);
                downloadDirPanel.Children.Add(downloadDirLabel);

                var downloadDirSelected = new TextBox {
                    Text = _settings.DownloadDir,
                    IsEnabled = false,
                    Visibility = Visibility.Visible,
                    Width = TextWidth,
                    Height = TextHeight,
                    MaxLines = 1
                };
                DockPanel.SetDock(downloadDirSelected, Dock.Left);
                downloadDirPanel.Children.Add(downloadDirSelected);

                var downloadDirChange = new Button {
                    Content = "...",
                    Width = BtnWidth,
                    Height = BtnHeight,
                    Visibility = Visibility.Visible
                };
                downloadDirChange.Click += (sender, args) => {
                    var dir = SelectDir(_settings.DownloadDir);
                    DownloadDir = dir;
                    downloadDirSelected.Text = dir;
                };
                DockPanel.SetDock(downloadDirChange, Dock.Right);
                downloadDirPanel.Children.Add(downloadDirChange);


                var simsDirPanel = new DockPanel {
                    Visibility = Visibility.Visible,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Top
                };
                DockPanel.SetDock(simsDirPanel, Dock.Top);
                mainPanel.Children.Add(simsDirPanel);

                var simsDirLabel = new TextBlock {
                    Text = "Sims Data Directory - Usually: <Documents>\\Electronic Arts\\The Sims 4",
                    IsEnabled = false,
                    Visibility = Visibility.Visible,
                    Width = TextWidth + BtnWidth,
                    Height = TextHeight
                };
                DockPanel.SetDock(simsDirLabel, Dock.Top);
                simsDirPanel.Children.Add(simsDirLabel);

                var simsDirSelected = new TextBox {
                    Text = _settings.SimsDir,
                    IsEnabled = false,
                    Visibility = Visibility.Visible,
                    Width = TextWidth,
                    Height = TextHeight,
                    MaxLines = 1
                };
                DockPanel.SetDock(simsDirSelected, Dock.Left);
                simsDirPanel.Children.Add(simsDirSelected);

                var simsDirChange = new Button {
                    Content = "...",
                    Width = BtnWidth,
                    Height = BtnWidth,
                    Visibility = Visibility.Visible
                };
                simsDirChange.Click += (sender, args) => {
                    var dir = SelectDir(_settings.SimsDir);
                    SimsDir = dir;
                    simsDirSelected.Text = dir;
                };
                DockPanel.SetDock(simsDirChange, Dock.Right);
                simsDirPanel.Children.Add(simsDirChange);


                var apply = new Button {
                    Content = "Apply",
                    Width = TextWidth + BtnWidth,
                    Height = BtnHeight,
                    Visibility = Visibility.Visible
                };
                apply.Click += (sender, args) => {
                    ApplyChanges();
                    Dispose();
                };
                DockPanel.SetDock(apply, Dock.Top);
                mainPanel.Children.Add(apply);


                Content = mainPanel;
            }

            private string DownloadDir { get; set; } = null;
            private string SimsDir { get; set; } = null;

            public void Dispose() {
                _settings._configurator = null;
                Close();
            }

            private void ApplyChanges() {
                if (DownloadDir != null) 
                    _settings.DownloadDir = DownloadDir;
                if (SimsDir != null)
                    _settings.SimsDir = SimsDir;

                App.Instance.ReInitialize();
            }

            private string SelectDir(string current) {
                var browserDialog = new FolderBrowserDialog();
                browserDialog.SelectedPath = current;

                var result = browserDialog.ShowDialog();

                return browserDialog.SelectedPath;
            }
        }
    }
}