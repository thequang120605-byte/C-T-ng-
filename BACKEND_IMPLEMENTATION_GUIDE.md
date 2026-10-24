# HƯỚNG DẪN TRIỂN KHAI BACKEND ASP.NET CORE CHO MOBILE & FLUTTER

Tài liệu này cung cấp mã nguồn mẫu chuẩn hóa phía server ASP.NET Core để đáp ứng đồng thời cả **Web MVC/Razor (Cookie)** và **Mobile/Flutter (JWT Bearer Token + SignalR)**.

---

## 1. Cấu hình Kép (Dual Authentication Scheme) trong `Program.cs`

Tích hợp xác thực thông minh ("SmartAuth") tự động điều hướng: Request có header `Authorization: Bearer <token>` sẽ được giải mã bằng JWT, các request trình duyệt thông thường tiếp tục dùng Cookie.

```csharp
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var jwtKey = builder.Configuration["Jwt:Key"] ?? "SecretKeyCoTuongXiangqiEngine2026SuperSecure!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "ChessCoreApi";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "ChessCoreMobile";

// 1. Đăng ký Dual Authentication Scheme
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "SmartAuth";
    options.DefaultChallengeScheme = "SmartAuth";
})
.AddPolicyScheme("SmartAuth", "JWT or Cookie", options =>
{
    options.ForwardDefaultSelector = context =>
    {
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return JwtBearerDefaults.AuthenticationScheme;
        }
        return CookieAuthenticationDefaults.AuthenticationScheme;
    };
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
        ClockSkew = TimeSpan.FromSeconds(30)
    };

    // Hỗ trợ truyền Token qua QueryString cho SignalR WebSocket
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/gameHub"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
})
.AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
});

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddSignalR();
```

---

## 2. API Xác thực `AuthController.cs` cho Mobile / Flutter

```csharp
[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly ITokenService _tokenService;

    public AuthController(IAuthService authService, ITokenService tokenService)
    {
        _authService = authService;
        _tokenService = tokenService;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
    {
        var user = await _authService.ValidateUserAsync(request.Username, request.Password);
        if (user == null)
            return Unauthorized(new { message = "Sai tên đăng nhập hoặc mật khẩu." });

        var token = _tokenService.GenerateAccessToken(user);
        var refreshToken = await _tokenService.GenerateAndSaveRefreshTokenAsync(user.Id);

        return Ok(new
        {
            token,
            refreshToken,
            user = new
            {
                id = user.Id,
                username = user.Username,
                email = user.Email,
                playerId = $"CT-{user.Id}",
                role = user.Role,
                elo = user.Elo,
                wins = user.Wins,
                losses = user.Losses,
                draws = user.Draws
            }
        });
    }

    [HttpPost("refresh-token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto request)
    {
        var result = await _tokenService.RefreshAsync(request.RefreshToken);
        if (result == null)
            return Unauthorized(new { message = "Refresh token không hợp lệ hoặc đã hết hạn." });

        return Ok(new { token = result.AccessToken, refreshToken = result.RefreshToken });
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();

        var user = await _authService.GetUserByIdAsync(int.Parse(userId));
        return Ok(user);
    }
}
```

---

## 3. SignalR Hub Chuẩn Hóa Danh Tính: `GameHub.cs`

Trích xuất trực tiếp `UserId` từ Claims của JWT token thay vì để client tự truyền tên/màu:

```csharp
[Authorize]
public class GameHub : Hub
{
    private readonly IGameService _gameService;

    public GameHub(IGameService gameService) => _gameService = gameService;

    public async Task JoinGame(string gameId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"game_{gameId}");
        var currentUserId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        // Ghi nhận spectator hoặc player...
    }

    public async Task LeaveGame(string gameId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"game_{gameId}");
    }

    // Server broadcast nước đi và trạng thái kèm Revision tăng dần
    public async Task BroadcastMove(int gameId, GameResponseDto snapshot)
    {
        await Clients.Group($"game_{gameId}").SendAsync("GameUpdated", snapshot);
    }
}
```

---

## 4. Bổ sung `Revision` đơn điệu trong `GameResponseDto.cs`

Mỗi khi bàn cờ thay đổi (nước đi mới, đầu hàng, hết giờ, undo), trường `Revision` bắt buộc phải tăng lên 1 đơn vị:

```csharp
public class GameResponseDto
{
    public int Id { get; set; }
    public long Revision { get; set; } // Số nguyên tăng đơn điệu: 1, 2, 3...
    public string Status { get; set; } // InProgress, RedWins, BlackWins, Draw
    public string CurrentTurn { get; set; } // Red, Black
    public int? RedUserId { get; set; }
    public int? BlackUserId { get; set; }
    public string? RedPlayerUsername { get; set; }
    public string? BlackPlayerUsername { get; set; }
    public List<PieceDto> BoardPieces { get; set; } = new();
    public List<MoveDto> Moves { get; set; } = new();
    public EvaluationDto? Evaluation { get; set; }
}
```
