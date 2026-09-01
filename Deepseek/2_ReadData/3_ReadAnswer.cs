using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using OllamaChat;
namespace Deepseek
{
    public static class ReadAnswer
    {

        //чтение ответов
        public static FileSystemWatcher fileSystemWatcher;
        public static async void PodpiskaMessageAnswer()
        {
            //подписка на папку с ответами
            string folderAnswer = MainWindow.mainWindow.chatData.outboxPath;
            if (!Directory.Exists(folderAnswer))
            {
                return;
            }
            using var FileSystemWatcher = new FileSystemWatcher
            {
                Path = folderAnswer,
                // Можно фильтровать по шаблону, например "*.txt" или "*.json"
                Filter = "*.*",
                IncludeSubdirectories = false, // true, если нужно следить и за вложенными папками
                EnableRaisingEvents = true
            };

            // Подписка на события
            FileSystemWatcher.Created += OnChanged;
            FileSystemWatcher.Changed += OnChanged;
            //FileSystemWatcher.Deleted += OnChanged;
            FileSystemWatcher.Renamed += OnChanged;
        }

        public static bool AnswerReady = false;
        private static readonly object _lockObj = new();
        public static void OnChanged(object sender, FileSystemEventArgs e)
        {

            string key = MainWindow.mainWindow.chatData.GetFileName;
            if (!e.Name.Contains(key))
            {
                lock (_lockObj)
                {
                    AnswerReady = false;
                }
                return;
            }
            // Безопасное изменение флага из фонового потока
            lock (_lockObj)
            {
                AnswerReady = true;
            }
        }

        public static ChatData? GetAnswerChatData()
        {
           var js= new JsonStorage<ChatData>(MainWindow.mainWindow.chatData.FullFilePathOtvet);
           return js.Load();
        }
    }
}
