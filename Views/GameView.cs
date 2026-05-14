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

public sealed class GameView : UserControl
{
    private readonly int _gameId;
    private readonly Action<int> _openArticle;
    private readonly Action<int> _openCategory;
    private readonly GameService _games = new();
    private readonly CategoryService _categories = new();
    private readonly ArticleService _articles = new();

    private readonly StackPanel _content = new();
    private readonly StackPanel _categoryPanel = new();
    private readonly WrapPanel _articlesPanel = new();
    private readonly TextBlock _title = new();
    private readonly TextBlock _description = new();
    private readonly TextBlock _stats = new();
    private readonly Border _banner = new();
    private Game? _game;
    private int? _selectedCategoryId;

    public GameView(int gameId, Action<int> openArticle, Action<int> openCategory)
    {
        _gameId = gameId;
        _openArticle = openArticle;
        _openCategory = openCategory;

        var scroll = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            Content = _content,
            Background = ThemePalette.BgPrimaryBrush
        };

        _content.Spacing = 14;
        _content.Children.Add(BuildHeader());
        _content.Children.Add(BuildCategoriesShell());
        _content.Children.Add(UiFactory.CreateSectionHeader("Articles", "Pages from this game"));
        _content.Children.Add(_articlesPanel);

        _articlesPanel.Orientation = Orientation.Horizontal;
        _articlesPanel.ItemWidth = 320;
        _articlesPanel.ItemHeight = 170;
        _articlesPanel.Margin = new Thickness(0, 0, 0, 24);

