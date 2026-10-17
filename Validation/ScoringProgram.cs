using ChessCore.Data;
using ChessCore.Engine;
using ChessCore.Enums;
using ChessCore.Models;
using ChessCore.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text;

int checks = 0;
void Check(bool ok, string name) { if (!ok) throw new Exception(name); Console.WriteLine("PASS " + name); checks++; }
Board Empty() { var b = new Board(); Array.Clear(b.Grid); return b; }
Board Wing(int n) {
    var b = Empty();
    b.Grid[9,4] = new(PieceType.King, PieceColor.Red);
    b.Grid[0,4] = new(PieceType.King, PieceColor.Black);
    b.Grid[5,4] = new(PieceType.Pawn, PieceColor.Red);
    b.Grid[1,0] = new(PieceType.Rook, PieceColor.Red);
    b.Grid[2,2] = new(PieceType.Horse, PieceColor.Red);
    if(n>=3) { b.Grid[0,0] = new(PieceType.Cannon, PieceColor.Red); b.Grid[0,1] = new(PieceType.Pawn, PieceColor.Red); }
    if(n>=4) b.Grid[3,1] = new(PieceType.Horse, PieceColor.Red);
    return b;
}
Check(BoardEvaluator.EvaluateWingCoordination(new Board(), PieceColor.Red) == 0, "Initial H4 zero");
Check(BoardEvaluator.EvaluateWingCoordination(Wing(2), PieceColor.Red) == 0, "Two attacking pieces do not earn H4");
Check(BoardEvaluator.EvaluateWingCoordination(Wing(3), PieceColor.Red) == 60, "Three-piece wing = 60");
Check(BoardEvaluator.EvaluateWingCoordination(Wing(4), PieceColor.Red) == 100, "Four-piece wing = 100, not 160");
var mirrored = Empty(); var wing = Wing(4);
for(int r=0;r<10;r++) for(int c=0;c<9;c++) {
    var p=wing.Grid[r,c]; if(p != null) mirrored.Grid[9-r,8-c] = new(p.Type, GameRules.Opponent(p.Color));
}
Check(BoardEvaluator.GetSideScores(mirrored, PieceColor.Black).H4 == 100, "H4 mirrors for Black");
Check(BoardEvaluator.Evaluate(wing, PieceColor.Red) == -BoardEvaluator.Evaluate(mirrored, PieceColor.Red), "All four heuristics mirror symmetrically");
var quiet=Empty(); quiet.Grid[4,0]=new(PieceType.Rook,PieceColor.Red); quiet.Grid[4,1]=new(PieceType.Horse,PieceColor.Red); quiet.Grid[4,2]=new(PieceType.Cannon,PieceColor.Red);
Check(BoardEvaluator.EvaluateWingCoordination(quiet,PieceColor.Red)==0,"Three grouped majors without palace pressure get zero");
Check(RatingCalculator.RedDelta(1200,1200,GameStatus.RedWins)==16,"Equal ratings win +16");
Check(RatingCalculator.RedDelta(1200,1200,GameStatus.BlackWins)==-16,"Equal ratings loss -16");
Check(RatingCalculator.RedDelta(1200,1200,GameStatus.Draw)==0,"Equal ratings draw zero");
Check(RatingCalculator.RedDelta(1000,1400,GameStatus.RedWins)>16,"Underdog win gains more");
Check(RatingCalculator.RedDelta(1000,1400,GameStatus.Draw)>0,"Underdog draw gains rating");
Check(RatingCalculator.RedDelta(1000,1400,GameStatus.BlackWins)>-16,"Expected underdog loss loses less");

