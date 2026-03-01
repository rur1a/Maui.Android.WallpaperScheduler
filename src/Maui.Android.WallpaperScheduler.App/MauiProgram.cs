using MAUI.Application.Abstractions;
using MAUI.Application.Services;
using MAUI.Infrastructure.Android;
using MAUI.Presentation.Pages;
using MAUI.Presentation.ViewModels;
using Microsoft.Extensions.Logging;
using Shared.Theming.Maui;

namespace MAUI;

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

		builder.Services.AddSingleton<IWallpaperSchedulerGateway, AndroidWallpaperSchedulerGateway>();
		builder.Services.AddSingleton<IWallpaperSchedulerService, WallpaperSchedulerService>();
		builder.Services.AddSingleton<IUserNotificationService, AndroidUserNotificationService>();
		builder.Services.AddSharedTheming();

		builder.Services.AddTransient<SchedulerManagerViewModel>();
		builder.Services.AddTransient<AddSchedulerViewModel>();

		builder.Services.AddSingleton<SchedulerManagerPage>();
		builder.Services.AddTransient<AddSchedulerPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
