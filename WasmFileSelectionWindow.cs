using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Data;

namespace MSFSCacheManager
{
    // No shell context menu or filesystem mutation commands.
    public sealed class WasmFileSelectionWindow : Window
    {
        private readonly ListView files = new() { SelectionMode = SelectionMode.Extended };
        private readonly TextBlock location = new() { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(12, 6, 0, 6) };
        private readonly TextBlock summary = new() { Margin = new Thickness(0, 8, 0, 0) };
        private readonly Button up = new() { Content = "Up", Padding = new Thickness(14, 6, 14, 6) };
        private readonly Button proceed = new() { Content = "Back up and remove selected…", Padding = new Thickness(14, 8, 14, 8), Margin = new Thickness(8), IsEnabled = false };
        private readonly string root;
        private string current;
        public string[] FileNames { get; private set; } = Array.Empty<string>();

        public WasmFileSelectionWindow(string folder, string version)
        {
            root = Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar);
            current = root;
            Title = $"Select {version} files";
            Width = 880;
            Height = 580;
            MinWidth = 640;
            MinHeight = 400;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            var layout = new DockPanel { Margin = new Thickness(18) };
            Content = layout;
            var heading = new TextBlock
            {
                Text = "Double-click a folder to open it. Select files with Ctrl-click or Shift-click.\n" +
                       "Backup confirmation is required before removal. Folders are never deleted.",
                TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 14)
            };
            DockPanel.SetDock(heading, Dock.Top);
            layout.Children.Add(heading);
            var navigation = new DockPanel { Margin = new Thickness(0, 0, 0, 12) };
            DockPanel.SetDock(navigation, Dock.Top);
            navigation.Children.Add(up);
            navigation.Children.Add(location);
            layout.Children.Add(navigation);
            up.Click += (_, _) => Navigate(Path.GetDirectoryName(current)!);
            var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            DockPanel.SetDock(actions, Dock.Bottom);
            layout.Children.Add(actions);
            proceed.Click += (_, _) =>
            {
                FileNames = files.SelectedItems.Cast<FileEntry>().Where(f => !f.IsDirectory).Select(f => f.FullPath).ToArray();
                if (FileNames.Length > 0) DialogResult = true;
            };
            actions.Children.Add(proceed);
            actions.Children.Add(new Button { Content = "Cancel", IsCancel = true, Padding = new Thickness(14, 8, 14, 8), Margin = new Thickness(8) });
            DockPanel.SetDock(summary, Dock.Bottom);
            layout.Children.Add(summary);
            files.View = new GridView
            {
                Columns =
                {
                    new GridViewColumn { Header = "Name", Width = 590, DisplayMemberBinding = new Binding(nameof(FileEntry.Name)) },
                    new GridViewColumn { Header = "Type", Width = 130, DisplayMemberBinding = new Binding(nameof(FileEntry.Kind)) }
                }
            };
            files.ContextMenu = null;
            files.MouseDoubleClick += (_, e) =>
            {
                if (ItemsControl.ContainerFromElement(files, e.OriginalSource as DependencyObject) is ListViewItem item &&
                    item.Content is FileEntry entry && entry.IsDirectory) Navigate(entry.FullPath);
            };
            files.KeyDown += (_, e) =>
            {
                if (e.Key == Key.Enter && files.SelectedItem is FileEntry entry && entry.IsDirectory)
                {
                    Navigate(entry.FullPath);
                    e.Handled = true;
                }
            };
            files.SelectionChanged += (_, _) =>
            {
                int count = files.SelectedItems.Cast<FileEntry>().Count(f => !f.IsDirectory);
                proceed.IsEnabled = count > 0;
                summary.Text = $"{count} file(s) selected";
            };
            layout.Children.Add(files);
            Navigate(root);
        }

        private void Navigate(string folder)
        {
            try
            {
                string path = Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar);
                if (!path.Equals(root, StringComparison.OrdinalIgnoreCase) &&
                    !path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) return;
                for (var parent = new DirectoryInfo(path); parent != null; parent = parent.Parent)
                    if ((parent.Attributes & FileAttributes.ReparsePoint) != 0)
                        throw new IOException("Linked folders cannot be opened.");
                var entries = new DirectoryInfo(path).EnumerateFileSystemInfos("*", new EnumerationOptions
                {
                    RecurseSubdirectories = false,
                    IgnoreInaccessible = false,
                    AttributesToSkip = FileAttributes.ReparsePoint
                }).Select(f => new FileEntry(f.FullName, f.Name, (f.Attributes & FileAttributes.Directory) != 0))
                  .OrderByDescending(f => f.IsDirectory).ThenBy(f => f.Name, StringComparer.OrdinalIgnoreCase).ToList();
                current = path;
                files.ItemsSource = entries;
                location.Text = path;
                up.IsEnabled = !path.Equals(root, StringComparison.OrdinalIgnoreCase);
                summary.Text = entries.Count == 0 ? "This folder is empty." : "0 file(s) selected";
                proceed.IsEnabled = false;
            }
            catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
            {
                MessageBox.Show(this, ex.Message, "Unable to open folder", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private sealed record FileEntry(string FullPath, string Name, bool IsDirectory)
        {
            public string Kind => IsDirectory ? "Folder" : "File";
        }
    }
}
