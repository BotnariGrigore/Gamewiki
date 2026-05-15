using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using GameWikiApp.Data;
using GameWikiApp.Helpers;
using GameWikiApp.Models;
using GameWikiApp.Services;

namespace GameWikiApp.Views;

public sealed class ArticleEditorWindow : Window
{
    private readonly int _articleId;
    private readonly int _initialGameId;
    private readonly ArticleService _articles = new();
    private readonly GameService _games = new();
    private readonly CategoryService _categories = new();
    private readonly ArticleImageRepository _images = new();

    private readonly ComboBox _gameBox;
    private readonly TextBox _titleBox;
    private readonly TextBox _summaryBox;
    private readonly TextBox _contentBox;
    private readonly TextBox _coverBox;
    private readonly TextBox _galleryBox;
    private readonly StackPanel _categoryPanel = new();
    private readonly CheckBox _publishedBox;
    private readonly Button _deleteButton;
    private readonly List<Category> _categoryList = new();
    private WikiArticle? _article;

    public ArticleEditorWindow(int articleId = 0, int initialGameId = 0)
    {
        _articleId = articleId;
        _initialGameId = initialGameId;

        Title = articleId > 0 ? "Edit article" : "Create article";
        Width = 980;
        Height = 780;
        MinWidth = 860;
        MinHeight = 680;
        Background = ThemePalette.BgPrimaryBrush;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        _gameBox = new ComboBox
        {
            Width = 360,
            Background = ThemePalette.BgInputBrush,
            BorderBrush = ThemePalette.BorderBrush,
            Foreground = ThemePalette.TextPrimaryBrush,
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(12, 10),
            DisplayMemberBinding = new Binding(nameof(Game.Title))
        };
        _gameBox.SelectionChanged += async (_, __) => await ReloadCategoriesAsync();
        _titleBox = UiFactory.CreateTextBox("Article title", 560);
        _summaryBox = UiFactory.CreateTextBox("Short summary", 820);
        _summaryBox.AcceptsReturn = true;
        _summaryBox.TextWrapping = TextWrapping.Wrap;
        _summaryBox.Height = 84;
        _contentBox = UiFactory.CreateTextBox("Article content — use [[Article Title]] to create inline wiki links", 820);
        _contentBox.AcceptsReturn = true;
        _contentBox.TextWrapping = TextWrapping.Wrap;
        _contentBox.Height = 260;
        _coverBox = UiFactory.CreateTextBox("Cover image url or path");
        _galleryBox = UiFactory.CreateTextBox("Gallery image urls, one per line");
        _galleryBox.AcceptsReturn = true;
        _galleryBox.TextWrapping = TextWrapping.Wrap;
        _galleryBox.Height = 120;
        _publishedBox = new CheckBox
        {
            Content = "Published",
            IsChecked = true,
            Foreground = ThemePalette.TextPrimaryBrush
        };
        _deleteButton = UiFactory.CreateSubtleButton("Delete", 120);
        _deleteButton.IsVisible = _articleId > 0;

        Content = BuildLayout();
        Loaded += async (_, __) => await LoadAsync();
    }

    private Control BuildLayout()
    {
        var stack = new StackPanel
        {
            Spacing = 14,
            Margin = new Thickness(20)
        };

        stack.Children.Add(new Border
        {
            Background = ThemePalette.SurfaceBrush,
            BorderBrush = ThemePalette.BorderLightBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(18),
            Padding = new Thickness(20),
            Child = new TextBlock
            {
                Text = Title ?? "Article editor",
                FontSize = 24,
                FontWeight = FontWeight.Bold,
                Foreground = ThemePalette.TextPrimaryBrush
            }
        });

        stack.Children.Add(BuildTwoColumns(
            BuildField("Game", _gameBox),
            BuildField("Title", _titleBox)));

        stack.Children.Add(BuildField("Summary", _summaryBox));
        stack.Children.Add(BuildField("Content", _contentBox));
        stack.Children.Add(BuildTwoColumns(
            BuildImageField("Cover image", _coverBox),
            BuildGalleryField()));
        stack.Children.Add(BuildCategoriesPanel());

        var footer = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10
        };

        var save = UiFactory.CreatePrimaryButton("Save", 120, "✓");
        save.Click += async (_, __) => await SaveAsync();
        footer.Children.Add(save);

