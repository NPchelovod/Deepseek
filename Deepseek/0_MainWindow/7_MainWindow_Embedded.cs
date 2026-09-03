using Deepseek;
using Deepseek;
using DocumentFormat.OpenXml.Wordprocessing;
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
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ProgressBar;
namespace OllamaChat
{
    public partial class MainWindow
    {
        //Метод для получения эмбеддингов
        public async Task<float[]> GetEmbeddingAsync(string text, ChatData outChatData)
        {
            
            var requestData = new
            {
                model = outChatData.EmbeddingModel,
                input = text,
                //prompt = text
            };
            var json = JsonSerializer.Serialize(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(outChatData.OllamaApiUrlEmbed, content);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            // Console.WriteLine($"Embedding response: {responseString}");
            using JsonDocument doc = JsonDocument.Parse(responseString);
            JsonElement root = doc.RootElement;

            // Ollama возвращает embedding как массив чисел в поле "embedding"
            // Ollama возвращает embedding как массив чисел в поле "embedding" или "embeddings"
            if (root.TryGetProperty("embedding", out JsonElement embeddingElement))
            {
                var embedding = new List<float>();
                foreach (var item in embeddingElement.EnumerateArray())
                {
                    embedding.Add(item.GetSingle());
                }
                return embedding.ToArray();
            }

            // Вариант 2: поле "embeddings" — массив массивов
            if (root.TryGetProperty("embeddings", out JsonElement embeddingsElement) &&
                embeddingsElement.GetArrayLength() > 0)
            {
                // Берём первый (и обычно единственный) вектор эмбеддинга
                var firstEmbeddingArray = embeddingsElement[0];

                var embedding = new List<float>();
                foreach (var item in firstEmbeddingArray.EnumerateArray())
                {
                    embedding.Add(item.GetSingle());
                }
                return embedding.ToArray();
            }
            throw new Exception("Embedding not found in response");
        }

       


        private List<string> SplitIntoChunks(string text, int chunkSize)
        {
            var chunks = new List<string>();
            for (int i = 0; i < text.Length; i += chunkSize)
            {
                chunks.Add(text.Substring(i, Math.Min(chunkSize, text.Length - i)));
            }
            return chunks;
        }
        // Поиск похожих чанков

        private async Task<List<string>> SearchRelevantChunksAsync(string query, ChatData outChatData)
        {
            int topK = outChatData.topK;
            ChankData cd = await ChankData.GetChankData(mainWindow, chatData);
            if(cd==null)
            {
                throw new ArgumentException("ChankData cd  не найдена");
            }
            List <ChunkInfo> chunks = await cd.GetChanks(mainWindow, chatData);


            if (chunks.Count == 0)
            {
                return new List<string>();
            }
            //это вектор вопроса
            var queryEmbedding = await GetEmbeddingAsync(query, outChatData);
            var similarities = new List<(float Score, string Text)>();

            

            foreach (ChunkInfo chunkInfo in chunks)
            {
                float score = CosineSimilarity(queryEmbedding, chunkInfo.Embedding);
                similarities.Add((score, chunkInfo.Text));
            }

            return similarities
                .OrderByDescending(s => s.Score)
                .Take(topK)
                .Select(s => s.Text)
                .ToList();
        }

        

        public float CosineSimilarity(float[] vec1, float[] vec2)
        {
            if (vec1.Length != vec2.Length)
                throw new ArgumentException("Vectors must have same length");

            float dot = 0, mag1 = 0, mag2 = 0;
            for (int i = 0; i < vec1.Length; i++)
            {
                dot += vec1[i] * vec2[i];
                mag1 += vec1[i] * vec1[i];
                mag2 += vec2[i] * vec2[i];
            }
            if (mag1 == 0 || mag2 == 0) return 0;
            return dot / (float)(Math.Sqrt(mag1) * Math.Sqrt(mag2));
        }

        //Формирование промпта и вызов генерации


    }
}