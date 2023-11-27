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
        private static readonly AiService aiService = new ();
        
        public ChatHub(AiService aiService)
        {            
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File(aiService.logFile, rollingInterval: RollingInterval.Day, shared: true, outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level}] {Message}{NewLine}")
                .CreateLogger();
            Log.Logger.Information($"AiService started.");
        }

        public override Task OnConnectedAsync()
        {
            //string userName = Context.ConnectionId;
            // ConnectedUsers[userName] = Context.ConnectionId;
            Log.Logger.Information($"User {Context.ConnectionId} connected!");
            return base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {             
            Log.Logger.Information($"User {Context.ConnectionId} disconnected!");
            if(exception != null )
            {
                Log.Logger.Error($"Exception: {exception}");
            }
            return base.OnDisconnectedAsync(exception);
        }

        public async Task SendMessage(string user, string message)
        {
            Log.Logger.Information($"Message sent by user {user}.");
            aiService.StatusUpdate += (sender, e) =>
            {
                Log.Logger.Error($" Receieve Update " + e);
            };
            

            string assistantResponse = aiService.ChatWithAI(message);
            await Clients.Client(Context.ConnectionId).SendAsync("ReceiveMessage", user, assistantResponse);
            Log.Logger.Information($"Responded to user {user}.");
        }
    }
}