        _deleteButton.Click += async (_, __) => await DeleteAsync();
        footer.Children.Add(_deleteButton);

        var close = UiFactory.CreateSubtleButton("Close", 120, "×");
        close.Click += (_, __) => Close(false);
        footer.Children.Add(close);

        stack.Children.Add(_publishedBox);
        stack.Children.Add(footer);

        return new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = stack,
            Background = ThemePalette.BgPrimaryBrush
        };
    }

    private static Control BuildField(string label, Control editor)
    {
        var stack = new StackPanel
        {
            Spacing = 6
        };

        stack.Children.Add(new TextBlock
        {
            Text = label,
            FontSize = 11,
            FontWeight = FontWeight.Bold,
            Foreground = ThemePalette.TextMutedBrush
        });

        stack.Children.Add(editor);
        return stack;
    }

    private static Control BuildTwoColumns(Control left, Control right)
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("1*,1*"),
            ColumnSpacing = 12
        };

        grid.Children.Add(left);
        grid.Children.Add(right);
        Grid.SetColumn(right, 1);
        return grid;
    }

    private static Border BuildImageField(string label, TextBox editor)
    {
        var stack = new StackPanel
        {
            Spacing = 6
        };

        stack.Children.Add(new TextBlock
        {
            Text = label,
            FontSize = 11,
            FontWeight = FontWeight.Bold,
            Foreground = ThemePalette.TextMutedBrush
        });

        var row = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            ColumnSpacing = 6
        };

        row.Children.Add(editor);
        var browse = UiFactory.CreateSubtleButton("Browse...", 80, "↑");
        browse.Height = 42;
        browse.Click += async (_, __) =>
        {
            var topLevel = TopLevel.GetTopLevel(editor);
            if (topLevel == null) return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = $"Select {label}",
                AllowMultiple = false,
                FileTypeFilter = [new FilePickerFileType("Images")
                {
                    Patterns = ["*.jpg", "*.jpeg", "*.png", "*.gif", "*.bmp", "*.webp"]
                }]
            });

            if (files.Count > 0)
            {
                var destPath = await CopyToPhotoFolderAsync(files[0].Path.LocalPath);
                if (destPath != null)
                {
                    editor.Text = destPath;
                }
            }
        };
        row.Children.Add(browse);
        Grid.SetColumn(browse, 1);

        stack.Children.Add(row);
        return new Border
        {
            Background = ThemePalette.BgSecondaryBrush,
            BorderBrush = ThemePalette.BorderLightBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(16),
            Padding = new Thickness(14),
            Child = stack
        };
    }

    private Control BuildGalleryField()
    {
        var stack = new StackPanel
        {
            Spacing = 6
        };

        stack.Children.Add(new TextBlock
        {
            Text = "Gallery images",
            FontSize = 11,
            FontWeight = FontWeight.Bold,
            Foreground = ThemePalette.TextMutedBrush
        });

        var row = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            ColumnSpacing = 6
        };

        row.Children.Add(_galleryBox);
        var browse = UiFactory.CreateSubtleButton("Browse...", 80, "↑");
        browse.Height = 42;
        browse.VerticalAlignment = VerticalAlignment.Center;
        browse.Click += async (_, __) =>
        {
            var topLevel = TopLevel.GetTopLevel(_galleryBox);
            if (topLevel == null) return;

            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Select gallery images",
                AllowMultiple = true,
                FileTypeFilter = [new FilePickerFileType("Images")
                {
                    Patterns = ["*.jpg", "*.jpeg", "*.png", "*.gif", "*.bmp", "*.webp"]
                }]
            });

            if (files.Count > 0)
            {
                var existing = new HashSet<string>(
                    (_galleryBox.Text ?? string.Empty)
                        .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => x.Trim()),
                    StringComparer.OrdinalIgnoreCase);

                foreach (var file in files)
                {
                    var destPath = await CopyToPhotoFolderAsync(file.Path.LocalPath);
                    if (destPath != null && existing.Add(destPath))
                    {
                        _galleryBox.Text = string.IsNullOrWhiteSpace(_galleryBox.Text)
                            ? destPath
                            : _galleryBox.Text + Environment.NewLine + destPath;
                    }
                }
            }
        };
        row.Children.Add(browse);
        Grid.SetColumn(browse, 1);

        stack.Children.Add(row);
        return new Border
        {
            Background = ThemePalette.BgSecondaryBrush,
            BorderBrush = ThemePalette.BorderLightBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(16),
            Padding = new Thickness(14),
            Child = stack
        };
    }

    private static async Task<string?> CopyToPhotoFolderAsync(string sourcePath)
    {
        try
        {
            var validation = await ImageValidation.ValidateLocalImageAsync(sourcePath);
            if (!validation.Success)
            {
                return null;
            }

            var photoDir = Path.Combine(AppContext.BaseDirectory, "Photo");
            Directory.CreateDirectory(photoDir);

            var ext = Path.GetExtension(sourcePath);
            var destName = $"img_{Guid.NewGuid():N}{ext}";
            var destPath = Path.Combine(photoDir, destName);

            await using var src = File.OpenRead(sourcePath);
            await using var dst = File.Create(destPath);
            await src.CopyToAsync(dst);

            return $"Photo/{destName}";
        }
        catch
        {
            return null;
        }
    }

    private Control BuildCategoriesPanel()
    {
        var shell = new Border
        {
            Background = ThemePalette.BgSecondaryBrush,
            BorderBrush = ThemePalette.BorderLightBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(16),
            Padding = new Thickness(16)
        };

        var stack = new StackPanel
        {
            Spacing = 10
        };

        stack.Children.Add(new TextBlock
        {
            Text = "Wiki categories",
            FontSize = 13,
            FontWeight = FontWeight.Bold,
            Foreground = ThemePalette.TextPrimaryBrush
        });

        var scroll = new ScrollViewer
        {
            Height = 160,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = _categoryPanel
        };
        _categoryPanel.Spacing = 8;
        stack.Children.Add(scroll);

        shell.Child = stack;
        return shell;
    }

    private async Task LoadAsync()
    {
        var games = (await _games.GetAllAsync()).ToList();
        _gameBox.ItemsSource = games;

        if (_initialGameId > 0)
        {
            _gameBox.SelectedItem = games.FirstOrDefault(x => x.GameId == _initialGameId);
        }
        else if (games.Count > 0)
        {
            _gameBox.SelectedIndex = 0;
        }

        if (_articleId > 0)
        {
            _article = await _articles.GetByIdAsync(_articleId);
            if (_article != null)
            {
                _titleBox.Text = _article.Title;
                _summaryBox.Text = _article.Summary ?? string.Empty;
                _contentBox.Text = _article.Content;
                _coverBox.Text = _article.CoverImage ?? string.Empty;
                _publishedBox.IsChecked = _article.IsPublished;

                var selectedGame = games.FirstOrDefault(x => x.GameId == _article.GameId);
                if (selectedGame != null)
                {
                    _gameBox.SelectedItem = selectedGame;
                }
            }
        }

        await ReloadCategoriesAsync();

        if (_articleId > 0)
        {
            var images = (await _images.GetByArticleIdAsync(_articleId)).Select(x => x.ImageUrl).ToArray();
            _galleryBox.Text = string.Join(Environment.NewLine, images);
        }
    }

    private async Task ReloadCategoriesAsync()
    {
        _categoryPanel.Children.Clear();

        if (_gameBox.SelectedItem is not Game selectedGame)
        {
            return;
        }

        var categories = (await _categories.GetByGameIdAsync(selectedGame.GameId)).ToList();
        foreach (var category in categories)
        {
            var check = new CheckBox
            {
                Content = category.CategoryName,
                Tag = category.CategoryId,
                Foreground = ThemePalette.TextPrimaryBrush
            };
            _categoryPanel.Children.Add(check);
        }

        if (_articleId > 0)
        {
            var selectedIds = (await _articles.GetCategoryIdsAsync(_articleId)).ToHashSet();
            foreach (var check in _categoryPanel.Children.OfType<CheckBox>())
            {
                if (check.Tag is int categoryId && selectedIds.Contains(categoryId))
                {
                    check.IsChecked = true;
                }
            }
        }
    }

    private async Task SaveAsync()
    {
        if (AppState.CurrentUser == null)
        {
            Close(false);
            return;
        }

        if (_gameBox.SelectedItem is not Game selectedGame)
        {
            await DialogHelper.ShowAsync(this, "Missing game", "Please choose a game before saving the article.");
            return;
        }

        var title = _titleBox.Text?.Trim() ?? string.Empty;
        var content = _contentBox.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
        {
            await DialogHelper.ShowAsync(this, "Missing details", "Please enter both an article title and content.");
            return;
        }

        var article = _article ?? new WikiArticle
        {
            AuthorId = AppState.CurrentUser.UserId
        };

        article.GameId = selectedGame.GameId;
        article.Title = title;
        article.Slug = SlugGenerator.Generate(title);
        article.Summary = string.IsNullOrWhiteSpace(_summaryBox.Text)
            ? (content.Length > 240 ? content[..240] : content)
            : _summaryBox.Text.Trim();
        article.Content = content;
        article.CoverImage = string.IsNullOrWhiteSpace(_coverBox.Text) ? null : _coverBox.Text.Trim();
        article.IsPublished = _publishedBox.IsChecked == true;

        var categoryIds = _categoryPanel.Children
            .OfType<CheckBox>()
            .Where(x => x.IsChecked == true && x.Tag is int)
            .Select(x => (int)x.Tag!)
            .ToList();

        if (_articleId <= 0)
        {
            var id = await _articles.CreateAsync(article);
            if (id > 0)
            {
                article.ArticleId = id;
                await _articles.SetCategoriesAsync(id, categoryIds);
                await _images.ReplaceAsync(id, AppState.CurrentUser.UserId, ParseGalleryUrls());
                await SaveWikiLinksAsync(article.ArticleId, selectedGame.GameId, content);
                Close(true);
                return;
            }

            await DialogHelper.ShowAsync(this, "Save failed", "The article could not be created. Please try again.");
        }
        else
        {
            article.ArticleId = _articleId;
            if (await _articles.UpdateAsync(article, AppState.CurrentUser.UserId))
            {
                await _articles.SetCategoriesAsync(_articleId, categoryIds);
                await _images.ReplaceAsync(_articleId, AppState.CurrentUser.UserId, ParseGalleryUrls());
                await SaveWikiLinksAsync(_articleId, selectedGame.GameId, content);
                Close(true);
                return;
            }

            await DialogHelper.ShowAsync(this, "Save failed", "The article could not be updated. Please try again.");
        }
    }

    private static readonly System.Text.RegularExpressions.Regex WikiLinkPattern =
        new(@"\[\[([^\]]+)\]\]", System.Text.RegularExpressions.RegexOptions.Compiled);

    private async Task SaveWikiLinksAsync(int articleId, int gameId, string content)
    {
        var matchCollection = WikiLinkPattern.Matches(content);
        if (matchCollection.Count == 0)
        {
            await _articles.ReplaceLinksAsync(articleId, Enumerable.Empty<(int, string)>());
            return;
        }

        var allMatches = matchCollection.Cast<System.Text.RegularExpressions.Match>().ToList();

        var linkTitles = allMatches
            .Select(m => m.Groups[1].Value.Trim())
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var resolved = await _articles.ResolveTitlesToIdsAsync(gameId, linkTitles);

        var links = new List<(int ToArticleId, string LinkText)>();
        foreach (var match in allMatches)
        {
            var linkText = match.Groups[1].Value.Trim();
            if (string.IsNullOrWhiteSpace(linkText))
            {
                continue;
            }

            if (resolved.TryGetValue(linkText, out var toArticleId))
            {
                links.Add((toArticleId, linkText));
            }
        }

        await _articles.ReplaceLinksAsync(articleId, links);
    }

    private IEnumerable<string> ParseGalleryUrls()
    {
        return (_galleryBox.Text ?? string.Empty)
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToArray();
    }

    private async Task DeleteAsync()
    {
        if (_articleId <= 0)
        {
            return;
        }

        if (AppState.CurrentUser == null)
        {
            Close(false);
            return;
        }

        if (_article != null &&
            !AppState.IsAdmin &&
            _article.AuthorId != AppState.CurrentUser.UserId)
        {
            return;
        }

        if (await _articles.DeleteAsync(_articleId))
        {
            Close(true);
        }
    }
}
