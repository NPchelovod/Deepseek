using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
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

        public int Id;

        public string ModelII = "qwen2.5:7b-instruct-q4_K_M";//"gemma2:9b",//"qwen2.5:32b-instruct-q4_K_M",//"deepseek-r1:8b",
        public int SimvolsMax = 140000;
        public bool UseCommonContext = false;// вгружать ли в себя файлы
        public bool IsAdminCheckBox = false;

        public List<string> ConversationHistory = new List<string>(); // История диалога
        public string ContextFromFiles = "";

        public string promptFolder = @"Y:\ИИ\_БазаДанных"; // можно указать начальную папку
        public DateTime Timestamp { get; set; }

        public string inboxPath = @"Y:\ИИ\_Разработчику\_Вопросы";
        public  string outboxPath = @"Y:\ИИ\_Разработчику\_Ответы";
        public  string archivePath = @"Y:\ИИ\_Разработчику\_Архив"; // для обработанных запросов (опционально)

        public  string settingsPath=> Path.Combine(Path.GetTempPath(), "ChatData.json");//уникальная версия для настроек

        public string userName => Environment.UserName;
        public void ChangeId()
        {
            Id = GetId();//смена id для продолжения истории
        }

        public string GetFileName => $"ChatData_{userName}_{Id}.json";

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