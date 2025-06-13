using CommunityToolkit.Maui;
using DevToolbox.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.FluentUI.AspNetCore.Components;
using Services;
using Microsoft.Maui.LifecycleEvents;
using DevToolbox.Services;

namespace DevToolbox;

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
			});

		// Set the window title
		builder.ConfigureLifecycleEvents(events =>
        {
			#if WINDOWS
            events.AddWindows(windows =>
            {
                windows.OnWindowCreated((window) =>
                {
                    window.Title = "Dev Toolbox";
                });
            });
			#endif
        });

		builder.UseMauiCommunityToolkit(); //Used for the FolderPicker;
		builder.Services.AddMauiBlazorWebView();
		builder.Services.AddFluentUIComponents();
		builder.Services.AddSingleton<InMemoryLoggerService>();

		#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
		#endif

		builder.Services.AddSingleton<IGitService, GitService>();
		builder.Services.AddScoped<ISQLService, SQLService>(); //Needs to be scoped since each session can set a different connection string

		return builder.Build();
	}
}
