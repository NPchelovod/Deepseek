using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Deepseek
{
    public class JsonStorage<T> where T : class
    {
        private readonly string _filePath;
        public JsonStorage(string filePath) => _filePath = filePath;

        public void Save(T data)
        {
            var json = JsonSerializer.Serialize(data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }

        public T Load()
        {
            if (!File.Exists(_filePath)) return null;
            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<T>(json);
        }
    }

}
