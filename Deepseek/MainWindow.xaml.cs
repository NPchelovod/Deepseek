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

        private void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            InitializeAnswerUsers();
            InitializeAnswerAdmin();
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
            IsAdminCheckBox.IsChecked = chatData.IsAdminCheckBox;
        }



        // ==================== ОБРАБОТЧИКИ СОБЫТИЙ ====================

        // Добавьте поле для предотвращения рекурсии в MaxTokensTextBox
        private bool _suppressMaxTokensTextChanged = false;

        // Обработчик изменения текста в MaxTokensTextBox
        private void MaxTokensTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_suppressMaxTokensTextChanged) return;
            if(chatData==null) return;
            //if (int.TryParse(MaxTokensTextBox.Text, out int value) && value > 0)
            //{
            //    chatData.WordVoprosMax = value;
            //}
            //else
            //{
            //    // Если введено некорректное значение, возвращаем предыдущее корректное
            //    _suppressMaxTokensTextChanged = true;
            //   // MaxTokensTextBox.Text = chatData.WordVoprosMax.ToString();
            //   // MaxTokensTextBox.CaretIndex = MaxTokensTextBox.Text.Length;
            //    _suppressMaxTokensTextChanged = false;
            //}
        }
        // Обработчик выбора модели в ComboBox
        private void ModelComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(chatData != null && ModelComboBox.SelectedItem is ComboBoxItem selectedItem)
             {
                chatData.ModelII = selectedItem.Content.ToString();
            }
        }
        private void UseContextCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            chatData.UseCommonContext = UseContextCheckBox.IsChecked == true;
            
            // готовим контекст
            //GetContextFileData(chatData);
           

        }
        private void IsAdminCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            chatData.IsAdminCheckBox = IsAdminCheckBox.IsChecked == true;

            if (chatData.IsAdminCheckBox)
            {
                //реализуем подписку на вопросы и ответ пользователю
                //предупереждение пользователю, Внимание ...
            }
            else
            {
                //отписка если была на папки и тд
            }
        }
        private async void ViewContextButton_Click(object sender, RoutedEventArgs e)
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

        }

        private void QuoteFromKnowledgeBaseCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            // Здесь логика: например, сохранить состояние или обновить контекст
            bool useQuotes = QuoteFromKnowledgeBaseCheckBox.IsChecked ?? false;
            chatData.OnlyUseCommonContext = useQuotes;
            if(chatData.OnlyUseCommonContext)
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

        // ==================== ГЕНЕРАЦИЯ ОТВЕТА (ПОТОКОВАЯ) ====================

        private async Task<string> GenerateTextStreamAsync(string prompt, ChatData outChatData)
        {
            var fullResponse = new StringBuilder();
            bool inThinkTag = false;

            try
            {
                var requestData = new
                {
                    model = outChatData.ModelII,
                    prompt = prompt,
                    temperature = 0.7,
                    max_tokens = Math.Min(3000, outChatData.WordVoprosMax*2),//max_tokens_для_ответа = лимит_контекста - токены_в_промпте 1024 безопасный вариант
                    stream = true,
                   // keep_alive = "10h"   // или "24h", "-1" для постоянного удержания
                };

                var json = JsonSerializer.Serialize(requestData);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(OllamaApiUrl, content);
                response.EnsureSuccessStatusCode();

                using (var streamReader = new StreamReader(await response.Content.ReadAsStreamAsync()))
                {
                    string line;
                    while ((line = await streamReader.ReadLineAsync()) != null)
                    {
                        if (!string.IsNullOrEmpty(line))
                        {
                            try
                            {
                                using JsonDocument document = JsonDocument.Parse(line);
                                JsonElement root = document.RootElement;

                                if (root.TryGetProperty("response", out JsonElement responseProperty))
                                {
                                    var token = responseProperty.GetString();

                                    // Пропускаем содержимое тегов <think>
                                    if (token.Contains("<think>"))
                                    {
                                        inThinkTag = true;
                                        continue;
                                    }
                                    else if (token.Contains("</think>"))
                                    {
                                        inThinkTag = false;
                                        continue;
                                    }
                                    else if (inThinkTag)
                                    {
                                        continue; // Пропускаем содержимое внутри тегов
                                    }

                                    // Пропускаем служебные префиксы, если модель их повторяет
                                    if (token.StartsWith("AI:") || token.StartsWith("Вы:"))
                                        continue;

                                    fullResponse.Append(token);

                                    // Выводим токен в реальном времени
                                    //Dispatcher.Invoke(() =>
                                    //{
                                    //    ChatBox.AppendText(token);
                                    //    ChatBox.ScrollToEnd();
                                    //});
                                }

                                if (root.TryGetProperty("done", out JsonElement doneProperty) &&
                                    doneProperty.GetBoolean())
                                {
                                    break;
                                }
                            }
                            catch (JsonException) { /* Игнорируем некорректные JSON-строки */ }
                        }
                    }
                }

                return fullResponse.ToString();
            }
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }
    }
}