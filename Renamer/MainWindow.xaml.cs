using System.Windows;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System;
using Microsoft.VisualBasic.FileIO;

namespace Renamer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly string _workingFolderPath = Directory.GetCurrentDirectory();
        private HashSet<string> _foldersWithConflicts;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void OnRenameClick(object sender, RoutedEventArgs e) => Rename();

        private void Rename()
        {
            _foldersWithConflicts = new HashSet<string>();
            var renamed = 0;

            var fromSeparator = fromSeparatorTextBox.Text[0];
            var toSeparator = toSeparatorTextBox.Text[0];
            var folders = GetFoldersToRename(fromSeparator);
            
            foreach (var folderName in folders)
            {
                try
                {
                    RenameFolderOrMoveFiles(folderName, fromSeparator, toSeparator);
                    renamed++;
                }
                catch (Exception exception)
                {
                    MessageBox.Show($"Cannot rename '{folderName}'.\n{exception.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            executionMessage.Content = $"Renamed {renamed} folders";

            if (_foldersWithConflicts.Count > 0 && deleteDuplicatesCheckBox.IsChecked == false)
                AllertFoldersWithConflicts();
        }

        private HashSet<string> GetFoldersToRename(char fromSeparator) =>
            Directory.GetDirectories(_workingFolderPath)
                     .Select(Path.GetFileName)
                     .Where(name => name.Contains(fromSeparator))
                     .ToHashSet();

        private void RenameFolderOrMoveFiles(string folderName, char fromSeparator, char toSeparator)
        {
            var oldFolderNameParts = folderName.Split(fromSeparator);
            var newFolderName = oldFolderNameParts[2] + toSeparator + oldFolderNameParts[1] + toSeparator + oldFolderNameParts[0];

            var oldFolderPath = Path.Combine(_workingFolderPath, folderName);
            var newFolderPath = Path.Combine(_workingFolderPath, newFolderName);

            if (!Directory.Exists(newFolderPath))
                RenameFolder(oldFolderPath, newFolderPath);
            else
            {
                MoveFiles(oldFolderPath, newFolderPath);
                DeleteFolderSafely(oldFolderPath);
            }
        }

        private static void RenameFolder(string oldFolderPath, string newFolderPath) =>
            Directory.Move(oldFolderPath, newFolderPath);

        /// <summary>
        /// Move the files to the existing folder, without overriding them.
        /// </summary>
        /// <param name="fromFolderPath">The folder from which all files will be moved</param>
        /// <param name="toFolderPath">The folder to which all files will be moved</param>
        private void MoveFiles(string fromFolderPath, string toFolderPath)
        {
            var filesToMove = new DirectoryInfo(fromFolderPath).GetFiles();
            foreach (var filePath in filesToMove)
                if (!File.Exists(Path.Combine(toFolderPath, filePath.Name)))
                    File.Move(filePath.FullName, Path.Combine(toFolderPath, filePath.Name));
                else
                    _foldersWithConflicts.Add(Path.GetFileName(fromFolderPath));
        }

        /// <summary>
        /// Move the old folder to Recycle Bin if is empty or if DeleteDuplicatesCheckBox is checked.
        /// </summary>
        /// <param name="folderPath"></param>
        private void DeleteFolderSafely(string folderPath)
        {
            var noOfFiles = new DirectoryInfo(folderPath).GetFiles().Count();
            if (noOfFiles == 0 || deleteDuplicatesCheckBox.IsChecked == true)
                MoveFolderToRecycleBin(folderPath);
        }

        private static void MoveFolderToRecycleBin(string oldFolderPath) => 
            FileSystem.DeleteDirectory(oldFolderPath,
                                       UIOption.OnlyErrorDialogs,
                                       RecycleOption.SendToRecycleBin,
                                       UICancelOption.ThrowException);

        private void AllertFoldersWithConflicts() => 
            MessageBox.Show($"There were some conflicts in the following folders:\n\n{string.Join("\n", _foldersWithConflicts)}", "Info");
    }
}
