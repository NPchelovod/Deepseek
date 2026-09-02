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
using Deepseek;
namespace OllamaChat
{
    public partial class MainWindow
    {
        private async void InitializeSettings()
        {
            if (File.Exists(chatData.settingsPath))
            {
                string json = await File.ReadAllTextAsync(chatData.settingsPath);
                var incomingChatData = JsonSerializer.Deserialize<ChatData>(json);
                if (incomingChatData == null) { return; }

                //иначе копируем настройки начальные

                chatData = incomingChatData;//так много проще
                chatData.ChangeId();
            }
            else
            {
                SaveFile();
            }
        }
        private void ClearHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            //очистка истории
            chatData = new ChatData(chatData);
            if (File.Exists(chatData.settingsPath))
            {
                File.Delete(chatData.settingsPath);

            }
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // открытие SettingsWindow.
            var sW = new SettingsWindow(this);
            sW.Show();

            SaveFile();

        }

        public void SaveFile()
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string outJson = JsonSerializer.Serialize(chatData, options);
            File.WriteAllTextAsync(chatData.settingsPath, outJson);
        }
    }
}