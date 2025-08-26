using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace OllamaChat
{
    public partial class MainWindow : Window
    {
        private readonly HttpClient _httpClient;
        private const string OllamaApiUrl = "http://localhost:11434/api/generate";

        public MainWindow()
        {
            InitializeComponent();
            _httpClient = new HttpClient();
            ChatBox.Text = "Привет! Задайте ваш вопрос...\n\n";
        }

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            await SendMessage();
        }

        private async void InputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                await SendMessage();
                e.Handled = true;
            }
        }

        private async Task SendMessage()
        {
            string prompt = InputBox.Text.Trim();
            if (string.IsNullOrEmpty(prompt)) return;

            AddMessage("Вы: ", prompt);
            InputBox.Clear();

            try
            {
                string response = await GenerateTextStreamAsync(prompt);
                AddMessage("AI: ", response);
            }
            catch (Exception ex)
            {
                AddMessage("Ошибка: ", ex.Message);
            }
        }

        private void AddMessage(string sender, string message)
        {
            ChatBox.AppendText($"{sender}{message}\n\n");
            ChatBox.ScrollToEnd();
        }

        private async Task<string> GenerateTextStreamAsync(string prompt)
        {
            var fullResponse = new StringBuilder();

            var requestData = new
            {
                model = "deepseek-r1:1.5b",
                prompt = prompt,
                temperature = 0.7,
                max_tokens = 150,
                stream = true
            };

            var json = JsonSerializer.Serialize(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(OllamaApiUrl, content);
            response.EnsureSuccessStatusCode();

            using (var streamReader = new System.IO.StreamReader(await response.Content.ReadAsStreamAsync()))
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
                                fullResponse.Append(token);

                                // Обновляем UI по мере поступления токенов
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
    }
}