using System.ComponentModel;

namespace ChatTheDoc.Client
{
    public class User
    {
        public string UserId { get; set; }
        public string StatusMessage { get; set; }
        public string PreferedTopic { get; set; }
        private readonly List<ChatMessage> ChatHistory;
        private static string pdfLocation = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", "sample_pdfs" , "xx");

        public User(string userId, string topic)
        {
            UserId = userId;
            PreferedTopic = topic;
            StatusMessage = string.Empty;
            ChatHistory = new List<ChatMessage>();            
        }           
       
        public void AddMessage(string message, ChatMessage.ChatMessageType type)
        {
            ChatHistory.Add(new ChatMessage(message, type));
            StatusMessage = string.Empty;
        }

        public void UpdateStatus(string message)
        {
            StatusMessage += message;
        }

        public List<ChatMessage> GetUserChatHistory()
        {
            return ChatHistory;
        }
    }
}
