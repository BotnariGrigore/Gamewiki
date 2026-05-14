using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using GameWikiApp.Data;
using GameWikiApp.Models;
using GameWikiApp.Services;

namespace GameWikiApp.Views;

public sealed class ShellView : UserControl
{
    private enum ShellRoute
    {
        Home,
        Wiki,
        Game,
        Article,
        Dashboard
    }

    private readonly Action _onLogout;
    private readonly UserRepository _users = new();
    private readonly GameService _games = new();
    private readonly ArticleService _articles = new();
    private readonly CategoryService _categories = new();

    private ContentControl _pageHost = new();
    private TextBox _searchBox = new();
    private TextBlock _userLabel = new();
    private TextBlock _roleLabel = new();
    private Button _homeButton = new();
    private Button _wikiButton = new();
    private Button _dashboardButton = new();
    private Button _themeButton = new();
    private Button _logoutButton = new();
    private readonly List<Button> _navButtons = new();

    private ShellRoute _route = ShellRoute.Home;
    private string _searchQuery = string.Empty;
    private int? _routeId;
    private int? _categoryId;
    private string? _categoryName;

    public ShellView(Action onLogout)
    {
        _onLogout = onLogout;
        Content = BuildLayout();
        Loaded += async (_, __) => await SafeRestoreRouteAsync();
    }

    private Control BuildLayout()
    {
        _navButtons.Clear();

        var root = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("250,*"),
            Background = ThemePalette.BgPrimaryBrush
        };

        var sidebar = BuildSidebar();
        root.Children.Add(sidebar);
        Grid.SetColumn(sidebar, 0);

        var main = new Grid
        {
            RowDefinitions = new RowDefinitions("78,*"),
            Background = ThemePalette.BgPrimaryBrush
        };
        root.Children.Add(main);
        Grid.SetColumn(main, 1);

        var topbar = BuildTopbar();
        main.Children.Add(topbar);
        Grid.SetRow(topbar, 0);

        _pageHost = new ContentControl
        {
            Background = ThemePalette.BgPrimaryBrush,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };
        main.Children.Add(_pageHost);
        Grid.SetRow(_pageHost, 1);

