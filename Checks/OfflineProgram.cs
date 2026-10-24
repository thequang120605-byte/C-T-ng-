
using ChessCore.Mobile.Offline;
using ChessCore.Mobile.Services;
using ChessCore.Mobile.Models;
using System.Diagnostics;
int count=0;
void Check(bool condition,string name) {if(!condition)throw new Exception(name);Console.WriteLine("PASS "+name);count++;}
var board=new Board();
var fen=board.ToFen();
Check(GameRules.GetAllLegalMoves(board,PieceColor.Red).Count==44,"initial position has 44 legal Red moves");
Check(BoardEvaluator.Evaluate(board,PieceColor.Red)==-BoardEvaluator.Evaluate(board,PieceColor.Black),"evaluation is symmetric");
var engine=new MinimaxEngine(4);var watch=Stopwatch.StartNew();
var best=engine.FindBestMove(board,PieceColor.Red,default,TimeSpan.FromMilliseconds(40));
Check(best!=null&&GameRules.GetAllLegalMoves(board,PieceColor.Red).Any(m=>m.From==best.From&&m.To==best.To),"time-bounded search returns a legal fallback or completed result");
Check(board.ToFen()==fen,"search does not modify original board");
using(var cts=new CancellationTokenSource()){
 cts.Cancel();bool canceled=false;
 try{engine.FindBestMove(board,PieceColor.Red,cts.Token);}catch(OperationCanceledException){canceled=true;}
 Check(canceled,"pre-canceled search is rejected");
}
using(var cts=new CancellationTokenSource(20)){
 bool canceled=false;
 try{new MinimaxEngine(4).FindBestMove(board,PieceColor.Red,cts.Token,TimeSpan.FromSeconds(5));}catch(OperationCanceledException){canceled=true;}
 Check(canceled&&board.ToFen()==fen,"in-flight search cancels without changing caller's board");
}
var local=new OfflineGameService();
var game=await local.CreateAsync();
Check(game.Id<0&&game.BoardPieces.Count==32&&game.CurrentTurn=="Red","offline game starts with 32 pieces and Red to move");
var moves=await local.ValidMovesAsync(game.Id,6,0);
Check(moves.Any(m=>m.Row==5&&m.Col==0),"Red pawn may advance");
bool invalid=false;try{await local.MoveAsync(game.Id,new(){FromX=6,FromY=0,ToX=4,ToY=0});}catch(InvalidOperationException){invalid=true;}
Check(invalid&&(await local.GetAsync(game.Id)).Moves.Count==0,"illegal move does not mutate game");
game=await local.MoveAsync(game.Id,new(){FromX=6,FromY=0,ToX=5,ToY=0});
Check(game.CurrentTurn=="Black"&&game.Moves.Count==1,"player move changes turn to Black");
Check((await local.ValidMovesAsync(game.Id,6,2)).Count==0,"player input is disabled on AI turn");
using(var cts=new CancellationTokenSource()){
 cts.Cancel();bool canceled=false;try{await local.ComputerAsync(game.Id,4,cts.Token);}catch(OperationCanceledException){canceled=true;}
 Check(canceled&&(await local.GetAsync(game.Id)).Moves.Count==1,"canceled service action does not commit an AI move");
}
game=await local.ComputerAsync(game.Id,1);
Check(game.CurrentTurn=="Red"&&game.Moves.Count==2,"local AI responds with a move without HTTP");
var revision=game.Revision;
game=await local.ComputerAsync(game.Id,1);
Check(game.Moves.Count==2&&game.Revision==revision,"retry does not play a second AI move on Red's turn");
var exposed=await local.GetAsync(game.Id);exposed.BoardPieces.Clear();exposed.Moves.Clear();
Check((await local.GetAsync(game.Id)).BoardPieces.Count>0&&(await local.GetAsync(game.Id)).Moves.Count==2,"returned snapshot cannot mutate stored game");
game=await local.ResignAsync(game.Id);
Check(game.Status=="BlackWins"&&!game.IsRated,"practice resignation finishes without changing Elo");
var empty=new Board();for(int row=0;row<10;row++)for(int col=0;col<9;col++)empty.Grid[row,col]=null;
empty.Grid[0,4]=new(PieceType.King,PieceColor.Black);empty.Grid[9,3]=new(PieceType.King,PieceColor.Red);
empty.Grid[1,0]=new(PieceType.Rook,PieceColor.Red);empty.Grid[2,3]=new(PieceType.Rook,PieceColor.Red);empty.Grid[2,5]=new(PieceType.Rook,PieceColor.Red);
empty.SetCurrentTurn(PieceColor.Black);
Check(!MoveGenerator.CheckIfInCheck(empty,PieceColor.Black)&&GameRules.StatusAfterMove(empty)==GameStatus.RedWins,"no legal moves without check is still a loss");
for(int i=0;i<10;i++)await local.CreateAsync();
bool evicted=false;try{await local.GetAsync(game.Id);}catch(InvalidOperationException){evicted=true;}
Check(evicted,"old local sessions are bounded and evicted");
Console.WriteLine($"{count} offline checks passed. Budgeted search smoke time: {watch.ElapsedMilliseconds}ms (not a device benchmark).");
