//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
//using OllamaChat;

//namespace Deepseek
//{
//    public static class WriteVopros
//    {
//        //чтение вопросов
//        public static FileSystemWatcher FileSystemWatcher;
//        public static async void PodpiskaMessageVopros()
//        {
//            //подписка на папку с ответами
//            string folderVopros = MainWindow.mainWindow.chatData.inboxPath;
//            if (!Directory.Exists(folderVopros))
//            {
//                return;
//            }
//            using FileSystemWatcher = new FileSystemWatcher
//            {
//                Path = folderVopros,
//                // Можно фильтровать по шаблону, например "*.txt" или "*.json"
//                Filter = "*.*",
//                IncludeSubdirectories = false, // true, если нужно следить и за вложенными папками
//                EnableRaisingEvents = true
//            };

//            // Подписка на события
//            watcher.Created += OnChanged;
//            watcher.Changed += OnChanged;
//            //watcher.Deleted += OnChanged;//пусть удаляется
//            watcher.Renamed += OnChanged;
//        }

//        public static bool VoprosReady = false;
//        private static readonly object _lockObj = new();
//        public static void OnChanged(object sender, FileSystemEventArgs e)
//        {
//            lock (_lockObj)
//            {
//                VoprosReady = false;
//            }
//            if (!MainWindow.mainWindow.chatData.IsAdminCheckBox)
//            { return; }

//            string key = MainWindow.mainWindow.chatData.GetFileName;

//            //мы читаем все вопросы!!!!

//            lock (_lockObj)
//            {
//                VoprosReady = true;
//            }
//             return;
//        }

//        public static List<ChatData> GetVoprosChatData()
//        {
//            var filePaths = Directory.GetFiles(MainWindow.mainWindow.chatData.FullFilePathVopros).ToList();
//            var answer = new List<ChatData>();
//            foreach
//                (var file in filePaths)
//            {
//                var js = new JsonStorage<ChatData>(file);
//                var jsl=js.Load();
//                if (jsl != null)
//                {
//                    answer.Add(jsl);
//                }
//            }
            
//            return answer;
//        }
//    }
//}
