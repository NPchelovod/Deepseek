using Deepseek;
using Deepseek;
using DocumentFormat.OpenXml.InkML;
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
        
        PeriodicFolderScanner scanerAnswerA = null;
        public void InitializeAnswerAdmin()
        {
            if (scanerAnswerA != null)
            {
                scanerAnswerA.Stop();//чтобы перезаписаться
            }

            if (!Directory.Exists(chatData.inboxPath))
            {
                chatData.Errors.Text += $"\n Не существует пути inboxPath={chatData.inboxPath}";
                ErrorsWriter();
            }
            else
            {
                //подписка на папку
                scanerAnswerA = new PeriodicFolderScanner(
                    folderPath: chatData.inboxPath,
                    fileProcessor: async filePath =>
                    {
                        // обработка файла
                        await ProcessQuestionAdminFileAsync(filePath);
                    },
                    intervalMs: 2000
                );
                scanerAnswerA.Start();
            }
        }

        

        private async Task ProcessQuestionAdminFileAsync(string filePath)
        {
            if (!chatData.IsAdminCheckBox) {  return; }
            // ...
            try
            {
                
                // Шаг 1: Дождаться, пока файл будет полностью записан.
                // Простая задержка 100-200 мс. Для надёжности можно проверять стабильность размера файла.
                await Task.Delay(200);

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
                incomingChatData.AnswerPromptVector = null;// на всякий случай
                incomingChatData.AnswerAndQuestionsPromptVector = null;// на всякий случай

                string fullPrompt = await BuildPromptWithHistory(incomingChatData);

                string response = "";
                if (incomingChatData.OnlyUseCommonContext && incomingChatData.UseCommonContext)
                {
                    //мы не входим во второй ИИ который требует ресурса
                }
                else
                {
                    // Шаг 4: Вызвать генерацию ответа (потоковую), передавая модель и другие параметры
                    response = await GenerateTextStreamAsync(fullPrompt, incomingChatData);

                    // Шаг 5: Добавить ответ в историю диалога
                    incomingChatData.ConversationHistory.Add(new ChatElement { Text = response, Id= incomingChatData.Id, Senders=ESenders.AI_Chat, StartTime=DateTime.Now });
                    
                }

                incomingChatData.AnswerAndQuestionsPromptVector = new ChatElement()
                {
                    Text = fullPrompt+"\n"+response,
                    Id = incomingChatData.Id,
                    Senders = ESenders.AI_Prompt_And_User_Questions,
                    StartTime = DateTime.Now
                };


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


        private async Task<string> GetQuestions(ChatData outChatData)//последний ответ пользователя
        {
            int MaxPastAnswers = outChatData.LastMessageInQuestion;
            ChatElement UsMessageCE = outChatData.ConversationHistory.Where(x => x.Id == outChatData.Id && x.Senders == ESenders.User).LastOrDefault();
            string question = "";
            if (UsMessageCE != null)
            {
                question = UsMessageCE.Text;
            }
            if (MaxPastAnswers<1 || string.IsNullOrEmpty(question))
            {
                return question;
            }

            //иначе возвращаем имбединг модели
            float[] questionEmbedding=null;
            if (outChatData.UseCommonContext)
            {
                //заполняем косинусовое сходство
                questionEmbedding = await GetEmbeddingAsync(question, outChatData); // ваш метод
                UsMessageCE.Embedding = questionEmbedding;

            }
            int maxSimvols= outChatData.SimvolsVoprosMax;

            //int maxSimvols = Math.Max(2000, outChatData.SimvolsVoprosMax -
            //         (outChatData.UseCommonContext ? outChatData.topK * outChatData._chunkWordSize : 0));
            int currentSimvols = question.Length;
            var selectedMessages = new List<string>();
            double threshold = 0.7; // подберите экспериментально

            var list = outChatData.ConversationHistory;
            // Идём от предпоследнего сообщения назад
            for (int i = list.Count - 2; i >= 0; i--)
            {
                var item = list[i];
                if (currentSimvols >= maxSimvols) break;

                float[] msgEmbedding = item.Embedding;
                if (msgEmbedding == null && outChatData.UseCommonContext)
                {
                    msgEmbedding = await GetEmbeddingAsync(item.Text, outChatData);
                    item.Embedding = msgEmbedding; // кэшируем
                }

                double similarity = CosineSimilarity(questionEmbedding, msgEmbedding);
                if (similarity < threshold) continue; // пропускаем нерелевантные

                string text = item.GetAnswerText();
                if (currentSimvols + text.Length > maxSimvols)
                {
                    // Можно обрезать или остановиться
                    break;
                }
                selectedMessages.Insert(0, text);
                currentSimvols += text.Length;
            }

            if (selectedMessages.Count == 0)
                return question;

            return string.Join("\n", selectedMessages) + "\n" + question;
        
        }

        private async Task<string> BuildPromptWithHistory(ChatData outChatData)
        {
            if (outChatData.ConversationHistory.Count == 0)
            {
                return "";
            }
            var promptBuilder = new StringBuilder();

            string question =await GetQuestions(outChatData);
            
            if(string.IsNullOrEmpty(question) )
            { 
                return question; 
            }


            // 1. Системная инструкция
            promptBuilder.AppendLine("Ты — полезный ассистент. Отвечай обычным текстом, без LaTeX-разметки.");
            if (outChatData.UseCommonContext)
            {
                // 1. Ищем релевантные фрагменты
                promptBuilder.AppendLine("Используй предоставленный контекст для ответа на вопросы.");
                var relevantChunks = await SearchRelevantChunksAsync(question, outChatData);
                var context = string.Join("\n\n", relevantChunks.Select((c, i) => $"Документ {i + 1}:\n{c}"));



                // 2. Контекст из файлов (если есть)
                if (string.IsNullOrEmpty(context))
                {

                    context = GetContextFileData(outChatData);
                }

                // Вы можете настроить N в зависимости от модели и её контекстного окна.
                    int maxContextLength = outChatData.SimvolsVoprosMax;
                context = context.Length > maxContextLength
                    ? context.Substring(0, maxContextLength)
                    : context;
                
                outChatData.AnswerPromptVector = new ChatElement()
                {

                    Id = outChatData.Id,
                    StartTime = DateTime.Now,
                    Senders = ESenders.AI_Prompt,
                    Text = context,
                    PromptQuestion = question,
                };
                
                if (!string.IsNullOrEmpty(context))
                {
                    promptBuilder.AppendLine("=== КОНТЕКСТ ИЗ ФАЙЛОВ ===");
                    promptBuilder.AppendLine(context);
                    promptBuilder.AppendLine("=== КОНЕЦ КОНТЕКСТА ===");
                    promptBuilder.AppendLine();
                }

            }
            promptBuilder.AppendLine("Мой вопрос:");
            promptBuilder.AppendLine(question);
            





                // 4. Маркер для ответа ИИ
                promptBuilder.Append("AI: ");

            return promptBuilder.ToString();
        }

        private string GetContextFileData(ChatData outChatData)
        {
            // Дальше тот же код загрузки, что и раньше
            if (!outChatData.UseCommonContext)
            {
                return "";
            }
            else
            {
                try
                {
                    var extracted = TextExtractor.ExtractAllTextFromDirectory(outChatData.promptFolder);
                    var sb = new StringBuilder();
                    foreach (var file in extracted)
                    {
                        sb.AppendLine($"=== {file.FileName} ===");
                        sb.AppendLine(file.Text);
                        sb.AppendLine();
                    }
                    //UseContextCheckBox.IsChecked = true;
                    return sb.ToString();
                    /*MessageBox.Show($"Загружено {extracted.Count} файлов.", "Готово", MessageBoxButton.OK, *///MessageBoxImage.Information);

                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return "";
                }
            }
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
                    max_tokens = Math.Max(2100, outChatData.AllTokensWords),//max_tokens_для_ответа = лимит_контекста - токены_в_промпте 1024 безопасный вариант
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