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
using GameWikiApp.Data;
using GameWikiApp.Models;
using GameWikiApp.Services;

namespace GameWikiApp.Views;

public sealed class ArticleView : UserControl
{
    private readonly int _articleId;
    private readonly Action<int> _openGame;
    private readonly Action<int> _openArticle;
    private readonly ArticleService _articles = new();
    private readonly CommentService _comments = new();
    private readonly LikeService _likes = new();
    private readonly SavedArticleService _saved = new();
    private readonly ArticleImageRepository _images = new();

    private readonly StackPanel _content = new();
    private readonly WrapPanel _galleryPanel = new();
    private readonly WrapPanel _linkedPanel = new();
    private readonly WrapPanel _relatedPanel = new();
    private readonly StackPanel _commentsPanel = new();
    private readonly TextBox _commentBox = new();
    private readonly Button _likeButton = new();
    private readonly Button _saveButton = new();
    private readonly Button _editButton = new();
    private readonly TextBlock _title = new();
    private readonly TextBlock _meta = new();
    private readonly TextBlock _counts = new();
    private readonly TextBlock _gameChip = new();
    private readonly Border _cover = new();
    private readonly TextBlock _contentText = new();

    private WikiArticle? _article;
    private bool _isLiked;
    private bool _isSaved;

    public ArticleView(int articleId, Action<int> openGame, Action<int> openArticle)
    {
        _articleId = articleId;
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
        _content.Children.Add(BuildHero());
        _content.Children.Add(BuildContentCard());
        _content.Children.Add(BuildGallerySection());
        _content.Children.Add(BuildLinkedSection());
        _content.Children.Add(BuildRelatedSection());
        _content.Children.Add(BuildCommentsSection());

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
            Padding = new Thickness(0),
            Margin = new Thickness(0, 0, 0, 10)
        };

        var grid = new Grid
        {
            RowDefinitions = new RowDefinitions("260,Auto")
        };

        _cover.Height = 260;
        _cover.Background = ThemePalette.BgTertiaryBrush;
        _cover.Child = new TextBlock
        {
            Text = "Loading...",
            FontSize = 18,
            FontWeight = FontWeight.Bold,
            Foreground = ThemePalette.TextMutedBrush,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        grid.Children.Add(_cover);
        Grid.SetRow(_cover, 0);

        var header = new StackPanel
        {
            Spacing = 10,
            Margin = new Thickness(24, 20, 24, 24)
        };

        var actionRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10
        };

        var backButton = UiFactory.CreateSubtleButton("Back to game", 120);
        backButton.Click += (_, __) =>
        {
            if (_article != null)
            {
                _openGame(_article.GameId);
            }
        };
        actionRow.Children.Add(backButton);

        _likeButton.Content = "Like";
        _likeButton.Click += async (_, __) => await ToggleLikeAsync();
        actionRow.Children.Add(_likeButton);

        _saveButton.Content = "Save";
        _saveButton.Click += async (_, __) => await ToggleSaveAsync();
        actionRow.Children.Add(_saveButton);

        _editButton.Content = "Edit";
        _editButton.IsVisible = false;
        _editButton.Click += async (_, __) => await OpenEditorAsync();
        actionRow.Children.Add(_editButton);

        header.Children.Add(actionRow);

        _gameChip.Text = "Game";
        _gameChip.FontSize = 11;
        _gameChip.FontWeight = FontWeight.Bold;
        _gameChip.Foreground = ThemePalette.AccentBrush;
        header.Children.Add(_gameChip);

        _title.Text = "Loading...";
        _title.FontSize = 28;
        _title.FontWeight = FontWeight.Bold;
        _title.Foreground = ThemePalette.TextPrimaryBrush;
        header.Children.Add(_title);

        _meta.Text = string.Empty;
        _meta.FontSize = 12;
        _meta.Foreground = ThemePalette.TextSecondaryBrush;
        header.Children.Add(_meta);

        _counts.Text = string.Empty;
        _counts.FontSize = 11;
        _counts.Foreground = ThemePalette.TextMutedBrush;
        header.Children.Add(_counts);

        grid.Children.Add(header);
        Grid.SetRow(header, 1);

