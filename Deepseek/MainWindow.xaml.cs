using Deepseek;
using Microsoft.Win32; // Для OpenFileDialog
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Diagnostics; // добавить в using
using System.Windows.Controls;
namespace OllamaChat
{
    public partial class MainWindow : Window
    {
        private readonly HttpClient _httpClient;
        private  string OllamaApiUrl =>chatData.OllamaApiUrl;
        private string OllamaApiUrlEmbed => chatData.OllamaApiUrlEmbed;

        public static MainWindow mainWindow;

        public MainWindow()
        {
            mainWindow = this;
            InitializeComponent();
            _httpClient = new HttpClient();
            
            
            this.Loaded += SettingsWindow_Loaded;

        }
        public ChatData chatData=new ChatData();
        public static JsonSerializerOptions OptionsJson = new JsonSerializerOptions { IncludeFields = true, WriteIndented = true };
        private void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeAnswerUsers();
            
            InitializeSettings();//загрузка начальной модели старой преднастройке
            // Установка модели в ComboBox в соответствии с chatData.ModelII
            foreach (ComboBoxItem item in ModelComboBox.Items)
            {
                if (item.Content.ToString() == chatData.ModelII)
                {
                    item.IsSelected = true;
                    break;
                }
            }
            //MaxTokensTextBox.Text = chatData.SimvolsVoprosMax.ToString();
            
        }



        // ==================== ОБРАБОТЧИКИ СОБЫТИЙ ====================

        // Добавьте поле для предотвращения рекурсии в MaxTokensTextBox
        private bool _suppressMaxTokensTextChanged = false;

        // Обработчик изменения текста в MaxTokensTextBox
       
        // Обработчик выбора модели в ComboBox
        private bool _isModelComboBox=false;
        private void ModelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isModelComboBox) { return; }
            _isModelComboBox=true;
            bool ustan = false;
            if (chatData != null && ModelComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                string mI = selectedItem.Content.ToString();
                if (!string.IsNullOrEmpty(mI) && mI != chatData.ModelII)
                {
                    //chatData.ModelII = mI;

                    //cb.SelectedItem = e.RemovedItems[0];
                    if (chatData.IsAdminCheckBox || string.IsNullOrEmpty(chatData.ModelII))
                    {
                        chatData.ModelII = mI;
                        ustan = true;
                    }
                    
                }
            }
            if(!ustan)
            {
                //откат
                // Возвращаем предыдущий выбор, при инициализации простой откат не работает, поэтому так
                var cb = (System.Windows.Controls.ComboBox)sender;
                if (e.RemovedItems.Count > 0 && e.RemovedItems[0] is ComboBoxItem PastselectedItem)
                {
                    string pastChange = PastselectedItem.Content.ToString();
                    if (pastChange == chatData.ModelII)
                    {
                        cb.SelectedItem = e.RemovedItems[0];
                    }
                }
            }
            _isModelComboBox= false;



        }
        private void UseContextCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            chatData.UseCommonContext = UseContextCheckBox.IsChecked == true;
            
            if(UseContextCheckBox.IsChecked==false)
            {
                QuoteFromKnowledgeBaseCheckBox.IsChecked = false;
            }
            // готовим контекст
            //GetContextFileData(chatData);
           

        }


        private void UseHistoryVopros_Changed(object sender, RoutedEventArgs e)
        {
            if (UseHistoryVopros.IsChecked == false)
            {
                UseOnlyRelevantHistoryInVopros.IsChecked = false;
                UseOnlyYourQuestion.IsChecked = false;
            }
            chatData.UseHistoryVopros = UseHistoryVopros.IsChecked == true;
        }
        private void UseOnlyRelevantHistoryInVopros_Changed(object sender, RoutedEventArgs e)
        {
            if(UseOnlyRelevantHistoryInVopros.IsChecked == true)
            {
                UseHistoryVopros.IsChecked = true;
            }
            chatData.UseOnlyRelevantHistoryInVopros = UseOnlyRelevantHistoryInVopros.IsChecked == true;
        }
        private void  UseOnlyYourQuestion_Changed(object sender, RoutedEventArgs e)
        {
            if(UseOnlyYourQuestion.IsChecked==true)
            {
                UseHistoryVopros.IsChecked = true;
            }
            chatData.UseOnlyYourQuestionInHistory = UseOnlyYourQuestion.IsChecked == true;
        }
        private void IsAdminCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            chatData.IsAdminCheckBox = IsAdminCheckBox.IsChecked == true;

            if (chatData.IsAdminCheckBox)
            {
                //реализуем подписку на вопросы и ответ пользователю
                //предупереждение пользователю, Внимание ...
                InitializeAnswerAdmin();
            }
            else
            {
                if (scanerAnswerA != null)
                {
                    scanerAnswerA.Stop();//чтобы перезаписаться
                }
                //отписка если была на папки и тд
            }
        }
        private  void ViewContextButton_Click(object sender, RoutedEventArgs e)
        {
            // открытие SettingsWindow.
            var sW = new ContextWindow(this);
            sW.Show();
        }


        private void RollbackSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // Логика восстановления из резервной копии (например, загрузка .bak файла)

            chatData = new ChatData();
            ClearHistoryButton_Click(sender, e);
            LoadSettingsToWind();
        }


        private void RollbackDeepseekButton_Click(object sender, RoutedEventArgs e)
        {
            //открыть окно в браузере дипсика
        }


        private void ErrorsButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorsWriter();
        }
        private void ErrorsWriter()
        {
            //открытие окна с ошибками или запись всех ошибок??
            if (chatData.Errors == null)
            {
                chatData.Errors = new ChatElement()
                {
                    Senders = ESenders.Errors,
                    StartTime = DateTime.Now,
                    Id = chatData.Id,
                };
            }
            string answer = chatData.Errors.GetAnswerText();
            
            if(ChatBox.Text.Contains(answer)) {return; }
            AddMessage(answer, chatData);
        }
        private void QuoteFromKnowledgeBaseCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            // Здесь логика: например, сохранить состояние или обновить контекст
            bool useQuotes = QuoteFromKnowledgeBaseCheckBox.IsChecked ==true;
            chatData.OnlyUseCommonContext = useQuotes;
            if(useQuotes)
            {
                UseContextCheckBox.IsChecked = true;
            }
            // Например: chatData.UseQuotesFromKB = useQuotes;
        }


        //тут мы получаем и отвечаем на вопросы все польхователей
        
        private void AddMessage(string message, ChatData chatData)
        {
            // Очищаем сообщение от тегов <think> и служебных префиксов
            string cleanedMessage = Regex.Replace(message, @"<think>.*?</think>", "", RegexOptions.Singleline);
            
            ChatBox.AppendText($"{message}\n");
            ChatBox.ScrollToEnd();
        }

        
    }
}