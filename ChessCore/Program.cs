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
builder.Services.AddScoped<IGameService, GameService>();
builder.Services.AddSingleton<PuzzleCampaignService>();
builder.Services.AddScoped<StatisticsService>();

builder.Services.AddScoped<IMoveService, MoveService>();
builder.Services.AddHostedService<GameTimeoutWorker>();

var app = builder.Build();

// Tu dong cap nhat cac bang database con thieu khi ung dung khoi dong
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        dbContext.Database.Migrate();

        // Seed default Admin account if not exists
        var adminUser = dbContext.Users.FirstOrDefault(u => u.Username == "admin");
        if (adminUser == null)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hashBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes("Admin@123"));
            var passwordHash = Convert.ToBase64String(hashBytes);

            adminUser = new ChessCore.Models.User
            {
                Username = "admin",
                Email = "admin@cotuong.pro",
                PasswordHash = passwordHash,
                PlayerId = "CT-00001",
                Role = "Admin",
                Elo = 1500,
                CreatedAt = DateTime.UtcNow
            };
            dbContext.Users.Add(adminUser);
            dbContext.SaveChanges();
            logger.LogInformation(">>> Da tao tai khoan Admin mac dinh: admin / Admin@123");
        }
        else if (adminUser.Role != "Admin")
        {
            adminUser.Role = "Admin";
            dbContext.SaveChanges();
            logger.LogInformation(">>> Da cap quyen Admin cho tai khoan 'admin'");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Loi khi tu dong ap dung migration hoac seed du lieu.");
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
