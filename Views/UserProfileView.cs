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
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using GameWikiApp.Data;
using GameWikiApp.Models;
using GameWikiApp.Services;

namespace GameWikiApp.Views;

public sealed class UserProfileView : UserControl
{
    private readonly int _userId;
    private readonly Func<int, Task> _openChat;
    private readonly Func<Task> _openSettings;
    private readonly Func<int, Task> _openArticle;
    private readonly UserRepository _users = new();
    private readonly FriendService _friends = new();
    private readonly ArticleService _articles = new();
    private readonly SavedArticleService _savedArticles = new();

    private readonly Border _avatarHost = new();
    private readonly TextBlock _nameText = new();
    private readonly TextBlock _roleText = new();
    private readonly TextBlock _statusText = new();
    private readonly TextBlock _bioText = new();
    private readonly TextBlock _detailsText = new();
    private readonly TextBlock _connectionsValue = new();
    private readonly TextBlock _joinedValue = new();
    private readonly TextBlock _actionHint = new();
    private readonly Border _articlesCard = new();
    private readonly TextBlock _articlesTitle = new();
    private readonly TextBlock _articlesSubtitle = new();
    private readonly Button _savedTabButton = new();
    private readonly Button _myTabButton = new();
    private readonly WrapPanel _savedArticlesWrap = new();
    private readonly WrapPanel _myArticlesWrap = new();
    private readonly Button _primaryButton = new();
    private readonly Button _secondaryButton = new();

    private User? _profile;
    private bool _isSelf;
    private bool _isFriend;
    private Friend? _incomingRequest;
    private Friend? _outgoingRequest;
    private bool _loadedOnce;
    private ProfileArticlesTab _articlesTab = ProfileArticlesTab.MyArticles;

    private enum ProfileArticlesTab
    {
        Saved,
        MyArticles
    }

    public UserProfileView(int userId, Func<int, Task> openChat, Func<Task> openSettings, Func<int, Task> openArticle)
    {
        _userId = userId;
        _openChat = openChat;
        _openSettings = openSettings;
        _openArticle = openArticle;

        Content = BuildLayout();
        Loaded += async (_, __) =>
        {
            if (_loadedOnce)
            {
                return;
            }

            _loadedOnce = true;
            await LoadAsync();
        };
    }

    public async Task LoadAsync()
    {
        if (AppState.CurrentUser == null)
        {
            ShowMessage("Profile unavailable", "You need to sign in first.");
            return;
        }

        _profile = await _users.GetByIdAsync(_userId);
        if (_profile == null)
        {
            ShowMessage("Profile not found", "The selected user could not be loaded.");
            return;
        }

        _isSelf = AppState.CurrentUser.UserId == _profile.UserId;
        _isFriend = false;
        _incomingRequest = null;
        _outgoingRequest = null;

        if (!_isSelf)
        {
            _isFriend = await _friends.AreFriendsAsync(AppState.CurrentUser.UserId, _profile.UserId);
            if (!_isFriend)
            {
                _incomingRequest = (await _friends.GetIncomingRequestsAsync(AppState.CurrentUser.UserId))
                    .FirstOrDefault(x => x.OtherUserId == _profile.UserId);
                _outgoingRequest = (await _friends.GetOutgoingRequestsAsync(AppState.CurrentUser.UserId))
                    .FirstOrDefault(x => x.OtherUserId == _profile.UserId);
            }
        }

        var connections = (await _friends.GetFriendsAsync(_profile.UserId)).Count();

        _nameText.Text = _profile.Username;
        _roleText.Text = _isSelf
            ? "Your profile"
            : (_profile.RoleId == 1 ? "Administrator" : "Member");
        _statusText.Text = BuildPresenceText(_profile.IsOnline, _profile.LastSeen);
        _bioText.Text = string.IsNullOrWhiteSpace(_profile.Bio)
            ? "No bio added yet."
            : _profile.Bio!;

        _detailsText.Text = string.Join(Environment.NewLine, new[]
        {
            $"Role: {_profile.RoleName ?? (_profile.RoleId == 1 ? "Administrator" : "Member")}",
            $"Joined: {_profile.CreatedAt:g}",
            _isSelf ? $"Email: {_profile.Email}" : $"Status: {(_profile.IsOnline ? "Online now" : "Offline")}"
        });

        _connectionsValue.Text = connections.ToString();
        _joinedValue.Text = _profile.CreatedAt.ToString("dd MMM yyyy");

        await RefreshAvatarAsync();
        UpdateActions();
        await RefreshArticlesAsync();
    }