        Content = scroll;
    }

    private Control BuildHeader()
    {
        var shell = new Border
        {
            Background = ThemePalette.SurfaceBrush,
            BorderBrush = ThemePalette.BorderLightBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(20),
            Padding = new Thickness(0),
            Margin = new Thickness(0, 0, 0, 10)
        };

        var grid = new Grid
        {
            RowDefinitions = new RowDefinitions("280,Auto"),
            ColumnDefinitions = new ColumnDefinitions("*")
        };

        _banner.Height = 280;
        _banner.Background = ThemePalette.BgTertiaryBrush;
        _banner.Child = new TextBlock
        {
            Text = "Loading...",
            Foreground = ThemePalette.TextMutedBrush,
            FontSize = 18,
            FontWeight = FontWeight.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        grid.Children.Add(_banner);
        Grid.SetRow(_banner, 0);

        var header = new StackPanel
        {
            Spacing = 10,
            Margin = new Thickness(24, 20, 24, 24)
        };

        var topRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10
        };

        var backButton = UiFactory.CreateSubtleButton("Browse all", 110);
        backButton.Click += (_, __) => _openCategory(0);
        topRow.Children.Add(backButton);

        var newArticleButton = UiFactory.CreatePrimaryButton("New article", 120);
        newArticleButton.Click += async (_, __) => await OpenEditorAsync();
        topRow.Children.Add(newArticleButton);

        var editButton = UiFactory.CreateSubtleButton("Edit game", 110);
        editButton.IsVisible = AppState.IsAdmin;
        editButton.Click += async (_, __) => await OpenGameEditorAsync();
        topRow.Children.Add(editButton);

        header.Children.Add(topRow);

        _title.Text = "Loading...";
        _title.FontSize = 28;
        _title.FontWeight = FontWeight.Bold;
        _title.Foreground = ThemePalette.TextPrimaryBrush;
        header.Children.Add(_title);

        _description.Text = string.Empty;
        _description.FontSize = 13;
        _description.Foreground = ThemePalette.TextSecondaryBrush;
        _description.TextWrapping = TextWrapping.Wrap;
        _description.MaxWidth = 850;
        header.Children.Add(_description);

        _stats.Text = string.Empty;
        _stats.FontSize = 11;
        _stats.Foreground = ThemePalette.TextMutedBrush;
        header.Children.Add(_stats);

        grid.Children.Add(header);
        Grid.SetRow(header, 1);

        shell.Child = grid;
        return shell;
    }

    private Control BuildCategoriesShell()
    {
        var shell = new Border
        {
            Background = ThemePalette.BgSecondaryBrush,
            BorderBrush = ThemePalette.BorderLightBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(18),
            Padding = new Thickness(18),
            Margin = new Thickness(0, 0, 0, 4)
        };

        var stack = new StackPanel
        {
            Spacing = 12
        };

        stack.Children.Add(new TextBlock
        {
            Text = "Wiki categories",
            FontSize = 14,
            FontWeight = FontWeight.Bold,
            Foreground = ThemePalette.TextPrimaryBrush
        });

        _categoryPanel.Orientation = Orientation.Horizontal;
        _categoryPanel.Spacing = 10;
        _categoryPanel.Margin = new Thickness(0, 0, 0, 0);
        stack.Children.Add(_categoryPanel);

        shell.Child = stack;
        return shell;
    }

    public async Task LoadAsync()
    {
        _game = await _games.GetByIdAsync(_gameId);
        if (_game == null)
        {
            _title.Text = "Game not found";
            _description.Text = string.Empty;
            _stats.Text = string.Empty;
            _selectedCategoryId = null;
            _banner.Child = BuildBannerContent(null, "Game not found");
            _categoryPanel.Children.Clear();
            _categoryPanel.Children.Add(new TextBlock
            {
                Text = "No categories available.",
                Foreground = ThemePalette.TextMutedBrush
            });
            _articlesPanel.Children.Clear();
            _articlesPanel.Children.Add(new TextBlock
            {
                Text = "No articles available.",
                Foreground = ThemePalette.TextMutedBrush
            });
            return;
        }

        _title.Text = _game.Title;
        _description.Text = _game.ShortDescription ?? _game.FullDescription ?? "No description available.";
        _stats.Text = $"{_game.ArticleCount} articles | {_game.PopularityScore} views";

        var bitmap = await ImageLoader.LoadAsync(_game.BannerImage ?? _game.CoverImage);
        _banner.Child = BuildBannerContent(bitmap, _game.Title);

        await LoadCategoriesAsync();
        await LoadArticlesAsync();
    }

    private static Control BuildBannerContent(Bitmap? bitmap, string title)
    {
            if (bitmap != null)
            {
            return new Image
            {
                Source = bitmap,
                Stretch = Stretch.UniformToFill,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            }

        return new Border
        {
            Background = ThemePalette.BgTertiaryBrush,
            Child = new TextBlock
            {
                Text = title,
                FontSize = 20,
                FontWeight = FontWeight.Bold,
                Foreground = ThemePalette.TextSecondaryBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
    }

    private async Task LoadCategoriesAsync()
    {
        _categoryPanel.Children.Clear();

        var categories = (await _categories.GetByGameIdAsync(_gameId)).ToList();
        if (categories.Count == 0)
        {
            _categoryPanel.Children.Add(new TextBlock
            {
                Text = "No wiki categories yet.",
                Foreground = ThemePalette.TextMutedBrush
            });
            return;
        }

        foreach (var category in categories)
        {
            var chip = UiFactory.CreateSubtleButton(category.CategoryName, double.NaN);
            chip.Background = _selectedCategoryId == category.CategoryId
                ? ThemePalette.BgTertiaryBrush
                : ThemePalette.BgSecondaryBrush;
            chip.Click += async (_, __) =>
            {
                _selectedCategoryId = category.CategoryId;
                await LoadArticlesForCategoryAsync(category);
                await LoadCategoriesAsync();
            };
            _categoryPanel.Children.Add(chip);
        }
    }

    private async Task LoadArticlesAsync()
    {
        _selectedCategoryId = null;
        _articlesPanel.Children.Clear();

        var articles = (await _articles.GetByGameIdAsync(_gameId)).ToList();
        if (articles.Count == 0)
        {
            _articlesPanel.Children.Add(new TextBlock
            {
                Text = "No articles yet.",
                Foreground = ThemePalette.TextMutedBrush
            });
            return;
        }

        foreach (var article in articles)
        {
            var bitmap = await ImageLoader.LoadAsync(article.CoverImage);
            _articlesPanel.Children.Add(CreateArticleCard(article, bitmap));
        }
    }

    private async Task LoadArticlesForCategoryAsync(Category category)
    {
        _articlesPanel.Children.Clear();

        var articles = (await _categories.GetArticlesAsync(category.CategoryId)).ToList();
        if (articles.Count == 0)
        {
            _articlesPanel.Children.Add(new TextBlock
            {
                Text = $"No articles in {category.CategoryName}.",
                Foreground = ThemePalette.TextMutedBrush
            });
            return;
        }

        foreach (var article in articles)
        {
            var bitmap = await ImageLoader.LoadAsync(article.CoverImage);
            _articlesPanel.Children.Add(CreateArticleCard(article, bitmap));
        }
    }

    private Control CreateArticleCard(WikiArticle article, Bitmap? bitmap)
    {
        var card = new Border
        {
            Background = ThemePalette.BgCardBrush,
            BorderBrush = ThemePalette.BorderLightBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(18),
            Width = 320,
            Height = 170,
            Margin = new Thickness(10),
            Cursor = new Cursor(StandardCursorType.Hand)
        };

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("112,*"),
            ColumnSpacing = 14,
            Margin = new Thickness(12)
        };

        grid.Children.Add(BuildThumb(bitmap, article.Title));

        var stack = new StackPanel
        {
            Spacing = 8
        };

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
            Text = article.Summary ?? string.Empty,
            FontSize = 11,
            Foreground = ThemePalette.TextMutedBrush,
            TextWrapping = TextWrapping.Wrap,
            MaxLines = 4
        });

        stack.Children.Add(new TextBlock
        {
            Text = $"{article.ViewsCount} views | {article.LikeCount} likes",
            FontSize = 10,
            Foreground = ThemePalette.TextMutedBrush
        });

        var openButton = UiFactory.CreatePrimaryButton("Open", 90);
        openButton.Click += (_, __) => _openArticle(article.ArticleId);
        stack.Children.Add(openButton);

        grid.Children.Add(stack);
        Grid.SetColumn(stack, 1);

        card.Child = grid;
        card.PointerEntered += (_, __) => card.Background = ThemePalette.BgTertiaryBrush;
        card.PointerExited += (_, __) => card.Background = ThemePalette.BgCardBrush;
        card.PointerPressed += (_, __) => _openArticle(article.ArticleId);
        return card;
    }

    private static Control BuildThumb(Bitmap? bitmap, string title)
    {
        if (bitmap != null)
        {
            return new Border
            {
                Width = 112,
                Height = 138,
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
            Width = 112,
            Height = 138,
            CornerRadius = new CornerRadius(14),
            Background = ThemePalette.BgTertiaryBrush,
            Child = new TextBlock
            {
                Text = title,
                FontSize = 11,
                FontWeight = FontWeight.Bold,
                Foreground = ThemePalette.TextSecondaryBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center
            }
        };
    }

    private async Task OpenGameEditorAsync()
    {
        if (_game == null)
        {
            return;
        }

        var owner = TopLevel.GetTopLevel(this) as Window;
        if (owner == null)
        {
            return;
        }

        var editor = new GameEditorWindow(_game.GameId);
        await editor.ShowDialog<bool>(owner);
        await LoadAsync();
    }

    private async Task OpenEditorAsync()
    {
        if (_game == null)
        {
            return;
        }

        var owner = TopLevel.GetTopLevel(this) as Window;
        if (owner == null)
        {
            return;
        }

        var editor = new ArticleEditorWindow(0, _game.GameId);
        await editor.ShowDialog<bool>(owner);
        await LoadAsync();
    }
}
