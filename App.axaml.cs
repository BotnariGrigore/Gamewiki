using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using GameWikiApp.Services;

namespace GameWikiApp;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        AppState.LoadLocalThemePreference();
        AppState.ApplyThemePreference(AppState.PreferredTheme);

        // Run a background database check / minimal migration early at startup.
        _ = Task.Run(async () =>
        {
            try
            {
                var svc = new AuthService();
                var err = await svc.CheckDatabaseAsync();
                if (err != null)
                {
                    try { File.AppendAllText("startup_db_check.log", DateTime.UtcNow + " DB CHECK STARTUP: " + err + Environment.NewLine); } catch { }
                }
            }
            catch (Exception ex)
            {
                try { File.AppendAllText("startup_db_check.log", DateTime.UtcNow + " DB CHECK STARTUP EX: " + ex + Environment.NewLine); } catch { }
            }
        });

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
