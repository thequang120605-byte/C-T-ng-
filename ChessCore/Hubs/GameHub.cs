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

        // We can optionally use this to broadcast moves, but if we handle API calls, 
        // the API controller can just use IHubContext<GameHub> to notify other players.
    }
}
