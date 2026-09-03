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
    public partial class MainWindow
    {
        public void InitializeAnswerUsers()
        {
            var scanner = new PeriodicFolderScanner(
                folderPath: chatData.outboxPath,
                fileProcessor: async filePath =>
                {
                    // обработка файла
                    await ProcessQuestionUserFileAsync(filePath);
                },
                intervalMs: 2000
            );
            scanner.Start();
        }
        private async Task ProcessQuestionUserFileAsync(string filePath)
        {
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
                // Шаг 3: Проверяем, что это наш ответ
                if (incomingChatData.Id!=chatData.Id)
                {
                    return; // обрабатываем только свои ответы!
                }

                chatData = incomingChatData;//приравниваем их чтобы развитие было

                if(chatData.AnswerPromptVector!=null)
                {
                    chatData.AnswerPromptVector.EndTime = DateTime.Now;
                }

                if (chatData.OnlyUseCommonContext && chatData.UseCommonContext && chatData.AnswerPromptVector != null && chatData.Id == chatData.AnswerPromptVector.Id)
                {
                    //показываем наше окно
                    //показываем наше окно
                    await Dispatcher.InvokeAsync(() =>
                    {
                        var sW = new ContextWindow(this);
                        sW.Show();
                    });
                }
                else
                {
                    // Шаг 4: Извлекаем последнее сообщение от ИИ
                    // История содержит записи вида "User: ..." и "AI: ..."
                    ChatElement aiMessageCE = incomingChatData.ConversationHistory.Where(x => x.Id == chatData.Id && x.Senders == ESenders.AI_Chat).LastOrDefault();

                    string aiMessage = "";
                    if (aiMessageCE != null)
                    {
                        aiMessageCE.EndTime = DateTime.Now;
                        aiMessage = aiMessageCE.GetAnswerText();
                        aiMessage += $"\n{aiMessageCE.GetTime}";//время овтета
                    }

                    if (string.IsNullOrEmpty(aiMessage))
                    {
                        aiMessage = "Error: Ответа нет";
                    }
                    else
                    {

                        // Шаг 5: Выводим ответ в UI (потокобезопасно)
                        await Dispatcher.InvokeAsync(() =>
                        {
                            // Используем существующий метод AddMessage или напрямую AppendText
                            AddMessage(aiMessage, incomingChatData);
                            // Альтернатива: ChatBox.AppendText(aiMessage + "\n\n");
                        });

                        //chatData.ConversationHistory.Add(aiMessage);
                        // Шаг 6: Обновляем локальную историю пользователя,
                        // чтобы следующий вопрос учитывал этот ответ
                        //if (!chatData.ConversationHistory.Contains(aiMessage))
                        //{
                        //    chatData.ConversationHistory.Add(aiMessage);
                        //}
                    }
                }
                chatData.ChangeId();//меняем id 
                // Шаг 7: Удаляем файл ответа, чтобы не обрабатывать его повторно

                //сохраняем в историю для повторного запуска
                SaveFile();

                DeleteAllMessage(chatData.outboxPath,chatData);
                //File.Delete(filePath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при обработке ответа: {ex.Message}");
                // Файл останется и будет повторно обработан при следующем сканировании 
            }
        }
    }
}