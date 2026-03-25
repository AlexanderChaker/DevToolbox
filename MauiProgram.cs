using CommunityToolkit.Maui;
using DevToolbox.Services.Interfaces;
using Microsoft.Extensions.Logging;
using MudBlazor;
using MudBlazor.Services;
using Services;
using Microsoft.Maui.LifecycleEvents;
using DevToolbox.Services;
using DevToolbox.Platforms.Windows;

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

        // Set the window title and configure tray icon
        builder.ConfigureLifecycleEvents(events =>
        {
            #if WINDOWS
            events.AddWindows(windows =>
            {
                windows.OnWindowCreated((window) =>
                {
                    window.Title = "Dev Toolbox";

                    var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                    var windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hwnd);
                    var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

                    // Initialize system tray icon
                    TrayService.Initialize(window);

                    // Intercept close: hide to tray instead of closing
                    appWindow.Closing += (s, e) =>
                    {
                        if (!TrayService.IsExiting)
                        {
                            e.Cancel = true;
                            TrayService.HideWindow();
                        }
                    };
                });
            });
            #endif
        });

        builder.UseMauiCommunityToolkit(); //Used for the FolderPicker;
        builder.Services.AddMauiBlazorWebView();
        builder.Services.AddMudServices(config =>
        {
            config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomRight;
            config.SnackbarConfiguration.PreventDuplicates = true;
            config.SnackbarConfiguration.NewestOnTop = true;
            config.SnackbarConfiguration.VisibleStateDuration = 3500;
            config.SnackbarConfiguration.ShowTransitionDuration = 150;
            config.SnackbarConfiguration.HideTransitionDuration = 150;
            config.SnackbarConfiguration.MaxDisplayedSnackbars = 4;
        });
        builder.Services.AddSingleton<InMemoryLoggerService>();

        #if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
        #endif

        builder.Services.AddSingleton<IGitService, GitService>();
        builder.Services.AddScoped<ISQLService, SQLService>(); //Needs to be scoped since each session can set a different connection string
        builder.Services.AddSingleton<IKafkaSerializerService, KafkaSerializerService>();
        builder.Services.AddSingleton<TrayCommandService>();

        return builder.Build();
    }
}
