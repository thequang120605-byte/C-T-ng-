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

        public async Task JoinLobby()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "lobby");
        }

        public async Task LeaveLobby()
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "lobby");
        }

        public async Task RegisterUser(string playerId)
        {
            if (!string.IsNullOrWhiteSpace(playerId))
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{playerId.Trim()}");
            }
        }

        public async Task SendChallenge(string targetPlayerId, string fromPlayerName, string fromPlayerId, string roomCode)
        {
            if (string.IsNullOrWhiteSpace(targetPlayerId) || string.IsNullOrWhiteSpace(roomCode)) return;
            await Clients.Group($"user_{targetPlayerId.Trim()}").SendAsync("ReceiveChallenge", new
            {
                fromPlayerName = fromPlayerName ?? "Đối thủ",
                fromPlayerId = fromPlayerId ?? "",
                roomCode = roomCode,
                sentAt = System.DateTime.UtcNow.ToString("HH:mm:ss")
            });
        }

        public async Task JoinSpectator(string gameId, string viewerName)
        {
            if (string.IsNullOrWhiteSpace(gameId)) return;
            await Groups.AddToGroupAsync(Context.ConnectionId, gameId);
            await Clients.Group(gameId).SendAsync("SpectatorJoined", new
            {
                viewerName = viewerName ?? "Khán giả",
                gameId = gameId
            });
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
