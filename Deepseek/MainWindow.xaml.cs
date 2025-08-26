using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace OllamaChat
{
    public partial class MainWindow : Window
    {
        private readonly HttpClient _httpClient;
        private const string OllamaApiUrl = "http://localhost:11434/api/generate";
        private List<string> _conversationHistory = new List<string>(); // Хранение истории диалога

        public MainWindow()
        {
            InitializeComponent();
            _httpClient = new HttpClient();
            // Оставляем чат пустым при запуске
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

            // Добавляем сообщение пользователя в историю
            _conversationHistory.Add($"User: {prompt}");

            // Отображаем сообщение пользователя
            AddMessage("Вы: ", prompt);
            InputBox.Clear();

            try
            {
                // Формируем промпт с историей диалога
                string fullPrompt = BuildPromptWithHistory();
                string response = await GenerateTextStreamAsync(fullPrompt);

                // Добавляем ответ ИИ в историю
                _conversationHistory.Add($"AI: {response}");

                // Отображаем ответ ИИ
                AddMessage("AI: ", response);
            }
            catch (Exception ex)
            {
                AddMessage("Ошибка: ", ex.Message);
            }
        }

        private string BuildPromptWithHistory()
        {
            // Объединяем историю диалога в один промпт
            StringBuilder promptBuilder = new StringBuilder();

            // Ограничиваем историю последними 10 сообщениями (5 пар вопрос-ответ)
            int startIndex = Math.Max(0, _conversationHistory.Count - 10);

            for (int i = startIndex; i < _conversationHistory.Count; i++)
            {
                promptBuilder.AppendLine(_conversationHistory[i]);
            }

            // Добавляем текущий промпт
            promptBuilder.AppendLine("AI: ");

            return promptBuilder.ToString();
        }

        private void AddMessage(string sender, string message)
        {
            // Очищаем сообщение от тегов <think>
            string cleanedMessage = Regex.Replace(message, @"<think>.*?</think>", "", RegexOptions.Singleline);

            // Удаляем другие служебные сообщения
            cleanedMessage = cleanedMessage.Replace("AI:", "").Replace("Вы:", "").Trim();

            ChatBox.AppendText($"{sender}{cleanedMessage}\n\n");
            ChatBox.ScrollToEnd();
        }

        private async Task<string> GenerateTextStreamAsync(string prompt)
        {
            var fullResponse = new StringBuilder();
            bool inThinkTag = false;

            try
            {
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

                                    // Пропускаем другие служебные сообщения
                                    if (token.StartsWith("AI:") || token.StartsWith("Вы:"))
                                        continue;

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
            catch (Exception ex)
            {
                return $"Error: {ex.Message}";
            }
        }
    }


}