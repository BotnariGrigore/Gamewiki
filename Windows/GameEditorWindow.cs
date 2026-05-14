using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using GameWikiApp;
using GameWikiApp.Helpers;
using GameWikiApp.Models;
using GameWikiApp.Services;

namespace GameWikiApp.Views;

public sealed class GameEditorWindow : Window
{
    private readonly int _gameId;
    private readonly GameService _games = new();
    private readonly TagService _tags = new();

    private readonly TextBox _titleBox;
    private readonly TextBox _slugBox;
    private readonly TextBox _shortBox;
    private readonly TextBox _fullBox;
    private readonly TextBox _coverBox;
    private readonly TextBox _bannerBox;
    private readonly StackPanel _genrePanel = new();
    private Game? _game;

    private static readonly string[] ImageFilters = ["*.jpg", "*.jpeg", "*.png", "*.gif", "*.bmp", "*.webp"];

    public GameEditorWindow(int gameId = 0)
    {
        _gameId = gameId;
        Title = gameId > 0 ? "Edit game" : "Create game";
        Width = 900;
        Height = 720;
        MinWidth = 800;
        MinHeight = 620;
        Background = ThemePalette.BgPrimaryBrush;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        _titleBox = UiFactory.CreateTextBox("Game title", 400);
        _slugBox = UiFactory.CreateTextBox("Slug", 300);
        _slugBox.IsReadOnly = true;
        _shortBox = UiFactory.CreateTextBox("Short description", 760);
        _shortBox.AcceptsReturn = true;
        _shortBox.TextWrapping = TextWrapping.Wrap;
        _shortBox.Height = 90;
        _fullBox = UiFactory.CreateTextBox("Full description", 760);
        _fullBox.AcceptsReturn = true;
        _fullBox.TextWrapping = TextWrapping.Wrap;
        _fullBox.Height = 210;
        _coverBox = UiFactory.CreateTextBox("Cover image url or path", 280);
        _bannerBox = UiFactory.CreateTextBox("Banner image url or path", 280);

        Content = BuildLayout();
        Loaded += async (_, __) => await LoadAsync();
    }

    private Control BuildLayout()
    {
        var root = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = new StackPanel
            {
                Spacing = 14,
                Margin = new Thickness(20),
                Children =
                {
                    new Border
                    {
                        Background = ThemePalette.SurfaceBrush,
                        BorderBrush = ThemePalette.BorderLightBrush,
                        BorderThickness = new Thickness(1),
                        CornerRadius = new CornerRadius(18),
                        Padding = new Thickness(20),
                        Child = new TextBlock
                        {
                            Text = Title ?? "Game editor",
                            FontSize = 24,
                            FontWeight = FontWeight.Bold,
                            Foreground = ThemePalette.TextPrimaryBrush
                        }
                    },
                    BuildField("Title", _titleBox),
                    BuildField("Slug", _slugBox),
                    BuildField("Short description", _shortBox),
                    BuildField("Full description", _fullBox),
                    BuildTwoImageFields("Cover image", _coverBox, "Banner image", _bannerBox),
                    BuildGenresPanel(),
                    BuildButtons()
                }
            },
            Background = ThemePalette.BgPrimaryBrush
        };

        _titleBox.TextChanged += (_, __) =>
        {
            if (_game == null || string.IsNullOrWhiteSpace(_slugBox.Text))
            {
                _slugBox.Text = SlugGenerator.Generate(_titleBox.Text);
            }
        };

        return root;
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

