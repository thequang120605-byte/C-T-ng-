using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ChessCore.Hubs
{
    public class GameHub : Hub
    {
        public async Task JoinGame(string gameId)
        {
            if (string.IsNullOrWhiteSpace(gameId)) return;
            await Groups.AddToGroupAsync(Context.ConnectionId, gameId.Trim());
        }

        public async Task LeaveGame(string gameId)
        {
            if (string.IsNullOrWhiteSpace(gameId)) return;
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, gameId.Trim());
        }

        public async Task JoinLobby()
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "lobby");
        }

        public async Task LeaveLobby()
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, "lobby");
        }

        /// <summary>
        /// Đăng ký nhận thông báo cá nhân (thách đấu, tin nhắn riêng).
        /// Bắt buộc xác thực từ phiên làm việc máy chủ (JWT/Cookie), chống mạo danh.
        /// </summary>
        [Authorize]
        public async Task RegisterUser(string? ignoredPlayerId = null)
        {
            string? playerId = Context.User?.FindFirst("PlayerId")?.Value;
            if (string.IsNullOrWhiteSpace(playerId))
            {
                throw new HubException("Không xác định được danh tính người dùng.");
            }
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{playerId.Trim()}", Context.ConnectionAborted);
        }

        /// <summary>
        /// Gửi lời thách đấu. Danh tính người gửi bắt buộc trích xuất từ phiên đã xác thực.
        /// </summary>
        [Authorize]
        public async Task SendChallenge(string targetPlayerId, string fromPlayerName, string fromPlayerId, string roomCode)
        {
            if (string.IsNullOrWhiteSpace(targetPlayerId) || string.IsNullOrWhiteSpace(roomCode)) return;

            string verifiedSenderName = Context.User?.Identity?.Name ?? fromPlayerName ?? "Đối thủ";
            string verifiedSenderId = Context.User?.FindFirst("PlayerId")?.Value ?? fromPlayerId ?? "";

            await Clients.Group($"user_{targetPlayerId.Trim()}").SendAsync("ReceiveChallenge", new
            {
                fromPlayerName = verifiedSenderName,
                fromPlayerId = verifiedSenderId,
                roomCode = roomCode.Trim().ToUpperInvariant(),
                sentAt = System.DateTime.UtcNow.AddHours(7).ToString("HH:mm:ss")
            });
        }

        public async Task JoinSpectator(string gameId, string viewerName)
        {
            if (string.IsNullOrWhiteSpace(gameId)) return;
            var safeGameId = gameId.Trim();
            await Groups.AddToGroupAsync(Context.ConnectionId, safeGameId);

            string verifiedViewer = Context.User?.Identity?.Name ?? viewerName ?? "Khán giả";
            await Clients.Group(safeGameId).SendAsync("SpectatorJoined", new
            {
                viewerName = verifiedViewer,
                gameId = safeGameId
            });
        }

        public async Task SendChatMessage(string gameId, string sender, string message)
        {
            if (string.IsNullOrWhiteSpace(gameId) || string.IsNullOrWhiteSpace(message)) return;

            string verifiedSender = Context.User?.Identity?.Name ?? sender ?? "Người chơi";
            var safeMessage = message.Length > 200 ? message.Substring(0, 200) : message;

            await Clients.Group(gameId.Trim()).SendAsync("ReceiveChatMessage", new
            {
                gameId = gameId.Trim(),
                sender = verifiedSender,
                message = safeMessage,
                sentAt = System.DateTime.UtcNow.AddHours(7).ToString("HH:mm")
            });
        }
    }
}
