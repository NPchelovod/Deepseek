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
                Title = "Выберите папку с документами Базы Знаний",
                InitialDirectory = Directory.Exists( chatData.promptFolder)? chatData.promptFolder:"" // можно указать начальную папку
            };

            if (dialog.ShowDialog() == true)
            {
               
                string folderPath = dialog.FolderName;
                if (Directory.Exists(folderPath))
                {
                    chatData.promptFolder = folderPath;
                    UseContextCheckBox.IsChecked = true;
                    
                }
            }
        }

        
        // ==================== ЛОГИКА ОТПРАВКИ СООБЩЕНИЙ ====================

        private async Task SendMessage()
        {
            string prompt = InputBox.Text.Trim();
            if (string.IsNullOrEmpty(prompt))
            {
                return;
            }


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

            //удаляем прошлые вопросы из директории
            DeleteAllMessage(chatData.inboxPath, chatData);

            //сохранение данных

            // Сохраняем вопрос в папку inbox для администратора
            string json = JsonSerializer.Serialize(chatData, new JsonSerializerOptions { IncludeFields = true, WriteIndented = true });

            if (Directory.Exists(chatData.inboxPath))
            {
                await File.WriteAllTextAsync(chatData.FullFilePathVopros, json);
            }
            else
            {
                chatData.Errors.Text += $"\nОшибка в сохранении вопроса, в Task SendMessage(), Возможно не существует {chatData.inboxPath}";
                ErrorsWriter();
            }

        }
    }
}