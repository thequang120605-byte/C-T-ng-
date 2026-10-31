using Microsoft.EntityFrameworkCore;
using ChessCore.Data;
using ChessCore.Services;

using Microsoft.AspNetCore.Authentication.Cookies;

// Keep MVC's view lookup rooted at the ChessCore project when the app is
// launched from the solution/repository root (for example by an IDE task).
var contentRoot = Directory.GetCurrentDirectory();
if (!Directory.Exists(Path.Combine(contentRoot, "Views")))
{
    var projectDir = Path.Combine(contentRoot, "ChessCore");
    if (Directory.Exists(Path.Combine(projectDir, "Views")))
    {
        contentRoot = projectDir;
    }
    else
    {
        // `dotnet ChessCore.dll` can be started from an arbitrary directory.
        var candidate = AppContext.BaseDirectory;
        for (var i = 0; i < 5 && !Directory.Exists(Path.Combine(candidate, "Views")); i++)
        {
            candidate = Directory.GetParent(candidate)?.FullName ?? candidate;
        }

        if (Directory.Exists(Path.Combine(candidate, "Views")))
        {
            contentRoot = candidate;
        }
    }
}

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = contentRoot
});

// Ho tro bind cong khi deploy tren Render / Cloud Run / Railway qua bien moi truong PORT
var envPort = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(envPort))
{
    builder.WebHost.UseUrls($"http://0.0.0.0:{envPort}");
}

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddMemoryCache();
builder.Services.AddSignalR();

// Cấu hình Authentication (Cookie & Google)
var authBuilder = builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
})
.AddCookie(options =>
{
    options.LoginPath = "/Auth/Login";
    options.LogoutPath = "/Auth/Logout";
    options.AccessDeniedPath = "/Auth/AccessDenied";
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromDays(30);

    // FIX BUG_003: Trả về HTTP 403 Forbidden thay vì 302 Redirect khi truy cập /Admin hoặc /api trái phép
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/Admin") || context.Request.Path.StartsWithSegments("/api"))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        }
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
    // Thu hồi phiên Cookie ngay lập tức nếu tài khoản bị khóa
    options.Events.OnValidatePrincipal = async context =>
    {
        var userIdStr = context.Principal?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdStr, out int userId))
        {
            var db = context.HttpContext.RequestServices.GetRequiredService<ChessCore.Data.AppDbContext>();
            bool accountActive = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(
                Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AsNoTracking(
                    System.Linq.Queryable.Where(db.Users, u => u.Id == userId && !u.IsLocked)
                ),
                context.HttpContext.RequestAborted
            );
            if (!accountActive)
            {
                context.RejectPrincipal();
                await Microsoft.AspNetCore.Authentication.AuthenticationHttpContextExtensions.SignOutAsync(
                    context.HttpContext, 
                    Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme
                );
            }
        }
    };
});

var googleClientId = builder.Configuration["Authentication:Google:ClientId"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"];
if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    authBuilder.AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        options.CallbackPath = "/signin-google";
    });
}

// Cấu hình CORS cho phép Frontend truy cập
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Cấu hình Entity Framework Core với MySQL (Pomelo)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
ServerVersion serverVersion;
try
{
    serverVersion = ServerVersion.AutoDetect(connectionString);
}
catch
{
    serverVersion = new MySqlServerVersion(new Version(8, 0, 30));
}

builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseMySql(
        connectionString,
        serverVersion,
        mySqlOptions => mySqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null)
    );
});

// Đăng ký các Services
builder.Services.AddSingleton<ChessCore.Services.ITokenService, ChessCore.Services.TokenService>();
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddSingleton<PuzzleCampaignService>();
builder.Services.AddScoped<StatisticsService>();

builder.Services.AddScoped<IMoveService, MoveService>();
builder.Services.AddScoped<IGameSettlementService, GameSettlementService>();
builder.Services.AddSingleton<ChessCore.Services.AiCapacity>();
builder.Services.AddHostedService<GameTimeoutWorker>();

var app = builder.Build();

// Tu dong cap nhat schema va seed du lieu can thiet khi ung dung khoi dong (Self-Healing Database)
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        await DatabaseBootstrap.InitializeAsync(dbContext, logger);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Loi khi khoi tao va tu phuc hoi database khoi dong.");
    }
}

app.Services.GetRequiredService<PuzzleCampaignService>().Start(
    Path.Combine(app.Environment.ContentRootPath, "Data", "Puzzles"),
    Path.Combine(app.Environment.ContentRootPath, "Data", "PuzzleCache"));

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error/500");
    app.UseHsts();
}

// Bắt các mã lỗi HTTP như 404, 403, 500 và điều hướng sang trang lỗi thân thiện
app.UseStatusCodePagesWithReExecute("/Home/Error/{0}");

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowAll");

// Middleware Bearer Token cho Mobile Client & SignalR
app.Use(async (context, next) =>
{
    string? token = null;
    var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
    if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
    {
        token = authHeader.Substring("Bearer ".Length).Trim();
    }
    else if (context.Request.Query.TryGetValue("access_token", out var queryToken))
    {
        token = queryToken.FirstOrDefault();
    }

    if (!string.IsNullOrEmpty(token))
    {
        var tokenService = context.RequestServices.GetRequiredService<ChessCore.Services.ITokenService>();
        var principal = tokenService.ValidateToken(token);
        if (principal != null)
        {
            var userIdStr = principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdStr, out int userId))
            {
                var db = context.RequestServices.GetRequiredService<ChessCore.Data.AppDbContext>();
                bool accountActive = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AnyAsync(
                    Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.AsNoTracking(
                        System.Linq.Queryable.Where(db.Users, u => u.Id == userId && !u.IsLocked)
                    ),
                    context.RequestAborted
                );
                if (accountActive)
                {
                    context.User = principal;
                }
            }
            else
            {
                context.User = principal;
            }
        }
    }

    await next();
});

app.UseAuthentication();
app.UseAuthorization();

app.UseStaticFiles();

app.MapControllers();
app.MapHub<ChessCore.Hubs.GameHub>("/gameHub");

// Route cho giao diện MVC
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"
);

app.Run();
