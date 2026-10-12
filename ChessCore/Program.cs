using Microsoft.EntityFrameworkCore;
using ChessCore.Data;
using ChessCore.Services;

using Microsoft.AspNetCore.Authentication.Cookies;

// Tự động nhận diện chính xác ContentRootPath kể cả khi chạy từ thư mục gốc hoặc thư mục con
var contentRoot = Directory.GetCurrentDirectory();
if (!Directory.Exists(Path.Combine(contentRoot, "Views")))
{
    var projectDirInSub = Path.Combine(contentRoot, "ChessCore");
    if (Directory.Exists(Path.Combine(projectDirInSub, "Views")))
    {
        contentRoot = projectDirInSub;
    }
    else
    {
        var fromBin = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));
        if (Directory.Exists(Path.Combine(fromBin, "Views")))
        {
            contentRoot = fromBin;
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

var app = builder.Build();

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
