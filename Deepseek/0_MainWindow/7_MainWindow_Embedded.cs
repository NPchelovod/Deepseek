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
        private async Task<float[]> GetEmbeddingAsync(string text)
        {
            var requestData = new
            {
                model = chatData.EmbeddingModel,
                input = text
                //prompt = text
            };
            var json = JsonSerializer.Serialize(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(OllamaApiUrlEmbed, content);
            response.EnsureSuccessStatusCode();

            var responseString = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Embedding response: {responseString}");
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

        //Индексация документов
        public async Task IndexDocumentsAsync(IEnumerable<string> documents)
        {
            chatData._chunks.Clear();
            foreach (var doc in documents)
            {
                // Разбиваем документ на чанки (пример простого разбиения по 800 символов)
                var chunks = SplitIntoChunks(doc, 800);
                foreach (var chunk in chunks)
                {
                    var embedding = await GetEmbeddingAsync(chunk);
                    chatData._chunks.Add((chunk, embedding));
                }
            }
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

        private async Task<List<string>> SearchRelevantChunksAsync(string query, ChatData outChatData, int topK = 3)
        {
            List<(string Text, float[] Embedding)> _chunks = outChatData._chunks;
            if (_chunks.Count == 0)
            {
                _chunks = await GetChanks(outChatData);
            }

            var queryEmbedding = await GetEmbeddingAsync(query);
            var similarities = new List<(float Score, string Text)>();

            

            foreach (var (text, embedding) in outChatData._chunks)
            {
                float score = CosineSimilarity(queryEmbedding, embedding);
                similarities.Add((score, text));
            }

            return similarities
                .OrderByDescending(s => s.Score)
                .Take(topK)
                .Select(s => s.Text)
                .ToList();
        }

        public async Task<List<(string Text, float[] Embedding)>> GetChanks(ChatData outChatData, int chunkSize = 800)
        {
            outChatData._chunks.Clear(); // Очищаем предыдущую базу знаний (если нужно)
            if(outChatData.ContextFromFiles.Length == 0)
            {
                GetContextFileData(outChatData);
            }
            if(outChatData.ContextFromFiles.Length == 0) { return outChatData._chunks; }

            var chunks = TextExtractor.ChunkText(outChatData.ContextFromFiles);
            foreach (var chunk in chunks)
            {
                var embedding = await GetEmbeddingAsync(chunk);

                // Добавляем информацию об источнике в текст чанка
                string chunkWithMeta = $"[{"Документ X"}] {chunk}";
                outChatData._chunks.Add((chunkWithMeta, embedding));
            }
            return outChatData._chunks;
        }

        private float CosineSimilarity(float[] vec1, float[] vec2)
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