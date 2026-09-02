using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UglyToad.PdfPig;
namespace Deepseek

{
    public static class TextExtractor
    {
        /// <summary>
        /// Разбивает очищенный текст на чанки с учётом абзацев и предложений.
        /// </summary>
        /// <param name="text">Исходный текст (уже очищенный).</param>
        /// <param name="maxChunkSize">Максимальный размер чанка в символах.</param>
        /// <param name="overlapSize">Размер перекрытия между чанками в символах.</param>
        /// <returns>Список чанков.</returns>
        public static List<string> ChunkText(string text, int maxChunkSize = 800, int overlapSize = 100)
        {
            var chunks = new List<string>();
            if (string.IsNullOrWhiteSpace(text))
                return chunks;

            // 1. Разбиваем текст на абзацы по переводам строк
            var paragraphs = text.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.RemoveEmptyEntries)
                                 .Select(p => p.Trim())
                                 .Where(p => p.Length > 0)
                                 .ToList();

            var currentChunk = new StringBuilder();

            foreach (var paragraph in paragraphs)
            {
                // Если добавление абзаца не превышает maxChunkSize, добавляем целиком
                if (currentChunk.Length + paragraph.Length + 1 <= maxChunkSize)
                {
                    if (currentChunk.Length > 0)
                        currentChunk.Append(" ");
                    currentChunk.Append(paragraph);
                }
                else
                {
                    // Если абзац сам по себе больше maxChunkSize, разбиваем по предложениям
                    if (currentChunk.Length > 0)
                    {
                        chunks.Add(currentChunk.ToString().Trim());
                        // Добавляем перекрытие из последних символов предыдущего чанка
                        currentChunk.Clear();
                        if (overlapSize > 0 && chunks.Count > 0)
                        {
                            string lastChunk = chunks[^1];
                            int start = Math.Max(0, lastChunk.Length - overlapSize);
                            currentChunk.Append(lastChunk.Substring(start));
                        }
                    }

                    // Разбиваем длинный абзац на предложения
                    var sentences = Regex.Split(paragraph, @"(?<=[.!?])\s+")
                                         .Select(s => s.Trim())
                                         .Where(s => s.Length > 0)
                                         .ToList();

                    foreach (var sentence in sentences)
                    {
                        if (currentChunk.Length + sentence.Length + 1 <= maxChunkSize)
                        {
                            if (currentChunk.Length > 0)
                                currentChunk.Append(" ");
                            currentChunk.Append(sentence);
                        }
                        else
                        {
                            // Если предложение само длиннее maxChunkSize, режем по словам
                            if (sentence.Length > maxChunkSize)
                            {
                                if (currentChunk.Length > 0)
                                {
                                    chunks.Add(currentChunk.ToString().Trim());
                                    currentChunk.Clear();
                                    if (overlapSize > 0 && chunks.Count > 0)
                                    {
                                        string lastChunk = chunks[^1];
                                        int start = Math.Max(0, lastChunk.Length - overlapSize);
                                        currentChunk.Append(lastChunk.Substring(start));
                                    }
                                }

                                // Жёсткое разбиение длинного предложения по словам
                                var words = sentence.Split(' ');
                                foreach (var word in words)
                                {
                                    if (currentChunk.Length + word.Length + 1 > maxChunkSize)
                                    {
                                        chunks.Add(currentChunk.ToString().Trim());
                                        currentChunk.Clear();
                                        if (overlapSize > 0 && chunks.Count > 0)
                                        {
                                            string lastChunk = chunks[^1];
                                            int start = Math.Max(0, lastChunk.Length - overlapSize);
                                            currentChunk.Append(lastChunk.Substring(start));
                                        }
                                    }
                                    currentChunk.Append(word + " ");
                                }
                            }
                            else
                            {
                                // Начинаем новый чанк с этим предложением
                                chunks.Add(currentChunk.ToString().Trim());
                                currentChunk.Clear();
                                if (overlapSize > 0 && chunks.Count > 0)
                                {
                                    string lastChunk = chunks[^1];
                                    int start = Math.Max(0, lastChunk.Length - overlapSize);
                                    currentChunk.Append(lastChunk.Substring(start));
                                }
                                currentChunk.Append(sentence);
                            }
                        }
                    }
                }
            }

            if (currentChunk.Length > 0)
                chunks.Add(currentChunk.ToString().Trim());

