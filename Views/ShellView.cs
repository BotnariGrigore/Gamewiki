using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using System.IO;
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
        FriendsChat,
        Profile,
        Settings,
        Game,
        Article,
        Dashboard
    }

    private readonly Action _onLogout;
    private readonly UserRepository _users = new();
    private readonly NotificationService _notifications = new();
    private readonly GameService _games = new();
    private readonly ArticleService _articles = new();
    private readonly CategoryService _categories = new();
    private readonly DispatcherTimer _notificationTimer = new();

    private ContentControl _pageHost = new();
    private TextBox _searchBox = new();
    private TextBlock _userLabel = new();
    private TextBlock _roleLabel = new();
    private TextBlock _notificationBadgeText = new();
    private Button _homeButton = new();
    private Button _wikiButton = new();
    private Button _friendsChatButton = new();
    private Button _settingsNavButton = new();
    private Button _dashboardButton = new();
    private Button _themeButton = new();
    private Button _logoutButton = new();
    private Button _notificationButton = new();
    private readonly List<Button> _navButtons = new();
    private readonly StackPanel _notificationList = new();
    private readonly Popup _notificationPopup = new();
    private readonly Border _notificationBadgeHost = new();

    private ShellRoute _route = ShellRoute.Home;
    private string _searchQuery = string.Empty;
    private int? _routeId;
    private int? _categoryId;
    private string? _categoryName;

    public ShellView(Action onLogout)
    {
        _onLogout = onLogout;
        _notificationTimer.Interval = TimeSpan.FromSeconds(4);
        _notificationTimer.Tick += async (_, __) => await RefreshNotificationsAsync();
        NotificationService.NotificationsChanged += HandleNotificationsChanged;
        Content = BuildLayout();
        Loaded += async (_, __) =>
        {
            await SafeRestoreRouteAsync();
            await RefreshNotificationsAsync();
            _notificationTimer.Start();
        };
        Unloaded += (_, __) =>
        {
            _notificationTimer.Stop();
            NotificationService.NotificationsChanged -= HandleNotificationsChanged;
        };
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

        _homeButton = CreateNavButton("Home", "⌂");
        _homeButton.Click += async (_, __) => await NavigateHomeAsync();
        stack.Children.Add(_homeButton);

        _wikiButton = CreateNavButton("Wiki", "☰");
        _wikiButton.Click += async (_, __) => await NavigateWikiAsync();
        stack.Children.Add(_wikiButton);

        _friendsChatButton = CreateNavButton("Friends & Chat", "✉");
        _friendsChatButton.Click += async (_, __) => await NavigateFriendsChatAsync();
        stack.Children.Add(_friendsChatButton);

        _dashboardButton = CreateNavButton("Dashboard", "▦");
        _dashboardButton.Click += async (_, __) => await NavigateDashboardAsync();
        stack.Children.Add(_dashboardButton);

        stack.Children.Add(new Border
        {
            Background = ThemePalette.BorderLightBrush,
            Height = 1,
            Margin = new Thickness(0, 8, 0, 2)
        });

        _themeButton = CreateNavButton(AppState.IsDark ? "Light mode" : "Dark mode", "◐");
        _themeButton.Click += async (_, __) => await ToggleThemeAsync();
        stack.Children.Add(_themeButton);

        _settingsNavButton = CreateNavButton("Settings", "⚙");
        _settingsNavButton.Click += async (_, __) => await NavigateSettingsAsync();
        stack.Children.Add(_settingsNavButton);

        _logoutButton = CreateNavButton("Logout", "⏻");
        _logoutButton.Foreground = ThemePalette.TextMutedBrush;
        _logoutButton.Click += async (_, __) => await LogoutAsync();
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

        var searchRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            ColumnSpacing = 10,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        _searchBox = UiFactory.CreateTextBox("Search wiki pages, games, articles...");
        _searchBox.MinWidth = 320;
        _searchBox.KeyDown += async (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                await NavigateHomeAsync(_searchBox.Text ?? string.Empty);
            }
        };
        searchRow.Children.Add(_searchBox);

        var searchButton = UiFactory.CreatePrimaryButton("Search", 96, "⌕");
        searchButton.Click += async (_, __) => await NavigateHomeAsync(_searchBox.Text ?? string.Empty);
        searchRow.Children.Add(searchButton);
        Grid.SetColumn(searchButton, 1);

        grid.Children.Add(searchRow);

        var actionRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        _notificationButton = CreateIconButton("⚑");
        _notificationButton.Click += async (_, __) => await ToggleNotificationsAsync();
        actionRow.Children.Add(BuildNotificationButton());

        var userChip = new Border
        {
            Background = ThemePalette.BgSecondaryBrush,
            BorderBrush = ThemePalette.BorderLightBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(16),
            Padding = new Thickness(14, 8),
            Cursor = new Cursor(StandardCursorType.Hand)
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
        userChip.PointerPressed += async (_, __) =>
        {
            if (AppState.CurrentUser != null)
            {
                await NavigateProfileAsync(AppState.CurrentUser.UserId);
            }
        };

        actionRow.Children.Add(userChip);

        grid.Children.Add(actionRow);
        Grid.SetColumn(actionRow, 1);
        grid.Children.Add(BuildNotificationPopup());

        topbar.Child = grid;
        return topbar;
    }

    private Button BuildNotificationButton()
    {
        var grid = new Grid
        {
            Width = 42,
            Height = 42
        };

        grid.Children.Add(new TextBlock
        {
            Text = "⚑",
            FontSize = 16,
            FontWeight = FontWeight.Bold,
            Foreground = ThemePalette.TextPrimaryBrush,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        });

        _notificationBadgeHost.Width = 18;
        _notificationBadgeHost.Height = 18;
        _notificationBadgeHost.Background = ThemePalette.AccentBrush;
        _notificationBadgeHost.BorderBrush = ThemePalette.BgSecondaryBrush;
        _notificationBadgeHost.BorderThickness = new Thickness(1);
        _notificationBadgeHost.CornerRadius = new CornerRadius(9);
        _notificationBadgeHost.HorizontalAlignment = HorizontalAlignment.Right;
        _notificationBadgeHost.VerticalAlignment = VerticalAlignment.Top;
        _notificationBadgeHost.Margin = new Thickness(0, -3, -3, 0);
        _notificationBadgeHost.Child = _notificationBadgeText;
        _notificationBadgeText.FontSize = 9;
        _notificationBadgeText.FontWeight = FontWeight.Bold;
        _notificationBadgeText.Foreground = ThemePalette.AccentForegroundBrush;
        _notificationBadgeText.HorizontalAlignment = HorizontalAlignment.Center;
        _notificationBadgeText.VerticalAlignment = VerticalAlignment.Center;
        _notificationBadgeHost.IsVisible = false;

        grid.Children.Add(_notificationBadgeHost);
        _notificationButton.Background = ThemePalette.BgSecondaryBrush;
        _notificationButton.BorderBrush = ThemePalette.BorderLightBrush;
        _notificationButton.BorderThickness = new Thickness(1);
        _notificationButton.CornerRadius = new CornerRadius(12);
        _notificationButton.Padding = new Thickness(0);
        _notificationButton.Width = 42;
        _notificationButton.Height = 42;
        _notificationButton.Content = grid;
        return _notificationButton;
    }

    private Popup BuildNotificationPopup()
    {
        _notificationPopup.PlacementTarget = _notificationButton;
        _notificationPopup.IsLightDismissEnabled = true;
        _notificationPopup.HorizontalOffset = 0;
        _notificationPopup.VerticalOffset = 10;

        if (_notificationPopup.Child == null)
        {
            var card = new Border
            {
                Background = ThemePalette.BgSecondaryBrush,
                BorderBrush = ThemePalette.BorderLightBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(18),
                Padding = new Thickness(16),
                Width = 380,
                MaxHeight = 520,
                Child = new StackPanel
                {
                    Spacing = 12,
                    Children =
                    {
                        new Grid
                        {
                            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
                            Children =
                            {
                                new TextBlock
                                {
                                    Text = "Notifications",
                                    FontSize = 16,
                                    FontWeight = FontWeight.Bold,
                                    Foreground = ThemePalette.TextPrimaryBrush
                                }
                            }
                        },
                        new ScrollViewer
                        {
                            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                            MaxHeight = 420,
                            Content = _notificationList
                        }
                    }
                }
            };

            var headerGrid = (Grid)((StackPanel)card.Child!).Children[0];
            var markAll = UiFactory.CreateSubtleButton("Mark all read", 118);
            markAll.Click += async (_, __) => await MarkAllNotificationsReadAsync();
            headerGrid.Children.Add(markAll);
            Grid.SetColumn(markAll, 1);

            _notificationList.Spacing = 10;
            _notificationPopup.Child = card;
        }

        return _notificationPopup;
    }

    private static Button CreateIconButton(string icon)
    {
        return UiFactory.CreateIconButton(icon);
    }

    private Button CreateNavButton(string text, string? icon = null)
    {
        var button = UiFactory.CreateNavButton(text, icon);
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
        _themeButton.Content = UiFactory.CreateButtonContent(AppState.IsDark ? "Light mode" : "Dark mode", "◐");
        _dashboardButton.IsVisible = AppState.IsAuthenticated;
        _searchBox.Text = _searchQuery;
        _notificationPopup.IsOpen = false;

        SetActiveNav(_route);
    }

    private void HandleNotificationsChanged(int userId)
    {
        if (AppState.CurrentUser?.UserId != userId)
        {
            return;
        }

        if (Dispatcher.UIThread.CheckAccess())
        {
            _ = RefreshNotificationsAsync();
            return;
        }

        Dispatcher.UIThread.Post(() => _ = RefreshNotificationsAsync());
    }

    private async Task RefreshNotificationsAsync()
    {
        if (AppState.CurrentUser == null)
        {
            UpdateNotificationBadge(0);
            _notificationList.Children.Clear();
            return;
        }

        try
        {
            var userId = AppState.CurrentUser.UserId;
            var unread = await _notifications.GetUnreadCountAsync(userId);
            var recent = (await _notifications.GetRecentAsync(userId, 8)).ToList();

            UpdateNotificationBadge(unread);
            RebuildNotificationsList(recent);
        }
        catch
        {
            UpdateNotificationBadge(0);
        }
    }

    private void UpdateNotificationBadge(int count)
    {
        if (count <= 0)
        {
            _notificationBadgeHost.IsVisible = false;
            _notificationBadgeText.Text = string.Empty;
            return;
        }

        _notificationBadgeHost.IsVisible = true;
        _notificationBadgeText.Text = count > 99 ? "99+" : count.ToString();
    }

    private void RebuildNotificationsList(IReadOnlyList<Notification> notifications)
    {
        _notificationList.Children.Clear();

        if (notifications.Count == 0)
        {
            _notificationList.Children.Add(new Border
            {
                Background = ThemePalette.BgSecondaryBrush,
                BorderBrush = ThemePalette.BorderLightBrush,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(14),
                Child = new TextBlock
                {
                    Text = "No notifications yet.",
                    FontSize = 12,
                    Foreground = ThemePalette.TextMutedBrush
                }
            });
            return;
        }

        foreach (var notification in notifications)
        {
            _notificationList.Children.Add(BuildNotificationItem(notification));
        }
    }

    private Control BuildNotificationItem(Notification notification)
    {
        var card = new Border
        {
            Background = notification.IsRead ? ThemePalette.BgSecondaryBrush : ThemePalette.BgTertiaryBrush,
            BorderBrush = notification.IsRead ? ThemePalette.BorderLightBrush : ThemePalette.AccentBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(16),
            Padding = new Thickness(14),
            Cursor = new Cursor(StandardCursorType.Hand)
        };

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("46,*"),
            ColumnSpacing = 12
        };

        grid.Children.Add(BuildNotificationAvatar(notification));

        var stack = new StackPanel
        {
            Spacing = 4
        };

        var header = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8
        };

        header.Children.Add(new TextBlock
        {
            Text = notification.Title,
            FontSize = 12.5,
            FontWeight = FontWeight.Bold,
            Foreground = ThemePalette.TextPrimaryBrush,
            TextWrapping = TextWrapping.Wrap
        });

        if (!notification.IsRead)
        {
            header.Children.Add(new Border
            {
                Width = 8,
                Height = 8,
                Background = ThemePalette.AccentBrush,
                CornerRadius = new CornerRadius(4),
                VerticalAlignment = VerticalAlignment.Center
            });
        }

        stack.Children.Add(header);

        stack.Children.Add(new TextBlock
        {
            Text = notification.Message,
            FontSize = 11,
            Foreground = ThemePalette.TextSecondaryBrush,
            TextWrapping = TextWrapping.Wrap
        });

        stack.Children.Add(new TextBlock
        {
            Text = notification.TimeLabel,
            FontSize = 10,
            Foreground = ThemePalette.TextMutedBrush
        });

        grid.Children.Add(stack);
        Grid.SetColumn(stack, 1);

        card.Child = grid;
        card.PointerPressed += async (_, __) => await OpenNotificationAsync(notification);
        return card;
    }

    private Control BuildNotificationAvatar(Notification notification)
    {
        var shell = new Border
        {
            Width = 46,
            Height = 46,
            Background = ThemePalette.AccentBrush,
            BorderBrush = ThemePalette.BorderLightBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(16),
            ClipToBounds = true,
            Child = new TextBlock
            {
                Text = GetAvatarInitials(notification.SourceUsername ?? "U"),
                FontSize = 14,
                FontWeight = FontWeight.Bold,
                Foreground = ThemePalette.AccentForegroundBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };

        if (!string.IsNullOrWhiteSpace(notification.SourceProfileImage))
        {
            _ = LoadNotificationAvatarAsync(shell, notification.SourceProfileImage);
        }

        return shell;
    }

    private async Task LoadNotificationAvatarAsync(Border shell, string sourceImage)
    {
        var bitmap = await ImageLoader.LoadAsync(sourceImage);
        if (bitmap == null)
        {
            return;
        }

        shell.Background = ThemePalette.BgSecondaryBrush;
        shell.Child = new Image
        {
            Source = bitmap,
            Stretch = Stretch.UniformToFill,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
    }

    private static string GetAvatarInitials(string value)
    {
        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 0)
        {
            return "U";
        }

        if (parts.Length == 1)
        {
            return parts[0].Length > 1 ? parts[0][..1].ToUpperInvariant() : parts[0].ToUpperInvariant();
        }

        return string.Concat(parts.Take(2).Select(x => char.ToUpperInvariant(x[0])));
    }

    private async Task ToggleNotificationsAsync()
    {
        if (AppState.CurrentUser == null)
        {
            return;
        }

        if (_notificationPopup.IsOpen)
        {
            _notificationPopup.IsOpen = false;
            return;
        }

        await RefreshNotificationsAsync();
        _notificationPopup.IsOpen = true;
    }

    private async Task MarkAllNotificationsReadAsync()
    {
        if (AppState.CurrentUser == null)
        {
            return;
        }

        await _notifications.MarkAllAsReadAsync(AppState.CurrentUser.UserId);
        await RefreshNotificationsAsync();
    }

    private async Task OpenNotificationAsync(Notification notification)
    {
        if (AppState.CurrentUser == null)
        {
            return;
        }

        if (!notification.IsRead)
        {
            await _notifications.MarkAsReadAsync(notification.NotificationId, AppState.CurrentUser.UserId);
        }

        _notificationPopup.IsOpen = false;

        var route = (notification.ActionRoute ?? string.Empty).Trim().ToLowerInvariant();
        switch (route)
        {
            case "article" when notification.TargetId.HasValue:
                await NavigateArticleAsync(notification.TargetId.Value);
                break;
            case "friends-chat":
                await NavigateFriendsChatAsync(string.Equals(notification.TargetType, "user", StringComparison.OrdinalIgnoreCase) ? notification.TargetId : null);
                break;
            case "friends":
                await NavigateFriendsChatAsync();
                break;
            case "profile" when notification.TargetId.HasValue:
                await NavigateProfileAsync(notification.TargetId.Value);
                break;
            case "profile" when AppState.CurrentUser != null:
                await NavigateProfileAsync(AppState.CurrentUser.UserId);
                break;
            case "settings":
                await NavigateSettingsAsync();
                break;
        }
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
        ApplyNavState(_friendsChatButton, route == ShellRoute.FriendsChat);
        ApplyNavState(_settingsNavButton, route == ShellRoute.Settings);
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
            case ShellRoute.FriendsChat:
                await NavigateFriendsChatAsync(_routeId);
                break;
            case ShellRoute.Profile:
                if (_routeId.HasValue)
                {
                    await NavigateProfileAsync(_routeId.Value);
                }
                else if (AppState.CurrentUser != null)
                {
                    await NavigateProfileAsync(AppState.CurrentUser.UserId);
                }
                else
                {
                    await NavigateHomeAsync();
                }
                break;
            case ShellRoute.Settings:
                await NavigateSettingsAsync();
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

    public async Task NavigateFriendsChatAsync(int? openConversationUserId = null)
    {
        _route = ShellRoute.FriendsChat;
        _routeId = openConversationUserId;
        _categoryId = null;
        _categoryName = null;
        _searchQuery = string.Empty;
        UpdateChrome();

        try
        {
            var view = new FriendsChatView(userId => NavigateProfileAsync(userId));
            _pageHost.Content = view;
            await view.LoadInitialAsync();

            if (openConversationUserId.HasValue)
            {
                await view.OpenConversationByUserAsync(openConversationUserId.Value);
            }
        }
        catch
        {
            ShowPageError("Friends & Chat unavailable", "The messenger view could not be loaded. Please try again.");
        }
    }

    public async Task NavigateProfileAsync(int userId)
    {
        _route = ShellRoute.Profile;
        _routeId = userId;
        _categoryId = null;
        _categoryName = null;
        _searchQuery = string.Empty;
        UpdateChrome();

        try
        {
            var view = new UserProfileView(
                userId,
                conversationUserId => NavigateFriendsChatAsync(conversationUserId),
                NavigateSettingsAsync);

            _pageHost.Content = view;
            await view.LoadAsync();
        }
        catch
        {
            ShowPageError("Profile unavailable", "The selected profile could not be loaded. Please try again.");
        }
    }

    public async Task NavigateSettingsAsync()
    {
        _route = ShellRoute.Settings;
        _routeId = null;
        _categoryId = null;
        _categoryName = null;
        _searchQuery = string.Empty;
        UpdateChrome();

        try
        {
            var view = new ProfileSettingsView(LogoutAsync, UpdateChrome);
            _pageHost.Content = view;
            await view.LoadAsync();
        }
        catch
        {
            ShowPageError("Settings unavailable", "The profile settings page could not be loaded. Please try again.");
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
            if (AppState.IsAdmin)
            {
                var view = new DashboardView();
                _pageHost.Content = view;
                await view.LoadAsync();
            }
            else
            {
                var view = new UsersListView(userId => NavigateProfileAsync(userId));
                _pageHost.Content = view;
                await view.LoadAsync();
            }
        }
        catch (Exception ex)
        {
            var msg = $"The dashboard could not be loaded. {ex.Message}\n\n{ex.StackTrace}";
            try
            {
                var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GameWikiApp");
                Directory.CreateDirectory(dir);
                var file = Path.Combine(dir, "error.log");
                File.AppendAllText(file, $"{DateTime.UtcNow:O} - NavigateDashboardAsync: {msg}\n\n");
            }
            catch
            {
                // ignore logging failures
            }

            ShowPageError("Dashboard unavailable", msg);
        }
    }

    private async Task LogoutAsync()
    {
        try
        {
            if (AppState.CurrentUser != null)
            {
                await _users.UpdatePresenceAsync(AppState.CurrentUser.UserId, false, DateTime.UtcNow);
            }
        }
        catch
        {
            // ignore logout presence errors
        }

        _onLogout();
    }

    private async Task ToggleThemeAsync()
    {
        try
        {
            var nextTheme = AppState.IsDark ? "light" : "dark";
            AppState.ApplyThemePreference(nextTheme);
            _themeButton.Content = AppState.IsDark ? "Light mode" : "Dark mode";

            if (AppState.CurrentUser != null)
            {
                await _users.UpdateThemePreferenceAsync(AppState.CurrentUser.UserId, nextTheme);
            }
        }
        catch
        {
            // Keep the local theme change even if persistence fails.
        }
    }

    public Task RefreshThemeAsync()
    {
        try
        {
            UpdateChrome();
        }
        catch
        {
            // Ignore transient refresh issues; the theme already changed globally.
        }
        return Task.CompletedTask;
    }
}
