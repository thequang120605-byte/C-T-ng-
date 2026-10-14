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

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddMemoryCache();
builder.Services.AddSignalR();

// Cấu hình Authentication (Cookie)
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.LogoutPath = "/Auth/Logout";
        options.AccessDeniedPath = "/Auth/AccessDenied";
        options.Cookie.HttpOnly = true;
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
    });

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
builder.Services.AddDbContext<AppDbContext>(options =>
{
    options.UseMySql(
        connectionString,
        new MySqlServerVersion(new Version(8, 0, 30))
    );
});

// Đăng ký các Services
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddSingleton<PuzzleCampaignService>();

builder.Services.AddScoped<IMoveService, MoveService>();
builder.Services.AddHostedService<GameTimeoutWorker>();

var app = builder.Build();

// Tu dong cap nhat cac bang database con thieu khi ung dung khoi dong
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    try
    {
        dbContext.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Loi khi tu dong ap dung migration co so du lieu.");
    }
}

// Doc dataset Co The va xac minh loi giai (chay nen, co cache)
app.Services.GetRequiredService<PuzzleCampaignService>().Start(
    Path.Combine(app.Environment.ContentRootPath, "Data", "Puzzles"),
    Path.Combine(app.Environment.ContentRootPath, "Data", "PuzzleCache"));

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.UseStaticFiles();

app.MapControllers();
app.MapHub<ChessCore.Hubs.GameHub>("/gameHub");

// Route cho giao diện MVC
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Game}/{action=Index}/{id?}"
);

app.Run();