        UpdateChrome();
        return root;
    }

    private Control BuildSidebar()
    {
        var sidebar = new Border
        {
            Background = ThemePalette.SidebarBrush,
            BorderBrush = ThemePalette.BorderLightBrush,
            BorderThickness = new Thickness(0, 0, 1, 0),
            Padding = new Thickness(16, 18, 16, 16)
        };

        var stack = new StackPanel
        {
            Spacing = 14
        };

        var brand = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("64,*"),
            Margin = new Thickness(0, 0, 0, 6)
        };

        var badge = new Border
        {
            Width = 52,
            Height = 52,
            Background = ThemePalette.AccentBrush,
            CornerRadius = new CornerRadius(16),
            Child = new TextBlock
            {
                Text = "NX",
                FontSize = 18,
                FontWeight = FontWeight.Bold,
                Foreground = ThemePalette.AccentForegroundBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
        brand.Children.Add(badge);

        var brandStack = new StackPanel
        {
            Spacing = 2,
            VerticalAlignment = VerticalAlignment.Center
        };
        brandStack.Children.Add(new TextBlock
        {
            Text = "Nexoria",
            FontSize = 24,
            FontWeight = FontWeight.Bold,
            Foreground = ThemePalette.TextPrimaryBrush
        });
        brandStack.Children.Add(new TextBlock
        {
            Text = "Games, pages, and tools",
            FontSize = 11,
            Foreground = ThemePalette.TextMutedBrush
        });
        brand.Children.Add(brandStack);
        Grid.SetColumn(brandStack, 1);

        stack.Children.Add(brand);
        stack.Children.Add(UiFactory.CreateSeparator(1));

        _homeButton = CreateNavButton("Home");
        _homeButton.Click += async (_, __) => await NavigateHomeAsync();
        stack.Children.Add(_homeButton);

        _wikiButton = CreateNavButton("Wiki");
        _wikiButton.Click += async (_, __) => await NavigateWikiAsync();
        stack.Children.Add(_wikiButton);

        _dashboardButton = CreateNavButton("Dashboard");
        _dashboardButton.Click += async (_, __) =>
        {
            if (!AppState.IsAdmin)
            {
                return;
            }

            await NavigateDashboardAsync();
        };
        stack.Children.Add(_dashboardButton);

        stack.Children.Add(new Border
        {
            Background = ThemePalette.BorderLightBrush,
            Height = 1,
            Margin = new Thickness(0, 8, 0, 2)
        });

        _themeButton = CreateNavButton(AppState.IsDark ? "Light mode" : "Dark mode");
        _themeButton.Click += async (_, __) => await ToggleThemeAsync();
        stack.Children.Add(_themeButton);

        _logoutButton = CreateNavButton("Logout");
        _logoutButton.Foreground = ThemePalette.TextMutedBrush;
        _logoutButton.Click += (_, __) => _onLogout();
        stack.Children.Add(_logoutButton);

        sidebar.Child = stack;
        return sidebar;
    }

    private Control BuildTopbar()
    {
        var topbar = new Border
        {
            Background = ThemePalette.HeaderBrush,
            BorderBrush = ThemePalette.BorderLightBrush,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Padding = new Thickness(20, 16, 20, 14)
        };

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            ColumnSpacing = 12
        };

        var searchRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            VerticalAlignment = VerticalAlignment.Center
        };

        _searchBox = UiFactory.CreateTextBox("Search wiki pages, games, articles...", 560);
        _searchBox.KeyDown += async (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                await NavigateHomeAsync(_searchBox.Text ?? string.Empty);
            }
        };
        searchRow.Children.Add(_searchBox);

        var searchButton = UiFactory.CreatePrimaryButton("Search", 90);
        searchButton.Click += async (_, __) => await NavigateHomeAsync(_searchBox.Text ?? string.Empty);
        searchRow.Children.Add(searchButton);

        grid.Children.Add(searchRow);

        var userChip = new Border
        {
            Background = ThemePalette.BgSecondaryBrush,
            BorderBrush = ThemePalette.BorderLightBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(16),
            Padding = new Thickness(14, 8),
            HorizontalAlignment = HorizontalAlignment.Right
        };

        var chipStack = new StackPanel
        {
            Spacing = 2
        };

        _userLabel = new TextBlock
        {
            FontSize = 14,
            FontWeight = FontWeight.Bold,
            Foreground = ThemePalette.TextPrimaryBrush,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        _roleLabel = new TextBlock
        {
            FontSize = 10,
            Foreground = ThemePalette.TextMutedBrush,
            HorizontalAlignment = HorizontalAlignment.Center
        };
        chipStack.Children.Add(_userLabel);
        chipStack.Children.Add(_roleLabel);
        userChip.Child = chipStack;

        grid.Children.Add(userChip);
        Grid.SetColumn(userChip, 1);

        topbar.Child = grid;
        return topbar;
    }

    private Button CreateNavButton(string text)
    {
        var button = UiFactory.CreateNavButton(text);
        button.Margin = new Thickness(0, 0, 0, 2);
        button.HorizontalAlignment = HorizontalAlignment.Stretch;
        _navButtons.Add(button);
        return button;
    }

    private void UpdateChrome()
    {
        _userLabel.Text = AppState.CurrentUser?.Username ?? "Guest";
        _roleLabel.Text = AppState.CurrentUser == null
            ? "Visitor"
            : (AppState.IsAdmin ? "Administrator" : "Member");
        _themeButton.Content = AppState.IsDark ? "Light mode" : "Dark mode";
        _dashboardButton.IsVisible = AppState.IsAdmin;
        _searchBox.Text = _searchQuery;

        SetActiveNav(_route);
    }

    private void ShowPageError(string title, string message)
    {
        _pageHost.Content = new Border
        {
            Background = ThemePalette.BgSecondaryBrush,
            BorderBrush = ThemePalette.BorderLightBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(18),
            Margin = new Thickness(18),
            Padding = new Thickness(24),
            Child = new StackPanel
            {
                Spacing = 10,
                Children =
                {
                    new TextBlock
                    {
                        Text = title,
                        FontSize = 20,
                        FontWeight = FontWeight.Bold,
                        Foreground = ThemePalette.TextPrimaryBrush
                    },
                    new TextBlock
                    {
                        Text = message,
                        FontSize = 13,
                        Foreground = ThemePalette.TextSecondaryBrush,
                        TextWrapping = TextWrapping.Wrap
                    }
                }
            }
        };
    }

    private void SetActiveNav(ShellRoute route)
    {
        ApplyNavState(_homeButton, route == ShellRoute.Home);
        ApplyNavState(_wikiButton, route == ShellRoute.Wiki);
        ApplyNavState(_dashboardButton, route == ShellRoute.Dashboard);
    }

    private static void ApplyNavState(Button button, bool active)
    {
        button.Background = active ? ThemePalette.BgTertiaryBrush : Brushes.Transparent;
        button.Foreground = active ? ThemePalette.AccentBrush : ThemePalette.TextSecondaryBrush;
    }

    private async Task RestoreRouteAsync()
    {
        switch (_route)
        {
            case ShellRoute.Home:
                await NavigateHomeAsync(_searchQuery);
                break;
            case ShellRoute.Wiki:
                await NavigateWikiAsync(_searchQuery, _categoryId, _categoryName);
                break;
            case ShellRoute.Game:
                if (_routeId.HasValue)
                {
                    await NavigateGameAsync(_routeId.Value);
                }
                else
                {
                    await NavigateHomeAsync();
                }
                break;
            case ShellRoute.Article:
                if (_routeId.HasValue)
                {
                    await NavigateArticleAsync(_routeId.Value);
                }
                else
                {
                    await NavigateHomeAsync();
                }
                break;
            case ShellRoute.Dashboard:
                await NavigateDashboardAsync();
                break;
        }
    }

    private async Task SafeRestoreRouteAsync()
    {
        try
        {
            await RestoreRouteAsync();
        }
        catch
        {
            ShowPageError("Unable to load page", "Nexoria hit a problem loading the current view.");
        }
    }

    public async Task NavigateHomeAsync(string? query = null)
    {
        _route = ShellRoute.Home;
        _routeId = null;
        _categoryId = null;
        _categoryName = null;
        _searchQuery = query?.Trim() ?? string.Empty;
        UpdateChrome();

        try
        {
            var view = new HomeView(
                gameId => _ = NavigateGameAsync(gameId),
                articleId => _ = NavigateArticleAsync(articleId));

            _pageHost.Content = view;
            await view.LoadAsync(_searchQuery);
        }
        catch
        {
            ShowPageError("Home unavailable", "The home page could not be loaded. Please try again.");
        }
    }

    public async Task NavigateWikiAsync(string? query = null, int? categoryId = null, string? categoryName = null)
    {
        _route = ShellRoute.Wiki;
        _routeId = null;
        _searchQuery = query?.Trim() ?? string.Empty;
        _categoryId = categoryId;
        _categoryName = categoryName;
        UpdateChrome();

        try
        {
            var view = new WikiBrowserView(
                gameId => _ = NavigateGameAsync(gameId),
                articleId => _ = NavigateArticleAsync(articleId));

            _pageHost.Content = view;
            await view.LoadAsync(_searchQuery, _categoryId, _categoryName);
        }
        catch
        {
            ShowPageError("Wiki unavailable", "The wiki browser could not be loaded. Please try again.");
        }
    }

    public async Task NavigateGameAsync(int gameId)
    {
        _route = ShellRoute.Game;
        _routeId = gameId;
        UpdateChrome();

        try
        {
            var view = new GameView(
                gameId,
                articleId => _ = NavigateArticleAsync(articleId),
                categoryId => _ = NavigateWikiAsync(null, categoryId, null));

            _pageHost.Content = view;
            await view.LoadAsync();
        }
        catch
        {
            ShowPageError("Game unavailable", "The selected game could not be loaded. Please try again.");
        }
    }

    public async Task NavigateArticleAsync(int articleId)
    {
        _route = ShellRoute.Article;
        _routeId = articleId;
        UpdateChrome();

        try
        {
            var view = new ArticleView(
                articleId,
                gameId => _ = NavigateGameAsync(gameId),
                relatedArticleId => _ = NavigateArticleAsync(relatedArticleId));

            _pageHost.Content = view;
            await view.LoadAsync();
        }
        catch
        {
            ShowPageError("Article unavailable", "The selected article could not be loaded. Please try again.");
        }
    }

    public async Task NavigateDashboardAsync()
    {
        _route = ShellRoute.Dashboard;
        _routeId = null;
        UpdateChrome();

        try
        {
            var view = new DashboardView();
            _pageHost.Content = view;
            await view.LoadAsync();
        }
        catch
        {
            ShowPageError("Dashboard unavailable", "The admin dashboard could not be loaded. Please try again.");
        }
    }

    private async Task ToggleThemeAsync()
    {
        try
        {
            var nextTheme = AppState.IsDark ? "light" : "dark";
            AppState.ApplyThemePreference(nextTheme);

            if (AppState.CurrentUser != null)
            {
                await _users.UpdateThemePreferenceAsync(AppState.CurrentUser.UserId, nextTheme);
            }

            Content = BuildLayout();
            await SafeRestoreRouteAsync();
        }
        catch
        {
            ShowPageError("Theme update failed", "The theme could not be changed right now.");
        }
    }
}
