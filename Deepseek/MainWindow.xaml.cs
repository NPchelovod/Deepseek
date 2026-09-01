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
using Microsoft.Win32; // Для OpenFileDialog

namespace OllamaChat
{
    public partial class MainWindow : Window
    {
        private readonly HttpClient _httpClient;
        private const string OllamaApiUrl = "http://localhost:11434/api/generate";
        private List<string> _conversationHistory = new List<string>(); // История диалога
        private string _contextFromFiles = string.Empty; // Текст из загруженных .txt файлов

        public MainWindow()
        {
            InitializeComponent();
            _httpClient = new HttpClient();
            // Чат пуст при запуске
        }

        // ==================== ОБРАБОТЧИКИ СОБЫТИЙ ====================

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

        // Метод для кнопки загрузки файлов (добавьте кнопку в XAML и привяжите этот обработчик)
        private void LoadFilesButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Multiselect = true,
                Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*",
                Title = "Выберите текстовые файлы с предысторией"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                var sb = new StringBuilder();
                foreach (string filePath in openFileDialog.FileNames)
                {
                    try
                    {
                        sb.AppendLine(File.ReadAllText(filePath));
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка чтения файла {filePath}: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                _contextFromFiles = sb.ToString();
                // Опционально: показать пользователю, что файлы загружены
                // StatusText.Text = $"Загружено файлов: {openFileDialog.FileNames.Length}";
            }
        }

        // ==================== ЛОГИКА ОТПРАВКИ СООБЩЕНИЙ ====================

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
                // Формируем полный промпт (контекст из файлов + история диалога)
                string fullPrompt = BuildPromptWithHistory();
                string response = await GenerateTextStreamAsync(fullPrompt);

                // Добавляем ответ ИИ в историю
                _conversationHistory.Add($"AI: {response}");

                // Ответ уже выведен потоково в GenerateTextStreamAsync,
                // добавляем только перевод строки для визуального разделения
                Dispatcher.Invoke(() =>
                {
                    ChatBox.AppendText("\n");
                    ChatBox.ScrollToEnd();
                });
            }
            catch (Exception ex)
            {
                AddMessage("Ошибка: ", ex.Message);
            }
        }

        private string BuildPromptWithHistory()
        {
            var promptBuilder = new StringBuilder();

            // 1. Системная инструкция (если нужно)
            promptBuilder.AppendLine("Ты — полезный ассистент. Используй предоставленный контекст для ответа на вопросы.");

            // 2. Контекст из загруженных файлов (если есть)
            if (!string.IsNullOrWhiteSpace(_contextFromFiles))
            {
                // Ограничиваем размер контекста, чтобы не превысить лимиты модели (например, последние 5000 символов)
                string context = _contextFromFiles.Length > 5000
                    ? _contextFromFiles.Substring(_contextFromFiles.Length - 5000)
                    : _contextFromFiles;

                promptBuilder.AppendLine("=== КОНТЕКСТ ИЗ ФАЙЛОВ ===");
                promptBuilder.AppendLine(context);
                promptBuilder.AppendLine("=== КОНЕЦ КОНТЕКСТА ===\n");
            }

            // 3. История диалога (последние 10 сообщений)
            int startIndex = Math.Max(0, _conversationHistory.Count - 10);
            for (int i = startIndex; i < _conversationHistory.Count; i++)
            {
                promptBuilder.AppendLine(_conversationHistory[i]);
            }

            // 4. Маркер для ответа ИИ
            promptBuilder.Append("AI: ");

            return promptBuilder.ToString();
        }

        private void AddMessage(string sender, string message)
        {
            // Очищаем сообщение от тегов <think> и служебных префиксов
            string cleanedMessage = Regex.Replace(message, @"<think>.*?</think>", "", RegexOptions.Singleline);
            cleanedMessage = cleanedMessage.Replace("AI:", "").Replace("Вы:", "").Trim();

            ChatBox.AppendText($"{sender}{cleanedMessage}\n\n");
            ChatBox.ScrollToEnd();
        }

        // ==================== ГЕНЕРАЦИЯ ОТВЕТА (ПОТОКОВАЯ) ====================

        private async Task<string> GenerateTextStreamAsync(string prompt)
        {
            var fullResponse = new StringBuilder();
            bool inThinkTag = false;

            try
            {
                var requestData = new
                {
                    model = " gemma2:9b",//"qwen2.5:32b-instruct-q4_K_M",//"deepseek-r1:8b",
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