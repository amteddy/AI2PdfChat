using Python;
using Python.Runtime;

namespace AI2PdfChat
{
    public class ChatService
    {
        private List<ChatMessage> chatHistory = new List<ChatMessage>();
        public event Action OnMessageSent; //event to notify subscribers when new message is sent
        private readonly AIManager aiManager;

        public ChatService(AIManager aiManager)
        {
            this.aiManager = aiManager;
        }

        public List<ChatMessage> GetChatHistory()
        {
            return chatHistory;
        }

        public void SendMessage(string userMessage)
        {
            // AI logic to generate responses based on user input
            string assistantResponse = aiManager.ChatWithAI(userMessage);

            chatHistory.Add(new ChatMessage(userMessage, ChatMessage.ChatMessageType.User));
            chatHistory.Add(new ChatMessage(assistantResponse, ChatMessage.ChatMessageType.Assistant));
            OnMessageSent?.Invoke();
        }
    }
}
