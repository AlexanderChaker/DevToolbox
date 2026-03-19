using H.NotifyIcon;
using Microsoft.Win32;
using WinUIControls = Microsoft.UI.Xaml.Controls;

namespace DevToolbox.Platforms.Windows;

public static partial class TrayService
{
    private static TaskbarIcon? _trayIcon;
    private static Microsoft.UI.Xaml.Window? _window;

    public static bool IsExiting { get; private set; }

    public static void Initialize(Microsoft.UI.Xaml.Window window)
    {
        _window = window;

        var iconFile = IsDarkTheme()
            ? @"Resources\AppIcon\wrenchtool(white).ico"
            : @"Resources\AppIcon\wrenchtool.ico";

        _trayIcon = new TaskbarIcon
        {
            ToolTipText = "Dev Toolbox",
            Icon = new System.Drawing.Icon(Path.Combine(AppContext.BaseDirectory, iconFile)),
            NoLeftClickDelay = true,
            // Left-click restores the window
            LeftClickCommand = new SimpleCommand(RestoreWindow),
            // RightClickCommand = new SimpleCommand(ExitApplication)
        };

        // Right-click context menu
        var kafkaItem = new WinUIControls.MenuFlyoutItem { Text = "Kafka Deserialize", Command = new SimpleCommand(OnKafkaDeserialize) };
        var showItem = new WinUIControls.MenuFlyoutItem { Text = "Show", Command = new SimpleCommand(RestoreWindow) };
        var exitItem = new WinUIControls.MenuFlyoutItem { Text = "Exit", Command = new SimpleCommand(ExitApplication) };

        var menu = new WinUIControls.MenuFlyout();
        menu.Items.Add(kafkaItem);
        menu.Items.Add(new WinUIControls.MenuFlyoutSeparator());
        menu.Items.Add(showItem);
        menu.Items.Add(exitItem);

        _trayIcon.ContextFlyout = menu;

        _trayIcon.ForceCreate();
    }

    public static void RestoreWindow()
    {
        if (_window == null) return;

        _window.Show();
        _window.Activate();
    }

    public static void HideWindow()
    {
        _window?.Hide();
    }

    public static void ExitApplication()
    {
        IsExiting = true;
        _trayIcon?.Dispose();
        _trayIcon = null;
        _window?.Close();
    }

    private static void OnKafkaDeserialize()
    {
        RestoreWindow();
        DevToolbox.Services.TrayCommandService.Instance?.RequestKafkaDeserialize();
    }

    private static bool IsDarkTheme()
    {
        using var key = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        var value = key?.GetValue("AppsUseLightTheme");
        return value is int i && i == 0;
    }

    private partial class SimpleCommand(Action execute) : System.Windows.Input.ICommand
    {
        private readonly Action _execute = execute;
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => _execute();
#pragma warning disable CS0067
        public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067
    }
}
