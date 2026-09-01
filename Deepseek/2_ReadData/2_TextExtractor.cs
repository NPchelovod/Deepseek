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
