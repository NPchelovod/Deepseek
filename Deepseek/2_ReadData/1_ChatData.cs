using DocumentFormat.OpenXml.Spreadsheet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Net.WebRequestMethods;
using static System.Runtime.InteropServices.JavaScript.JSType;
namespace Deepseek
{
    public enum ESenders
    {
        User=0,
        AI_Chat,
        AI_Prompt,
        Errors
    }
    public class ChatElement
    {
        
        public ESenders Senders { get; set; } = ESenders.User;
        public int Id { get; set; } = 0;//id вопроса
        public DateTime StartTime { get; set; } = DateTime.Now;
        public DateTime EndTime { get; set; } = DateTime.Now;

        public string Text { get; set; } = "";
        public string PromptQuestion { get; set; } = "";//вопрос на который ИИ ответила текстом релевантным
        public string GetAnswerText()
        {
            string answer = $"\n{Text}";
            switch (Senders)
            {
                case ESenders.User:
                    answer = "Вы #" + Id + ":" + answer;
                    break;
                case ESenders.AI_Chat:
                    answer = "AI #" + Id + ":" + answer;
                    break;
                 case ESenders.AI_Prompt:
                    answer = "Вы #" + Id + ":"+$"\n{PromptQuestion}" + "\nAI #" + Id + ":" + answer;
                    break;
                default:
                    break;
            }
            return answer;
            //Senders == ESenders.User ? "Вы #" : "AI #") +Id + ":" + $"\n{Text}";
        }
       

        public string GetTime =>   $"AI Time {(int)(EndTime - StartTime).TotalSeconds} сек";

        public float[] Embedding { get; set; } = null; // можно заполнять при добавлении сообщения, чтобы определять косинусово сходство
    }

    public class ChatData
    {
        public ChatData() 
        {
            ChangeId();
        }
        public ChatData(ChatData chatData)
        {
            ModelII = chatData.ModelII;
            SimvolsVoprosMax = chatData.SimvolsVoprosMax;
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

        public string EmbeddingModel = "qwen3-embedding:8b";// "nomic-embed-text-v2-moe";// qwen3-embedding:8b";
        public int SimvolsVoprosMax { get; set; } = 4000;
        public int WordVoprosMax { get => SimvolsVoprosMax / 6; set => SimvolsVoprosMax = value * 6; }
        public bool UseCommonContext { get; set; } = false;// вгружать ли в себя файлы

        public bool OnlyUseCommonContext { get; set; } = false;//не использовать ИИ-чат

        public bool IsAdminCheckBox { get; set; } = false;

        public List<ChatElement> ConversationHistory { get; set; } = new List<ChatElement>(); // История диалога

        public ChatElement AnswerPromptVector { get; set; } =null;// ответ промежуточной ИИ на вопрос

        public ChatElement Errors { get; set; } = null;
        //public ChatElement 

        //public string ContextFromFiles { get; set; } = "";

        public string promptFolder { get; set; } = @"Y:\ИИ\_БазаДанных"; // можно указать начальную папку

        public string promptFolderVectors  => Path.Combine(promptFolder, "_Вектора"); // получение папки ветора
        public string nameVectors => Path.Combine(promptFolderVectors, $"{EmbeddingModel}_D{_chunkWordSize}x{topK}_Vectors.json");//уникальная версия для настроек
        public DateTime Timestamp { get; set; }

        public string inboxPath { get; set; } = @"Y:\ИИ\_Разработчику\_Вопросы";
        public string outboxPath { get; set; } = @"Y:\ИИ\_Разработчику\_Ответы";
        public string archivePath { get; set; } = @"Y:\ИИ\_Разработчику\_Архив"; // для обработанных запросов (опционально)

        public string OllamaApiUrl { get; set; } = "http://localhost:11434/api/generate";

        public string OllamaApiUrlEmbed { get; set; } = "http://localhost:11434/api/embed";// "http://localhost:11434/api/embeddings";// "http://localhost:11434/api/embed";

        // Хранилище чанков: текст + вектор
        //public List<(string Text, float[] Embedding)> _chunks = new();

        public  string settingsPath=> Path.Combine(Path.GetTempPath(), "ChatData_"+userName+".json");//уникальная версия для настроек

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


        public int _chunkWordSize { get; set; } = 900;//слов в одном текстве Средние чанки: 512–1024 токена (~400–800 слов).
        public int topK { get; set; } = 5;//выборка сообщений

        public int LastMessageInQuestion { get; set; } = 1;//сколько последних сообщений в контекст вводить
        public void Clear()
        {
            ConversationHistory.Clear();
            Errors = null;
            AnswerPromptVector = null;
        }
    }


}