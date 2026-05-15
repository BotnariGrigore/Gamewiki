using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using GameWikiApp.Data;
using GameWikiApp.Models;

namespace GameWikiApp.Views;

public sealed class UsersListView : UserControl
{
    private readonly Func<int, Task> _navigateProfile;
    private readonly UserRepository _users = new();
    private readonly StackPanel _content = new();
    private readonly StackPanel _usersPanel = new();
    private readonly TextBlock _statsText = new();
    private readonly TextBlock _usersCount = new();
    private readonly TextBlock _gamesTotal = new();
    private readonly TextBlock _articlesTotal = new();

    public UsersListView(Func<int, Task> navigateProfile)
    {
        _navigateProfile = navigateProfile;

        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = _content,
            Background = ThemePalette.BgPrimaryBrush
        };

        _content.Spacing = 14;
        _content.Children.Add(BuildHero());
        _content.Children.Add(BuildUsersSection());

        Content = scroll;
    }

    private Control BuildHero()
    {
        var shell = new Border
        {
            Background = ThemePalette.SurfaceBrush,
            BorderBrush = ThemePalette.BorderLightBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(20),
            Padding = new Thickness(24)
        };

        var stack = new StackPanel
        {
            Spacing = 12
        };

        stack.Children.Add(new TextBlock
        {
            Text = "Community",
            FontSize = 28,
            FontWeight = FontWeight.Bold,
            Foreground = ThemePalette.TextPrimaryBrush
        });

        _statsText.Text = "Loading statistics...";
        _statsText.FontSize = 12;
        _statsText.Foreground = ThemePalette.TextSecondaryBrush;
        stack.Children.Add(_statsText);

        var stats = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            ItemWidth = 180,
            Margin = new Thickness(0, 2, 0, 0)
        };
        stats.Children.Add(CreateStatTile("Members", _usersCount));
        stats.Children.Add(CreateStatTile("Games created", _gamesTotal));
        stats.Children.Add(CreateStatTile("Articles written", _articlesTotal));
        stack.Children.Add(stats);

        shell.Child = stack;
        return shell;
    }

    private static Border CreateStatTile(string title, TextBlock value)
    {
        var tile = UiFactory.CreateCard(180, 76, 0);
        tile.Background = ThemePalette.BgSecondaryBrush;
        tile.Padding = new Thickness(16, 12);

        var stack = new StackPanel
        {
            Spacing = 2
        };

        stack.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 11,
            FontWeight = FontWeight.Bold,
            Foreground = ThemePalette.TextMutedBrush
        });

        value.FontSize = 22;
        value.FontWeight = FontWeight.Bold;
        value.Foreground = ThemePalette.TextPrimaryBrush;
        stack.Children.Add(value);

        tile.Child = stack;
        return tile;
    }

    private Control BuildUsersSection()
    {
        var shell = UiFactory.CreateCard();
        shell.Background = ThemePalette.BgSecondaryBrush;

        var stack = new StackPanel
        {
            Spacing = 12
        };

        var headerGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto")
        };
        headerGrid.Children.Add(new TextBlock
        {
            Text = "All Members",
            FontSize = 18,
            FontWeight = FontWeight.Bold,
            Foreground = ThemePalette.TextPrimaryBrush
        });
        headerGrid.Children.Add(new TextBlock
        {
            Text = "Click a member to view their profile",
            FontSize = 12,
            FontWeight = FontWeight.SemiBold,
            Foreground = ThemePalette.TextMutedBrush,
            HorizontalAlignment = HorizontalAlignment.Right
        });
        Grid.SetColumn(headerGrid.Children[1], 1);

        stack.Children.Add(new Border
        {
            Background = Brushes.Transparent,
            Child = headerGrid
        });

        _usersPanel.Spacing = 10;
        stack.Children.Add(_usersPanel);

        shell.Child = stack;
        return shell;
    }

    public async Task LoadAsync()
    {
        await LoadUsersAsync();

        var allUsers = (await _users.GetAllWithStatsAsync()).ToList();
        _usersCount.Text = allUsers.Count.ToString();
        _gamesTotal.Text = allUsers.Sum(u => u.GameCount).ToString();
        _articlesTotal.Text = allUsers.Sum(u => u.ArticleCount).ToString();
        _statsText.Text = $"Loaded {allUsers.Count} members with {_articlesTotal.Text} articles and {_gamesTotal.Text} games.";
    }

    private async Task LoadUsersAsync()
    {
        _usersPanel.Children.Clear();
        var users = (await _users.GetAllWithStatsAsync()).ToList();
        if (users.Count == 0)
        {
            _usersPanel.Children.Add(new TextBlock
            {
                Text = "No users found.",
                Foreground = ThemePalette.TextMutedBrush
            });
            return;
        }

        foreach (var user in users)
        {
            _usersPanel.Children.Add(CreateUserCard(user));
        }
    }

    private Control CreateUserCard(UserWithStats user)
    {
        var card = new Border
        {
            Background = ThemePalette.BgCardBrush,
            BorderBrush = ThemePalette.BorderLightBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(16),
            Padding = new Thickness(16),
            Cursor = new Cursor(StandardCursorType.Hand)
        };

        card.PointerPressed += async (_, __) =>
        {
            await _navigateProfile(user.UserId);
        };

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,2*,Auto"),
            ColumnSpacing = 14
        };

        // Avatar
        var avatar = new Border
        {
            Width = 48,
            Height = 48,
            Background = ThemePalette.AccentBrush,
            CornerRadius = new CornerRadius(14),
            Child = new TextBlock
            {
                Text = user.Username.Length > 0 ? user.Username[..1].ToUpperInvariant() : "?",
                FontSize = 18,
                FontWeight = FontWeight.Bold,
                Foreground = ThemePalette.AccentForegroundBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
        grid.Children.Add(avatar);

        // Info column
        var info = new StackPanel
        {
            Spacing = 4,
            VerticalAlignment = VerticalAlignment.Center
        };

        var nameRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8
        };
        nameRow.Children.Add(new TextBlock
        {
            Text = user.Username,
            FontSize = 13,
            FontWeight = FontWeight.Bold,
            Foreground = ThemePalette.TextPrimaryBrush
        });
        nameRow.Children.Add(BuildRoleBadge(user.RoleName ?? (user.RoleId == 1 ? "admin" : "member")));
        info.Children.Add(nameRow);

        info.Children.Add(new TextBlock
        {
            Text = user.Email,
            FontSize = 11,
            Foreground = ThemePalette.TextSecondaryBrush
        });

        info.Children.Add(new TextBlock
        {
            Text = $"Joined {user.CreatedAt:yyyy-MM-dd}",
            FontSize = 10,
            Foreground = ThemePalette.TextMutedBrush
        });

        grid.Children.Add(info);
        Grid.SetColumn(info, 1);

        // Stats column
        var statsPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 12,
            VerticalAlignment = VerticalAlignment.Center
        };

        statsPanel.Children.Add(CreateStatBadge("🎮", user.GameCount, "games"));
        statsPanel.Children.Add(CreateStatBadge("📄", user.ArticleCount, "articles"));
        statsPanel.Children.Add(CreateStatBadge("💬", user.CommentCount, "comments"));
        statsPanel.Children.Add(CreateStatBadge("👥", user.FriendCount, "friends"));

        grid.Children.Add(statsPanel);
        Grid.SetColumn(statsPanel, 2);

        card.Child = grid;
        return card;
    }

    private static Border BuildRoleBadge(string role)
    {
        return new Border
        {
            Background = ThemePalette.BgSecondaryBrush,
            BorderBrush = ThemePalette.BorderLightBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(10, 4),
            VerticalAlignment = VerticalAlignment.Center,
            Child = new TextBlock
            {
                Text = role,
                FontSize = 10,
                Foreground = ThemePalette.AccentBrush,
                FontWeight = FontWeight.Bold
            }
        };
    }

    private static StackPanel CreateStatBadge(string icon, int count, string label)
    {
        return new StackPanel
        {
            Spacing = 2,
            Children =
            {
                new TextBlock
                {
                    Text = $"{icon} {count}",
                    FontSize = 12,
                    FontWeight = FontWeight.Bold,
                    Foreground = ThemePalette.TextPrimaryBrush
                },
                new TextBlock
                {
                    Text = label,
                    FontSize = 9,
                    Foreground = ThemePalette.TextMutedBrush
                }
            }
        };
    }
}