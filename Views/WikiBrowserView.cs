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
using GameWikiApp.Models;
using GameWikiApp.Services;

namespace GameWikiApp.Views;

public sealed class WikiBrowserView : UserControl
{
    private readonly GameService _games = new();
    private readonly ArticleService _articles = new();
    private readonly TagService _tags = new();
    private readonly Action<int> _openGame;
    private readonly Action<int> _openArticle;

    private readonly StackPanel _content = new();
    private readonly WrapPanel _gamesPanel = new();
    private readonly WrapPanel _articlesPanel = new();
    private readonly TextBlock _heroTitle = new();
    private readonly TextBlock _heroSubtitle = new();
    private readonly TextBox _searchBox;
    private readonly ComboBox _genreBox;
    private readonly List<GameTag> _genreList = new();
    private bool _suppressFilterEvents;

    private string _activeQuery = string.Empty;
    private int? _activeGenreId;
    private string? _activeGenreName;

    public WikiBrowserView(Action<int> openGame, Action<int> openArticle)
    {
        _openGame = openGame;
        _openArticle = openArticle;

        _searchBox = UiFactory.CreateTextBox("Search games and articles...", 320);
        _genreBox = new ComboBox
        {
            Width = 240,
            Background = ThemePalette.BgInputBrush,
            BorderBrush = ThemePalette.BorderBrush,
            Foreground = ThemePalette.TextPrimaryBrush,
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(12, 10)
        };
        _genreBox.DisplayMemberBinding = new Avalonia.Data.Binding(nameof(GameTag.TagName));

        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = _content,
            Background = ThemePalette.BgPrimaryBrush
        };

        _content.Spacing = 30;
        _content.Margin = new Thickness(24, 0, 24, 28);
        _content.Children.Add(BuildHeroShell());
        _content.Children.Add(UiFactory.CreateSectionHeader("Games"));
        _content.Children.Add(_gamesPanel);
        _content.Children.Add(UiFactory.CreateSectionHeader("Articles"));
        _content.Children.Add(_articlesPanel);

        _gamesPanel.Orientation = Orientation.Horizontal;
        _gamesPanel.ItemWidth = 260;
        _gamesPanel.ItemHeight = 270;
        _gamesPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
        _gamesPanel.Margin = new Thickness(0, 0, 0, 36);

        _articlesPanel.Orientation = Orientation.Horizontal;
        _articlesPanel.ItemWidth = 260;
        _articlesPanel.ItemHeight = 200;
        _articlesPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
        _articlesPanel.Margin = new Thickness(0, 0, 0, 36);