    private Control BuildLayout()
    {
        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Background = ThemePalette.BgPrimaryBrush
        };

        var stack = new StackPanel
        {
            Spacing = 16,
            Margin = new Thickness(16)
        };

        stack.Children.Add(BuildHero());
        stack.Children.Add(BuildStatsPanel());
        stack.Children.Add(BuildDetailsCard());
        stack.Children.Add(BuildArticlesCard());

        scroll.Content = stack;
        return scroll;
    }

    private Control BuildHero()
    {
        var shell = new Border
        {
            Background = ThemePalette.SurfaceBrush,
            BorderBrush = ThemePalette.BorderLightBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(24),
            Padding = new Thickness(24),
            ClipToBounds = true
        };

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*"),
            ColumnSpacing = 18
        };

        _avatarHost.Width = 112;
        _avatarHost.Height = 112;
        _avatarHost.Background = ThemePalette.AccentBrush;
        _avatarHost.BorderBrush = ThemePalette.BorderLightBrush;
        _avatarHost.BorderThickness = new Thickness(1);
        _avatarHost.CornerRadius = new CornerRadius(32);
        _avatarHost.ClipToBounds = true;
        _avatarHost.Child = new TextBlock
        {
            Text = GetInitials("U"),
            FontSize = 30,
            FontWeight = FontWeight.Bold,
            Foreground = ThemePalette.AccentForegroundBrush,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        grid.Children.Add(_avatarHost);

        var stack = new StackPanel
        {
            Spacing = 8,
            VerticalAlignment = VerticalAlignment.Center
        };

        var badgeRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8
        };

        badgeRow.Children.Add(BuildBadge("NX PROFILE", ThemePalette.AccentBrush, ThemePalette.AccentForegroundBrush));
        stack.Children.Add(badgeRow);

        _nameText.FontSize = 30;
        _nameText.FontWeight = FontWeight.Bold;
        _nameText.Foreground = ThemePalette.TextPrimaryBrush;
        _nameText.TextWrapping = TextWrapping.NoWrap;
        _nameText.TextTrimming = TextTrimming.CharacterEllipsis;
        stack.Children.Add(_nameText);

        _roleText.FontSize = 12;
        _roleText.FontWeight = FontWeight.SemiBold;
        _roleText.Foreground = ThemePalette.AccentBrush;
        stack.Children.Add(_roleText);

        _statusText.FontSize = 13;
        _statusText.Foreground = ThemePalette.TextSecondaryBrush;
        _statusText.TextWrapping = TextWrapping.Wrap;
        stack.Children.Add(_statusText);

        var actionRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            Margin = new Thickness(0, 12, 0, 0)
        };

        _primaryButton.Content = UiFactory.CreateButtonContent("...", null);
        _primaryButton.Background = ThemePalette.AccentBrush;
        _primaryButton.Foreground = ThemePalette.AccentForegroundBrush;
        _primaryButton.BorderBrush = Brushes.Transparent;
        _primaryButton.BorderThickness = new Thickness(1);
        _primaryButton.CornerRadius = new CornerRadius(14);
        _primaryButton.Padding = new Thickness(16, 11);
        _primaryButton.MinHeight = 44;
        _primaryButton.IsVisible = false;
        _primaryButton.Click += async (_, __) => await HandlePrimaryActionAsync();
        actionRow.Children.Add(_primaryButton);

        _secondaryButton.Content = UiFactory.CreateButtonContent("...", null);
        _secondaryButton.Background = ThemePalette.BgTertiaryBrush;
        _secondaryButton.BorderBrush = ThemePalette.BorderLightBrush;
        _secondaryButton.BorderThickness = new Thickness(1);
        _secondaryButton.Foreground = ThemePalette.TextPrimaryBrush;
        _secondaryButton.CornerRadius = new CornerRadius(14);
        _secondaryButton.Padding = new Thickness(16, 11);
        _secondaryButton.MinHeight = 44;
        _secondaryButton.IsVisible = false;
        _secondaryButton.Click += async (_, __) => await HandleSecondaryActionAsync();
        actionRow.Children.Add(_secondaryButton);

        _actionHint.FontSize = 11;
        _actionHint.Foreground = ThemePalette.TextMutedBrush;
        _actionHint.TextWrapping = TextWrapping.Wrap;
        _actionHint.Margin = new Thickness(0, 4, 0, 0);
        stack.Children.Add(actionRow);
        stack.Children.Add(_actionHint);

        grid.Children.Add(stack);
        Grid.SetColumn(stack, 1);

        shell.Child = grid;
        return shell;
    }

    private Control BuildStatsPanel()
    {
        var panel = new WrapPanel
        {
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        panel.Children.Add(BuildStatCard("Connections", _connectionsValue, "Friends across the platform"));
        panel.Children.Add(BuildStatCard("Joined", _joinedValue, "Member since"));
        return panel;
    }

    private Control BuildDetailsCard()
    {
        var shell = new Border
        {
            Background = ThemePalette.BgSecondaryBrush,
            BorderBrush = ThemePalette.BorderLightBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(22),
            Padding = new Thickness(22),
            ClipToBounds = true
        };

        var stack = new StackPanel
        {
            Spacing = 12
        };

        stack.Children.Add(UiFactory.CreateSectionHeader("About", "Profile details and account info"));
        _detailsText.FontSize = 12;
        _detailsText.Foreground = ThemePalette.TextSecondaryBrush;
        _detailsText.TextWrapping = TextWrapping.Wrap;
        stack.Children.Add(_detailsText);

        stack.Children.Add(new Border
        {
            Background = ThemePalette.BorderLightBrush,
            Height = 1,
            Margin = new Thickness(0, 4, 0, 2)
        });

        stack.Children.Add(new TextBlock
        {
            Text = "Bio",
            FontSize = 13,
            FontWeight = FontWeight.Bold,
            Foreground = ThemePalette.TextPrimaryBrush
        });

        _bioText.FontSize = 13;
        _bioText.Foreground = ThemePalette.TextSecondaryBrush;
        _bioText.TextWrapping = TextWrapping.Wrap;
        stack.Children.Add(_bioText);

        shell.Child = stack;
        return shell;
    }

    private Control BuildArticlesCard()
    {
        _articlesCard.Background = ThemePalette.BgSecondaryBrush;
        _articlesCard.BorderBrush = ThemePalette.BorderLightBrush;
        _articlesCard.BorderThickness = new Thickness(1);
        _articlesCard.CornerRadius = new CornerRadius(22);
        _articlesCard.Padding = new Thickness(22);

        _articlesTitle.FontSize = 20;
        _articlesTitle.FontWeight = FontWeight.Bold;
        _articlesTitle.Foreground = ThemePalette.TextPrimaryBrush;
        _articlesTitle.Text = "Articles";

        _articlesSubtitle.FontSize = 12;
        _articlesSubtitle.Foreground = ThemePalette.TextSecondaryBrush;
        _articlesSubtitle.TextWrapping = TextWrapping.Wrap;
        _articlesSubtitle.Text = "Loading saved articles and your latest work...";

        _savedTabButton.Content = UiFactory.CreateButtonContent("Saved Articles", char.ConvertFromUtf32(0x1F516), 8);
        _savedTabButton.Background = ThemePalette.BgTertiaryBrush;
        _savedTabButton.BorderBrush = ThemePalette.BorderLightBrush;
        _savedTabButton.BorderThickness = new Thickness(1);
        _savedTabButton.Foreground = ThemePalette.TextPrimaryBrush;
        _savedTabButton.CornerRadius = new CornerRadius(999);
        _savedTabButton.Padding = new Thickness(14, 9);
        _savedTabButton.MinHeight = 38;
        _savedTabButton.IsVisible = false;
        _savedTabButton.Click += (_, __) =>
        {
            _articlesTab = ProfileArticlesTab.Saved;
            ApplyArticlesTab();
        };

        _myTabButton.Content = UiFactory.CreateButtonContent("My Articles", char.ConvertFromUtf32(0x270F), 8);
        _myTabButton.Background = ThemePalette.AccentBrush;
        _myTabButton.BorderBrush = ThemePalette.BorderLightBrush;
        _myTabButton.BorderThickness = new Thickness(1);
        _myTabButton.Foreground = ThemePalette.AccentForegroundBrush;
        _myTabButton.CornerRadius = new CornerRadius(999);
        _myTabButton.Padding = new Thickness(14, 9);
        _myTabButton.MinHeight = 38;
        _myTabButton.IsVisible = false;
        _myTabButton.Click += (_, __) =>
        {
            _articlesTab = ProfileArticlesTab.MyArticles;
            ApplyArticlesTab();
        };

        _savedArticlesWrap.Orientation = Orientation.Horizontal;
        _savedArticlesWrap.ItemWidth = 280;
        _savedArticlesWrap.ItemHeight = 320;
        _savedArticlesWrap.HorizontalAlignment = HorizontalAlignment.Stretch;
        _savedArticlesWrap.Margin = new Thickness(0, 0, 0, 8);

        _myArticlesWrap.Orientation = Orientation.Horizontal;
        _myArticlesWrap.ItemWidth = 280;
        _myArticlesWrap.ItemHeight = 320;
        _myArticlesWrap.HorizontalAlignment = HorizontalAlignment.Stretch;
        _myArticlesWrap.Margin = new Thickness(0, 0, 0, 8);

        var stack = new StackPanel
        {
            Spacing = 16
        };

        var header = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            ColumnSpacing = 12
        };

        var titleStack = new StackPanel
        {
            Spacing = 4
        };
        titleStack.Children.Add(_articlesTitle);
        titleStack.Children.Add(_articlesSubtitle);
        header.Children.Add(titleStack);

        var tabRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8
        };
        tabRow.Children.Add(_savedTabButton);
        tabRow.Children.Add(_myTabButton);
        header.Children.Add(tabRow);
        Grid.SetColumn(tabRow, 1);

        stack.Children.Add(header);
        stack.Children.Add(_savedArticlesWrap);
        stack.Children.Add(_myArticlesWrap);

        _articlesCard.Child = stack;
        return _articlesCard;
    }

    private async Task RefreshArticlesAsync()
    {
        try
        {
            if (_profile == null)
            {
                _articlesCard.Child = BuildArticlesPlaceholder("Articles unavailable", "The selected profile could not be loaded.");
                return;
            }

            if (_isSelf)
            {
                var savedTask = _savedArticles.GetByUserIdAsync(_profile.UserId);
                var authoredTask = _articles.GetByAuthorIdAsync(_profile.UserId, false);
                await Task.WhenAll(savedTask, authoredTask);

                var savedArticles = savedTask.Result.ToList();
                var myArticles = authoredTask.Result.ToList();

                _articlesTitle.Text = "Saved Articles & My Articles";
                _articlesSubtitle.Text = "Switch between the articles you've saved and the ones you've written.";
                _savedTabButton.IsVisible = true;
                _myTabButton.IsVisible = true;
                _savedTabButton.Content = UiFactory.CreateButtonContent($"Saved Articles ({savedArticles.Count})", char.ConvertFromUtf32(0x1F516), 8);
                _myTabButton.Content = UiFactory.CreateButtonContent($"My Articles ({myArticles.Count})", char.ConvertFromUtf32(0x270F), 8);

                await PopulateArticleWrapAsync(
                    _savedArticlesWrap,
                    savedArticles,
                    false,
                    "You have not saved any articles yet.");

                await PopulateArticleWrapAsync(
                    _myArticlesWrap,
                    myArticles,
                    true,
                    "You have not written any articles yet.");

                if (_articlesTab == ProfileArticlesTab.MyArticles && myArticles.Count == 0 && savedArticles.Count > 0)
                {
                    _articlesTab = ProfileArticlesTab.Saved;
                }

                if (_articlesTab == ProfileArticlesTab.Saved && savedArticles.Count == 0 && myArticles.Count > 0)
                {
                    _articlesTab = ProfileArticlesTab.MyArticles;
                }

                ApplyArticlesTab();
                return;
            }

            var publishedArticles = (await _articles.GetByAuthorIdAsync(_profile.UserId, true)).ToList();
            _articlesTitle.Text = $"{_profile.Username}'s Articles";
            _articlesSubtitle.Text = "Public articles created by this user.";
            _savedTabButton.IsVisible = false;
            _myTabButton.IsVisible = false;
            _savedArticlesWrap.Children.Clear();
            _savedArticlesWrap.IsVisible = false;

            await PopulateArticleWrapAsync(
                _myArticlesWrap,
                publishedArticles,
                false,
                "This profile has not published any articles yet.");

            _myArticlesWrap.IsVisible = true;
            ApplyArticlesTab();
        }
        catch
        {
            _savedTabButton.IsVisible = false;
            _myTabButton.IsVisible = false;
            _savedArticlesWrap.Children.Clear();
            _myArticlesWrap.Children.Clear();
            _articlesCard.Child = BuildArticlesPlaceholder(
                "Articles unavailable",
                "The article lists could not be loaded right now.");
        }
    }

    private void ApplyArticlesTab()
    {
        if (!_isSelf)
        {
            _savedArticlesWrap.IsVisible = false;
            _myArticlesWrap.IsVisible = true;
            _savedTabButton.Background = ThemePalette.BgTertiaryBrush;
            _myTabButton.Background = ThemePalette.AccentBrush;
            _savedTabButton.Foreground = ThemePalette.TextPrimaryBrush;
            _myTabButton.Foreground = ThemePalette.AccentForegroundBrush;
            return;
        }

        var showSaved = _articlesTab == ProfileArticlesTab.Saved;
        _savedArticlesWrap.IsVisible = showSaved;
        _myArticlesWrap.IsVisible = !showSaved;

        _savedTabButton.Background = showSaved ? ThemePalette.AccentBrush : ThemePalette.BgTertiaryBrush;
        _savedTabButton.Foreground = showSaved ? ThemePalette.AccentForegroundBrush : ThemePalette.TextPrimaryBrush;
        _myTabButton.Background = showSaved ? ThemePalette.BgTertiaryBrush : ThemePalette.AccentBrush;
        _myTabButton.Foreground = showSaved ? ThemePalette.TextPrimaryBrush : ThemePalette.AccentForegroundBrush;
    }

    private async Task PopulateArticleWrapAsync(
        WrapPanel wrapPanel,
        IReadOnlyCollection<WikiArticle> articles,
        bool editable,
        string emptyMessage)
    {
        wrapPanel.Children.Clear();

        var articleList = articles.ToList();
        if (articleList.Count == 0)
        {
            wrapPanel.Children.Add(BuildArticlesPlaceholder("Nothing here yet", emptyMessage));
            return;
        }

        var bitmaps = await Task.WhenAll(articleList.Select(article => ImageLoader.LoadAsync(article.CoverImage)));
        for (var i = 0; i < articleList.Count; i++)
        {
            wrapPanel.Children.Add(BuildArticleCard(articleList[i], bitmaps[i], editable));
        }
    }

    private Control BuildArticleCard(WikiArticle article, Avalonia.Media.Imaging.Bitmap? bitmap, bool editable)
    {
        var card = new Border
        {
            Width = 280,
            Height = 320,
            Background = ThemePalette.BgCardBrush,
            BorderBrush = ThemePalette.BorderLightBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(18),
            Padding = new Thickness(14),
            Margin = new Thickness(0, 0, 14, 14),
            Cursor = new Cursor(StandardCursorType.Hand),
            ClipToBounds = true
        };

        var stack = new StackPanel
        {
            Spacing = 8
        };

        stack.Children.Add(UiFactory.CreateMediaFrame(bitmap, article.GameTitle ?? article.Title, 252, 132, true, 14));

        stack.Children.Add(new TextBlock
        {
            Text = article.Title,
            FontSize = 13,
            FontWeight = FontWeight.Bold,
            Foreground = ThemePalette.TextPrimaryBrush,
            TextWrapping = TextWrapping.Wrap,
            MaxLines = 2
        });

        var categoryRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 6
        };

        categoryRow.Children.Add(new Border
        {
            Background = ThemePalette.AccentDimBrush,
            CornerRadius = new CornerRadius(999),
            Padding = new Thickness(10, 4),
            Child = new TextBlock
            {
                Text = article.CategoryLabel,
                FontSize = 10,
                FontWeight = FontWeight.Bold,
                Foreground = ThemePalette.AccentForegroundBrush
            }
        });

        stack.Children.Add(categoryRow);

        stack.Children.Add(new TextBlock
        {
            Text = article.GameTitle ?? "Unknown game",
            FontSize = 10.5,
            Foreground = ThemePalette.AccentBrush,
            TextWrapping = TextWrapping.Wrap
        });

        stack.Children.Add(new TextBlock
        {
            Text = $"{article.ViewsCount} views",
            FontSize = 10,
            Foreground = ThemePalette.TextMutedBrush
        });

        if (editable)
        {
            var editButton = UiFactory.CreateSubtleButton("Quick edit", 118, char.ConvertFromUtf32(0x270F));
            editButton.Height = 38;
            editButton.Click += async (_, __) => await OpenArticleEditorAsync(article.ArticleId);
            stack.Children.Add(editButton);
        }

        card.Child = stack;
        card.PointerEntered += (_, __) => card.Background = ThemePalette.BgTertiaryBrush;
        card.PointerExited += (_, __) => card.Background = ThemePalette.BgCardBrush;
        card.PointerPressed += async (_, __) => await OpenArticleAsync(article.ArticleId);
        return card;
    }

    private static Control BuildArticlesPlaceholder(string title, string message)
    {
        return new Border
        {
            Background = ThemePalette.BgSecondaryBrush,
            BorderBrush = ThemePalette.BorderLightBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(16),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 14, 14),
            Child = new StackPanel
            {
                Spacing = 4,
                Children =
                {
                    new TextBlock
                    {
                        Text = title,
                        FontSize = 13,
                        FontWeight = FontWeight.Bold,
                        Foreground = ThemePalette.TextPrimaryBrush
                    },
                    new TextBlock
                    {
                        Text = message,
                        FontSize = 11,
                        Foreground = ThemePalette.TextMutedBrush,
                        TextWrapping = TextWrapping.Wrap
                    }
                }
            }
        };
    }

    private async Task OpenArticleAsync(int articleId)
    {
        await _openArticle(articleId);
    }

    private async Task OpenArticleEditorAsync(int articleId)
    {
        if (AppState.CurrentUser == null)
        {
            return;
        }

        var owner = TopLevel.GetTopLevel(this) as Window;
        if (owner == null)
        {
            return;
        }

        var editor = new ArticleEditorWindow(articleId);
        await editor.ShowDialog<bool>(owner);
        await LoadAsync();
    }

    private static Control BuildBadge(string text, IBrush background, IBrush foreground)
    {
        return new Border
        {
            Background = background,
            CornerRadius = new CornerRadius(999),
            Padding = new Thickness(10, 4),
            Child = new TextBlock
            {
                Text = text,
                FontSize = 10,
                FontWeight = FontWeight.Bold,
                Foreground = foreground,
                TextWrapping = TextWrapping.NoWrap
            }
        };
    }

    private static Control BuildStatCard(string title, TextBlock value, string caption)
    {
        var card = new Border
        {
            Width = 220,
            Background = ThemePalette.BgSecondaryBrush,
            BorderBrush = ThemePalette.BorderLightBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(18),
            Padding = new Thickness(16),
            Margin = new Thickness(0, 0, 14, 14),
            ClipToBounds = true
        };

        var stack = new StackPanel
        {
            Spacing = 4
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

        stack.Children.Add(new TextBlock
        {
            Text = caption,
            FontSize = 11,
            Foreground = ThemePalette.TextMutedBrush,
            TextWrapping = TextWrapping.Wrap
        });

        card.Child = stack;
        return card;
    }

    private void UpdateActions()
    {
        if (_profile == null)
        {
            _primaryButton.IsVisible = false;
            _secondaryButton.IsVisible = false;
            _actionHint.Text = string.Empty;
            return;
        }

        _primaryButton.IsVisible = true;
        _secondaryButton.IsVisible = true;

        if (_isSelf)
        {
            _primaryButton.Content = UiFactory.CreateButtonContent("Edit profile", "✎");
            _primaryButton.IsEnabled = true;
            _secondaryButton.IsVisible = false;
            _actionHint.Text = "Settings stay in the sidebar. This card is just the profile view.";
            return;
        }

        if (_isFriend)
        {
            _primaryButton.Content = UiFactory.CreateButtonContent("Message", "✉");
            _primaryButton.IsEnabled = true;
            _secondaryButton.Content = UiFactory.CreateButtonContent("Remove friend", "−");
            _secondaryButton.IsEnabled = true;
            _actionHint.Text = "You are connected and can start a private chat.";
            return;
        }

        if (_incomingRequest != null)
        {
            _primaryButton.Content = UiFactory.CreateButtonContent("Accept request", "✓");
            _primaryButton.IsEnabled = true;
            _secondaryButton.Content = UiFactory.CreateButtonContent("Decline", "×");
            _secondaryButton.IsEnabled = true;
            _actionHint.Text = "This user already sent you a request.";
            return;
        }

        if (_outgoingRequest != null)
        {
            _primaryButton.Content = UiFactory.CreateButtonContent("Request sent", "•");
            _primaryButton.IsEnabled = false;
            _secondaryButton.IsVisible = false;
            _actionHint.Text = "Your friend request is pending.";
            return;
        }

        _primaryButton.Content = UiFactory.CreateButtonContent("Send request", "＋");
        _primaryButton.IsEnabled = true;
        _secondaryButton.IsVisible = false;
        _actionHint.Text = "Not connected yet.";
    }

    private async Task HandlePrimaryActionAsync()
    {
        if (_profile == null || AppState.CurrentUser == null)
        {
            return;
        }

        if (_isSelf)
        {
            await _openSettings();
            return;
        }

        if (_isFriend)
        {
            await _openChat(_profile.UserId);
            return;
        }

        if (_incomingRequest != null)
        {
            var result = await _friends.AcceptRequestAsync(AppState.CurrentUser.UserId, _incomingRequest.FriendshipId);
            _actionHint.Text = result.message;
            if (result.success)
            {
                await LoadAsync();
            }

            return;
        }

        if (_outgoingRequest != null)
        {
            return;
        }

        var send = await _friends.SendRequestAsync(AppState.CurrentUser.UserId, _profile.UserId);
        _actionHint.Text = send.message;
        if (send.success)
        {
            await LoadAsync();
        }
    }

    private async Task HandleSecondaryActionAsync()
    {
        if (_profile == null || AppState.CurrentUser == null)
        {
            return;
        }

        if (_isFriend)
        {
            var result = await _friends.RemoveFriendAsync(AppState.CurrentUser.UserId, _profile.UserId);
            _actionHint.Text = result.message;
            if (result.success)
            {
                await LoadAsync();
            }
            return;
        }

        if (_incomingRequest != null)
        {
            var result = await _friends.DeclineRequestAsync(AppState.CurrentUser.UserId, _incomingRequest.FriendshipId);
            _actionHint.Text = result.message;
            if (result.success)
            {
                await LoadAsync();
            }
        }
    }

    private async Task RefreshAvatarAsync()
    {
        if (_profile == null)
        {
            return;
        }

        try
        {
            if (!string.IsNullOrWhiteSpace(_profile.ProfileImage))
            {
                var bitmap = await ImageLoader.LoadAsync(_profile.ProfileImage);
                if (bitmap != null)
                {
                    _avatarHost.Background = ThemePalette.BgTertiaryBrush;
                    _avatarHost.Child = new Image
                    {
                        Source = bitmap,
                        Stretch = Stretch.UniformToFill,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    return;
                }
            }
        }
        catch
        {
            // Fall back to initials below.
        }

        _avatarHost.Background = ThemePalette.AccentBrush;
        _avatarHost.Child = new TextBlock
        {
            Text = GetInitials(_profile.Username),
            FontSize = 30,
            FontWeight = FontWeight.Bold,
            Foreground = ThemePalette.AccentForegroundBrush,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
    }

    private void ShowGuestState()
    {
        _statusText.Foreground = ThemePalette.WarningBrush;
        _statusText.Text = "You need to sign in to edit your profile.";
        _savedTabButton.IsVisible = false;
        _myTabButton.IsVisible = false;
        _savedArticlesWrap.Children.Clear();
        _myArticlesWrap.Children.Clear();
        _articlesCard.Child = BuildArticlesPlaceholder(
            "Articles unavailable",
            "Sign in to view saved and authored articles.");
    }

    private void ShowMessage(string title, string message)
    {
        _nameText.Text = title;
        _roleText.Text = string.Empty;
        _statusText.Text = message;
        _bioText.Text = string.Empty;
        _detailsText.Text = string.Empty;
        _connectionsValue.Text = "0";
        _joinedValue.Text = "-";
        _primaryButton.IsVisible = false;
        _secondaryButton.IsVisible = false;
        _actionHint.Text = string.Empty;
        _savedTabButton.IsVisible = false;
        _myTabButton.IsVisible = false;
        _savedArticlesWrap.Children.Clear();
        _myArticlesWrap.Children.Clear();
        _articlesCard.Child = BuildArticlesPlaceholder(title, message);
    }

    private static string GetInitials(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "U";
        }

        var parts = value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length == 1)
        {
            return parts[0].Length > 1 ? parts[0][..1].ToUpperInvariant() : parts[0].ToUpperInvariant();
        }

        return string.Concat(parts.Take(2).Select(x => char.ToUpperInvariant(x[0])));
    }

    private static string BuildPresenceText(bool isOnline, DateTime? lastSeen)
    {
        if (isOnline)
        {
            return "Online now";
        }

        if (!lastSeen.HasValue)
        {
            return "Offline";
        }

        var diff = DateTime.UtcNow - lastSeen.Value.ToUniversalTime();
        if (diff.TotalMinutes < 1)
        {
            return "Last seen just now";
        }

        if (diff.TotalHours < 1)
        {
            return $"Last seen {Math.Max(1, (int)diff.TotalMinutes)} min ago";
        }

        if (diff.TotalDays < 1)
        {
            return $"Last seen {Math.Max(1, (int)diff.TotalHours)} h ago";
        }

        return $"Last seen {lastSeen.Value.ToLocalTime():dd MMM HH:mm}";
    }
}
