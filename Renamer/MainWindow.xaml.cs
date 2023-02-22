using System.Windows;
using System.IO;
using Renamer.Services;

namespace Renamer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly string _workingFolderPath = Directory.GetCurrentDirectory();
        private readonly FolderService _folderService;
        private readonly FileService _fileService;

        public MainWindow()
        {
            InitializeComponent();
            _folderService = new FolderService(_workingFolderPath, deleteDuplicatesCheckBox);
            _fileService = new FileService(_workingFolderPath, executionMessage);
        }

        private void OnRenameFoldersClick(object sender, RoutedEventArgs e) => RenameFolders();

        private void OnRenameFilesClick(object sender, RoutedEventArgs e) => RenameFiles();

        private void OnCleanupPicturesClick(object sender, RoutedEventArgs e) => CleanupPictures();

        private void OnCleanupFoldersClick(object sender, RoutedEventArgs e) => CleanupBuildFolders();

        private void RenameFolders() {
            var fromSeparator = fromSeparatorTextBox.Text[0];
            var toSeparator = toSeparatorTextBox.Text[0];
            _folderService.RenameFolders(fromSeparator, toSeparator, executionMessage);
        }

        private void RenameFiles() => _fileService.RenameFilesNameToTimeName();

        private void CleanupPictures() {
            bool deleteRaw = cleanupRawCheckBox.IsChecked ?? false;
            bool deleteUneditedJpg = cleanupUneditedJpgCheckBox.IsChecked ?? false;
            bool deleteVideo = cleanupVideoCheckBox.IsChecked ?? false;
            _fileService.DeleteOriginalImages(deleteRaw, deleteUneditedJpg, deleteVideo);
        }

        private void CleanupBuildFolders() {
            _folderService.DeleteBuildFolders(executionMessage);
        }
    }
}