            return chunks;
        }
        /// <summary>
        /// Рекурсивно обходит указанную папку и извлекает текст из всех найденных .txt, .pdf, .docx файлов.
        /// </summary>
        /// <param name="directoryPath">Корневая папка для поиска.</param>
        /// <returns>Список объектов с именем файла и его текстовым содержимым.</returns>
        public static List<FileText> ExtractAllTextFromDirectory(string directoryPath)
        {
            var result = new List<FileText>();
            if (!Directory.Exists(directoryPath))
            {
                Console.WriteLine($"Папка не найдена: {directoryPath}");
                return result;
            }

            string[] extensions = { ".txt", ".pdf", ".docx" };
            var files = Directory.EnumerateFiles(directoryPath, "*.*", SearchOption.AllDirectories)
                                 .Where(f => extensions.Contains(Path.GetExtension(f).ToLower()));

            foreach (var file in files)
            {
                try
                {
                    string rawText = ExtractTextFromFile(file);
                    string cleanedText = CleanText(rawText);   // <- очистка
                    result.Add(new FileText { FileName = file, Text = cleanedText });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при обработке файла {file}: {ex.Message}");
                    result.Add(new FileText { FileName = file, Text = string.Empty });
                }
            }

            return result;
        }
        /// <summary>
        /// Очищает текст от мусора: пустых строк, строк-разделителей,
        /// множественных пробелов, повторяющихся символов и т.п.
        /// </summary>
        private static string CleanText(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            var lines = input.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var cleanedLines = new List<string>();

            foreach (var line in lines)
            {
                string trimmed = line.Trim();

                if (string.IsNullOrWhiteSpace(trimmed))
                    continue;

                // Пропускаем строки, состоящие только из мусорных символов (точки, дефисы, подчёркивания, звёздочки)
                if (Regex.IsMatch(trimmed, @"^[.*_-]{3,}$"))
                    continue;

                // Заменяем длинные последовательности этих символов на пробел
                trimmed = Regex.Replace(trimmed, @"[.*_-]{3,}", " ");

                // Схлопываем множественные пробелы
                trimmed = Regex.Replace(trimmed, @"\s+", " ").Trim();

                if (!string.IsNullOrWhiteSpace(trimmed))
                    cleanedLines.Add(trimmed);
            }

            return string.Join(Environment.NewLine, cleanedLines);
        }

        public static string GetString(List<FileText> extracted)
        {
            var allText = new StringBuilder();
            foreach (var item in extracted)
            {
                allText.AppendLine($"=== Файл: {item.FileName} ===");
                allText.AppendLine(item.Text);
                allText.AppendLine();
            }
            return allText.ToString();
        }
        /// <summary>
        /// Определяет тип файла по расширению и вызывает соответствующий метод извлечения текста.
        /// </summary>
        public static string ExtractTextFromFile(string filePath)
        {
            string extension = Path.GetExtension(filePath).ToLower();
            switch (extension)
            {
                case ".txt":
                    return ExtractTextFromTxt(filePath);
                case ".pdf":
                    return ExtractTextFromPdf(filePath);
                case ".docx":
                    return ExtractTextFromDocx(filePath);
                // Можно добавить .doc (старый формат) при необходимости, но это сложнее
                default:
                    throw new NotSupportedException($"Формат {extension} не поддерживается.");
            }
        }

        // ---------- Извлечение из TXT ----------
        private static string ExtractTextFromTxt(string filePath)
        {
            return File.ReadAllText(filePath, Encoding.UTF8); // или Encoding.Default, если кодировка отличается
        }

        // ---------- Извлечение из PDF ----------
        private static string ExtractTextFromPdf(string filePath)
        {
            var sb = new StringBuilder();
            using (var pdf = PdfDocument.Open(filePath))
            {
                foreach (var page in pdf.GetPages())
                {
                    sb.AppendLine(page.Text);
                }
            }
            return sb.ToString();
        }

        // ---------- Извлечение из DOCX ----------
        private static string ExtractTextFromDocx(string filePath)
        {
            var sb = new StringBuilder();
            using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(filePath, false))
            {
                var body = wordDoc.MainDocumentPart?.Document.Body;
                if (body != null)
                {
                    foreach (var paragraph in body.Elements<Paragraph>())
                    {
                        sb.AppendLine(paragraph.InnerText);
                    }
                }
            }
            return sb.ToString();
        }
    }

    /// <summary>
    /// Вспомогательный класс для хранения имени файла и текста.
    /// </summary>
    public class FileText
    {
        public string FileName { get; set; }
        public string Text { get; set; }
    }


}
