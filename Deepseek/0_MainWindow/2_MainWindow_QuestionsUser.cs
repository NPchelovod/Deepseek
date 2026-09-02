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
                InitialDirectory = chatData.promptFolder // можно указать начальную папку
            };

            if (dialog.ShowDialog() == true)
            {
                string folderPath = dialog.FolderName;
                if (Directory.Exists(folderPath))
                {
                    chatData.promptFolder = folderPath;
                }
            }
        }

        private void GetContextFileData()
        {
            // Дальше тот же код загрузки, что и раньше
            if (!chatData.UseCommonContext)
            {
                chatData.ContextFromFiles = "";
            }
            else
            {
                try
                {
                    var extracted = TextExtractor.ExtractAllTextFromDirectory(chatData.promptFolder);
                    var sb = new StringBuilder();
                    foreach (var file in extracted)
                    {
                        sb.AppendLine($"=== {file.FileName} ===");
                        sb.AppendLine(file.Text);
                        sb.AppendLine();
                    }
                    chatData.ContextFromFiles = sb.ToString();
                    /*MessageBox.Show($"Загружено {extracted.Count} файлов.", "Готово", MessageBoxButton.OK, *///MessageBoxImage.Information);

                    UseContextCheckBox.IsChecked = true;
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        // ==================== ЛОГИКА ОТПРАВКИ СООБЩЕНИЙ ====================

        private async Task SendMessage()
        {
            string prompt = InputBox.Text.Trim();
            if (string.IsNullOrEmpty(prompt)) return;

            // Добавляем сообщение пользователя в историю
            GetContextFileData();
            // Добавляем сообщение пользователя в историю
            chatData.ConversationHistory.Add($"User: {prompt}");

            // Отображаем сообщение пользователя
            AddMessage($"\nВы_{chatData.Id}: ", prompt,chatData);
            InputBox.Clear();

            //удаляем прошлые вопросы 
            DeleteAllMessage(chatData.inboxPath);

            //сохранение данных

            // Сохраняем вопрос в папку inbox для администратора
            string json = JsonSerializer.Serialize(chatData, new JsonSerializerOptions { IncludeFields = true, WriteIndented = true });
            await File.WriteAllTextAsync(chatData.FullFilePathVopros, json);

        }
    }
}