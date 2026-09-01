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

namespace OllamaChat
{
    public partial class MainWindow : Window
    {
        private readonly HttpClient _httpClient;
        private const string OllamaApiUrl = "http://localhost:11434/api/generate";
        private List<string> _conversationHistory = new List<string>(); // История диалога
        private string _contextFromFiles = string.Empty; // Текст из загруженных .txt файлов
        private bool _useCommonContext = false; // по умолчанию контекст не используется
        private FileRequestProcessor fileProcessor;
        public MainWindow()
        {
            InitializeComponent();
            _httpClient = new HttpClient();
            // Чат пуст при запуске
            //_ = LoadInitialContextAsync(); // запускаем без ожидания, чтобы не блоки
            //fileProcessor = new FileRequestProcessor(this);
            //fileProcessor.Start();

            int c = 0;
        }

        public string nameModel = "qwen2.5:7b-instruct-q4_K_M";//"gemma2:9b",//"qwen2.5:32b-instruct-q4_K_M",//"deepseek-r1:8b",
        public int maxSimvols = 140000;

        //ollama pull qwen2.5:14b-instruct-q4_K_M
        // ==================== ОБРАБОТЧИКИ СОБЫТИЙ ====================

        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            await SendMessage();
        }

        private async void InputBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)// && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                await SendMessage();
                e.Handled = true;
            }
        }


        private void UseContextCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            _useCommonContext = UseContextCheckBox.IsChecked == true;
        }

        // Метод для кнопки загрузки файлов (добавьте кнопку в XAML и привяжите этот обработчик)
        private void LoadFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Выберите папку с документами",
                InitialDirectory = @"Y:\ИИ\_БазаДанных" // можно указать начальную папку
            };

            if (dialog.ShowDialog() == true)
            {
                string folderPath = dialog.FolderName;
                // Дальше тот же код загрузки, что и раньше
                try
                {
                    var extracted = TextExtractor.ExtractAllTextFromDirectory(folderPath);
                    var sb = new StringBuilder();
                    foreach (var file in extracted)
                    {
                        sb.AppendLine($"=== {file.FileName} ===");
                        sb.AppendLine(file.Text);
                        sb.AppendLine();
                    }
                    _contextFromFiles = sb.ToString();
                    MessageBox.Show($"Загружено {extracted.Count} файлов.", "Готово", MessageBoxButton.OK, MessageBoxImage.Information);

                    UseContextCheckBox.IsChecked = true;
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
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
                // Запускаем таймер
                var stopwatch = Stopwatch.StartNew();
                // после AddMessage("Вы: ", prompt);
                Dispatcher.Invoke(() => ChatBox.AppendText("AI: "));
                string response = await GenerateTextStreamAsync(fullPrompt);
                // Останавливаем таймер
                stopwatch.Stop();
                // Добавляем ответ ИИ в историю
                _conversationHistory.Add($"AI: {response}");

                // Ответ уже выведен потоково в GenerateTextStreamAsync,
                // добавляем только перевод строки для визуального разделения
                // Выводим время ответа
                Dispatcher.Invoke(() =>
                {
                    ChatBox.AppendText($"\n\n[Время ответа: {stopwatch.Elapsed.TotalSeconds:F2} сек | {stopwatch.ElapsedMilliseconds} мс |{nameModel}]");
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

            // 1. Системная инструкция
            promptBuilder.AppendLine("Ты — полезный ассистент. Отвечай обычным текстом, без LaTeX-разметки. Используй предоставленный контекст для ответа на вопросы.");

            // 2. Контекст из файлов (если есть)
            if (!string.IsNullOrWhiteSpace(_contextFromFiles))
            {
                // Берём первые N символов, чтобы не превысить лимит модели.
                // Вы можете настроить N в зависимости от модели и её контекстного окна.
                int maxContextLength = maxSimvols; // символов
                string context = _contextFromFiles.Length > maxContextLength
                    ? _contextFromFiles.Substring(0, maxContextLength)
                    : _contextFromFiles;

                promptBuilder.AppendLine("=== КОНТЕКСТ ИЗ ФАЙЛОВ ===");
                promptBuilder.AppendLine(context);
                promptBuilder.AppendLine("=== КОНЕЦ КОНТЕКСТА ===");
                promptBuilder.AppendLine();
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
                    model =nameModel,
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