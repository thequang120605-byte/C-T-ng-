using ChessCore.Engine;
using ChessCore.Models;
using ChessCore.Enums;
using System.Diagnostics;

void Check(bool condition, string message) { if (!condition) throw new Exception(message); Console.WriteLine("PASS " + message); }
Board Empty(PieceColor turn = PieceColor.Red) {
 var b = new Board(); for(int r=0;r<10;r++) for(int c=0;c<9;c++) b.Grid[r,c]=null;
 b.SetCurrentTurn(turn); return b;
}
var board=new Board();
Check(GameRules.GetAllLegalMoves(board,PieceColor.Red).Count==44,"Initial position has 44 legal red moves");
var original=board.ToFen();
var e=new MinimaxEngine(2);var move=e.FindBestMove(board,PieceColor.Red);
Check(move!=null && GameRules.GetAllLegalMoves(board,PieceColor.Red).Any(m=>m.From==move.From && m.To==move.To),"AI selects a legal move");
Check(board.ToFen()==original,"Search restores board and turn");
Check(BoardEvaluator.Evaluate(board,PieceColor.Red)==-BoardEvaluator.Evaluate(board,PieceColor.Black),"Evaluation perspectives are opposites");
var capture=Empty();capture.Grid[9,4]=new(PieceType.King,PieceColor.Red);capture.Grid[0,3]=new(PieceType.King,PieceColor.Black);
capture.Grid[5,0]=new(PieceType.Rook,PieceColor.Red);capture.Grid[5,3]=new(PieceType.Horse,PieceColor.Black);
int netBefore=BoardEvaluator.GetSideScores(capture,PieceColor.Red).H1-BoardEvaluator.GetSideScores(capture,PieceColor.Black).H1;
capture.Grid[5,3]=capture.Grid[5,0];capture.Grid[5,0]=null;
int netAfter=BoardEvaluator.GetSideScores(capture,PieceColor.Red).H1-BoardEvaluator.GetSideScores(capture,PieceColor.Black).H1;
Check(netAfter-netBefore==400,"Capturing a horse adds 400 H1 advantage");
var defended=Empty();defended.Grid[5,0]=new(PieceType.Rook,PieceColor.Red);defended.Grid[5,3]=new(PieceType.Horse,PieceColor.Red);
Check(MoveGenerator.IsSquareAttacked(defended,5,3,PieceColor.Red),"H3 recognizes a friendly defended piece");
var mate=Empty(PieceColor.Black);mate.Grid[0,4]=new(PieceType.King,PieceColor.Black);mate.Grid[9,3]=new(PieceType.King,PieceColor.Red);
mate.Grid[1,4]=new(PieceType.Rook,PieceColor.Red);mate.Grid[1,3]=new(PieceType.Rook,PieceColor.Red);mate.Grid[1,5]=new(PieceType.Rook,PieceColor.Red);
Check(MoveGenerator.CheckIfInCheck(mate,PieceColor.Black) && !GameRules.HasLegalMove(mate,PieceColor.Black),"Checkmate detected");
Check(GameRules.StatusAfterMove(mate)==GameStatus.RedWins,"Checkmate is a loss");
var stale=Empty(PieceColor.Black);stale.Grid[0,4]=new(PieceType.King,PieceColor.Black);stale.Grid[9,3]=new(PieceType.King,PieceColor.Red);
stale.Grid[1,0]=new(PieceType.Rook,PieceColor.Red);stale.Grid[2,3]=new(PieceType.Rook,PieceColor.Red);stale.Grid[2,5]=new(PieceType.Rook,PieceColor.Red);
Check(!MoveGenerator.CheckIfInCheck(stale,PieceColor.Black) && !GameRules.HasLegalMove(stale,PieceColor.Black),"No-legal-move position without check");
Check(GameRules.StatusAfterMove(stale)==GameStatus.RedWins,"No legal moves is a loss, not draw");
Check(new MinimaxEngine(1).EvaluatePosition(stale,PieceColor.Black)<-900000,"AI scores no moves as loss");
var kings=Empty();kings.Grid[0,4]=new(PieceType.King,PieceColor.Black);kings.Grid[9,4]=new(PieceType.King,PieceColor.Red);
Check(MoveGenerator.CheckIfInCheck(kings,PieceColor.Red),"Facing kings detected");
Check(!MoveGenerator.GetValidMoves(kings,new(9,4)).Any(m=>m.To==new Position(0,4)),"King capture excluded");
for(int d=1;d<=4;d++) {var ai=new MinimaxEngine(d); var result=ai.FindBestMove(board,PieceColor.Red);Check(result!=null,$"Depth {d} completed: ms={ai.LastStats.ElapsedMilliseconds:F0}, nodes={ai.LastStats.NodesEvaluated}, cutoffs={ai.LastStats.BetaCutoffs}");}

