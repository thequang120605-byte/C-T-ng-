using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using ChessCore.Mobile.Services;
using ChessCore.Mobile.ViewModels;
using ChessCore.Mobile.Views;

namespace ChessCore.Mobile;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
				fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
			});

		// 1. Đăng ký Services
		builder.Services.AddSingleton<IConfigService, ConfigService>();
		builder.Services.AddSingleton<IAuthService, AuthService>();
		builder.Services.AddSingleton<ApiService>();
        builder.Services.AddSingleton<OfflineGameService>();
        builder.Services.AddSingleton<IApiService, HybridApiService>();
		builder.Services.AddSingleton<ISignalRService, SignalRService>();

		// 2. Đăng ký ViewModels
		builder.Services.AddTransient<LoginViewModel>();
		builder.Services.AddTransient<RegisterViewModel>();
		builder.Services.AddTransient<HomeViewModel>();
		builder.Services.AddTransient<GameViewModel>();
		builder.Services.AddTransient<OnlineLobbyViewModel>();
		builder.Services.AddTransient<ProfileViewModel>();
		builder.Services.AddTransient<SettingsViewModel>();

		// 3. Đăng ký Views (Pages)
		builder.Services.AddTransient<LoginPage>();
		builder.Services.AddTransient<RegisterPage>();
		builder.Services.AddTransient<HomePage>();
		builder.Services.AddTransient<GamePage>();
		builder.Services.AddTransient<OnlineLobbyPage>();
		builder.Services.AddTransient<ProfilePage>();
		builder.Services.AddTransient<SettingsPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
