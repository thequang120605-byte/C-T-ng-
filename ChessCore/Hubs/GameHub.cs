using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace ChessCore.Hubs
{
    public class GameHub : Hub
    {
        public async Task JoinGame(string gameId)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
        }

        public async Task LeaveGame(string gameId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, gameId);
        }

        public async Task SendChatMessage(string gameId, string sender, string message)
        {
            if (string.IsNullOrWhiteSpace(gameId) || string.IsNullOrWhiteSpace(message)) return;
            // Giới hạn độ dài tin nhắn
            var safeMessage = message.Length > 200 ? message.Substring(0, 200) : message;
            await Clients.Group(gameId).SendAsync("ReceiveChatMessage", new
            {
                gameId = gameId,
                sender = sender ?? "Người chơi",
                message = safeMessage,
                sentAt = System.DateTime.UtcNow.ToString("HH:mm")
            });
        }
    }
}
