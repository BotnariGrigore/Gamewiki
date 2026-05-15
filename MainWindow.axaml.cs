using Avalonia.Controls;
using Avalonia.Threading;
using GameWikiApp.Models;
using GameWikiApp.Views;

namespace GameWikiApp;

public partial class MainWindow : Window
{
    private ShellView? _shell;

    public MainWindow()
    {
        InitializeComponent();
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        AppState.ThemeChanged += HandleThemeChanged;
        Closed += (_, __) => AppState.ThemeChanged -= HandleThemeChanged;
        LoadInitialView();
    }

    private void LoadInitialView()
    {
        if (AppState.IsAuthenticated && AppState.CurrentUser != null)
        {
            ShowShell();
            return;
        }

        Content = new AuthView(OnAuthenticated);
    }

    private void OnAuthenticated(User user)
    {
        ShowShell();
    }

    private void ShowShell()
    {
        _shell = new ShellView(HandleLogout);
        Content = _shell;
    }

    private void HandleThemeChanged()
    {
        if (Dispatcher.UIThread.CheckAccess())
        {
            RefreshThemeContent();
            return;
        }

        Dispatcher.UIThread.Post(RefreshThemeContent);
    }

    private void RefreshThemeContent()
    {
        if (AppState.IsAuthenticated && _shell != null && ReferenceEquals(Content, _shell))
        {
            _ = _shell.RefreshThemeAsync();
            return;
        }

        if (Content is AuthView authView)
        {
            _ = authView.RefreshThemeAsync();
        }
    }

    private void HandleLogout()
    {
        AppState.EndSession();
        _shell = null;
        Content = new AuthView(OnAuthenticated);
    }
}
