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

public sealed class HomeView : UserControl
{
    private readonly GameService _games = new();
    private readonly ArticleService _articles = new();
    private readonly TagService _tags = new();
    private readonly Action<int> _openGame;
    private readonly Action<int> _openArticle;

    private readonly StackPanel _content = new();
    private readonly TextBlock _heroTitle = new();
    private readonly TextBlock _heroSubtitle = new();
    private readonly TextBlock _heroTag = new();
    private readonly WrapPanel _genrePanel = new();
    private readonly WrapPanel _gamesPanel = new();
    private readonly WrapPanel _articlesPanel = new();
    private readonly TextBlock _gamesStatValue = new();
    private readonly TextBlock _articlesStatValue = new();
    private readonly StackPanel _genreSection = new();
    private readonly StackPanel _gameSection = new();
    private readonly StackPanel _articleSection = new();

    private string? _selectedGenre;
    private string _activeQuery = string.Empty;

    public HomeView(Action<int> openGame, Action<int> openArticle)
    {
        _openGame = openGame;
        _openArticle = openArticle;

        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = _content,
            Background = ThemePalette.BgPrimaryBrush
        };

        _content.Spacing = 14;
        _content.Margin = new Thickness(0);
        _content.Children.Add(BuildHeroShell());

        // Genre section
        _genreSection.Spacing = 12;
        _content.Children.Add(_genreSection);

        // Game section
        _gameSection.Spacing = 12;
        _content.Children.Add(_gameSection);

        // Article section
        _articleSection.Spacing = 12;
        _content.Children.Add(_articleSection);

        _genrePanel.Orientation = Orientation.Horizontal;
        _genrePanel.ItemWidth = 180;
        _genrePanel.ItemHeight = 60;
        _genrePanel.HorizontalAlignment = HorizontalAlignment.Stretch;
        _genrePanel.VerticalAlignment = VerticalAlignment.Top;
        _genrePanel.Margin = new Thickness(0, 0, 0, 16);

        _gamesPanel.Orientation = Orientation.Horizontal;
        _gamesPanel.ItemWidth = 260; // Original width
        _gamesPanel.ItemHeight = 300; // Updated height
        _gamesPanel.HorizontalAlignment = HorizontalAlignment.Stretch; // Original alignment
        _gamesPanel.VerticalAlignment = VerticalAlignment.Top;
        _gamesPanel.Margin = new Thickness(0, 0, 0, 16);

        _articlesPanel.Orientation = Orientation.Horizontal;
        _articlesPanel.ItemWidth = 260;
        _articlesPanel.ItemHeight = 180;
        _articlesPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
        _articlesPanel.VerticalAlignment = VerticalAlignment.Top;
        _articlesPanel.Margin = new Thickness(0, 0, 0, 24);

