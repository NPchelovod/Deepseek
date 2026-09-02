using Deepseek;
using Deepseek;
using DocumentFormat.OpenXml.Office.SpreadSheetML.Y2023.MsForms;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using Microsoft.Win32; // Для OpenFileDialog
using System;
using System.Collections.Generic;
using System.Diagnostics; // добавить в using
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
    public partial class MainWindow
    {
        public void InitializeAnswerAdmin()
        {
            var scanner = new PeriodicFolderScanner(
                folderPath:chatData.inboxPath,
                fileProcessor: async filePath =>
                {
                    // обработка файла
                    await ProcessQuestionAdminFileAsync(filePath);
                },
                intervalMs: 3000
            );
            scanner.Start();
        }
        private async Task ProcessQuestionAdminFileAsync(string filePath)
        {
            if (!chatData.IsAdminCheckBox) {  return; }
            // ...
            try
            {
                
                // Шаг 1: Дождаться, пока файл будет полностью записан.
                // Простая задержка 100-200 мс. Для надёжности можно проверять стабильность размера файла.
                await Task.Delay(150);

                // Шаг 2: Прочитать JSON из файла
                string json = await File.ReadAllTextAsync(filePath);
                var incomingChatData = JsonSerializer.Deserialize<ChatData>(json);
                if (incomingChatData == null)
                {
                    // Файл пустой или повреждён – можно переместить в отдельную папку ошибок или удалить
                    //File.Delete(filePath);
                    return;
                }

                // Шаг 3: Сформировать полный промпт с учётом контекста и истории
                string fullPrompt = await BuildPromptWithHistory(incomingChatData);

                // Шаг 4: Вызвать генерацию ответа (потоковую), передавая модель и другие параметры
                string response = await GenerateTextStreamAsync(fullPrompt, incomingChatData);

                // Шаг 5: Добавить ответ в историю диалога
                incomingChatData.ConversationHistory.Add($"AI: {response}");

                // Шаг 6: Сохранить обновлённый ChatData в папку ответов
                string outFilePath = Path.Combine(incomingChatData.outboxPath, incomingChatData.GetFileName);
                var options = new JsonSerializerOptions { WriteIndented = true };
                string outJson = JsonSerializer.Serialize(incomingChatData, options);
                await File.WriteAllTextAsync(outFilePath, outJson);

                // Шаг 7: Переместить исходный файл вопроса в архив (или удалить)
                string archiveDir = incomingChatData.archivePath;
                if (!string.IsNullOrEmpty(archiveDir))
                {
                    Directory.CreateDirectory(archiveDir);
                    string archiveFilePath = Path.Combine(archiveDir, Path.GetFileName(filePath));
                    File.Move(filePath, archiveFilePath, overwrite: true);
                }
                else
                {
                    File.Delete(filePath);
                }

                // Опционально: вывести информацию в лог или в UI администратора
                Debug.WriteLine($"Обработан вопрос от {incomingChatData.Id}: {incomingChatData.ConversationHistory.LastOrDefault()}");
            }
            catch (Exception ex)
            {
                // Логирование ошибки. Файл можно оставить для повторной попытки.
                Debug.WriteLine($"Ошибка при обработке {filePath}: {ex.Message}");
                // Если ошибка фатальна, можно переместить файл в папку с ошибками.
            }
        }

        private async Task<string> BuildPromptWithHistory(ChatData outChatData)
        {
            var promptBuilder = new StringBuilder();
            outChatData.PromptVectorAnswer = "";// на всякий случай
            if (outChatData.ConversationHistory.Count == 0)
            { 
                return promptBuilder.ToString();
            }

            string question = outChatData.ConversationHistory[outChatData.ConversationHistory.Count - 1].Replace("User:","");

            // 1. Системная инструкция
            promptBuilder.AppendLine("Ты — полезный ассистент. Отвечай обычным текстом, без LaTeX-разметки. Используй предоставленный контекст для ответа на вопросы.");
            if (outChatData.UseCommonContext)
            {
                // 1. Ищем релевантные фрагменты
                
                var relevantChunks = await SearchRelevantChunksAsync(question, outChatData);
                var context = string.Join("\n\n", relevantChunks.Select((c, i) => $"Документ {i + 1}:\n{c}"));

                // 2. Контекст из файлов (если есть)
                if (string.IsNullOrEmpty(context))
                {

                    context = GetContextFileData(outChatData);
                    

                   // Вы можете настроить N в зависимости от модели и её контекстного окна.
                        int maxContextLength = outChatData.SimvolsMax;
                    context = context.Length > maxContextLength
                        ? context.Substring(0, maxContextLength)
                        : context;
                }
                else
                {
                    outChatData.PromptVectorAnswer = context;
                }
                promptBuilder.AppendLine("=== КОНТЕКСТ ИЗ ФАЙЛОВ ===");
                promptBuilder.AppendLine(context);
                promptBuilder.AppendLine("=== КОНЕЦ КОНТЕКСТА ===");
                promptBuilder.AppendLine();

            }


            

            //// 2. Контекст из файлов (если есть)
            //if (!string.IsNullOrWhiteSpace(outChatData.ContextFromFiles) && outChatData.UseCommonContext)
            //{
            //    // Берём первые N символов, чтобы не превысить лимит модели.
            //    // Вы можете настроить N в зависимости от модели и её контекстного окна.
            //    int maxContextLength = outChatData.SimvolsMax;
            //    string context = outChatData.ContextFromFiles.Length > maxContextLength
            //        ? outChatData.ContextFromFiles.Substring(0, maxContextLength)
            //        : outChatData.ContextFromFiles;

            //    promptBuilder.AppendLine("=== КОНТЕКСТ ИЗ ФАЙЛОВ ===");
            //    promptBuilder.AppendLine(context);
            //    promptBuilder.AppendLine("=== КОНЕЦ КОНТЕКСТА ===");
            //    promptBuilder.AppendLine();
            //}

            // 3. История диалога (последние 10 сообщений)
            int startIndex = Math.Max(0, outChatData.ConversationHistory.Count - 10);
            for (int i = startIndex; i < outChatData.ConversationHistory.Count; i++)
            {
                promptBuilder.AppendLine(outChatData.ConversationHistory[i]);
            }

            // 4. Маркер для ответа ИИ
            promptBuilder.Append("AI: ");

            return promptBuilder.ToString();
        }
    }
}