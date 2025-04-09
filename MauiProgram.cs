using CommunityToolkit.Maui;
using DeployGitBranch.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.FluentUI.AspNetCore.Components;
using Services;

namespace DeployGitBranch;

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

		builder.UseMauiCommunityToolkit(); //Used for the FolderPicker;
        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddFluentUIComponents();

		#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
		#endif

        builder.Services.AddSingleton<IGitService, GitService>();
        builder.Services.AddScoped<ISQLService, SQLService>(); //Needs to be scoped since each session can set a different connection string

		return builder.Build();
	}
}
