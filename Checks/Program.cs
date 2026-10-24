using System.Text.Json;
using ChessCore.Mobile.Models;
int passed = 0;
void Check(bool condition, string name) { if (!condition) throw new Exception(name); Console.WriteLine("PASS " + name); passed++; }
GameResponseDto Parse(string json) { using var d = JsonDocument.Parse(json); return JoinResponseParser.Parse(d.RootElement); }
Check(Parse("{\"game\":{\"id\":42,\"currentTurn\":\"Black\"},\"opponentUsername\":\"Red\"}").Id == 42, "wrapped join response");
Check(Parse("{\"id\":43,\"currentTurn\":\"Red\"}").Id == 43, "direct join response without a second POST");
bool rejected = false; try { Parse("{\"game\":null}"); } catch (InvalidOperationException) { rejected = true; }
Check(rejected, "malformed join response rejected");
var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
var game = JsonSerializer.Deserialize<GameResponseDto>("{\"id\":9,\"redPlayerId\":11,\"blackPlayerId\":22,\"currentTurn\":\"Black\",\"status\":\"InProgress\"}", options)!;
var user = new AuthUserDto { Id = 22, PlayerId = "CT-22", Username = "Black" };
Check(game.BlackPlayerId == "22", "numeric player IDs deserialize without a crash");
Check(GameSnapshotRules.ResolveColor(game, user, "Online", "Red") == "Black", "server identity overrides a route hint");
Check(GameSnapshotRules.ResolveColor(game, new AuthUserDto { Id = 99, Username = "viewer" }, "Online", "Red") == "", "spectator is not silently assigned Red");
Check(GameSnapshotRules.IsMyTurn(game, "Black"), "Black can move on Black turn");
Check(!GameSnapshotRules.IsMyTurn(game, "Red"), "Red cannot move on Black turn");
game.Status = "WaitingForPlayers";
Check(!GameSnapshotRules.IsMyTurn(game, "Black"), "waiting room cannot accept a move");
game.Status = "BlackWins";
Check(!GameSnapshotRules.IsMyTurn(game, "Black"), "finished game cannot accept a move");
var legacy = new GameResponseDto();
Check(GameSnapshotRules.ResolveColor(legacy, user, "Online", "") == "", "missing identity and missing confirmed seat is read only");
Check(GameSnapshotRules.ResolveColor(legacy, user, "Online", "Black") == "Black", "legacy confirmed join flow retains Black seat");
Check(GameSnapshotRules.ResolveColor(legacy, null, "PvE", "") == "Red", "guest PvE uses Red");
Console.WriteLine($"{passed} checks passed.");
