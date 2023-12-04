using Microsoft.AspNetCore.SignalR;
using ChatTheDoc.Server.AIService;
using System.Text.Json;
using System.Formats.Asn1;
using Serilog;
using Serilog.Events;

namespace ChatTheDoc.Server.Hubs
{
    public class ChatHub : Hub
    {
        private readonly AiService _aiService = new();
        public ChatHub(AiService aiService)
        {
            this._aiService = aiService;
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File(aiService.logFile, rollingInterval: RollingInterval.Day, shared: true, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level}] {Message}{NewLine}")
                .CreateLogger();
            Log.Logger.Information($"ChatHub started.");
        }

        public override Task OnConnectedAsync()
        {
            //ConnectedUsers.Add(Context.ConnectionId);            
            Log.Logger.Information($"User {Context.ConnectionId} connected!");
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            //ConnectedUsers.Remove(Context.ConnectionId);
            Log.Logger.Information($"User {Context.ConnectionId} disconnected!");
            if (exception != null)
            {
                Log.Logger.Error($"Exception: {exception}");
            }
            return base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(string user, string message, string topic)
        {
            Log.Logger.Information($"Message sent by user {user}.");
            string assistantResponse = string.Empty;
            _aiService.SubscribeUser(user, async (sender, e) =>
           {
               Log.Logger.Information($" Receieve Update " + e.Data);
               await Clients.Client(Context.ConnectionId).SendAsync("UpdateMessage", e.User, e.Data);
           });

            assistantResponse = _aiService.ChatWithAI(user, message, topic);

            await Clients.Client(Context.ConnectionId).SendAsync("ReceiveMessage", user, true);
            _aiService.UnSubscribeUser(user);
            Log.Logger.Information($"Responded to user {user}.");
        }

        public async Task SetPreferedTopicCategories(string user)
        {
            Log.Logger.Information($"Handle Prefered PreferedTopic Categories Data.");
            var response = _aiService.SetPreferedTopicCategories();

            await Clients.Client(Context.ConnectionId).SendAsync("SetPreferedTopicCategories", user, response);
            _aiService.UnSubscribeUser(user);
        }
    }
}