var dbFile = Path.Combine(Path.GetTempPath(), "chess-scoring-" + Guid.NewGuid() + ".db");
var services = new ServiceCollection();
services.AddLogging(); services.AddDbContext<AppDbContext>(o=>o.UseSqlite("Data Source="+dbFile));
services.AddScoped<GameResultService>(); services.AddScoped<StatisticsService>();
await using var provider=services.BuildServiceProvider();
int redId,blackId,onlineId,pveId,aiId,otherId,unratedId,customId;
using(var scope=provider.CreateScope()) {
    var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
    var red=new User{Username="score-red",Email="score-red@example.test",PasswordHash="test",PlayerId="TEST-R",Elo=1200};
    var black=new User{Username="score-black",Email="score-black@example.test",PasswordHash="test",PlayerId="TEST-B",Elo=1200};
    db.Users.AddRange(red,black); await db.SaveChangesAsync(); redId=red.Id;blackId=black.Id;
    Game New(string code,string mode,GameStatus status,bool rated=false) => new() {
        RoomCode=code,GameMode=mode,Status=status,RedPlayerId=red.Id,
        BlackPlayerId=mode==GameModes.Online?black.Id:null,IsRated=rated };
    var online=New("TESTON",GameModes.Online,GameStatus.RedWins,true);
    var pve=New("TESTAI",GameModes.PvE,GameStatus.BlackWins);
    var ai=New("TESTAA",GameModes.AiVsAi,GameStatus.RedWins);
    var other=New("TESTIP",GameModes.Online,GameStatus.InProgress,true);
    var unrated=New("TESTUN",GameModes.Online,GameStatus.Draw);
    var custom=New("TESTCU",GameModes.Online,GameStatus.RedWins,true);custom.InitialFen=new Board().ToFen();
    db.Games.AddRange(online,pve,ai,other,unrated,custom); await db.SaveChangesAsync();
    onlineId=online.Id;pveId=pve.Id;aiId=ai.Id;otherId=other.Id;unratedId=unrated.Id;customId=custom.Id;
}
async Task Settle(int id) { using var s=provider.CreateScope();await s.ServiceProvider.GetRequiredService<GameResultService>().ProcessAsync(id); }
await Settle(onlineId); await Settle(onlineId); await Settle(pveId);await Settle(aiId);await Settle(otherId);await Settle(unratedId);await Settle(customId);
using(var scope=provider.CreateScope()) {
 var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();
 var red=await db.Users.FindAsync(redId); var black=await db.Users.FindAsync(blackId);
 Check(red!.Elo==1216 && black!.Elo==1184,"Only rated standard Online changes Elo, zero-sum");
 Check(await db.UserEloHistories.CountAsync()==2,"Settlement repeated twice creates only one history per player");
 Check(red.GamesPlayed==4,"PvE counts; AiVsAi and unfinished games do not count in user totals");
 Check((await db.Games.FindAsync(otherId))!.ResultsProcessedAt==null,"Unfinished game not settled");
 var stats=scope.ServiceProvider.GetRequiredService<StatisticsService>();
 var rows=await stats.ModesAsync(redId);
 var on=rows.Single(r=>r.Mode==GameModes.Online);var pv=rows.Single(r=>r.Mode==GameModes.PvE);
 Check(on.Finished==3 && on.InProgress==1 && on.Wins==2 && on.Draws==1,"Personal Online aggregates exclude unfinished from denominator");
 Check(pv.Finished==1 && pv.Losses==1,"PvE statistics separated");
 var admin=await stats.ModesAsync(); Check(admin.Single(r=>r.Mode==GameModes.Online).Created==4,"Admin aggregate from database");
 var displayUsers=await db.Users.AsNoTracking().Where(u=>u.Id==redId || u.Id==blackId).ToListAsync();
 await stats.ApplyDisplayCountersAsync(displayUsers);
 Check(displayUsers.Single(u=>u.Id==redId).GamesPlayed==4,"Admin users counters aggregate actual human games");
 var learning=await stats.LearningAsync(redId);Check(learning.PuzzlesSolved==0 && learning.QuizAttempts==0,"Empty personal learning statistics are zero");
 Check(Encoding.UTF8.GetString(StatisticsService.Csv(rows,false)).Contains("Chơi với máy"),"UTF8 CSV export includes mode labels");
}
// Exercise the real MVC quiz action: forged client scores cannot complete a lesson.
using(var scope=provider.CreateScope()) {
    var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var lesson=await db.CourseLessons.Include(l=>l.QuizQuestions)
        .Where(l=>l.ModuleId==1 && l.QuizQuestions.Any()).OrderBy(l=>l.Id).FirstAsync();
    var principal=new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[] {
        new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier,redId.ToString()) },"test"));
    var controller=new ChessCore.Controllers.CourseController(db) {
        ControllerContext=new Microsoft.AspNetCore.Mvc.ControllerContext {
            HttpContext=new Microsoft.AspNetCore.Http.DefaultHttpContext { User=principal } } };
    var wrong=lesson.QuizQuestions.ToDictionary(q=>q.Id,q=>(q.CorrectAnswer+1)%System.Text.Json.JsonSerializer.Deserialize<List<string>>(q.OptionsJson)!.Count);
    var request=new ChessCore.ViewModels.CourseProgressRequest { LessonId=lesson.Id,Answers=wrong,QuizScore=100,IsCompleted=true,QuizAttempts=999 };
    var action=await controller.SaveProgress(request);
    Check(action is Microsoft.AspNetCore.Mvc.OkObjectResult,"Quiz endpoint accepts valid submitted options");
    var progress=await db.UserCourseProgresses.SingleAsync(p=>p.UserId==redId && p.LessonId==lesson.Id);
    Check(progress.BestQuizScore==0 && !progress.IsCompleted,"Forged score=100 and completed=true ignored");
    Check(progress.QuizAttempts==1,"Forged browser attempt count ignored");
    request.Answers=lesson.QuizQuestions.ToDictionary(q=>q.Id,q=>q.CorrectAnswer);
    await controller.SaveProgress(request);
    Check(progress.BestQuizScore==100 && progress.IsCompleted && progress.QuizAttempts==2,"Correct answers graded and completed on server");
    request.Answers=wrong;await controller.SaveProgress(request);
    Check(progress.BestQuizScore==100 && progress.IsCompleted && progress.QuizAttempts==3,"Retake preserves best score and completion");
    Check((await db.Users.FindAsync(redId))!.Elo==1216,"Quiz does not alter Elo");
    request.Answers=new();
    Check(await controller.SaveProgress(request) is Microsoft.AspNetCore.Mvc.BadRequestObjectResult,"Missing answers rejected");
    request.Answers=lesson.QuizQuestions.ToDictionary(q=>q.Id,q=>999);
    Check(await controller.SaveProgress(request) is Microsoft.AspNetCore.Mvc.BadRequestObjectResult,"Out-of-range answers rejected");
    var locked=await db.CourseLessons.Include(l=>l.QuizQuestions)
        .Where(l=>l.ModuleId==2 && l.DisplayOrder>=10 && l.QuizQuestions.Any()).OrderBy(l=>l.DisplayOrder).FirstAsync();
    request.LessonId=locked.Id;request.Answers=locked.QuizQuestions.ToDictionary(q=>q.Id,q=>q.CorrectAnswer);
    Check(await controller.SaveProgress(request) is Microsoft.AspNetCore.Mvc.ForbidResult,"Locked lesson cannot be completed via direct API");
    var stats=scope.ServiceProvider.GetRequiredService<StatisticsService>();
    var learning=await stats.LearningAsync(redId);
    Check(learning.CompletedLessonRecords==1 && learning.QuizAttempts==3 && learning.AverageBestQuizScore==100,"Learning dashboard uses saved quiz results");
}
// Real game API actions, using a no-op SignalR transport.
ChessCore.Controllers.GamesController Controller(IServiceScope scope, int actor) {
    var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var controller=new ChessCore.Controllers.GamesController(new GameService(db),new MoveService(db),new SilentHub(),db,
        new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions()),
        scope.ServiceProvider.GetRequiredService<GameResultService>());
    controller.ControllerContext=new Microsoft.AspNetCore.Mvc.ControllerContext { HttpContext=new Microsoft.AspNetCore.Http.DefaultHttpContext {
        User=new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[] {
            new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier,actor.ToString()) },"test")) } };
    return controller;
}
int invitedId;string invitedCode;
using(var scope=provider.CreateScope()) {
    var result=await Controller(scope,redId).CreateOnlineGame();
    var created=(Microsoft.AspNetCore.Mvc.CreatedAtActionResult)result.Result!;
    var dto=(ChessCore.DTOs.GameResponseDto)created.Value!;
    invitedId=dto.Id;invitedCode=dto.RoomCode;
    Check(dto.GameMode==GameModes.Online && dto.IsRated,"Online creation persists its mode and rating policy");
}
using(var scope=provider.CreateScope()) {
    await Controller(scope,redId).InvitePlayer(invitedId,new ChessCore.Controllers.InvitePlayerRequest{PlayerId="TEST-B"});
    var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var game=await db.Games.AsNoTracking().SingleAsync(g=>g.Id==invitedId);
    Check(game.Status==GameStatus.WaitingForPlayers && game.LastMoveAt==null,"Invitation cannot start a rated game without opponent joining");
}
using(var scope=provider.CreateScope()) {
    var joined=await Controller(scope,blackId).JoinOnlineGame(invitedCode);
    Check(joined.Result is Microsoft.AspNetCore.Mvc.OkObjectResult,"Invited player can accept reserved seat");
    var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();
    Check((await db.Games.AsNoTracking().SingleAsync(g=>g.Id==invitedId)).Status==GameStatus.InProgress,"Joining starts the waiting game");
}
using(var scope=provider.CreateScope()) {
    var api=Controller(scope,blackId);
    var moved=await api.MakeMove(invitedId,new ChessCore.DTOs.MakeMoveRequestDto{FromX=6,FromY=0,ToX=5,ToY=0});
    Check(moved.Result is Microsoft.AspNetCore.Mvc.ForbidResult,"Black player cannot move Red's pawn");
    Check(await api.GetHint(invitedId) is Microsoft.AspNetCore.Mvc.BadRequestObjectResult,"Online hint disabled");
    Check((await api.ComputerMove(invitedId)).Result is Microsoft.AspNetCore.Mvc.BadRequestObjectResult,"Online cannot delegate a move to AI");
}
using(var scope=provider.CreateScope()) {
    var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var game=await db.Games.SingleAsync(g=>g.Id==invitedId);game.Status=GameStatus.Draw;await db.SaveChangesAsync();
}
using(var scope=provider.CreateScope()) {
    await Controller(scope,blackId).JoinOnlineGame(invitedCode);
    var db=scope.ServiceProvider.GetRequiredService<AppDbContext>();
    Check((await db.Games.AsNoTracking().SingleAsync(g=>g.Id==invitedId)).Status==GameStatus.Draw,"Rejoin never revives a completed game");
}
File.Delete(dbFile);
Console.WriteLine($"SCORING CHECKS PASSED: {checks}");