        _searchBox.KeyDown += async (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                await LoadAsync(_searchBox.Text ?? string.Empty, _activeGenreId, _activeGenreName);
            }
        };

        _genreBox.SelectionChanged += async (_, __) =>
        {
            if (_suppressFilterEvents)
            {
                return;
            }

            var selected = _genreBox.SelectedItem as GameTag;
            _activeGenreId = selected?.TagId;
            _activeGenreName = selected?.TagName;
            await LoadAsync(_searchBox.Text ?? string.Empty, _activeGenreId, _activeGenreName);
        };

        Content = scroll;
    }

    private Control BuildHeroShell()
    {
        var hero = new Border
        {
            Background = ThemePalette.SurfaceBrush,
            BorderBrush = ThemePalette.BorderLightBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(20),
            Padding = new Thickness(24),
            Margin = new Thickness(0, 0, 0, 24)
        };

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*")
        };

        var left = new StackPanel
        {
            Spacing = 10
        };

        left.Children.Add(new TextBlock
        {
            Text = "WIKI BROWSER",
            FontSize = 11,
            FontWeight = FontWeight.Bold,
            Foreground = ThemePalette.AccentBrush
        });

        _heroTitle.Text = "All wiki pages";
        _heroTitle.FontSize = 30;
        _heroTitle.FontWeight = FontWeight.Bold;
        _heroTitle.Foreground = ThemePalette.TextPrimaryBrush;

        _heroSubtitle.Text = "Search by title, game, or genre and jump directly into a page.";
        _heroSubtitle.FontSize = 13;
        _heroSubtitle.Foreground = ThemePalette.TextSecondaryBrush;
        _heroSubtitle.TextWrapping = TextWrapping.Wrap;
        _heroSubtitle.MaxWidth = 700;

        left.Children.Add(_heroTitle);
        left.Children.Add(_heroSubtitle);

        var filters = new WrapPanel
        {
            Margin = new Thickness(0, 14, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        _searchBox.Margin = new Thickness(0, 0, 10, 10);
        _genreBox.Margin = new Thickness(0, 0, 10, 10);

        var searchButton = UiFactory.CreatePrimaryButton("Search", 90);
        searchButton.Click += async (_, __) => await LoadAsync(_searchBox.Text ?? string.Empty, _activeGenreId, _activeGenreName);
        searchButton.Margin = new Thickness(0, 0, 10, 10);

        var clearButton = UiFactory.CreateSubtleButton("Clear", 90);
        clearButton.Click += async (_, __) =>
        {
            _suppressFilterEvents = true;
            _searchBox.Text = string.Empty;
            _genreBox.SelectedIndex = 0;
            _suppressFilterEvents = false;
            await LoadAsync(string.Empty, null, null);
        };
        clearButton.Margin = new Thickness(0, 0, 10, 10);

        filters.Children.Add(_searchBox);
        filters.Children.Add(_genreBox);
        filters.Children.Add(searchButton);
        filters.Children.Add(clearButton);

        left.Children.Add(filters);
        grid.Children.Add(left);

        hero.Child = grid;
        return hero;
    }

    public async Task LoadAsync(string query, int? genreId = null, string? genreName = null)
    {
        _activeQuery = query?.Trim() ?? string.Empty;
        _activeGenreId = genreId;
        _activeGenreName = genreName;

        var genres = (await _tags.GetAllAsync()).ToList();
        _genreList.Clear();
        _genreList.Add(new GameTag { TagId = 0, TagName = "All genres" });
        _genreList.AddRange(genres);
        _suppressFilterEvents = true;
        _genreBox.ItemsSource = _genreList;
        if (_activeGenreId.HasValue)
        {
            _genreBox.SelectedItem = _genreList.FirstOrDefault(x => x.TagId == _activeGenreId.Value) ?? _genreList[0];
        }
        else
        {
            _genreBox.SelectedIndex = 0;
        }
        _suppressFilterEvents = false;

        var search = _activeQuery;
        var selectedGenre = _genreBox.SelectedItem as GameTag;
        var selectedGenreId = selectedGenre?.TagId ?? 0;
        var selectedGenreLabel = selectedGenreId > 0
            ? selectedGenre?.TagName
            : _activeGenreName;
        _activeGenreName = selectedGenreLabel;

        _heroTitle.Text = string.IsNullOrWhiteSpace(search)
            ? string.IsNullOrWhiteSpace(selectedGenreLabel)
                ? "All wiki pages"
                : selectedGenreLabel!
            : $"Results for \"{search}\"";

        _heroSubtitle.Text = string.IsNullOrWhiteSpace(search)
            ? "Search by title, game, or genre and jump directly into a page."
            : $"Showing matches for \"{search}\".";

        var games = await LoadGamesAsync(search, selectedGenre);
        var articles = await LoadArticlesAsync(search, selectedGenreId);

        _gamesPanel.Children.Clear();
        if (games.Count == 0)
        {
            _gamesPanel.Children.Add(BuildEmptyState("No games found."));
        }
        else
        {
            foreach (var game in games)
            {
                var bitmap = await ImageLoader.LoadAsync(game.CoverImage);
                _gamesPanel.Children.Add(CreateGameCard(game, bitmap));
            }
        }

        _articlesPanel.Children.Clear();
        if (articles.Count == 0)
        {
            _articlesPanel.Children.Add(BuildEmptyState("No articles found."));
        }
        else
        {
            foreach (var article in articles)
            {
                var bitmap = await ImageLoader.LoadAsync(article.CoverImage);
                _articlesPanel.Children.Add(CreateArticleCard(article, bitmap));
            }
        }
    }

    private async Task<List<Game>> LoadGamesAsync(string query, GameTag? selectedGenre)
    {
        IEnumerable<Game> games;
        if (selectedGenre != null && selectedGenre.TagId > 0)
        {
            games = await _tags.GetGamesByTagNameAsync(selectedGenre.TagName);
        }
        else if (!string.IsNullOrWhiteSpace(query))
        {
            games = await _games.SearchAsync(query);
        }
        else
        {
            games = await _games.GetPopularAsync(24);
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            games = games.Where(game =>
                game.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                (game.ShortDescription ?? string.Empty).Contains(query, StringComparison.OrdinalIgnoreCase) ||
                (game.FullDescription ?? string.Empty).Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        return games.GroupBy(g => g.GameId).Select(g => g.First()).ToList();
    }

    private async Task<List<WikiArticle>> LoadArticlesAsync(string query, int selectedGenreId)
    {
        IEnumerable<WikiArticle> articles;
        if (selectedGenreId > 0)
        {
            var selectedGenre = _genreList.FirstOrDefault(g => g.TagId == selectedGenreId);
            if (selectedGenre != null)
            {
                var games = (await _tags.GetGamesByTagNameAsync(selectedGenre.TagName)).ToList();
                var articleList = new List<WikiArticle>();
                foreach (var game in games)
                {
                    var art = await _articles.GetByGameIdAsync(game.GameId);
                    articleList.AddRange(art);
                }
                articles = articleList;
            }
            else
            {
                articles = Array.Empty<WikiArticle>();
            }
        }
        else if (!string.IsNullOrWhiteSpace(query))
        {
            articles = await _articles.SearchAsync(query);
        }
        else
        {
            articles = await _articles.GetPopularAsync(24);
        }

        if (!string.IsNullOrWhiteSpace(query))
        {
            articles = articles.Where(article =>
                article.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                (article.Summary ?? string.Empty).Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        return articles.GroupBy(a => a.ArticleId).Select(g => g.First()).ToList();
    }

    private Control BuildEmptyState(string text)
    {
        return new Border
        {
            Background = ThemePalette.BgSecondaryBrush,
            BorderBrush = ThemePalette.BorderLightBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(16),
            Padding = new Thickness(18),
            Margin = new Thickness(0, 0, 0, 14),
            Child = new TextBlock
            {
                Text = text,
                Foreground = ThemePalette.TextMutedBrush,
                FontSize = 13
            }
        };
    }

    private Control CreateGameCard(Game game, Bitmap? bitmap)
    {
        var card = new Border
        {
            Background = ThemePalette.BgCardBrush,
            BorderBrush = ThemePalette.BorderLightBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(18),
            Width = 260,
            Height = 300,
            Margin = new Thickness(12),
            Padding = new Thickness(12),
            Cursor = new Cursor(StandardCursorType.Hand),
            RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative),
            ClipToBounds = true
        };

        var stack = new StackPanel
        {
            Spacing = 10
        };

        stack.Children.Add(BuildMedia(bitmap, 236, 136, MediaLabel(game.Title)));
        stack.Children.Add(new TextBlock
        {
            Text = game.Title,
            FontSize = 13,
            FontWeight = FontWeight.Bold,
            Foreground = ThemePalette.TextPrimaryBrush,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        stack.Children.Add(new TextBlock
        {
            Text = game.ShortDescription ?? string.Empty,
            FontSize = 11,
            Foreground = ThemePalette.TextMutedBrush,
            TextWrapping = TextWrapping.Wrap,
            MaxLines = 3
        });
        stack.Children.Add(new TextBlock
        {
            Text = $"{game.ArticleCount} articles | {game.PopularityScore} views",
            FontSize = 10,
            Foreground = ThemePalette.TextMutedBrush
        });

        // Create subtle shadow behind the card by overlaying a semi-transparent border
        var shadow = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(28, 0, 0, 0)),
            CornerRadius = new CornerRadius(20),
            Width = card.Width,
            Height = card.Height,
            Margin = new Thickness(0, 6, 0, 0),
            Opacity = 0.0,
            IsHitTestVisible = false
        };

        card.Child = stack;
        card.PointerEntered += (_, __) =>
        {
            _ = GameWikiApp.Helpers.UiAnimation.BackgroundColorToAsync(card, ThemePalette.BgTertiary, 180);
            _ = GameWikiApp.Helpers.UiAnimation.ScaleToAsync(card, 1.03, 150);
            _ = GameWikiApp.Helpers.UiAnimation.OpacityToAsync(shadow, 1.0, 150);
        };
        card.PointerExited += (_, __) =>
        {
            _ = GameWikiApp.Helpers.UiAnimation.BackgroundColorToAsync(card, ThemePalette.BgCard, 180);
            _ = GameWikiApp.Helpers.UiAnimation.ScaleToAsync(card, 1.0, 150);
            _ = GameWikiApp.Helpers.UiAnimation.OpacityToAsync(shadow, 0.0, 150);
        };
        card.PointerPressed += (_, __) => _openGame(game.GameId);

        var container = new Grid
        {
            Width = card.Width,
            Height = card.Height + 8
        };
        container.Children.Add(shadow);
        container.Children.Add(card);
        return container;
    }

    private Control CreateArticleCard(WikiArticle article, Bitmap? bitmap)
    {
        var card = new Border
        {
            Background = ThemePalette.BgCardBrush,
            BorderBrush = ThemePalette.BorderLightBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(18),
            Width = 260,
            Height = 200,
            Margin = new Thickness(12),
            Cursor = new Cursor(StandardCursorType.Hand),
            ClipToBounds = true
        };

        var stack = new StackPanel
        {
            Spacing = 8
        };

        stack.Children.Add(BuildMedia(bitmap, 236, 96, MediaLabel(article.Title)));
        stack.Children.Add(new TextBlock
        {
            Text = article.Title,
            FontSize = 12.5,
            FontWeight = FontWeight.Bold,
            Foreground = ThemePalette.TextPrimaryBrush,
            TextTrimming = TextTrimming.CharacterEllipsis
        });
        stack.Children.Add(new TextBlock
        {
            Text = article.GameTitle ?? "Unknown game",
            FontSize = 10.5,
            Foreground = ThemePalette.AccentBrush
        });
        stack.Children.Add(new TextBlock
        {
            Text = article.Summary ?? string.Empty,
            FontSize = 11,
            Foreground = ThemePalette.TextMutedBrush,
            TextWrapping = TextWrapping.Wrap,
            MaxLines = 2
        });

        card.Child = stack;
        card.PointerEntered += (_, __) => card.Background = ThemePalette.BgTertiaryBrush;
        card.PointerExited += (_, __) => card.Background = ThemePalette.BgCardBrush;
        card.PointerPressed += (_, __) => _openArticle(article.ArticleId);
        return card;
    }

    private static Control BuildMedia(Bitmap? bitmap, double width, double height, string label)
    {
        if (bitmap != null)
        {
            return new Border
            {
                Width = width,
                Height = height,
                CornerRadius = new CornerRadius(14),
                ClipToBounds = true,
                Child = new Image
                {
                    Source = bitmap,
                    Stretch = Stretch.UniformToFill,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
        }

        return new Border
        {
            Width = width,
            Height = height,
            CornerRadius = new CornerRadius(14),
            Background = ThemePalette.BgTertiaryBrush,
            Child = new TextBlock
            {
                Text = label,
                Foreground = ThemePalette.TextSecondaryBrush,
                FontSize = 16,
                FontWeight = FontWeight.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
    }

    private static string MediaLabel(string? title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return "GAME";
        }

        var parts = title.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2
            ? $"{parts[0]} {parts[1]}"
            : parts[0];
    }
}
