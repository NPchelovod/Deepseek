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
        AI_Chat=1,
        AI_Prompt=2,
        AI_Prompt_And_User_Questions,
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
                case ESenders.AI_Prompt_And_User_Questions:
                case ESenders.AI_Prompt:
                    answer = "Вы #" + Id + ":"+$"\n{PromptQuestion}" + "\nAI #" + Id + ":" + answer;
                    break;
                    case ESenders.Errors:
                    answer = "Errors #" + Id + ":" + (string.IsNullOrEmpty(Text)?" Ошибок нет" :Text);
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
        public double WordVoprosMax { get => SimvolsVoprosMax / _simvolsToWord; set => SimvolsVoprosMax =(int)( value * _simvolsToWord); }

        public int SimvolsContextMax { get; set; } = 900*4*6;
        public double WordContextMax { get => SimvolsContextMax / _simvolsToWord; set => SimvolsContextMax = (int)(value * _simvolsToWord); }

        public const double _simvolsToWord = 6.0;
        public const double _wordToToken = 1.2;
        public int AllTokensWords => (int)((WordVoprosMax + WordContextMax) * _wordToToken);//всего токенов сколько может быть

        public bool UseCommonContext { get; set; } = false;// вгружать ли в себя файлы

        public bool OnlyUseCommonContext { get; set; } = false;//не использовать ИИ-чат

        public bool UseHistoryVopros { get; set; } =true;//использовать историю вопросво


        public bool UseOnlyRelevantHistoryInVopros { get; set; } = true;//использовать историю вопросво
        public bool UseOnlyYourQuestionInHistory { get; set; }=true;
        public bool IsAdminCheckBox { get; set; } = false;

        public List<ChatElement> ConversationHistory { get; set; } = new List<ChatElement>(); // История диалога
       
        public ChatElement AnswerPromptVector = null; // ответ промежуточной ИИ на вопрос

        public ChatElement AnswerAndQuestionsPromptVector = null;// ответ промежуточной ИИ на вопрос + ответ ИИ чата и весь контекст одним словом
        
        public ChatElement _errors { get; set; } = null;
        public ChatElement Errors
        {
            get
            {
                if (_errors == null)
                {
                    _errors = new ChatElement() { Id = Id, Senders = ESenders.Errors, StartTime = DateTime.Now };
                 }
                return _errors;
            }
            set =>_errors = value;
        }

        //public ChatElement 

        //public string ContextFromFiles { get; set; } = "";

        public string promptFolder { get; set; } = @"Y:\ИИ\_БазаДанных"; // можно указать начальную папку

        public string promptFolderVectors  => Path.Combine(promptFolder, "_Вектора"); // получение папки ветора
                                                                                      // Безопасное имя модели для использования в именах файлов
        private string SafeEmbeddingModel => EmbeddingModel.Replace(':', '_');
        public string nameVectors => Path.Combine(
    promptFolderVectors,$"{SafeEmbeddingModel}_W{_chunkWordSize}S{topK}_Vector.json");//уникальная версия для настроек
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
        public int topK { get; set; } = 4;//выборка сообщений

        public int LastMessageInQuestion { get; set; } = 1;//сколько последних сообщений в контекст вводить
        public void Clear()
        {
            ConversationHistory.Clear();
            Errors = null;
            AnswerPromptVector = null;
            AnswerAndQuestionsPromptVector = null;
        }
    }


}