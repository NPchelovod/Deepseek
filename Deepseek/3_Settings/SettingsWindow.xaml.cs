using OllamaChat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms; // Для FolderBrowserDialog
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
namespace Deepseek
{
    /// <summary>
    /// Логика взаимодействия для SettingsWindow.xaml
    /// </summary>
    public partial class SettingsWindow : Window
    {
        public MainWindow _mainWindow;
        public SettingsWindow(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;
            InitializeComponent();
            InitializePuth();


        }
        private void InitializePuth()
        {
            InboxPathTextBox.Text = _mainWindow.chatData.inboxPath;
            OutboxPathTextBox.Text = _mainWindow.chatData.outboxPath;
            ArchivePathTextBox.Text = _mainWindow.chatData.archivePath;
            PromptFolderTextBox.Text = _mainWindow.chatData.promptFolder;
            
            FileModelII.Text= _mainWindow.chatData.ModelII;
            OllamaUrlTextBox.Text = _mainWindow.chatData.OllamaApiUrl;

            FileModelVectorII.Text = _mainWindow.chatData.EmbeddingModel;
            OllamaUrlVectorTextBox.Text = _mainWindow.chatData.OllamaApiUrlEmbed;

            WordsSplitCountTextBox.Text = _mainWindow.chatData._chunkSize.ToString();
            WordsCountTextBox.Text = _mainWindow.chatData.topK.ToString();

            WordsCountMaxTextBox.Text = _mainWindow.chatData.WordMax.ToString();

        }
        // Обработчик кнопки "Обзор..." для поля "Папка входящих"
        private void BrowseInbox_Click(object sender, RoutedEventArgs e)
        {
            string folder = GetFolder(_mainWindow.chatData.inboxPath);
            if (!string.IsNullOrEmpty(folder))
            {
                //_mainWindow.chatData.inboxPath = folder;
                InboxPathTextBox.Text = folder;
            }
        }
        private void BrowseOutbox_Click(object sender, RoutedEventArgs e)
        {
            string folder = GetFolder(_mainWindow.chatData.outboxPath);
            if (!string.IsNullOrEmpty(folder))
            {
                //_mainWindow.chatData.outboxPath = folder;
                OutboxPathTextBox.Text = folder;
            }
        }
        private void BrowseArchive_Click(object sender, RoutedEventArgs e)
        {
            string folder = GetFolder(_mainWindow.chatData.archivePath);
            if (!string.IsNullOrEmpty(folder))
            {
                //_mainWindow.chatData.archivePath = folder;
                ArchivePathTextBox.Text = folder;
            }
        }
        private void BrowsePromptFolder_Click(object sender, RoutedEventArgs e)
        {
            string folder = GetFolder(_mainWindow.chatData.promptFolder);
            if (!string.IsNullOrEmpty(folder))
            {
                //_mainWindow.chatData.promptFolder = folder;
                PromptFolderTextBox.Text = folder;
            }
        }
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(InboxPathTextBox.Text) && System.IO.Directory.Exists(InboxPathTextBox.Text))
            {
                _mainWindow.chatData.inboxPath = InboxPathTextBox.Text;
            }
            if (!string.IsNullOrEmpty(OutboxPathTextBox.Text) && System.IO.Directory.Exists(OutboxPathTextBox.Text))
            {
                _mainWindow.chatData.outboxPath = OutboxPathTextBox.Text;
            }
            if (!string.IsNullOrEmpty(ArchivePathTextBox.Text) && System.IO.Directory.Exists(ArchivePathTextBox.Text))
            {
                _mainWindow.chatData.archivePath = ArchivePathTextBox.Text;
            }
            if (!string.IsNullOrEmpty(PromptFolderTextBox.Text) && System.IO.Directory.Exists(PromptFolderTextBox.Text))
            {
                _mainWindow.chatData.promptFolder = PromptFolderTextBox.Text;
            }

            //сохраняем 
            if (!string.IsNullOrEmpty(OllamaUrlTextBox.Text))
            {
                _mainWindow.chatData.OllamaApiUrl = OllamaUrlTextBox.Text;
            }
            if (!string.IsNullOrEmpty(FileModelII.Text))
            {
                _mainWindow.chatData.ModelII = FileModelII.Text;
            }
            if (!string.IsNullOrEmpty(FileModelVectorII.Text))
            {
                 _mainWindow.chatData.EmbeddingModel= FileModelVectorII.Text;
            }
            if (!string.IsNullOrEmpty(OllamaUrlVectorTextBox.Text))
            {
                _mainWindow.chatData.OllamaApiUrlEmbed= OllamaUrlVectorTextBox.Text;
            }
            if (!string.IsNullOrEmpty(FileModelVectorII.Text) && int.TryParse( WordsSplitCountTextBox.Text, out int val))
            {
                _mainWindow.chatData._chunkSize = val;
            }
            if (!string.IsNullOrEmpty(WordsCountTextBox.Text) && int.TryParse(WordsCountTextBox.Text, out int val2))
            {
                _mainWindow.chatData.topK= val2;
            }

            if (!string.IsNullOrEmpty(WordsCountMaxTextBox.Text) && int.TryParse(WordsCountMaxTextBox.Text, out int val3))
            {
                _mainWindow.chatData.WordMax = val3;
            }
           
            Close();
        }
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        public static string GetFolder(string initialPuth)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Выберите папку входящих сообщений";
                dialog.ShowNewFolderButton = true;

                // Если текущий путь существует, начинаем с него
                if (!string.IsNullOrEmpty(initialPuth) && System.IO.Directory.Exists(initialPuth))
                {
                    dialog.SelectedPath = initialPuth;
                }

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    return dialog.SelectedPath;
                   
                }
            }
            return "";
        }
    }
}
