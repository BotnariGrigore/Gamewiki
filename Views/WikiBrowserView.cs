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
    private readonly CategoryService _categories = new();
    private readonly Action<int> _openGame;
    private readonly Action<int> _openArticle;

    private readonly StackPanel _content = new();
    private readonly WrapPanel _gamesPanel = new();
    private readonly WrapPanel _articlesPanel = new();
    private readonly TextBlock _heroTitle = new();
    private readonly TextBlock _heroSubtitle = new();
    private readonly TextBox _searchBox;
    private readonly ComboBox _categoryBox;
    private readonly List<Category> _categoryList = new();
    private bool _suppressFilterEvents;

    private string _activeQuery = string.Empty;
    private int? _activeCategoryId;
    private string? _activeCategoryName;

    public WikiBrowserView(Action<int> openGame, Action<int> openArticle)
    {
        _openGame = openGame;
        _openArticle = openArticle;

        _searchBox = UiFactory.CreateTextBox("Search games and articles...", 360);
        _categoryBox = new ComboBox
        {
            Width = 260,
            Background = ThemePalette.BgInputBrush,
            BorderBrush = ThemePalette.BorderBrush,
            Foreground = ThemePalette.TextPrimaryBrush,
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(12, 10)
        };

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
        _content.Children.Add(UiFactory.CreateSectionHeader("Games", "Browse all wiki pages"));
        _content.Children.Add(_gamesPanel);
        _content.Children.Add(UiFactory.CreateSectionHeader("Articles", "Community pages and guides"));
        _content.Children.Add(_articlesPanel);

        _gamesPanel.Orientation = Orientation.Horizontal;
        _gamesPanel.ItemWidth = 260;
        _gamesPanel.ItemHeight = 270;
        _gamesPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
        _gamesPanel.Margin = new Thickness(0, 0, 0, 16);

        _articlesPanel.Orientation = Orientation.Horizontal;
        _articlesPanel.ItemWidth = 260;
        _articlesPanel.ItemHeight = 200;
        _articlesPanel.HorizontalAlignment = HorizontalAlignment.Stretch;
        _articlesPanel.Margin = new Thickness(0, 0, 0, 24);

        _searchBox.KeyDown += async (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                await LoadAsync(_searchBox.Text ?? string.Empty, _activeCategoryId, _activeCategoryName);
            }
        };

        _categoryBox.SelectionChanged += async (_, __) =>
        {
            if (_suppressFilterEvents)
            {
                return;
            }

            var selected = _categoryBox.SelectedItem as Category;
            _activeCategoryId = selected?.CategoryId;
            _activeCategoryName = selected?.ToString();
            await LoadAsync(_searchBox.Text ?? string.Empty, _activeCategoryId, _activeCategoryName);
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
            Margin = new Thickness(0, 0, 0, 10)
        };

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("2*,1*")
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

        _heroSubtitle.Text = "Search by title, game, or category and jump directly into a page.";
        _heroSubtitle.FontSize = 13;
        _heroSubtitle.Foreground = ThemePalette.TextSecondaryBrush;
        _heroSubtitle.TextWrapping = TextWrapping.Wrap;
        _heroSubtitle.MaxWidth = 700;

        left.Children.Add(_heroTitle);
        left.Children.Add(_heroSubtitle);

        var filters = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10,
            Margin = new Thickness(0, 14, 0, 0)
        };
        filters.Children.Add(_searchBox);
        filters.Children.Add(_categoryBox);

        var searchButton = UiFactory.CreatePrimaryButton("Search", 90);
        searchButton.Click += async (_, __) => await LoadAsync(_searchBox.Text ?? string.Empty, _activeCategoryId, _activeCategoryName);
        filters.Children.Add(searchButton);

        var clearButton = UiFactory.CreateSubtleButton("Clear", 90);
        clearButton.Click += async (_, __) =>
        {
            _suppressFilterEvents = true;
            _searchBox.Text = string.Empty;
            _categoryBox.SelectedIndex = 0;
            _suppressFilterEvents = false;
            await LoadAsync(string.Empty, null, null);
        };
        filters.Children.Add(clearButton);

        left.Children.Add(filters);
        grid.Children.Add(left);

        var right = new StackPanel
        {
            Spacing = 12,
            VerticalAlignment = VerticalAlignment.Top
        };
        right.Children.Add(CreateStatTile("Games", "Browse all"));
        right.Children.Add(CreateStatTile("Articles", "Read guides"));
        grid.Children.Add(right);
        Grid.SetColumn(right, 1);

        hero.Child = grid;
        return hero;
    }

    private Border CreateStatTile(string title, string caption)
    {
        var card = UiFactory.CreateCard(220, 74, 0);
        card.Background = ThemePalette.BgSecondaryBrush;
        card.Padding = new Thickness(16, 12);

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

        stack.Children.Add(new TextBlock
        {
            Text = caption,
            FontSize = 20,
            FontWeight = FontWeight.Bold,
            Foreground = ThemePalette.TextPrimaryBrush
        });

        card.Child = stack;
        return card;
    }

    public async Task LoadAsync(string query, int? categoryId = null, string? categoryName = null)
    {
        _activeQuery = query?.Trim() ?? string.Empty;
        _activeCategoryId = categoryId;
        _activeCategoryName = categoryName;

        var categories = (await _categories.GetAllAsync()).ToList();
        _categoryList.Clear();
        _categoryList.Add(new Category { CategoryId = 0, CategoryName = "All categories" });
        _categoryList.AddRange(categories);
        _suppressFilterEvents = true;
        _categoryBox.ItemsSource = _categoryList;
        if (_activeCategoryId.HasValue)
        {
            _categoryBox.SelectedItem = _categoryList.FirstOrDefault(x => x.CategoryId == _activeCategoryId.Value) ?? _categoryList[0];
        }
        else
        {
            _categoryBox.SelectedIndex = 0;
        }
        _suppressFilterEvents = false;

        var search = _activeQuery;
        var selectedCategory = _categoryBox.SelectedItem as Category;
        var selectedCategoryId = selectedCategory?.CategoryId ?? 0;
        var selectedCategoryLabel = selectedCategoryId > 0
            ? selectedCategory?.ToString()
            : _activeCategoryName;
        _activeCategoryName = selectedCategoryLabel;

        _heroTitle.Text = string.IsNullOrWhiteSpace(search)
            ? string.IsNullOrWhiteSpace(selectedCategoryLabel)
                ? "All wiki pages"
                : selectedCategoryLabel!
            : $"Results for \"{search}\"";

        _heroSubtitle.Text = string.IsNullOrWhiteSpace(search)
            ? "Search by title, game, or category and jump directly into a page."
            : $"Showing matches for \"{search}\".";

        var games = await LoadGamesAsync(search, selectedCategory);
        var articles = await LoadArticlesAsync(search, selectedCategoryId);

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

    private async Task<List<Game>> LoadGamesAsync(string query, Category? selectedCategory)
    {
        IEnumerable<Game> games;
        if (selectedCategory != null && selectedCategory.CategoryId > 0)
        {
            var game = await _games.GetByIdAsync(selectedCategory.GameId);
            games = game != null ? new[] { game } : Array.Empty<Game>();
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

        return games.ToList();
    }

    private async Task<List<WikiArticle>> LoadArticlesAsync(string query, int selectedCategoryId)
    {
        IEnumerable<WikiArticle> articles;
        if (selectedCategoryId > 0)
        {
            articles = await _categories.GetArticlesAsync(selectedCategoryId);
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
                (article.Summary ?? string.Empty).Contains(query, StringComparison.OrdinalIgnoreCase) ||
                (article.Content ?? string.Empty).Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        return articles.ToList();
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
            RenderTransformOrigin = new RelativePoint(0.5, 0.5, RelativeUnit.Relative)
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
            Height = card.Height + 6
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
            Margin = new Thickness(10),
            Cursor = new Cursor(StandardCursorType.Hand)
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
