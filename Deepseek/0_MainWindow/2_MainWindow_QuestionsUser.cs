using Deepseek;
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
    //отправка сообщения
    public partial class MainWindow
    {
        private async void SendButton_Click(object sender, RoutedEventArgs e)
        {
            await SendMessage();
        }

        private async void InputBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)// && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
            {
                await SendMessage();
                e.Handled = true;
            }
        }

        // Метод для кнопки загрузки файлов (добавьте кнопку в XAML и привяжите этот обработчик)
        private void LoadFolderButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "Выберите папку с документами",
                InitialDirectory = Directory.Exists( chatData.promptFolder)? chatData.promptFolder:"" // можно указать начальную папку
            };

            if (dialog.ShowDialog() == true)
            {
               
                string folderPath = dialog.FolderName;
                if (Directory.Exists(folderPath))
                {
                    UseContextCheckBox.IsChecked = true;
                    chatData.promptFolder = folderPath;
                }
            }
        }

        private string  GetContextFileData(ChatData outChatData)
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
        // ==================== ЛОГИКА ОТПРАВКИ СООБЩЕНИЙ ====================

        private async Task SendMessage()
        {
            string prompt = InputBox.Text.Trim();
            if (string.IsNullOrEmpty(prompt)) return;


            chatData.Errors = null;//обнуление для сбора ошибок

            // Добавляем сообщение пользователя в историю
            var sms = new ChatElement()
            {
                Senders = ESenders.User,
                Id = chatData.Id,
                Text = prompt,
                StartTime = DateTime.Now,
            };
            chatData.ConversationHistory.Add(sms);

            // Отображаем сообщение пользователя
            AddMessage($"\n{sms.GetAnswerText()}",chatData);

            InputBox.Clear();

            //удаляем прошлые вопросы 
            DeleteAllMessage(chatData.inboxPath, chatData);

            //сохранение данных

            // Сохраняем вопрос в папку inbox для администратора
            string json = JsonSerializer.Serialize(chatData, new JsonSerializerOptions { IncludeFields = true, WriteIndented = true });
            await File.WriteAllTextAsync(chatData.FullFilePathVopros, json);

        }
    }
}