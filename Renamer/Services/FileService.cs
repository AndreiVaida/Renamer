using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Directory = System.IO.Directory;
using Image = System.Drawing.Image;
using System.Drawing.Imaging;
using Microsoft.VisualBasic.FileIO;

namespace Renamer.Services
{
    public class FileService
    {
        private readonly string _workingFolderPath;
        private readonly Label _executionMessage;
        private const string ShortcutSuffix = " - Shortcut.lnk";
        private const int DateLenght = 8;
        private const int TimeLenght = 6; // 20220806_144753
        private readonly List<string> RawFileExtensions = new List<string> { ".CR2", ".ARW", ".dng", ".tif" };
        private readonly List<string> VideoFileExtensions = new List<string> { ".mp4" };
        private static readonly Regex colonRegex = new Regex(":");

        public FileService(string workingFolderPath, Label executionMessage)
        {
            _workingFolderPath = workingFolderPath;
            this._executionMessage = executionMessage;
        }

        public void RenameFilesNameToTimeName()
        {
            var sonyFilesCount = 0;
            var canonFilesCount = 0;
            var unknownFiles = new List<string>();

            var files = GetNonSmartphoneFiles();

            foreach (var fileName in files)
            {
                try
                {
                    if (IsSonyHDRCX405File(fileName))
                    {
                        RenameSonyFile(fileName);
                        sonyFilesCount++;
                    }
                    else if (IsCanonFile(fileName))
                    {
                        RenameCanonFile(fileName);
                        canonFilesCount++;
                    }
                    else
                    {
                        unknownFiles.Add(fileName);
                    }
                }
                catch (Exception exception)
                {
                    MessageBox.Show($"Cannot rename '{fileName}'.\n{exception.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            _executionMessage.Content = $"Renamed: {sonyFilesCount} Sony, {canonFilesCount} Canon. Found {unknownFiles.Count} unknown: {string.Join(", ", unknownFiles)}";
        }

        public void DeleteOriginalImages(bool deleteRaw, bool deleteUneditedJpg, bool deleteVideo) {
            var originalImages = GetOriginalImages(deleteRaw, deleteUneditedJpg, deleteVideo);
            MoveFilesToRecycleBin(originalImages);

            var nrOfRaw = originalImages.Where(IsRaw).Count();
            var nrOfUneditedJpg = originalImages.Where(IsUneditedJpg).Count();
            var nrOfVideo = originalImages.Where(IsVideo).Count();
            _executionMessage.Content = $"Cleanup complete: {nrOfRaw} RAW, {nrOfUneditedJpg} unedited JPG, {nrOfVideo} video";
        }

        private List<string> GetOriginalImages(bool deleteRaw, bool deleteUneditedJpg, bool deleteVideo) =>
            Directory.GetFiles(_workingFolderPath)
                     .Select(Path.GetFileName)
                     .Where(file => deleteRaw && IsRaw(file)
                                 || deleteUneditedJpg && IsUneditedJpg(file)
                                 || deleteVideo && IsVideo(file))
                     .ToList();

        private bool IsRaw(string fileName) => RawFileExtensions.Any(extension => fileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase));

        private bool IsUneditedJpg(string fileName) => IsJpg(fileName) && !IsEdited(fileName);

        private bool IsJpg(string fileName) => fileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase);

        private bool IsEdited(string fileName) => fileName.Contains("-");

        private bool IsVideo(string fileName) => VideoFileExtensions.Any(extension => fileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase));

        private List<string> GetNonSmartphoneFiles() =>
            Directory.GetFiles(_workingFolderPath)
                     .Select(Path.GetFileName)
                     .Where(name => !IsSmartphoneFile(name))
                     .Select(RemoveShortcutSuffix)
                     .ToList();

        private bool IsSmartphoneFile(string name)
        {
            if (!name.Contains('_'))
                return false;

            var parts = name.Split('_');
            return parts[0].Length == DateLenght && parts[1].Length >= TimeLenght;
        }

        private string RemoveShortcutSuffix(string name)
        {
            if (name.EndsWith(ShortcutSuffix))
                return name.Remove(name.Length - ShortcutSuffix.Length);

            return name;
        }

        private bool IsSonyHDRCX405File(string fileName) => fileName.All(char.IsDigit);
        private bool IsCanonFile(string fileName) => fileName.StartsWith("IMG_");
        private bool IsSonyA6600File(string fileName) => fileName.StartsWith("DSC") || fileName.StartsWith("C"); // Date taken + Date modified

        private void RenameSonyFile(string fileName)
        {
            var newName = fileName.Insert(DateLenght, "_") + " - Sony";
            RenameShortcut(fileName, newName);
        }

        private void RenameCanonFile(string fileName)
        {
            var originalFilePath = GetFilePathFromShortcut(fileName + ShortcutSuffix);
            var dateTime = GetDateTakenFromImage(originalFilePath);

            var newName = $"{dateTime.Year}{dateTime.Month:D2}{dateTime.Day:D2}_{dateTime.Hour:D2}{dateTime.Minute:D2}{dateTime.Second:D2} - {fileName}";
            RenameShortcut(fileName, newName);
        }

        public static DateTime GetDateTakenFromImage(string path)
        {
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            using (Image myImage = Image.FromStream(fs, false, false))
            {
                PropertyItem propItem = myImage.GetPropertyItem(36867);
                string dateTaken = colonRegex.Replace(Encoding.UTF8.GetString(propItem.Value), "-", 2);
                return DateTime.Parse(dateTaken);
            }
        }

        private void RenameShortcut(string fileName, string newName)
        {
            fileName += ShortcutSuffix;
            newName += ".lnk";
            File.Move(fileName, newName);
        }

        private string GetFilePathFromShortcut(string fileName)
        {
            IWshRuntimeLibrary.WshShell shell = new IWshRuntimeLibrary.WshShell();
            IWshRuntimeLibrary.IWshShortcut link = (IWshRuntimeLibrary.IWshShortcut)shell.CreateShortcut(fileName);
            return link.TargetPath;
        }

        private void MoveFilesToRecycleBin(List<string> files) {
            foreach (var file in files) {
                FileSystem.DeleteFile(file,
                                      UIOption.OnlyErrorDialogs,
                                      RecycleOption.SendToRecycleBin,
                                      UICancelOption.ThrowException);
            }
        }
    }
}
