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
using Shell32;
using Microsoft.WindowsAPICodePack.Shell;

namespace Renamer.Services {
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
            var sonyHDRCX405FilesCount = 0;
            var canonFilesCount = 0;
            var sonyA6600FilesCount = 0;
            var xiaomiMiMax3FilesCount = 0;
            var unknownFiles = new List<string>();

            var files = GetNonSmartphoneFiles();

            foreach (var fileName in files)
            {
                try
                {
                    if (IsSonyHDRCX405File(fileName))
                    {
                        RenameSonyHDRCX405File(fileName);
                        sonyHDRCX405FilesCount++;
                    }
                    else if (IsCanonFile(fileName))
                    {
                        RenameCanonFile(fileName);
                        canonFilesCount++;
                    }
                    else if (IsSonyA6600File(fileName))
                    {
                        RenameSonyA6600File(fileName);
                        sonyA6600FilesCount++;
                    }
                    else if (IsXiaomiMiMax3IncognitoFile(fileName))
                    {
                        RenameXiaomiMiMax3IncognitoFile(fileName);
                        xiaomiMiMax3FilesCount++;
                    }
                    else if (IsXiaomiMiMax3File(fileName))
                    {
                        RenameXiaomiMiMax3File(fileName);
                        xiaomiMiMax3FilesCount++;
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

            _executionMessage.Content = $"Renamed: {sonyHDRCX405FilesCount} SonyHDRCX405, {canonFilesCount} Canon, {sonyA6600FilesCount} SonyA6600, {xiaomiMiMax3FilesCount} XiaomiMiMax3. Found {unknownFiles.Count} unknown: {string.Join(", ", unknownFiles)}";
        }

        public void DeleteOriginalImages(bool deleteRaw, bool deleteUneditedJpg, bool deleteVideo) {
            var originalImagesPaths = GetOriginalImages(deleteRaw, deleteUneditedJpg, deleteVideo);
            MoveFilesToRecycleBin(originalImagesPaths);

            var originalImagesNames = originalImagesPaths.Select(Path.GetFileName);
            var nrOfRaw = originalImagesNames.Where(IsRaw).Count();
            var nrOfUneditedJpg = originalImagesNames.Where(IsUneditedJpg).Count();
            var nrOfVideo = originalImagesNames.Where(IsVideo).Count();
            _executionMessage.Content = $"Cleanup complete: {nrOfRaw} RAW, {nrOfUneditedJpg} unedited JPG, {nrOfVideo} video";
        }

        private List<string> GetOriginalImages(bool deleteRaw, bool deleteUneditedJpg, bool deleteVideo) =>
            Directory.GetFiles(_workingFolderPath, "*.*", System.IO.SearchOption.AllDirectories)
                     .Where(path => !IsWatermark(path))
                     .Where(path => {
                         var file = Path.GetFileName(path);
                         return deleteRaw && IsRaw(file)
                         || deleteUneditedJpg && IsUneditedJpg(file)
                         || deleteVideo && IsVideo(file)
                         || IsMetadata(file);
                     })
                     .ToList();

        private bool IsWatermark(string path) => path.Contains("Watermark");

        private bool IsRaw(string fileName) => RawFileExtensions.Any(extension => fileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase));

        private bool IsUneditedJpg(string fileName) => IsJpg(fileName) && !IsEdited(fileName);

        private bool IsJpg(string fileName) => fileName.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase);

        private bool IsEdited(string fileName) => fileName.Contains("-");

        private bool IsVideo(string fileName) => VideoFileExtensions.Any(extension => fileName.EndsWith(extension, StringComparison.OrdinalIgnoreCase));

        private bool IsMetadata(string fileName) => fileName.EndsWith(".sfk", StringComparison.OrdinalIgnoreCase);

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

        private bool IsSonyHDRCX405File(string fileName) => fileName.StartsWith("20") && fileName.All(char.IsDigit);
        private bool IsCanonFile(string fileName) => fileName.StartsWith("IMG_") && Has4DigitName(fileName.Substring(4));
        private bool IsSonyA6600File(string fileName) => IsSonyA6600Photo(fileName) || IsSonyA6600Video(fileName);
        private bool IsSonyA6600Photo(string fileName) => fileName.StartsWith("DSC");
        private bool IsSonyA6600Video(string fileName) => fileName.StartsWith("C");
        private bool IsXiaomiMiMax3IncognitoFile(string fileName) => !fileName.StartsWith("20") && fileName.All(character => char.IsDigit(character) || character == '-');
        private bool IsXiaomiMiMax3File(string fileName) => fileName.StartsWith("IMG_") && HasDateName(fileName)
                                                            || fileName.StartsWith("VID_");

