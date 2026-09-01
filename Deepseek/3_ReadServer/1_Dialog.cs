using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Deepseek
{
    public class ChatMessage
    {
        public string Role { get; set; } // "user" или "assistant"
        public string Content { get; set; }
    }

    public class RequestModel
    {
        public string UserName { get; set; }
        public string Prompt { get; set; }
        public string Context { get; set; }
        public List<ChatMessage> History { get; set; }
        public string RequestId { get; set; } // уникальный идентификатор запроса (если нужен)
    }

    public class ResponseModel
    {
        public string UserName { get; set; }
        public string RequestId { get; set; }
        public string Response { get; set; }
        public DateTime Timestamp { get; set; }
    }



}
