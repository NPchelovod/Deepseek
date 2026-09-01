//using Deepseek;
//using OllamaChat;
//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Net.Http;
//using System.Text;
//using System.Text.Json;
//using System.Threading;
//using System.Threading.Tasks;

//public class FileRequestProcessor
//{
//    private MainWindow _mainWindow;
//    public FileRequestProcessor(MainWindow mainWindow)
//    {
//        _mainWindow = mainWindow;
//    }

//    private static readonly HttpClient httpClient = new HttpClient()
//    {
//        BaseAddress = new Uri("http://localhost:11434")
//    };

//    private readonly string inboxPath = @"Y:\ИИ\_Вопросы";
//    private readonly string outboxPath = @"Y:\ИИ\_Ответы";
//    private readonly string archivePath = @"Y:\ИИ\_Архив"; // для обработанных запросов (опционально)

//    private FileSystemWatcher watcher;
//    private CancellationTokenSource cts;

//    public void Start()
//    {
//        Directory.CreateDirectory(inboxPath);
//        Directory.CreateDirectory(outboxPath);
//        Directory.CreateDirectory(archivePath);

//        watcher = new FileSystemWatcher(inboxPath)
//        {
//            NotifyFilter = NotifyFilters.FileName | NotifyFilters.LastWrite,
//            Filter = "*.json"
//        };

//        watcher.Created += async (s, e) => await ProcessFileAsync(e.FullPath);
//        watcher.EnableRaisingEvents = true;

//        // Периодическая проверка на случай пропущенных событий
//        cts = new CancellationTokenSource();
//        Task.Run(() => PeriodicallyCheckForMissedFilesAsync(cts.Token));
//    }

//    public void Stop()
//    {
//        watcher?.Dispose();
//        cts?.Cancel();
//    }

//    private async Task PeriodicallyCheckForMissedFilesAsync(CancellationToken token)
//    {
//        while (!token.IsCancellationRequested)
//        {
//            try
//            {
//                foreach (var file in Directory.GetFiles(inboxPath, "*.json"))
//                {
//                    await ProcessFileAsync(file);
//                }
//            }
//            catch { /* ignore */ }
//            await Task.Delay(TimeSpan.FromSeconds(5), token);
//        }
//    }

//    private async Task ProcessFileAsync(string filePath)
//    {
//        try
//        {
//            // Ждём, пока файл полностью запишется (может быть ещё открыт)
//            await WaitForFileReadyAsync(filePath);

//            string json = await File.ReadAllTextAsync(filePath);
//            var request = JsonSerializer.Deserialize<RequestModel>(json);

//            if (request == null)
//                return;

//            // Строим полный промпт
//            string fullPrompt = BuildPrompt(request);
//            // Получаем ответ от Ollama
//            string responseText = await QueryOllamaAsync(fullPrompt);

//            // Формируем ответ
//            var response = new ResponseModel
//            {
//                UserName = request.UserName,
//                RequestId = request.RequestId ?? Path.GetFileNameWithoutExtension(filePath),
//                Response = responseText,
//                Timestamp = DateTime.UtcNow
//            };

//            // Сохраняем ответ
//            string outFile = Path.Combine(outboxPath, $"{response.RequestId}_response.json");
//            await File.WriteAllTextAsync(outFile, JsonSerializer.Serialize(response, new JsonSerializerOptions { WriteIndented = true }));

//            // Перемещаем обработанный запрос в архив
//            if (!string.IsNullOrEmpty(archivePath))
//                File.Move(filePath, Path.Combine(archivePath, Path.GetFileName(filePath)));
//            else
//                File.Delete(filePath);
//        }
//        catch (Exception ex)
//        {
//            // Логируем ошибку, файл оставляем или перемещаем в папку с ошибками
//            File.AppendAllText(Path.Combine(outboxPath, "errors.log"), $"{DateTime.UtcNow}: {ex.Message}\n");
//        }
//    }

//    private async Task WaitForFileReadyAsync(string filePath)
//    {
//        for (int i = 0; i < 10; i++)
//        {
//            try
//            {
//                using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
//                {
//                    // Файл доступен для чтения
//                    return;
//                }
//            }
//            catch (IOException)
//            {
//                await Task.Delay(500);
//            }
//        }
//        throw new IOException($"Файл {filePath} не стал доступен для чтения.");
//    }

//    private string BuildPrompt(RequestModel request)
//    {
//        var sb = new StringBuilder();
//        sb.AppendLine("Ты — полезный ассистент. Отвечай обычным текстом, без LaTeX-разметки.");
//        if (!string.IsNullOrWhiteSpace(request.Context))
//        {
//            sb.AppendLine("=== КОНТЕКСТ ИЗ ФАЙЛОВ ===");
//            sb.AppendLine(request.Context);
//            sb.AppendLine("=== КОНЕЦ КОНТЕКСТА ===");
//            sb.AppendLine();
//        }
//        if (request.History != null && request.History.Count > 0)
//        {
//            foreach (var msg in request.History)
//            {
//                string role = msg.Role == "user" ? "User" : "AI";
//                sb.AppendLine($"{role}: {msg.Content}");
//            }
//        }
//        sb.AppendLine($"User: {request.Prompt}");
//        sb.Append("AI: ");
//        return sb.ToString();
//    }

//    private async Task<string> QueryOllamaAsync(string prompt)
//    {
//        var requestData = new
//        {
//            model = _mainWindow.nameModel, // выберите нужную модель
//            prompt = prompt,
//            temperature = 0.7,
//            max_tokens = _mainWindow.maxSimvols,
//            stream = false
//        };

//        var json = JsonSerializer.Serialize(requestData);
//        var content = new StringContent(json, Encoding.UTF8, "application/json");
//        var response = await httpClient.PostAsync("/api/generate", content);
//        response.EnsureSuccessStatusCode();
//        string responseBody = await response.Content.ReadAsStringAsync();

//        using JsonDocument doc = JsonDocument.Parse(responseBody);
//        return doc.RootElement.GetProperty("response").GetString();
//    }
//}