        // Input example: 4275-1
        private bool Has4DigitName(string name) => name.Length <= 4 || !char.IsDigit(name[4]);
        // Input example: 20200818_193630-1
        private bool HasDateName(string name) => name.Length >= 8 + 1 + 6 || name.Where((char c, int i) => i != 8 && i < 8 + 1 + 6).All(c => char.IsDigit(c));

        private void RenameSonyHDRCX405File(string fileName)
        {
            var newName = fileName.Insert(DateLenght, "_") + " - SonyHDRCX405";
            RenameShortcut(fileName, newName);
        }

        private void RenameCanonFile(string fileName)
        {
            var originalFilePath = GetFilePathFromShortcut(fileName + ShortcutSuffix);
            var dateTime = GetDateTakenFromImage(originalFilePath);

            var newName = $"{dateTime.Year}{dateTime.Month:D2}{dateTime.Day:D2}_{dateTime.Hour:D2}{dateTime.Minute:D2}{dateTime.Second:D2} - {fileName}";
            RenameShortcut(fileName, newName);
        }

        private void RenameSonyA6600File(string fileName)
        {
            var originalFilePath = GetFilePathFromShortcut(fileName + ShortcutSuffix);
            var dateTime = IsSonyA6600Photo(fileName)
                ? GetDateTakenFromImage(originalFilePath)
                : GetDateMediaCreated(originalFilePath);

            var newName = $"{dateTime.Year}{dateTime.Month:D2}{dateTime.Day:D2}_{dateTime.Hour:D2}{dateTime.Minute:D2}{dateTime.Second:D2} - {fileName}";
            RenameShortcut(fileName, newName);
        }

        private void RenameXiaomiMiMax3IncognitoFile(string fileName)
        {
            var originalFilePath = GetFilePathFromShortcut(fileName + ShortcutSuffix);
            var dateTime = IsJpg(originalFilePath)
                ? GetDateTakenFromImage(originalFilePath)
                : GetDateMediaCreated(originalFilePath);

            var newName = $"{dateTime.Year}{dateTime.Month:D2}{dateTime.Day:D2}_{dateTime.Hour:D2}{dateTime.Minute:D2}{dateTime.Second:D2} - {fileName}";
            RenameShortcut(fileName, newName);
        }

        private void RenameXiaomiMiMax3File(string fileName) {
            var newName = $"{fileName.Substring(4)} - {fileName}";
            RenameShortcut(fileName, newName);
        }

        public static DateTime GetDateTakenFromImage(string path)
        {
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            using (var myImage = Image.FromStream(fs, false, false))
            {
                PropertyItem propItem = myImage.GetPropertyItem(36867);
                string dateTaken = colonRegex.Replace(Encoding.UTF8.GetString(propItem.Value), "-", 2);
                return DateTime.Parse(dateTaken);
            }
        }

        public static DateTime GetDateMediaCreated(string path)
        {
            var shell = ShellObject.FromParsingName(path);
            var mediaCreatedDate = shell.Properties.System.Media.DateEncoded;
            return mediaCreatedDate.Value.Value;
        }

        // var correctDateString = AdjustSonyHDRCX405Date(fileName, 7, 25);
        private static string AdjustSonyHDRCX405Date(string fileName, int minutes, int seconds)
        {
            var dateString = fileName.Substring(0, DateLenght);
            var timeString = fileName.Substring(DateLenght, TimeLenght);

            var year = int.Parse(dateString.Substring(0, 4));
            var month = int.Parse(dateString.Substring(4, 2));
            var day = int.Parse(dateString.Substring(6, 2));
            var hour = int.Parse(timeString.Substring(0, 2));
            var minute = int.Parse(timeString.Substring(2, 2));
            var second = int.Parse(timeString.Substring(4, 2));
            var recordedTime = new DateTime(year, month, day, hour, minute, second);

            var correctDate = recordedTime.AddMinutes(minutes).AddSeconds(seconds);
            return $"{correctDate.Year}{correctDate.Month:D2}{correctDate.Day:D2}{correctDate.Hour:D2}{correctDate.Minute:D2}{correctDate.Second:D2}";
        }

        private void RenameShortcut(string fileName, string newName)
        {
            fileName += ShortcutSuffix;
            newName += ".lnk";
            File.Move(fileName, newName);
        }

        private string GetFilePathFromShortcut(string fileName)
        {
            var directoryPath = Directory.GetCurrentDirectory();
            var fullShortcutPath = Path.Combine(directoryPath, fileName);
            var shell = new Shell();
            var folder = shell.NameSpace(Path.GetDirectoryName(fullShortcutPath));
            var folderItem = folder.Items().Item(Path.GetFileName(fileName));
            var currentLink = (ShellLinkObject)folderItem.GetLink;
            return currentLink.Path;
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