sealed class SilentProxy : Microsoft.AspNetCore.SignalR.IClientProxy {
 public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken=default)=>Task.CompletedTask;
}
sealed class SilentClients : Microsoft.AspNetCore.SignalR.IHubClients {
 readonly SilentProxy p=new();
 public Microsoft.AspNetCore.SignalR.IClientProxy All=>p;
 public Microsoft.AspNetCore.SignalR.IClientProxy AllExcept(IReadOnlyList<string> ids)=>p;
 public Microsoft.AspNetCore.SignalR.IClientProxy Client(string id)=>p;
 public Microsoft.AspNetCore.SignalR.IClientProxy Clients(IReadOnlyList<string> ids)=>p;
 public Microsoft.AspNetCore.SignalR.IClientProxy Group(string id)=>p;
 public Microsoft.AspNetCore.SignalR.IClientProxy Groups(IReadOnlyList<string> ids)=>p;
 public Microsoft.AspNetCore.SignalR.IClientProxy GroupExcept(string id,IReadOnlyList<string> excluded)=>p;
 public Microsoft.AspNetCore.SignalR.IClientProxy User(string id)=>p;
 public Microsoft.AspNetCore.SignalR.IClientProxy Users(IReadOnlyList<string> ids)=>p;
}
sealed class SilentGroups : Microsoft.AspNetCore.SignalR.IGroupManager {
 public Task AddToGroupAsync(string connectionId,string groupName,CancellationToken ct=default)=>Task.CompletedTask;
 public Task RemoveFromGroupAsync(string connectionId,string groupName,CancellationToken ct=default)=>Task.CompletedTask;
}
sealed class SilentHub : Microsoft.AspNetCore.SignalR.IHubContext<ChessCore.Hubs.GameHub> {
 public Microsoft.AspNetCore.SignalR.IHubClients Clients {get;}=new SilentClients();
 public Microsoft.AspNetCore.SignalR.IGroupManager Groups {get;}=new SilentGroups();
}
