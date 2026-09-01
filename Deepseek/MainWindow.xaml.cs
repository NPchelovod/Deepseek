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
        private const string OllamaApiUrl = "http://localhost:11434/api/generate";

        public static MainWindow mainWindow;

        public MainWindow()
        {
            InitializeComponent();
            _httpClient = new HttpClient();
            
            mainWindow=this;
            Initalize();

        }
        public ChatData chatData=new ChatData();

        public void Initalize()
        {
            InitializeAnswerUsers();
            InitializeAnswerAdmin();
            // Установка модели в ComboBox в соответствии с chatData.ModelII
            foreach (ComboBoxItem item in ModelComboBox.Items)
            {
                if (item.Content.ToString() == chatData.ModelII)
                {
                    item.IsSelected = true;
                    break;
                }
            }
        }



        // ==================== ОБРАБОТЧИКИ СОБЫТИЙ ====================

        // Добавьте поле для предотвращения рекурсии в MaxTokensTextBox
        private bool _suppressMaxTokensTextChanged = false;

        // Обработчик изменения текста в MaxTokensTextBox
        private void MaxTokensTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_suppressMaxTokensTextChanged) return;
            if(chatData==null) return;
            if (int.TryParse(MaxTokensTextBox.Text, out int value) && value > 0)
            {
                chatData.SimvolsMax = value;
            }
            else
            {
                // Если введено некорректное значение, возвращаем предыдущее корректное
                _suppressMaxTokensTextChanged = true;
                MaxTokensTextBox.Text = chatData.SimvolsMax.ToString();
                MaxTokensTextBox.CaretIndex = MaxTokensTextBox.Text.Length;
                _suppressMaxTokensTextChanged = false;
            }
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









        
       
        

        

        //тут мы получаем и отвечаем на вопросы все польхователей
        
        



        
        private void AddMessage(string sender, string message)
        {
            // Очищаем сообщение от тегов <think> и служебных префиксов
            string cleanedMessage = Regex.Replace(message, @"<think>.*?</think>", "", RegexOptions.Singleline);
            cleanedMessage = cleanedMessage.Replace("AI:", "").Replace("Вы:", "").Trim();

            ChatBox.AppendText($"{sender}{cleanedMessage}\n\n");
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
                    max_tokens = 150,
                    stream = true
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
                                    Dispatcher.Invoke(() =>
                                    {
                                        ChatBox.AppendText(token);
                                        ChatBox.ScrollToEnd();
                                    });
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