    private static Control BuildTwoFields(string leftLabel, Control leftEditor, string rightLabel, Control rightEditor)
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("1*,1*"),
            ColumnSpacing = 12
        };

        var left = BuildField(leftLabel, leftEditor);
        var right = BuildField(rightLabel, rightEditor);
        grid.Children.Add(left);
        grid.Children.Add(right);
        Grid.SetColumn(right, 1);
        return grid;
    }

    private Control BuildTwoImageFields(string leftLabel,TextBox leftEditor, string rightLabel,TextBox rightEditor)
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("1*,1*"),
            ColumnSpacing = 12
        };

        var left = BuildImageField(leftLabel, leftEditor);
        var right = BuildImageField(rightLabel, rightEditor);
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
        var browse = UiFactory.CreateSubtleButton("Browse...", 80);
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

    private static async Task<string?> CopyToPhotoFolderAsync(string sourcePath)
    {
        try
        {
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

    private Control BuildGenresPanel()
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
            Text = "Genres",
            FontSize = 13,
            FontWeight = FontWeight.Bold,
            Foreground = ThemePalette.TextPrimaryBrush
        });

        stack.Children.Add(new TextBlock
        {
            Text = "Select the global genres that should appear on this game.",
            FontSize = 11,
            Foreground = ThemePalette.TextMutedBrush,
            TextWrapping = TextWrapping.Wrap
        });

        var scroll = new ScrollViewer
        {
            Height = 170,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            Content = _genrePanel
        };
        _genrePanel.Spacing = 8;
        stack.Children.Add(scroll);

        shell.Child = stack;
        return shell;
    }

    private Control BuildButtons()
    {
        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10
        };

        var save = UiFactory.CreatePrimaryButton("Save", 120);
        save.Click += async (_, __) => await SaveAsync();
        row.Children.Add(save);

        var delete = UiFactory.CreateSubtleButton("Delete", 120);
        delete.IsVisible = _gameId > 0;
        delete.Click += async (_, __) => await DeleteAsync();
        row.Children.Add(delete);

        var close = UiFactory.CreateSubtleButton("Close", 120);
        close.Click += (_, __) => Close(false);
        row.Children.Add(close);

        return row;
    }

    private async Task LoadAsync()
    {
        if (_gameId <= 0)
        {
            _slugBox.Text = string.Empty;
            await LoadGenresAsync();
            return;
        }

        _game = await _games.GetByIdAsync(_gameId);
        if (_game == null)
        {
            Close(false);
            return;
        }

        _titleBox.Text = _game.Title;
        _slugBox.Text = _game.Slug;
        _shortBox.Text = _game.ShortDescription ?? string.Empty;
        _fullBox.Text = _game.FullDescription ?? string.Empty;
        _coverBox.Text = _game.CoverImage ?? string.Empty;
        _bannerBox.Text = _game.BannerImage ?? string.Empty;
        await LoadGenresAsync();
    }

    private async Task LoadGenresAsync()
    {
        _genrePanel.Children.Clear();

        var genres = (await _tags.GetAllAsync()).ToList();
        if (genres.Count == 0)
        {
            _genrePanel.Children.Add(new TextBlock
            {
                Text = "No genres yet. Create them in the dashboard first.",
                Foreground = ThemePalette.TextMutedBrush
            });
            return;
        }

        var selectedGenres = (_game?.Genres ?? new List<string>())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var genre in genres)
        {
            var check = new CheckBox
            {
                Content = $"{genre.TagName} ({genre.GameCount})",
                Tag = genre.TagId,
                Foreground = ThemePalette.TextPrimaryBrush,
                IsChecked = selectedGenres.Contains(genre.TagName)
            };
            _genrePanel.Children.Add(check);
        }
    }

    private async Task SaveAsync()
    {
        if (!AppState.IsAdmin || AppState.CurrentUser == null)
        {
            Close(false);
            return;
        }

        var title = _titleBox.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(title))
        {
            await DialogHelper.ShowAsync(this, "Missing title", "Please enter a game title before saving.");
            return;
        }

        var game = _game ?? new Game { CreatedBy = AppState.CurrentUser.UserId };
        game.Title = title;
        game.Slug = SlugGenerator.Generate(title);
        game.ShortDescription = string.IsNullOrWhiteSpace(_shortBox.Text) ? null : _shortBox.Text.Trim();
        game.FullDescription = string.IsNullOrWhiteSpace(_fullBox.Text) ? null : _fullBox.Text.Trim();
        game.CoverImage = string.IsNullOrWhiteSpace(_coverBox.Text) ? null : _coverBox.Text.Trim();
        game.BannerImage = string.IsNullOrWhiteSpace(_bannerBox.Text) ? null : _bannerBox.Text.Trim();

        if (_gameId <= 0)
        {
            var id = await _games.CreateAsync(game);
            if (id > 0)
            {
                if (await _tags.SetGameTagsAsync(id, GetSelectedGenreIds()))
                {
                    Close(true);
                    return;
                }

                await DialogHelper.ShowAsync(this, "Save failed", "The game was created, but genres could not be saved.");
                return;
            }

            await DialogHelper.ShowAsync(this, "Save failed", "The game could not be created. Please try again.");
        }
        else
        {
            game.GameId = _gameId;
            if (await _games.UpdateAsync(game))
            {
                if (await _tags.SetGameTagsAsync(_gameId, GetSelectedGenreIds()))
                {
                    Close(true);
                    return;
                }

                await DialogHelper.ShowAsync(this, "Save failed", "The game was updated, but genres could not be saved.");
                return;
            }

            await DialogHelper.ShowAsync(this, "Save failed", "The game could not be updated. Please try again.");
        }
    }

    private IEnumerable<int> GetSelectedGenreIds()
    {
        return _genrePanel.Children
            .OfType<CheckBox>()
            .Where(x => x.IsChecked == true && x.Tag is int)
            .Select(x => (int)x.Tag!)
            .ToArray();
    }

    private async Task DeleteAsync()
    {
        if (_gameId <= 0)
        {
            return;
        }

        if (!AppState.IsAdmin)
        {
            Close(false);
            return;
        }

        if (await _games.DeleteAsync(_gameId))
        {
            Close(true);
        }
    }
}
