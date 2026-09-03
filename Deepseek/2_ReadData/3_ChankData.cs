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


        public async Task<List<(string Text, string Folder, float[] Embedding)>> GetChanks(MainWindow mainWindow, ChatData chatData, bool allClear=false)
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
            string folderData = chatData.promptFolder;
            var files = TextExtractor.GetSupportedFiles(folderData).ToHashSet();
            //находим разницу - уделенный документ
            var missingInDocuments = _documents.Except(files).ToList();

            if (missingInDocuments.Count > 0)
            {
                // Удаляем чанки удалённых документов за один проход
                _chunks = _chunks.Where(x => !missingInDocuments.Contains(x.Folder)).ToList();
                _documents = _documents.Where(x => !missingInDocuments.Contains(x)).ToHashSet();
            }

            var newFiles = files.Except(_documents).ToList();
            if (newFiles.Any())
            {
                List<FileText> lft = TextExtractor.ExtractAllText(newFiles);

                foreach (var l in lft)
                {
                    //идем по документам
                    var chunks = TextExtractor.ChunkText(l.Text, chatData.topK);

                    if (_documents.Contains(l.FileName))
                    {
                        continue;
                    }

                    foreach (var chunk in chunks)
                    {
                        var embedding = await mainWindow.GetEmbeddingAsync(chunk, chatData);

                        // Добавляем информацию об источнике в текст чанка
                        string fileName = Path.GetFileName(l.FileName);
                        string chunkWithMeta = $"[{fileName}] {chunk}";
                        _chunks.Add((chunkWithMeta, l.FileName, embedding));

                    }
                    _documents.Add(l.FileName);
                    //allText.AddRange(chunks);
                }
                //находим разницу - уделенный документ

            }

            if (newFiles.Any() || missingInDocuments.Count!=0)
            {
                SaveChankData(mainWindow, chatData);
            }

            return _chunks;
        }
        //public async Task<List<(string Text, string Folder, float[] Embedding)>> Update(MainWindow mainWindow, ChatData chatData, int chunkSize)
        //{
        //    string folderData = chatData.promptFolder;
        //    List<FileText> lft = TextExtractor.ExtractAllTextFromDirectory(folderData);


        //    SaveChankData(mainWindow, chatData);
        //    return _chunks;
        //}

        public void SaveChankData(MainWindow mainWindow, ChatData chatData)
        {
            string folderVectors = chatData.promptFolderVectors;
            if (!Directory.Exists(folderVectors))
            {
                Directory.CreateDirectory(folderVectors);
                if (!Directory.Exists(folderVectors)) { return; }
            }
            string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { IncludeFields = true, WriteIndented = true });
            File.WriteAllTextAsync(chatData.nameVectors, json);
        }

    }
}
