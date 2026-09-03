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
    public class ChunkInfo
    {
        public string Text { get; set; }
        public string Folder { get; set; }
        public float[] Embedding { get; set; }
    }
    class ChankData
    {
        public ChankData() { }

        // Хранилище чанков: текст + вектор
        //public List<(string Text,string Folder, float[] Embedding)> _chunks { get; set; } = new();
        public List<ChunkInfo> _chunks { get; set; } = new();
        public HashSet<string> _documents { get; set; } = new();

        public string EmbeddedModel { get; set; } = "";// модель ИИ


        public async Task<List<ChunkInfo>> GetChanks(MainWindow mainWindow, ChatData chatData, bool allClear = false)
        {

            //ChankData cd = await ChankData.GetChankData(mainWindow, chatData);
            
            if (allClear)
            {
                _chunks.Clear();
                _documents.Clear();
            }

            string folderData = chatData.promptFolder;
            var files = TextExtractor.GetSupportedFiles(folderData).ToHashSet();

            // Удаляем чанки, соответствующие удалённым файлам
            var removedFiles = _documents.Except(files).ToList();
            if (removedFiles.Count > 0)
            {
                _chunks = _chunks.Where(x => !removedFiles.Contains(x.Folder)).ToList();
                _documents = _documents.Where(x => !removedFiles.Contains(x)).ToHashSet();
            }

            // Находим новые файлы
            var newFiles = files.Except(_documents).ToList();
            if (newFiles.Any())
            {
                var extracted = TextExtractor.ExtractAllText(newFiles);
                foreach (var fileText in extracted)
                {
                    if (_documents.Contains(fileText.FileName))
                        continue;

                    //!!!! Внимание важная вещь
                    var chunks = TextExtractor.ChunkTextByWords(fileText.Text, chatData._chunkWordSize);

                    EmbeddedModel =chatData.EmbeddingModel;//парамтеры по которым мы создавали нашу модель
                    string fileName = Path.GetFileName(fileText.FileName);
                    var addChanks=await mainWindow.GetAllEmbeddingsBatchAsync(chunks, chatData, fileName);
                    _chunks.AddRange(addChanks);
                    //foreach (var chunk in chunks)
                    //{
                    //    var embedding = await mainWindow.GetEmbeddingAsync(chunk, chatData);
                        
                    //    string chunkWithMeta = $"[{fileName}] {chunk}";
                    //    _chunks.Add(new ChunkInfo
                    //    {
                    //        Text = chunkWithMeta,
                    //        Folder = fileText.FileName,
                    //        Embedding = embedding
                    //    });
                    //}
                    _documents.Add(fileText.FileName);
                }
            }

            if (newFiles.Any() || removedFiles.Count != 0)
            {
                SaveChankData(mainWindow, chatData);
            }

            return _chunks;
        }
        public static async Task<ChankData> GetChankData(MainWindow mainWindow, ChatData chatData)
        {
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
                if (!Directory.Exists(folderVectors))
                    return null;
            }
            var jsonFiles = Directory.GetFiles(folderVectors, "*.json").ToList();
            var jsonFile = jsonFiles.FirstOrDefault();
            var jsonFile2 = jsonFiles.Where(x=>x.Contains(chatData.EmbeddingModel)).FirstOrDefault();
            if (!string.IsNullOrEmpty(jsonFile2))
            {
                jsonFile = jsonFile2;
            }
            if (!string.IsNullOrEmpty(jsonFile))
            {
                var json = File.ReadAllText(jsonFile);
                var chankData = JsonSerializer.Deserialize<ChankData>(json);
                if (chankData != null)
                {
                    // Проверяем, что список чанков не содержит null-элементов (на случай старого формата)
                    if (chankData._chunks.Any(c => c == null))
                    {
                        // Если есть null, пересоздаём заново
                        chankData = new ChankData();
                        await chankData.GetChanks(mainWindow, chatData, allClear: true);
                    }
                    else if(chankData.EmbeddedModel!= chatData.EmbeddingModel)
                    {
                        //не наша ИИ модель вектора
                        chankData = new ChankData();
                    }

                    return chankData;
                }
            }

            // Если файла нет или он повреждён — создаём новую базу
            var newData = new ChankData();
            await newData.GetChanks(mainWindow, chatData, allClear: true);
            return newData;
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
