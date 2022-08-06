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

        public MainWindow()
        {
            InitializeComponent();
            _folderService = new FolderService(_workingFolderPath, deleteDuplicatesCheckBox);
        }

        private void OnRenameFoldersClick(object sender, RoutedEventArgs e) => RenameFolders();

        private void RenameFolders()
        {
            var fromSeparator = fromSeparatorTextBox.Text[0];
            var toSeparator = toSeparatorTextBox.Text[0];
            _folderService.RenameFolders(fromSeparator, toSeparator, executionMessage);
        }
    }
}
