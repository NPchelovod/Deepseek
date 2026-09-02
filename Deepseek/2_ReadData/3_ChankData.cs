using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using OllamaChat;

namespace Deepseek
{
    class ChankData
    {
        public ChankData() { }

        // Хранилище чанков: текст + вектор
        private List<(string Text,string Folder, float[] Embedding)> _chunks { get; set; } = new();
        private HashSet<string> _documents { get; set; } = new();
        
        public async static Task<ChankData> GetChankData(MainWindow mainWindow, ChatData chatData)
        {
            //возвращение
            string folderData = chatData.promptFolder;
            if (!Directory.Exists(folderData))
            {
                Console.WriteLine($"Папка не найдена: {folderData}");
                return null;
            }

            string folderVectors = chatData.promptFolderVectors;
            if (!Directory.Exists(folderVectors))
            {
                Directory.CreateDirectory(folderVectors);
                if (!Directory.Exists(folderVectors)) { return null; }
            }

            //пытаемся прочитать
            // Ищем первый JSON-файл в папке
            var jsonFile = Directory.GetFiles(folderVectors, "*.json")
                                    .FirstOrDefault();

            if (jsonFile == null)
            {
                //создаем свой
                return new ChankData(mainWindow, chatData);
            }
            else
            {
                var json = File.ReadAllText(jsonFile);
                var chankData = JsonSerializer.Deserialize<ChankData>(json);
                if (chankData == null)
                {
                    //создаем свой
                    return new ChankData(mainWindow, chatData);
                }

                
                return chankData;
            }

        }

        public ChankData(MainWindow mainWindow, ChatData chatData)
        {
            //начинаем создание
            string folderData = chatData.promptFolder;
            if (!Directory.Exists(folderData))
            {
                Console.WriteLine($"Папка не найдена: {folderData}");
                return;
            }

            //var chanks = GetChanks(mainWindow, chatData);

            
        }


        public async Task<List<(string Text, string Folder, float[] Embedding)>> GetChanks(MainWindow mainWindow, ChatData chatData, int chunkSize = 800, bool allClear=false)
        {
            //это внешний метод для доступа
            //if(_chunks!=null && _chunks.Count!=0)
            //{
            //    return await Update(mainWindow, chatData, chunkSize);
            //}
            if (allClear)
            {
                _chunks = new List<(string Text, string Folder, float[] Embedding)>(); // Очищаем предыдущую базу знаний (если нужно)
                _documents.Clear();
            }
            var _documents2 = new HashSet<string>();

            string folderData = chatData.promptFolder;

            List<FileText>  lft= TextExtractor.ExtractAllTextFromDirectory(folderData);

            List<string> allText= new List<string>();
            foreach (var l in lft)
            {
                //идем по документам
                var chunks = TextExtractor.ChunkText(l.Text);

                if(_documents.Contains(l.FileName))
                {
                    continue;
                }

                _documents2.Add(l.FileName);
                foreach (var chunk in chunks)
                {
                    var embedding = await mainWindow.GetEmbeddingAsync(chunk);

                    // Добавляем информацию об источнике в текст чанка
                    string chunkWithMeta = $"[{l.FileName}] {chunk}";
                    _chunks.Add((chunkWithMeta,l.FileName, embedding));
                    
                }
                //allText.AddRange(chunks);
            }
            //находим разницу - уделенный документ
            var missingInDocuments2 = _documents.Except(_documents2).ToList();
            // Удаляем чанки удалённых документов за один проход
            _chunks = _chunks.Where(x => !missingInDocuments2.Contains(x.Folder)).ToList();

            _documents = _documents2;

            SaveChankData(mainWindow, chatData);
            return _chunks;
        }
        public async Task<List<(string Text, string Folder, float[] Embedding)>> Update(MainWindow mainWindow, ChatData chatData, int chunkSize)
        {
            string folderData = chatData.promptFolder;
            List<FileText> lft = TextExtractor.ExtractAllTextFromDirectory(folderData);


            SaveChankData(mainWindow, chatData);
            return _chunks;
        }

        public void SaveChankData(MainWindow mainWindow, ChatData chatData)
        {
            string folderVectors = chatData.promptFolderVectors;
            if (!Directory.Exists(folderVectors))
            {
                Directory.CreateDirectory(folderVectors);
                if (!Directory.Exists(folderVectors)) { return; }
            }
            string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { IncludeFields = true, WriteIndented = true });
            File.WriteAllTextAsync(folderVectors, json);
        }

    }
}