        Content = scroll;
    }

    public async Task LoadAsync(string query)
    {
        _activeQuery = query?.Trim() ?? string.Empty;
        var search = _activeQuery;
        var selectedGenre = _selectedGenre;

        var genres = (await _tags.GetAllAsync()).ToList();

        IEnumerable<Game> gameSource;
        if (!string.IsNullOrWhiteSpace(selectedGenre))
        {
            gameSource = await _tags.GetGamesByTagNameAsync(selectedGenre);
        }
        else if (!string.IsNullOrWhiteSpace(search))
        {
            gameSource = await _games.SearchAsync(search);
        }
        else
        {
            gameSource = await _games.GetPopularAsync(8);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            gameSource = gameSource.Where(game =>
                game.Title.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (game.ShortDescription ?? string.Empty).Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (game.FullDescription ?? string.Empty).Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        var games = gameSource.Take(8).ToList();

        var articles = (string.IsNullOrWhiteSpace(search)
                ? await _articles.GetPopularAsync(8)
                : await _articles.SearchAsync(search))
            .Take(8)
            .ToList();

        if (!string.IsNullOrWhiteSpace(search) && !string.IsNullOrWhiteSpace(selectedGenre))
        {
            _heroTitle.Text = $"Results for \"{search}\" in {selectedGenre}";
            _heroSubtitle.Text = $"Showing {games.Count} games and {articles.Count} articles in {selectedGenre}.";
            _heroTag.Text = "FILTERED";
        }
        else if (!string.IsNullOrWhiteSpace(search))
        {
            _heroTitle.Text = $"Results for \"{search}\"";
            _heroSubtitle.Text = $"Showing {games.Count} games and {articles.Count} articles matching your search.";
            _heroTag.Text = "SEARCH RESULTS";
        }
        else if (!string.IsNullOrWhiteSpace(selectedGenre))
        {
            _heroTitle.Text = $"Genre: {selectedGenre}";
            _heroSubtitle.Text = $"Showing {games.Count} games in this genre and trending articles from Nexoria.";
            _heroTag.Text = "GENRE VIEW";
        }
        else
        {
            _heroTitle.Text = "Nexoria";
            _heroSubtitle.Text = "Browse games by genre or discover trending articles.";
            _heroTag.Text = "NEXORIA";
        }
        _gamesStatValue.Text = games.Count.ToString();
        _articlesStatValue.Text = articles.Count.ToString();

        // Genre chips
        _genreSection.Children.Clear();
        _genreSection.Children.Add(UiFactory.CreateSectionHeader(
            string.IsNullOrWhiteSpace(_selectedGenre) ? "Browse by Genre" : $"Genre: {_selectedGenre}",
            $"{genres.Count} genres"));
        _genrePanel.Children.Clear();
        if (genres.Count == 0)
        {
            _genrePanel.Children.Add(BuildEmptyState("No genres found. Add game tags to your games."));
        }
        else
        {
            // "All" chip
            _genrePanel.Children.Add(CreateGenreChip("All", null));
            foreach (var genre in genres)
            {
                _genrePanel.Children.Add(CreateGenreChip($"{genre.TagName} ({genre.GameCount})", genre.TagName));
            }
        }
        _genreSection.Children.Add(_genrePanel);

        // Games
        _gameSection.Children.Clear();
        _gameSection.Children.Add(UiFactory.CreateSectionHeader("Games", "Click a genre above to filter"));
        _gamesPanel.Children.Clear();
        if (games.Count == 0)
        {
            _gamesPanel.Children.Add(BuildEmptyState("No games found."));
        }
        else
        {
            for (var i = 0; i < games.Count; i++)
            {
                _gamesPanel.Children.Add(CreateGameCard(games[i]));
            }
        }
        _gameSection.Children.Add(_gamesPanel);

        // Articles
        _articleSection.Children.Clear();
        _articleSection.Children.Add(UiFactory.CreateSectionHeader("Trending Articles", "Most viewed community pages"));
        _articlesPanel.Children.Clear();
        if (articles.Count == 0)
        {
            _articlesPanel.Children.Add(BuildEmptyState("No articles found."));
        }
        else
        {
            for (var i = 0; i < articles.Count; i++)
            {
                var card = await CreateArticleCardAsync(articles[i]);
                _articlesPanel.Children.Add(card);
            }
        }
        _articleSection.Children.Add(_articlesPanel);
    }

    private Control CreateGenreChip(string label, string? tagName)
    {
        var isSelected = !string.IsNullOrWhiteSpace(tagName) &&
                        string.Equals(_selectedGenre, tagName, StringComparison.OrdinalIgnoreCase);
        var isAll = tagName == null && string.IsNullOrWhiteSpace(_selectedGenre);

        var chip = new Border
        {
            Background = (isSelected || isAll) ? ThemePalette.AccentBrush : ThemePalette.BgCardBrush,
            BorderBrush = ThemePalette.BorderLightBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(16),
            Padding = new Thickness(20, 14),
            Cursor = new Cursor(StandardCursorType.Hand),
            Child = new TextBlock
            {
                Text = label,
                Foreground = (isSelected || isAll) ? ThemePalette.AccentForegroundBrush : ThemePalette.TextPrimaryBrush,
                FontSize = 13,
                FontWeight = FontWeight.Bold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };

        chip.PointerEntered += (_, __) =>
        {
            if (!isSelected && !isAll) chip.Background = ThemePalette.BgTertiaryBrush;
        };
        chip.PointerExited += (_, __) =>
        {
            if (!isSelected && !isAll) chip.Background = ThemePalette.BgCardBrush;
        };
        chip.PointerPressed += async (_, __) =>
        {
            _selectedGenre = tagName;
            if (string.IsNullOrWhiteSpace(tagName))
            {
                _selectedGenre = null;
            }

            await LoadAsync(_activeQuery);
        };

        return chip;
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
            Margin = new Thickness(0, 0, 0, 10)
        };

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("2.2*,1*"),
        };

        var left = new StackPanel
        {
            Spacing = 10
        };

        _heroTag.Text = "NEXORIA";
        _heroTag.FontSize = 11;
        _heroTag.FontWeight = FontWeight.Bold;
        _heroTag.Foreground = ThemePalette.AccentBrush;

        _heroTitle.Text = "Nexoria";
        _heroTitle.FontSize = 30;
        _heroTitle.FontWeight = FontWeight.Bold;
        _heroTitle.Foreground = ThemePalette.TextPrimaryBrush;

        _heroSubtitle.Text = "Browse games by genre or discover trending articles.";
        _heroSubtitle.FontSize = 13;
        _heroSubtitle.Foreground = ThemePalette.TextSecondaryBrush;
        _heroSubtitle.TextWrapping = TextWrapping.Wrap;
        _heroSubtitle.MaxWidth = 680;

        left.Children.Add(_heroTag);
        left.Children.Add(_heroTitle);
        left.Children.Add(_heroSubtitle);

        grid.Children.Add(left);

        var stats = new StackPanel
        {
            Spacing = 12,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Top
        };
        stats.Children.Add(CreateStatTile("Games shown", _gamesStatValue));
        stats.Children.Add(CreateStatTile("Articles shown", _articlesStatValue));
        grid.Children.Add(stats);
        Grid.SetColumn(stats, 1);

        hero.Child = grid;
        return hero;
    }

    private Border CreateStatTile(string title, TextBlock value)
    {
        var tile = UiFactory.CreateCard(220, 72, 0);
        tile.Background = ThemePalette.BgSecondaryBrush;
        tile.Padding = new Thickness(16, 12);

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("4,*,Auto")
        };

        grid.Children.Add(new Border
        {
            Background = ThemePalette.AccentBrush,
            CornerRadius = new CornerRadius(2),
            Width = 4,
            Height = 34,
            VerticalAlignment = VerticalAlignment.Center
        });

        value.FontSize = 22;
        value.FontWeight = FontWeight.Bold;
        value.Foreground = ThemePalette.TextPrimaryBrush;
        value.Margin = new Thickness(12, 0, 0, 0);
        grid.Children.Add(value);
        Grid.SetColumn(value, 1);

        var label = new TextBlock
        {
            Text = title,
            FontSize = 11,
            FontWeight = FontWeight.Bold,
            Foreground = ThemePalette.TextMutedBrush,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom
        };
        grid.Children.Add(label);
        Grid.SetColumn(label, 2);

        tile.Child = grid;
        tile.Tag = value;
        return tile;
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

    private Control CreateGameCard(Game game)
    {
        var card = new Border
        {
            Background = ThemePalette.BgCardBrush,
            BorderBrush = ThemePalette.BorderLightBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(18),
            Width = 260, // Original width
            Height = 300, // Updated height
            Margin = new Thickness(12),
            Padding = new Thickness(12),
            Cursor = new Cursor(StandardCursorType.Hand),
            RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative)
        };

        var stack = new StackPanel
        {
            Spacing = 8
        };

        // Auto-load game cover image
        _ = LoadGameMediaAsync(game, stack);

        stack.Children.Add(new TextBlock
        {
            Text = game.Title,
            FontSize = 13,
            FontWeight = FontWeight.Bold,
            Foreground = ThemePalette.TextPrimaryBrush,
            TextTrimming = TextTrimming.CharacterEllipsis
        });

        // Genre badges
        if (game.Genres.Count > 0)
        {
            var genreRow = new WrapPanel
            {
                Margin = new Thickness(0, 0, 0, 0)
            };
            foreach (var g in game.Genres.Take(3))
            {
                genreRow.Children.Add(new Border
                {
                    Background = ThemePalette.AccentDimBrush,
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(8, 2),
                    Child = new TextBlock
                    {
                        Text = g,
                        FontSize = 9,
                        Foreground = ThemePalette.AccentForegroundBrush,
                        FontWeight = FontWeight.Bold
                    }
                });
            }
            stack.Children.Add(genreRow);
        }

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
            Height = card.Height + 6
        };
        container.Children.Add(shadow);
        container.Children.Add(card);
        return container;
    }

    private static async Task LoadGameMediaAsync(Game game, StackPanel target)
    {
        var bitmap = await ImageLoader.LoadAsync(game.CoverImage);
        var mediaContent = bitmap != null
            ? new Border
            {
                Width = 236,
                Height = 140,
                CornerRadius = new CornerRadius(12),
                ClipToBounds = true,
                Child = new Image
                {
                    Source = bitmap,
                    Stretch = Stretch.UniformToFill,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            } as Control
            : new Border
            {
                Width = 236,
                Height = 140,
                CornerRadius = new CornerRadius(12),
                Background = ThemePalette.BgTertiaryBrush,
                Child = new TextBlock
                {
                    Text = game.Title,
                    Foreground = ThemePalette.TextSecondaryBrush,
                    FontSize = 14,
                    FontWeight = FontWeight.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };

        // Insert at position 0 (top of the card)
        target.Children.Insert(0, mediaContent);
    }

    private async Task<Control> CreateArticleCardAsync(WikiArticle article)
    {
        var card = new Border
        {
            Background = ThemePalette.BgCardBrush,
            BorderBrush = ThemePalette.BorderLightBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(18),
            Width = 260,
            Height = 180,
            Margin = new Thickness(10),
            Cursor = new Cursor(StandardCursorType.Hand)
        };

        var stack = new StackPanel
        {
            Spacing = 6
        };

        var bitmap = await ImageLoader.LoadAsync(article.CoverImage);
        if (bitmap != null)
        {
            stack.Children.Add(new Border
            {
                Width = 236,
                Height = 64,
                CornerRadius = new CornerRadius(14),
                ClipToBounds = true,
                Child = new Image
                {
                    Source = bitmap,
                    Stretch = Stretch.UniformToFill,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            });
        }
        else
        {
            stack.Children.Add(new Border
            {
                Width = 236,
                Height = 64,
                CornerRadius = new CornerRadius(14),
                Background = ThemePalette.BgTertiaryBrush,
                Child = new TextBlock
                {
                    Text = article.Title,
                    Foreground = ThemePalette.TextSecondaryBrush,
                    FontSize = 13,
                    FontWeight = FontWeight.Bold,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            });
        }

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

        card.Child = stack;
        card.PointerEntered += (_, __) => card.Background = ThemePalette.BgTertiaryBrush;
        card.PointerExited += (_, __) => card.Background = ThemePalette.BgCardBrush;
        card.PointerPressed += (_, __) => _openArticle(article.ArticleId);
        return card;
    }
}