using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

internal class OllamaDeepSeekClient
{
    private readonly HttpClient _httpClient;
    private const string OllamaApiUrl = "http://localhost:11434/api/generate";

    public OllamaDeepSeekClient()
    {
        _httpClient = new HttpClient();
    }

    private async Task<string> GenerateTextAsync(string prompt)
    {
        var requestData = new
        {
            model = "deepseek-r1:1.5b",
            prompt = prompt,
            temperature = 0.7,
            max_tokens = 150,
            stream = false
        };

        var json = JsonSerializer.Serialize(requestData);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(OllamaApiUrl, content);
        response.EnsureSuccessStatusCode();

        var responseString = await response.Content.ReadAsStringAsync();
        using JsonDocument document = JsonDocument.Parse(responseString);
        JsonElement root = document.RootElement;

        return root.TryGetProperty("response", out JsonElement responseProperty)
            ? responseProperty.GetString()
            : "No response generated";
    }

    // Альтернативный метод с потоковой обработкой
    public async Task<string> GenerateTextStreamAsync(string prompt)
    {
        var fullResponse = new StringBuilder();

        try
        {
            var requestData = new
            {
                model = "deepseek-r1:1.5b",
                prompt = prompt,
                temperature = 0.7,
                max_tokens = 150,
                stream = true // Включаем потоковый режим
            };

            var json = JsonSerializer.Serialize(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(OllamaApiUrl, content);
            response.EnsureSuccessStatusCode();

            using (var streamReader = new StreamReader(await response.Content.ReadAsStreamAsync()))
            {
                string line;
                while ((line = await streamReader.ReadLineAsync()) != null)
                {
                    if (!string.IsNullOrEmpty(line))
                    {
                        try
                        {
                            using JsonDocument document = JsonDocument.Parse(line);
                            JsonElement root = document.RootElement;

                            if (root.TryGetProperty("response", out JsonElement responseProperty))
                            {
                                var token = responseProperty.GetString();
                                fullResponse.Append(token);
                                Console.Write(token); // Вывод по токенам
                            }

                            if (root.TryGetProperty("done", out JsonElement doneProperty) &&
                                doneProperty.GetBoolean())
                            {
                                break;
                            }
                        }
                        catch (JsonException)
                        {
                            // Пропускаем невалидные JSON строки
                            continue;
                        }
                    }
                }
            }

            return fullResponse.ToString();
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }
}

internal class Program
{
    static async Task Main(string[] args)
    {
        var client = new OllamaDeepSeekClient();
        string prompt = " из трех яблок я съел два сколько станет";

        Console.WriteLine("Generating response...");

        // Попробуйте оба метода:
        // string result = await client.GenerateTextAsync(prompt);
        string result = await client.GenerateTextStreamAsync(prompt);

        Console.WriteLine("\n\nGenerated text:");
        Console.WriteLine(result);
    }
}