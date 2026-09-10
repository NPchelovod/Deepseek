using Deepseek;
using Deepseek;
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
using System.Windows.Controls;
using System.Windows.Input;
namespace OllamaChat
{
    public partial class MainWindow
    {
        private async void InitializeSettings()
        {
            if (!File.Exists(chatData.settingsPath))
            {
                await SaveFile();
                LoadSettingsToWind();
                return;
            }
            try
            {
                string json = await File.ReadAllTextAsync(chatData.settingsPath);
                var incomingChatData = JsonSerializer.Deserialize<ChatData>(json, OptionsJson);
                if (incomingChatData == null) { return; }

                //иначе копируем настройки начальные

                chatData = incomingChatData;//так много проще
                chatData.ChangeId();
                LoadSettingsToWind();


            }
            catch (Exception ex) // На всякий случай
            {
                chatData.Errors.Text += $"\nВ InitializeSettings() {ex.Message}";
                Debug.WriteLine($"Unexpected error: {ex.Message}");
                try
                {
                    await SaveFile();
                    LoadSettingsToWind();
                }
                catch
                {
                    Debug.WriteLine($"Unexpected error: {ex.Message}");
                }
            }

        }

        private void LoadSettingsToWind()
        {
            foreach (var s in chatData.ConversationHistory)
            {
                ChatBox.AppendText($"{s.GetAnswerText()}\n\n");
            }

            //теперь настройки копируем
            UseContextCheckBox.IsChecked = chatData.UseCommonContext;
            QuoteFromKnowledgeBaseCheckBox.IsChecked = chatData.OnlyUseCommonContext;


            UseHistoryVopros.IsChecked=chatData.UseHistoryVopros;
            UseOnlyRelevantHistoryInVopros.IsChecked = chatData.UseOnlyRelevantHistoryInVopros;
            UseOnlyYourQuestion.IsChecked = chatData.UseOnlyYourQuestionInHistory;

            IsAdminCheckBox.IsChecked = chatData.IsAdminCheckBox;

            //комбобокса ИИ
            // Находим элемент по старому значению
            foreach (var item in ModelComboBox.Items)
            {
                if (item is ComboBoxItem cbItem && cbItem.Content.ToString() == chatData.ModelII)
                {
                    ModelComboBox.SelectedItem = cbItem;
                    break;
                }
            }

        }
        private async void ClearHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            //очистка истории
            //chatData = new ChatData(chatData);
            chatData.Clear();
            ChatBox.Clear();
            if (File.Exists(chatData.settingsPath))
            {
                 File.Delete(chatData.settingsPath);
            }
            //сохранение файла
            await SaveFile();
        }

        private async void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            // открытие SettingsWindow.
            var sW = new SettingsWindow(this);
            sW.Show();

            await SaveFile();

        }

        public async Task SaveFile()
        {
            
            string outJson = JsonSerializer.Serialize(chatData, OptionsJson);
            await File.WriteAllTextAsync(chatData.settingsPath, outJson);
        }
    }
}