// Validate the actual response builder used by HTTP and SignalR, without requiring MySQL.
var buildResponse=typeof(ChessCore.Controllers.GamesController).GetMethod("BuildGameResponse",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static)!;
var game = new Game { Id=1, Status=GameStatus.InProgress, CurrentTurn=PieceColor.Black, Moves=new() {
 new Move { FromX=7,FromY=1,ToX=0,ToY=1,PieceType=PieceType.Cannon,PieceColor=PieceColor.Red,MoveOrder=0 }
}};
var afterCapture=GameRules.RebuildBoard(null,game.Moves,game.CurrentTurn);
var response=(ChessCore.DTOs.GameResponseDto)buildResponse.Invoke(null,new object[]{game,afterCapture})!;
Check(response.LastMoveEval?.DeltaH1==400,"HTTP/SignalR DTO retains H1 capture delta");
Check(response.RedScore==-response.BlackScore,"DTO reports opposite perspective scores");
var json=System.Text.Json.JsonSerializer.Serialize(response,new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web));
Check(json.Contains("\"lastMoveEval\"") && json.Contains("\"totalDelta\""),"UI camelCase evaluation contract");
var terminalGame=new Game { Id=2,Status=GameStatus.RedWins,CurrentTurn=PieceColor.Black,InitialFen=stale.ToFen() };
var terminalResponse=(ChessCore.DTOs.GameResponseDto)buildResponse.Invoke(null,new object[]{terminalGame,stale})!;
Check(terminalResponse.HasNoLegalMoves && !terminalResponse.IsCheckmate && terminalResponse.EndReason=="NoLegalMoves","DTO distinguishes no moves from checkmate");
terminalGame.InitialFen=mate.ToFen();
terminalResponse=(ChessCore.DTOs.GameResponseDto)buildResponse.Invoke(null,new object[]{terminalGame,mate})!;
Check(terminalResponse.IsCheckmate && terminalResponse.EndReason=="Checkmate","DTO reports checkmate");
Check(response.Evaluation.H1 + response.Evaluation.H2 + response.Evaluation.H3 + response.Evaluation.H4 == response.RedScore,
    "Current H1 H2 H3 H4 breakdown adds up to the displayed Red advantage");
Check(terminalResponse.InitialFen == terminalGame.InitialFen,
    "DTO preserves initial FEN for accurate captured-piece reconstruction");

var shell = new Board();
int intactSafety = BoardEvaluator.GetSideScores(shell, PieceColor.Red).H3;
shell.Grid[9,3] = null;
Check(intactSafety - BoardEvaluator.GetSideScores(shell, PieceColor.Red).H3 >= 60,
    "Breaking the advisor pair carries a meaningful H3 defence penalty");
var development = new Board();
int undevelopedPosition = BoardEvaluator.GetSideScores(development, PieceColor.Red).H2;
development.Grid[8,0] = development.Grid[9,0]; development.Grid[9,0] = null;
Check(BoardEvaluator.GetSideScores(development, PieceColor.Red).H2 > undevelopedPosition,
    "A developed rook is preferred to a blocked home rook");
var mirror = Empty(PieceColor.Black);
for(int r=0;r<10;r++) for(int c=0;c<9;c++) {
 var p=development.Grid[r,c]; if(p!=null) mirror.Grid[9-r,8-c]=new(p.Type,p.Color==PieceColor.Red?PieceColor.Black:PieceColor.Red);
}
Check(BoardEvaluator.Evaluate(mirror,PieceColor.Red)==-BoardEvaluator.Evaluate(development,PieceColor.Red),
    "Strategic evaluation treats mirrored Red and Black symmetrically");
var bait=Empty(); bait.Grid[9,4]=new(PieceType.King,PieceColor.Red);bait.Grid[0,3]=new(PieceType.King,PieceColor.Black);
bait.Grid[5,0]=new(PieceType.Rook,PieceColor.Red);bait.Grid[5,2]=new(PieceType.Pawn,PieceColor.Black);bait.Grid[5,4]=new(PieceType.Rook,PieceColor.Black);
bait.Grid[7,4]=new(PieceType.Pawn,PieceColor.Red); // Block the enemy rook's check on our king.
Check(GameRules.GetAllLegalMoves(bait,PieceColor.Red).Any(m=>m.From==new Position(5,0)&&m.To==new Position(5,2)),
    "The poisoned pawn capture is legal, so declining it is a search decision");
var cautious=new MinimaxEngine(3);var baitMove=cautious.FindBestMove(bait,PieceColor.Red);
Check(baitMove!=null && !(baitMove.From==new Position(5,0) && baitMove.To==new Position(5,2)),
    "AI does not trade its rook for a defended pawn");
Check(cautious.LastStats.QuiescenceNodes>0 && cautious.LastStats.CompletedDepth==3,
    "Depth 3 completes with quiescence enabled");
Check(e.LastStats.CompletedDepth==2,"Iterative deepening reports the actual completed depth");
var tacticalEngine=new MinimaxEngine(1);
Check(tacticalEngine.EvaluatePosition(mate,PieceColor.Black)<-900000,
    "A checkmate at the tactical horizon remains a loss");
// A deterministic developed position exercises depth 4 beyond the opening.
var middle=new Board();
foreach(var coords in new[]{(7,1,7,4),(0,1,2,2),(9,7,7,6),(2,7,2,4)}) {
 var legal=GameRules.GetAllLegalMoves(middle,middle.CurrentTurn).First(m=>m.From==new Position(coords.Item1,coords.Item2)&&m.To==new Position(coords.Item3,coords.Item4));
 middle.MakeMove(legal);
}
var middleEngine=new MinimaxEngine(4);var middleMove=middleEngine.FindBestMove(middle,middle.CurrentTurn);
Check(middleMove!=null && middleEngine.LastStats.CompletedDepth==4,
    $"Developed position depth 4 completed: ms={middleEngine.LastStats.ElapsedMilliseconds:F0}, nodes={middleEngine.LastStats.NodesEvaluated}, qNodes={middleEngine.LastStats.QuiescenceNodes}");

var forwardElephant=new Board();
int homeShell=BoardEvaluator.GetSideScores(forwardElephant,PieceColor.Red).H3;
forwardElephant.Grid[5,2]=forwardElephant.Grid[9,2];forwardElephant.Grid[9,2]=null;
Check(homeShell>BoardEvaluator.GetSideScores(forwardElephant,PieceColor.Red).H3,
    "An unnecessary early elephant advance weakens the defensive formation");
