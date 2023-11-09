using Microsoft.AspNetCore.SignalR;
using AIChat.Server.AIService;
using System.Text.Json;
using System.Formats.Asn1;

namespace AIChat.Server.Hubs
{
    public class ChatHub : Hub
    {
        private AiService aiService = new ();

        public ChatHub(AiService aiService)
        {
            //AiService is added as scopped class to instantiate for every user.
            this.aiService = aiService;            
        }

        // private static Dictionary<string, string> ConnectedUsers = new Dictionary<string, string>();
        public override Task OnConnectedAsync()
        {
            //string userName = Context.ConnectionId;
            // ConnectedUsers[userName] = Context.ConnectionId;
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            // string? userName = Context.ConnectionId;
            // ConnectedUsers.Remove(userName);            
            return base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(string user, string message)
        {
            string assistantResponse = aiService.ChatWithAI(message);
            await Clients.Client(Context.ConnectionId).SendAsync("ReceiveMessage", user, assistantResponse);
        }
    }
}