        shell.Child = grid;
        return shell;
    }

    private Control BuildContentCard()
    {
        var shell = new Border
        {
            Background = ThemePalette.BgSecondaryBrush,
            BorderBrush = ThemePalette.BorderLightBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(18),
            Padding = new Thickness(22)
        };

        _contentText.TextWrapping = TextWrapping.Wrap;
        _contentText.Foreground = ThemePalette.TextPrimaryBrush;
        _contentText.FontSize = 13;
        shell.Child = _contentText;
        return shell;
    }

    private Control BuildGallerySection()
    {
        var shell = UiFactory.CreateCard();
        shell.Margin = new Thickness(0, 0, 0, 0);
        shell.Background = ThemePalette.BgSecondaryBrush;

        var stack = new StackPanel
        {
            Spacing = 12
        };

        stack.Children.Add(SectionTitle("Gallery"));

        _galleryPanel.Orientation = Orientation.Horizontal;
        stack.Children.Add(_galleryPanel);

        shell.Child = stack;
        return shell;
    }

    private Control BuildLinkedSection()
    {
        var shell = UiFactory.CreateCard();
        shell.Background = ThemePalette.BgSecondaryBrush;

        var stack = new StackPanel
        {
            Spacing = 12
        };
        stack.Children.Add(SectionTitle("Linked articles"));

        _linkedPanel.Orientation = Orientation.Horizontal;
        stack.Children.Add(_linkedPanel);

        shell.Child = stack;
        return shell;
    }

    private Control BuildRelatedSection()
    {
        var shell = UiFactory.CreateCard();
        shell.Background = ThemePalette.BgSecondaryBrush;

        var stack = new StackPanel
        {
            Spacing = 12
        };
        stack.Children.Add(SectionTitle("Related articles"));

        _relatedPanel.Orientation = Orientation.Horizontal;
        stack.Children.Add(_relatedPanel);

        shell.Child = stack;
        return shell;
    }

    private Control BuildCommentsSection()
    {
        var shell = UiFactory.CreateCard();
        shell.Background = ThemePalette.BgSecondaryBrush;

        var stack = new StackPanel
        {
            Spacing = 12
        };
        stack.Children.Add(SectionTitle("Comments"));

        var inputRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10
        };

        _commentBox.Width = 720;
        _commentBox.Height = 42;
        _commentBox.PlaceholderText = "Write a comment...";
        _commentBox.Background = ThemePalette.BgInputBrush;
        _commentBox.BorderBrush = ThemePalette.BorderBrush;
        _commentBox.Foreground = ThemePalette.TextPrimaryBrush;
        _commentBox.CaretBrush = ThemePalette.AccentBrush;
        _commentBox.CornerRadius = new CornerRadius(12);
        _commentBox.Padding = new Thickness(12, 10);
        inputRow.Children.Add(_commentBox);

        var postButton = UiFactory.CreatePrimaryButton("Post", 90);
        postButton.Click += async (_, __) => await PostCommentAsync();
        inputRow.Children.Add(postButton);

        stack.Children.Add(inputRow);
        stack.Children.Add(_commentsPanel);

        shell.Child = stack;
        return shell;
    }

    private static Control SectionTitle(string text)
    {
        return new TextBlock
        {
            Text = text,
            FontSize = 15,
            FontWeight = FontWeight.Bold,
            Foreground = ThemePalette.TextPrimaryBrush
        };
    }

    public async Task LoadAsync()
    {
        _article = await _articles.GetByIdAsync(_articleId);
        if (_article == null)
        {
            _title.Text = "Article not found";
            _meta.Text = string.Empty;
            _counts.Text = string.Empty;
            _gameChip.Text = "Unknown game";
            _contentText.Text = string.Empty;
            _cover.Child = BuildCoverContent(null, "Article not found");
            _isLiked = false;
            _isSaved = false;
            _editButton.IsVisible = false;
            UpdateActionButtons();
            _galleryPanel.Children.Clear();
            _linkedPanel.Children.Clear();
            _relatedPanel.Children.Clear();
            _commentsPanel.Children.Clear();
            _galleryPanel.Children.Add(new TextBlock
            {
                Text = "No gallery images.",
                Foreground = ThemePalette.TextMutedBrush
            });
            _linkedPanel.Children.Add(new TextBlock
            {
                Text = "No linked articles.",
                Foreground = ThemePalette.TextMutedBrush
            });
            _relatedPanel.Children.Add(new TextBlock
            {
                Text = "No related articles.",
                Foreground = ThemePalette.TextMutedBrush
            });
            _commentsPanel.Children.Add(new TextBlock
            {
                Text = "No comments yet.",
                Foreground = ThemePalette.TextMutedBrush
            });
            return;
        }

        await _articles.IncrementViewsAsync(_articleId);
        _article = await _articles.GetByIdAsync(_articleId);
        if (_article == null)
        {
            _title.Text = "Article not found";
            _meta.Text = string.Empty;
            _counts.Text = string.Empty;
            _gameChip.Text = "Unknown game";
            _contentText.Text = string.Empty;
            _cover.Child = BuildCoverContent(null, "Article not found");
            _isLiked = false;
            _isSaved = false;
            _editButton.IsVisible = false;
            UpdateActionButtons();
            _galleryPanel.Children.Clear();
            _linkedPanel.Children.Clear();
            _relatedPanel.Children.Clear();
            _commentsPanel.Children.Clear();
            return;
        }

        _title.Text = _article.Title;
        _gameChip.Text = _article.GameTitle ?? "Unknown game";
        _meta.Text = $"By {_article.AuthorUsername ?? "Unknown"} | Updated {_article.UpdatedAt:g}";
        _counts.Text = $"{_article.ViewsCount} views | {_article.LikeCount} likes | {_article.CommentCount} comments";
        _contentText.Text = _article.Content;

        var bitmap = await ImageLoader.LoadAsync(_article.CoverImage);
        _cover.Child = BuildCoverContent(bitmap, _article.Title);

        _editButton.IsVisible = AppState.CurrentUser != null &&
                                (AppState.IsAdmin || AppState.CurrentUser.UserId == _article.AuthorId);

        if (AppState.CurrentUser != null)
        {
            _isLiked = await _likes.HasLikedAsync(_articleId, AppState.CurrentUser.UserId);
            _isSaved = await _saved.IsSavedAsync(_articleId, AppState.CurrentUser.UserId);
        }
        else
        {
            _isLiked = false;
            _isSaved = false;
        }

        UpdateActionButtons();
        await LoadGalleryAsync();
        await LoadLinkedArticlesAsync();
        await LoadRelatedArticlesAsync();
        await LoadCommentsAsync();
    }

    private static Control BuildCoverContent(Bitmap? bitmap, string title)
    {
        if (bitmap != null)
        {
            return new Image
            {
                Source = bitmap,
                Stretch = Stretch.Uniform,
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

    private void UpdateActionButtons()
    {
        var hasArticle = _article != null;
        _likeButton.IsEnabled = hasArticle && AppState.CurrentUser != null;
        _saveButton.IsEnabled = hasArticle && AppState.CurrentUser != null;
        _likeButton.Content = _isLiked ? "Liked" : "Like";
        _saveButton.Content = _isSaved ? "Saved" : "Save";
        _commentBox.IsEnabled = hasArticle && AppState.CurrentUser != null;
        _commentBox.PlaceholderText = !hasArticle
            ? "Article unavailable"
            : (AppState.CurrentUser == null ? "Sign in to comment" : "Write a comment...");
    }

    private async Task LoadGalleryAsync()
    {
        _galleryPanel.Children.Clear();
        var list = (await _images.GetByArticleIdAsync(_articleId)).ToList();
        if (list.Count == 0)
        {
            _galleryPanel.Children.Add(new TextBlock
            {
                Text = "No gallery images.",
                Foreground = ThemePalette.TextMutedBrush
            });
            return;
        }

        foreach (var image in list)
        {
            var bitmap = await ImageLoader.LoadAsync(image.ImageUrl);
            _galleryPanel.Children.Add(CreateGalleryThumb(bitmap, image.AltText ?? _article?.Title ?? "Image"));
        }
    }

    private static Control CreateGalleryThumb(Bitmap? bitmap, string title)
    {
        if (bitmap != null)
        {
            return new Border
            {
                Width = 160,
                Height = 110,
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
            Width = 160,
            Height = 110,
            CornerRadius = new CornerRadius(14),
            Background = ThemePalette.BgTertiaryBrush,
            Child = new TextBlock
            {
                Text = title,
                Foreground = ThemePalette.TextSecondaryBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };
    }

    private async Task LoadLinkedArticlesAsync()
    {
        _linkedPanel.Children.Clear();
        var list = (await _articles.GetLinkedArticlesAsync(_articleId)).ToList();
        if (list.Count == 0)
        {
            _linkedPanel.Children.Add(new TextBlock
            {
                Text = "No linked articles.",
                Foreground = ThemePalette.TextMutedBrush
            });
            return;
        }

        foreach (var link in list)
        {
            var button = UiFactory.CreateSubtleButton(string.IsNullOrWhiteSpace(link.LinkText) ? link.TargetTitle ?? "Open" : link.LinkText, double.NaN);
            button.Click += (_, __) => _openArticle(link.ToArticleId);
            _linkedPanel.Children.Add(button);
        }
    }

    private async Task LoadRelatedArticlesAsync()
    {
        _relatedPanel.Children.Clear();
        if (_article == null)
        {
            return;
        }

        var list = (await _articles.GetRelatedAsync(_articleId, _article.GameId, 8)).ToList();
        if (list.Count == 0)
        {
            _relatedPanel.Children.Add(new TextBlock
            {
                Text = "No related articles.",
                Foreground = ThemePalette.TextMutedBrush
            });
            return;
        }

        foreach (var article in list)
        {
            _relatedPanel.Children.Add(CreateRelatedCard(article));
        }
    }

    private Control CreateRelatedCard(WikiArticle article)
    {
        var card = new Border
        {
            Background = ThemePalette.BgCardBrush,
            BorderBrush = ThemePalette.BorderLightBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(16),
            Width = 230,
            Padding = new Thickness(14),
            Cursor = new Cursor(StandardCursorType.Hand)
        };

        var stack = new StackPanel
        {
            Spacing = 8
        };

        stack.Children.Add(new TextBlock
        {
            Text = article.Title,
            FontSize = 11.5,
            FontWeight = FontWeight.Bold,
            Foreground = ThemePalette.TextPrimaryBrush
        });

        stack.Children.Add(new TextBlock
        {
            Text = article.Summary ?? string.Empty,
            FontSize = 10,
            Foreground = ThemePalette.TextMutedBrush,
            TextWrapping = TextWrapping.Wrap,
            MaxLines = 4
        });

        stack.Children.Add(new TextBlock
        {
            Text = $"{article.ViewsCount} views",
            FontSize = 9,
            Foreground = ThemePalette.TextMutedBrush
        });

        card.Child = stack;
        card.PointerPressed += (_, __) => _openArticle(article.ArticleId);
        return card;
    }

    private async Task LoadCommentsAsync()
    {
        _commentsPanel.Children.Clear();
        var list = (await _comments.GetByArticleIdAsync(_articleId)).ToList();
        if (list.Count == 0)
        {
            _commentsPanel.Children.Add(new TextBlock
            {
                Text = "No comments yet.",
                Foreground = ThemePalette.TextMutedBrush
            });
            return;
        }

        foreach (var comment in list)
        {
            _commentsPanel.Children.Add(CreateCommentCard(comment));
        }
    }

    private static Control CreateCommentCard(ArticleComment comment)
    {
        var card = new Border
        {
            Background = ThemePalette.BgCardBrush,
            BorderBrush = ThemePalette.BorderLightBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(16),
            Padding = new Thickness(14),
            Margin = new Thickness(0, 0, 0, 10)
        };

        var stack = new StackPanel
        {
            Spacing = 4
        };

        stack.Children.Add(new TextBlock
        {
            Text = comment.Username,
            FontSize = 11,
            FontWeight = FontWeight.Bold,
            Foreground = ThemePalette.TextPrimaryBrush
        });

        stack.Children.Add(new TextBlock
        {
            Text = comment.CreatedAt.ToString("g"),
            FontSize = 9,
            Foreground = ThemePalette.TextMutedBrush
        });

        stack.Children.Add(new TextBlock
        {
            Text = comment.CommentText,
            FontSize = 11,
            Foreground = ThemePalette.TextSecondaryBrush,
            TextWrapping = TextWrapping.Wrap
        });

        card.Child = stack;
        return card;
    }

    private async Task ToggleLikeAsync()
    {
        if (AppState.CurrentUser == null || _article == null)
        {
            return;
        }

        _isLiked = await _likes.ToggleLikeAsync(_articleId, AppState.CurrentUser.UserId);
        if (_article != null)
        {
            _article.LikeCount = await _likes.GetCountAsync(_articleId);
            _counts.Text = $"{_article.ViewsCount} views | {_article.LikeCount} likes | {_article.CommentCount} comments";
        }

        UpdateActionButtons();
    }

    private async Task ToggleSaveAsync()
    {
        if (AppState.CurrentUser == null || _article == null)
        {
            return;
        }

        _isSaved = await _saved.ToggleSaveAsync(_articleId, AppState.CurrentUser.UserId);
        UpdateActionButtons();
    }

    private async Task PostCommentAsync()
    {
        if (AppState.CurrentUser == null || _article == null)
        {
            return;
        }

        var text = _commentBox.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        await _comments.CreateAsync(new Comment
        {
            ArticleId = _articleId,
            UserId = AppState.CurrentUser.UserId,
            CommentText = text
        });

        if (_article != null)
        {
            var comments = await _comments.GetByArticleIdAsync(_articleId);
            _article.CommentCount = comments.Count();
            _counts.Text = $"{_article.ViewsCount} views | {_article.LikeCount} likes | {_article.CommentCount} comments";
        }

        _commentBox.Text = string.Empty;
        await LoadCommentsAsync();
    }

    private async Task OpenEditorAsync()
    {
        if (_article == null)
        {
            return;
        }

        var owner = TopLevel.GetTopLevel(this) as Window;
        if (owner == null)
        {
            return;
        }

        var editor = new ArticleEditorWindow(_article.ArticleId, _article.GameId);
        await editor.ShowDialog<bool>(owner);
        await LoadAsync();
    }
}
