using Avalonia.Controls;
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

    private void HandleLogout()
    {
        AppState.EndSession();
        _shell = null;
        Content = new AuthView(OnAuthenticated);
    }
}
