using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using Path = System.IO.Path;

namespace TarkovBundleHelper
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private byte[] fileBytes = { };
        private string bundlePath = string.Empty;
        public static ObservableCollection<Dependency> dependencyFiles = new ObservableCollection<Dependency>();
        private static byte[] cabPrefix = Encoding.UTF8.GetBytes("CAB-");
        private static Random rand = new Random();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void OpenMenuBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Multiselect = false,
                CheckFileExists = true,
                CheckPathExists = true,
                Filter = "Unity Bundle (*.bundle) | *.bundle"
            };
            var result = dialog.ShowDialog();
            if (!Convert.ToBoolean(result)) return;

            bundlePath = dialog.FileName;
            fileBytes = File.ReadAllBytes(dialog.FileName);

            StatusLabel.Content = dialog.SafeFileName;
            dependencyFiles.Clear();

            EnableUi();
        }

        private void AddBtn_OnClick(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Multiselect = true,
                CheckFileExists = true,
                CheckPathExists = true,
                Filter = "Unity Bundle (*.bundle) | *.bundle"
            };
            var result = dialog.ShowDialog();
            if (!Convert.ToBoolean(result)) return;

            var path = Path.GetFullPath(bundlePath);
            var dependencies = dialog.FileNames.Select(x => new Dependency(Path.GetFullPath(x)));
            foreach (var d in dependencies)
            {
                if (string.Equals(d.FilePath, path, StringComparison.OrdinalIgnoreCase)) continue;
                if (!dependencyFiles.Contains(d))
                    dependencyFiles.Add(d);
            }
        }

        private void DependencyListBox_Loaded(object sender, RoutedEventArgs e)
        {
            DependencyListBox.ItemsSource = dependencyFiles;
        }

        private void RemoveBtn_OnClick(object sender, RoutedEventArgs e)
        {
            var items = new Dependency[DependencyListBox.SelectedItems.Count];
            DependencyListBox.SelectedItems.CopyTo(items, 0);

            foreach (var d in items)
                dependencyFiles.Remove(d);
        }

        private void OpenFromFolderBtn_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new FolderBrowserDialog
            {
                ShowNewFolderButton = true,
                SelectedPath = Path.GetDirectoryName(bundlePath)
            };
            var result = dialog.ShowDialog();
            if (result != System.Windows.Forms.DialogResult.OK) return;

            var files = Directory.GetFiles(dialog.SelectedPath, "*.bundle");
            var currentBundlePath = Path.GetFullPath(bundlePath);
            foreach (var file in files)
            {
                var fullPath = Path.GetFullPath(Path.Combine(dialog.SelectedPath, file));
                if (string.Equals(fullPath, currentBundlePath, StringComparison.OrdinalIgnoreCase))
                    continue;

                var dep = new Dependency(fullPath);
                dependencyFiles.Add(dep);
            }
        }

        private void CloseMenuBtn_Click(object sender, RoutedEventArgs e)
        {
            DisableUi();

            dependencyFiles.Clear();
            bundlePath = string.Empty;
            fileBytes = new byte[] { };
            StatusLabel.Content = "No bundle selected";
        }

        private void DisableUi()
        {
            Dispatcher.Invoke(() =>
            {
                OpenFromFolderBtn.IsEnabled = false;
                AddBtn.IsEnabled = false;
                RemoveBtn.IsEnabled = false;
                DependencyListBox.IsEnabled = false;
                GenerateIdsBtn.IsEnabled = false;
            });
        }

        private void EnableUi()
        {
            Dispatcher.Invoke(() =>
            {
                OpenFromFolderBtn.IsEnabled = true;
                AddBtn.IsEnabled = true;
                RemoveBtn.IsEnabled = true;
                DependencyListBox.IsEnabled = true;
                GenerateIdsBtn.IsEnabled = true;
            });
        }

        private void GenerateIdsBtn_Click(object sender, RoutedEventArgs e)
        {
            new Thread(GenerateIds).Start();
        }

        private void SetStatusLabel(string text)
        {
            Dispatcher.Invoke(() => StatusLabel.Content = text);
        }

        private void GenerateIds()
        {
            DisableUi();
            var cabIndexes = fileBytes.Locate(cabPrefix);
            var cabId = fileBytes.Skip(cabIndexes.First() + 4).Take(32).ToArray();
            var newCabBytes = new byte[16];
            rand.NextBytes(newCabBytes);
            var newCab = Encoding.UTF8.GetBytes(BitConverter.ToString(newCabBytes).Replace("-", "").ToLower());
            var dependencyIds = new Dictionary<byte[], byte[]>();

            SetStatusLabel("Changing main bundle cab...");
            foreach (var index in fileBytes.Locate(cabId))
            {
                for (var b = 0; b < 32; b++)
                {
                    fileBytes[index + b] = newCab[b];
                }
            }

            SetStatusLabel("Generating new cabs for dependencies...");
            foreach (var obj in dependencyFiles)
            {
                var bytes = File.ReadAllBytes(obj.FilePath);
                var newBytes = new byte[16];
                rand.NextBytes(newBytes);
                var newId = Encoding.UTF8.GetBytes(BitConverter.ToString(newBytes).Replace("-", "").ToLower());
                dependencyIds.Add(bytes.Skip(bytes.Locate(cabPrefix).First() + 4).Take(32).ToArray(), newId);
            }

            SetStatusLabel("Replacing dependency cabs in main bundle...");
            foreach (var id in dependencyIds.Keys)
            {
                var matches = fileBytes.Locate(id);
                foreach (var index in matches)
                {
                    for (var b = 0; b < 32; b++)
                    {
                        fileBytes[index + b] = dependencyIds[id][b];
                    }
                }
            }
            File.WriteAllBytes(bundlePath, fileBytes);

            SetStatusLabel("Replacing cabs in dependencies...");
            foreach (var file in dependencyFiles)
            {
                var bytes = File.ReadAllBytes(file.FilePath);
                foreach (var index in bytes.Locate(cabId))
                {
                    for (var b = 0; b < 32; b++)
                    {
                        bytes[index + b] = newCab[b];
                    }
                }

                var thisCabId = bytes.Skip(bytes.Locate(cabPrefix).First() + 4).Take(32).ToArray();

                foreach (var index in bytes.Locate(thisCabId))
                {
                    for (var b = 0; b < 32; b++)
                    {
                        bytes[index + b] = dependencyIds.First(x => x.Key.SequenceEqual(thisCabId)).Value[b];
                    }
                }
                File.WriteAllBytes(file.FilePath, bytes);
            }

            SetStatusLabel(Path.GetFileName(bundlePath));
            EnableUi();
        }
    }
}
