using System.Windows;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace Renamer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly string _currentFolderPath = Directory.GetCurrentDirectory();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void OnRenameClick(object sender, RoutedEventArgs e)
        {
            var fromSeparator = fromSeparatorTextBox.Text[0];
            var toSeparator = toSeparatorTextBox.Text[0];

            var folders = GetFoldersToRename(fromSeparator);
            RenameFolders(folders, fromSeparator, toSeparator);

            executionMessage.Content = $"Renamed {folders.Count} folders";
        }

        private HashSet<string> GetFoldersToRename(char fromSeparator) =>
            Directory.GetDirectories(_currentFolderPath)
                     .Select(Path.GetFileName)
                     .Where(name => name.Contains(fromSeparator))
                     .ToHashSet();

        private void RenameFolders(HashSet<string> folders, char fromSeparator, char toSeparator)
        {
            foreach (var folderName in folders)
            {
                var oldName = folderName.Split(fromSeparator);
                var newName = oldName[2] + toSeparator + oldName[1] + toSeparator + oldName[0];

                var currentPath = Path.Combine(_currentFolderPath, folderName);
                var newPath = Path.Combine(_currentFolderPath, newName);
                Directory.Move(currentPath, newPath);
            }
        }
    }
}
