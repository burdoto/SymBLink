using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using Application = System.Windows.Forms.Application;
using Button = System.Windows.Controls.Button;
using ContextMenu = System.Windows.Forms.ContextMenu;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using MenuItem = System.Windows.Forms.MenuItem;
using Orientation = System.Windows.Controls.Orientation;
using Panel = System.Windows.Controls.Panel;

namespace SymBLink.Old {
    internal class App : ApplicationContext {
        internal static readonly App Instance = new App();

        private readonly IContainer _components;
        private readonly Window _window;

        private App() {
            WhenDebug(() => Console.WriteLine("Debug Mode ON!"));

            //_window.Initialized += null; // todo needs serialization
            //_window.Loaded += null; // todo move ui initialization here

            _components = new Container();
            _window = new Window();
            _window.Visibility = Visibility.Hidden;
            _window.Width = 720;
            _window.Height = 300;
            _window.Closed += (sender, args) => _window.Visibility = Visibility.Hidden;

            #region Generate UI

            // window
            var grid = new Grid();
            PopulateSelf(grid);

            // tray
            var trayMenu = new ContextMenu();
            PopulateTrayMenu(trayMenu);

            var trayIcon = new NotifyIcon(_components);
            trayIcon.Icon = new Icon("Resources/tray.ico");

            trayIcon.ContextMenu = trayMenu;

            trayIcon.Text = _window.Name;
            trayIcon.Visible = true;
            trayIcon.DoubleClick += ToggleWindow;

            #endregion
        }

        private void PopulateSelf(Grid grid) {
            _window.Title = $@"SymBLink v{Application.ProductVersion}";

            #region Members

            var createPreset = new Button();
            createPreset.Content = "Create Preset";
            createPreset.Click += (sender, args) => Console.WriteLine("Create");

            var editPreset = new Button();
            editPreset.Content = "Edit Preset";
            editPreset.Click += (sender, args) => Console.WriteLine("Edit");

            var deletePreset = new Button();
            deletePreset.Content = "Delete Preset";
            deletePreset.Click += (sender, args) => Console.WriteLine("Delete");

            var submitBox = new StackPanel();
            submitBox.Orientation = Orientation.Vertical;
            submitBox.HorizontalAlignment = HorizontalAlignment.Stretch;
            submitBox.VerticalAlignment = VerticalAlignment.Stretch;

            var revertAll = new Button();
            revertAll.Content = "Revert all Modifications";
            revertAll.Click += (sender, args) => Console.WriteLine("Revert");
            revertAll.HorizontalAlignment = HorizontalAlignment.Stretch;
            revertAll.VerticalAlignment = VerticalAlignment.Stretch;

            var apply = new Button();
            apply.Content = "Apply current Modifications";
            apply.Click += (sender, args) => Console.WriteLine("Apply");
            apply.HorizontalAlignment = HorizontalAlignment.Stretch;
            apply.VerticalAlignment = VerticalAlignment.Stretch;

            submitBox.Children.Add(revertAll);
            submitBox.Children.Add(apply);

            UIElement presetBox = GenPresetList();

            #endregion

            #region Grid

            // Todo: Setup Grid
            // https://docs.microsoft.com/en-us/dotnet/framework/wpf/controls/how-to-create-a-grid-element
            grid.Width = _window.Width;
            grid.Height = _window.Height;
            grid.HorizontalAlignment = HorizontalAlignment.Stretch;
            grid.VerticalAlignment = VerticalAlignment.Stretch;
            WhenDebug(() => grid.ShowGridLines = true);

            var col1 = new ColumnDefinition();
            var col2 = new ColumnDefinition();
            var col3 = new ColumnDefinition();
            var col4 = new ColumnDefinition();
            grid.ColumnDefinitions.Add(col1);
            grid.ColumnDefinitions.Add(col2);
            grid.ColumnDefinitions.Add(col3);
            grid.ColumnDefinitions.Add(col4);

            var row1 = new RowDefinition();
            var row2 = new RowDefinition();
            grid.RowDefinitions.Add(row1);
            grid.RowDefinitions.Add(row2);

            Grid.SetColumn(createPreset, 0);
            Grid.SetColumn(editPreset, 1);
            Grid.SetColumn(deletePreset, 2);
            Grid.SetColumn(submitBox, 3);
            
            Grid.SetRow(presetBox, 1);
            Grid.SetColumnSpan(presetBox, 4);

            foreach (UIElement child in new[]{createPreset, editPreset, deletePreset, submitBox, presetBox})
                grid.Children.Add(child);

            #endregion

            _window.Content = grid;
        }

        private Panel GenPresetList() {
            return new StackPanel(); //todo
        }

        [Conditional("DEBUG")]
        public static void WhenDebug(Action perform) {
            perform.Invoke();
        }

        [STAThread]
        public static void OldMain() {
            Console.WriteLine("STARTING...");

            var ts4Mover = new Ts4Mover("D:\\Downloads\\sems4cc", "D:\\Dokumente\\Electronic Arts\\The Sims 4\\Mods");

            Application.Run(Instance);
            Instance._components?.Dispose();
            
            Console.WriteLine("GOODBYE");
        }

        private void PopulateTrayMenu(ContextMenu contextMenu) {
            var configure = new MenuItem();
            var exit = new MenuItem();

            contextMenu.MenuItems.AddRange(new[] {configure, exit});

            configure.Index = 0;
            configure.Text = "&Configure";
            configure.DefaultItem = true;
            configure.Click += ToggleWindow;

            exit.Index = contextMenu.MenuItems.Count - 1;
            exit.Text = "E&xit";
            exit.Click += (sender, args) => Application.Exit();
        }

        private void ToggleWindow(object sender, EventArgs e) {
            switch (_window.Visibility) {
                case Visibility.Visible:
                    _window.Visibility = Visibility.Hidden;
                    break;
                case Visibility.Hidden:
                case Visibility.Collapsed:
                    _window.Visibility = Visibility.Visible;
                    _window.Topmost = true;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
