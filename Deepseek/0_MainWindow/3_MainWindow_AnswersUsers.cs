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
                intervalMs: 3000
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
                // Шаг 3: Проверяем, что это наш ответ
                if (incomingChatData.Id!=chatData.Id)
                {
                    return; // обрабатываем только свои ответы!
                }
                // Шаг 4: Извлекаем последнее сообщение от ИИ
                // История содержит записи вида "User: ..." и "AI: ..."
                string aiMessage = incomingChatData.ConversationHistory
                    .LastOrDefault(m => m.StartsWith("AI: "));

                if (aiMessage != null)
                {
                    // Убираем префикс "AI: " для аккуратного отображения
                    string cleanMessage = aiMessage.Substring(4).Trim();

                    // Шаг 5: Выводим ответ в UI (потокобезопасно)
                    await Dispatcher.InvokeAsync(() =>
                    {
                        // Используем существующий метод AddMessage или напрямую AppendText
                        AddMessage("AI: ", cleanMessage);
                        // Альтернатива: ChatBox.AppendText(aiMessage + "\n\n");
                    });

                    // Шаг 6: Обновляем локальную историю пользователя,
                    // чтобы следующий вопрос учитывал этот ответ
                    if (!chatData.ConversationHistory.Contains(aiMessage))
                    {
                        chatData.ConversationHistory.Add(aiMessage);
                    }
                }

                chatData.ChangeId();//меняем id 
                // Шаг 7: Удаляем файл ответа, чтобы не обрабатывать его повторно
                File.Delete(filePath);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Ошибка при обработке ответа: {ex.Message}");
                // Файл останется и будет повторно обработан при следующем сканировании 
            }
        }
    }
}