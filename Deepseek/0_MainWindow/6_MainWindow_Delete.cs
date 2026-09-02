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

        public void DeleteChatData()
        {

        }
        public void DeleteAllMessage(string inboxPath)
        {
            if (!Directory.Exists(inboxPath))
                return;

            var files = Directory.GetFiles(inboxPath, $"{chatData.GetFilePrefix}*"); // шаблон: ChatData_*

            foreach (var file in files)
            {
                try
                {
                    File.Delete(file);
                }
                catch (IOException ex)
                {
                    // файл занят или нет прав — логируй или показывай ошибку
                    Console.WriteLine($"Не удалось удалить {file}: {ex.Message}");
                }
                catch (UnauthorizedAccessException ex)
                {
                    Console.WriteLine($"Нет прав на удаление {file}: {ex.Message}");
                }
            }
        }

    }
}