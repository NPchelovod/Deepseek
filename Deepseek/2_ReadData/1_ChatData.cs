using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;
namespace Deepseek
{

    public class ChatData
    {
        public ChatData() 
        {
            ChangeId();
        }
        public ChatData(ChatData chatData)
        {
            ModelII = chatData.ModelII;
            SimvolsMax = chatData.SimvolsMax;
            UseCommonContext = chatData.UseCommonContext;
            IsAdminCheckBox = chatData.IsAdminCheckBox;
            promptFolder = chatData.promptFolder;
            inboxPath = chatData.inboxPath;
            outboxPath = chatData.outboxPath;
            archivePath = chatData.archivePath;
            ChangeId();
        }

        public int Id { get; set; }=0;

        public string ModelII { get; set; } = "qwen2.5:7b-instruct-q4_K_M";//"gemma2:9b",//"qwen2.5:32b-instruct-q4_K_M",//"deepseek-r1:8b",

        public string EmbeddingModel = "nomic-embed-text-v2-moe";// qwen3-embedding:8b";
        public int SimvolsMax { get; set; } = 40000;
        public bool UseCommonContext { get; set; } = false;// вгружать ли в себя файлы
        public bool IsAdminCheckBox { get; set; } = false;

        public List<string> ConversationHistory { get; set; } = new List<string>(); // История диалога
        
        //public string ContextFromFiles { get; set; } = "";

        public string promptFolder { get; set; } = @"Y:\ИИ\_БазаДанных"; // можно указать начальную папку

        public string promptFolderVectors  => Path.Combine(promptFolder, "_Вектора"); // получение папки ветора
        public string nameVectors=> Path.Combine(promptFolderVectors, "Vectors.json");//уникальная версия для настроек
        public DateTime Timestamp { get; set; }

        public string inboxPath { get; set; } = @"Y:\ИИ\_Разработчику\_Вопросы";
        public string outboxPath { get; set; } = @"Y:\ИИ\_Разработчику\_Ответы";
        public string archivePath { get; set; } = @"Y:\ИИ\_Разработчику\_Архив"; // для обработанных запросов (опционально)

        public string OllamaApiUrl { get; set; } = "http://localhost:11434/api/generate";

        public string OllamaApiUrlEmbed { get; set; } = "http://localhost:11434/api/embed";// "http://localhost:11434/api/embeddings";// "http://localhost:11434/api/embed";

        // Хранилище чанков: текст + вектор
        //public List<(string Text, float[] Embedding)> _chunks = new();

        public  string settingsPath=> Path.Combine(Path.GetTempPath(), "ChatData.json");//уникальная версия для настроек

        public string userName => Environment.UserName;
        public void ChangeId()
        {
            Id = GetId();//смена id для продолжения истории
        }
        public string GetFilePrefix => $"ChatData_{userName}";
        public string GetFileName => GetFilePrefix+$"_{Id}.json";

        // Сборка полного пути:
        public string FullFilePathVopros => Path.Combine(inboxPath, GetFileName);
        public string FullFilePathOtvet => Path.Combine(outboxPath, GetFileName);

        public static Random random = new Random();
        public static int GetId()
        {
            int number = random.Next(10000, 99000);
            return number;
        }
    }


}