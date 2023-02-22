using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualBasic.FileIO;
using FileSystem = Microsoft.VisualBasic.FileIO.FileSystem;
using SearchOption = System.IO.SearchOption;

namespace Renamer.Services
{
    public class FolderService
    {
        private readonly string _workingFolderPath;
        private HashSet<string> _foldersWithConflicts;
        private readonly CheckBox _deleteDuplicatesCheckBox;
        private readonly List<string> _buildAndBinFolders = new List<string> { "build", "bin", "obj", "packages", "node_modules" };

        public FolderService(string workingFolderPath, CheckBox deleteDuplicatesCheckBox)
        {
            _workingFolderPath = workingFolderPath;
            _deleteDuplicatesCheckBox = deleteDuplicatesCheckBox;
        }

        public void RenameFolders(char fromSeparator, char toSeparator, Label executionMessage)
        {
            _foldersWithConflicts = new HashSet<string>();
            var renamed = 0;

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

            if (_foldersWithConflicts.Count > 0 && _deleteDuplicatesCheckBox.IsChecked == false)
                AllertFoldersWithConflicts();
        }

        public void DeleteBuildFolders(Label executionMessage) {
            var foldersToSkip = new List<string> { "Platforms", ".git", ".idea", ".vs" };
            var buildAndBinFolders = GetBuildAndBinFolders(foldersToSkip);

            var errors = new List<string>();

            foreach (var folder in buildAndBinFolders)
                try
                {
                    MoveFolderToRecycleBin(folder);
                }
                catch (Exception exception)
                {
                    errors.Add(exception.Message);
                }

            var errorMessage = errors.Any() ? $"{errors.Count} errors: {string.Join("\n", errors)}" : "OK";
            var nrOfFoldersDeleted = buildAndBinFolders.Count - errors.Count;
            executionMessage.Content = $"Cleanup complete: {nrOfFoldersDeleted} folders deleted. {errorMessage}";
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
            if (noOfFiles == 0 || _deleteDuplicatesCheckBox.IsChecked == true)
                MoveFolderToRecycleBin(folderPath);
        }

        private List<string> GetBuildAndBinFolders(List<string> foldersNameToSkip)
        {
            var allPaths = Directory.GetDirectories(_workingFolderPath, "*", SearchOption.AllDirectories)
                .Where(folderPath => !foldersNameToSkip.Any(folderPath.Contains))
                .Where(folderPath => _buildAndBinFolders.Any(buildFolderName => Path.GetFileName(folderPath) == buildFolderName))
                .OrderByDescending(path => path.Length)
                .ToList();

            return ExtractOnlyUniquePaths(allPaths);
        }

        private static List<string> ExtractOnlyUniquePaths(List<string> allPaths)
        {
            var uniquePaths = new List<string>();

            for (var i = 0; i < allPaths.Count; i++)
            {
                var path = allPaths[i];
                if (!IsIncludedInShorterPath(allPaths, path, i))
                    uniquePaths.Add(path);
            }

            return uniquePaths;
        }

        private static bool IsIncludedInShorterPath(List<string> allPaths, string path, int pathIndex)
        {
            for (var j = pathIndex + 1; j < allPaths.Count; j++)
            {
                var shorterPath = allPaths[j];
                if (path.Contains(shorterPath))
                    return true;
            }

            return false;
        }

        private static void MoveFolderToRecycleBin(string folderPath) =>
            FileSystem.DeleteDirectory(folderPath,
                                       UIOption.OnlyErrorDialogs,
                                       RecycleOption.SendToRecycleBin,
                                       UICancelOption.ThrowException);

        private void AllertFoldersWithConflicts() =>
            MessageBox.Show($"There were some conflicts in the following folders:\n\n{string.Join("\n", _foldersWithConflicts)}", "Info");
    }
}
