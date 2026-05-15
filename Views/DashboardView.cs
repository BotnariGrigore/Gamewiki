using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using GameWikiApp.Data;
using GameWikiApp.Models;
using GameWikiApp.Services;

namespace GameWikiApp.Views;

public sealed class DashboardView : UserControl
{
    private readonly UserRepository _users = new();
    private readonly GameService _games = new();
    private readonly TagService _tags = new();
    private readonly CategoryService _categories = new();
    private readonly ArticleService _articles = new();

    private readonly StackPanel _content = new();
    private readonly StackPanel _usersPanel = new();
    private readonly StackPanel _gamesPanel = new();
    private readonly StackPanel _genresPanel = new();
    private readonly StackPanel _categoriesPanel = new();
    private readonly ComboBox _categoryGameBox = new();
    private readonly TextBox _categoryNameBox;
    private readonly TextBox _categoryDescriptionBox;
    private readonly TextBox _genreNameBox;
    private readonly TextBlock _statsText = new();
    private readonly TextBlock _usersCount = new();
    private readonly TextBlock _gamesCount = new();
    private readonly TextBlock _articlesCount = new();
    private readonly TextBlock _genresCount = new();
    private readonly TextBlock _categoriesCount = new();
    private readonly List<Game> _gameList = new();

    private int _selectedCategoryId;

    public DashboardView()
    {
        _categoryNameBox = UiFactory.CreateTextBox("Category name", 260);
        _categoryDescriptionBox = UiFactory.CreateTextBox("Description", 420);
        _genreNameBox = UiFactory.CreateTextBox("Genre name", 260);

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
        _content.Children.Add(BuildGamesSection());
        _content.Children.Add(BuildGenresSection());
        _content.Children.Add(BuildCategoriesSection());

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
            Text = "Admin Dashboard",
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
        stats.Children.Add(CreateStatTile("Users", _usersCount));
        stats.Children.Add(CreateStatTile("Games", _gamesCount));
        stats.Children.Add(CreateStatTile("Genres", _genresCount));
        stats.Children.Add(CreateStatTile("Articles", _articlesCount));
        stats.Children.Add(CreateStatTile("Categories", _categoriesCount));
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

        stack.Children.Add(CreateSectionHeader("Users", "Role management and moderation"));

        _usersPanel.Spacing = 10;
        stack.Children.Add(_usersPanel);

        shell.Child = stack;
        return shell;
    }

    private Control BuildGamesSection()
    {
        var shell = UiFactory.CreateCard();
        shell.Background = ThemePalette.BgSecondaryBrush;

        var stack = new StackPanel
        {
            Spacing = 12
        };

        var headerRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"),
            ColumnSpacing = 10
        };
        headerRow.Children.Add(CreateSectionHeader("Games", "Wiki page management"));

        var addButton = UiFactory.CreatePrimaryButton("Add game", 110, "＋");
        addButton.Click += async (_, __) => await OpenGameEditorAsync(0);
        headerRow.Children.Add(addButton);
        Grid.SetColumn(addButton, 1);

        var refreshButton = UiFactory.CreateSubtleButton("Refresh", 90, "↻");
        refreshButton.Click += async (_, __) => await LoadGamesAsync();
        headerRow.Children.Add(refreshButton);
        Grid.SetColumn(refreshButton, 2);

        stack.Children.Add(headerRow);

        _gamesPanel.Spacing = 10;
        stack.Children.Add(_gamesPanel);

        shell.Child = stack;
        return shell;
    }

    private Control BuildGenresSection()
    {
        var shell = UiFactory.CreateCard();
        shell.Background = ThemePalette.BgSecondaryBrush;

        var stack = new StackPanel
        {
            Spacing = 12
        };

        stack.Children.Add(CreateSectionHeader("Genres", "Global tags used to group games"));

        var editor = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto,Auto"),
            ColumnSpacing = 10
        };

        editor.Children.Add(_genreNameBox);

        var addButton = UiFactory.CreatePrimaryButton("Add genre", 110, "＋");
        addButton.Click += async (_, __) => await AddGenreAsync();
        editor.Children.Add(addButton);
        Grid.SetColumn(addButton, 1);

        var refreshButton = UiFactory.CreateSubtleButton("Refresh", 90, "↻");
        refreshButton.Click += async (_, __) => await LoadGenresAsync();
        editor.Children.Add(refreshButton);
        Grid.SetColumn(refreshButton, 2);

        stack.Children.Add(editor);

        _genresPanel.Spacing = 10;
        stack.Children.Add(_genresPanel);

        shell.Child = stack;
        return shell;
    }

    private Control BuildCategoriesSection()
    {
        var shell = UiFactory.CreateCard();
        shell.Background = ThemePalette.BgSecondaryBrush;

        var stack = new StackPanel
        {
            Spacing = 12
        };

        stack.Children.Add(CreateSectionHeader("Wiki categories", "Per-game article groups like Bosses, Weapons, NPCs"));

        var editor = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("240,260,*,Auto"),
            ColumnSpacing = 10
        };

        _categoryGameBox.Width = 220;
        _categoryGameBox.Background = ThemePalette.BgInputBrush;
        _categoryGameBox.BorderBrush = ThemePalette.BorderBrush;
        _categoryGameBox.Foreground = ThemePalette.TextPrimaryBrush;
        _categoryGameBox.CornerRadius = new CornerRadius(12);
        _categoryGameBox.Padding = new Thickness(12, 10);
        _categoryGameBox.DisplayMemberBinding = new Binding(nameof(Game.Title));
        editor.Children.Add(_categoryGameBox);

        editor.Children.Add(_categoryNameBox);
        Grid.SetColumn(_categoryNameBox, 1);

        _categoryDescriptionBox.Width = 420;
        editor.Children.Add(_categoryDescriptionBox);
        Grid.SetColumn(_categoryDescriptionBox, 2);

        var saveButton = UiFactory.CreatePrimaryButton("Save", 90, "✓");
        saveButton.Click += async (_, __) => await SaveCategoryAsync();
        editor.Children.Add(saveButton);
        Grid.SetColumn(saveButton, 3);

        stack.Children.Add(editor);

        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10
        };

        var clearButton = UiFactory.CreateSubtleButton("Clear", 90);
        clearButton.Click += (_, __) => ClearCategoryEditor();
        actions.Children.Add(clearButton);

        var refreshButton = UiFactory.CreateSubtleButton("Refresh", 90);
        refreshButton.Click += async (_, __) => await LoadCategoriesAsync();
        actions.Children.Add(refreshButton);

        stack.Children.Add(actions);

        _categoriesPanel.Spacing = 10;
        stack.Children.Add(_categoriesPanel);

        shell.Child = stack;
        return shell;
    }

    private static Border CreateSectionHeader(string title, string caption)
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto")
        };

        grid.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 18,
            FontWeight = FontWeight.Bold,
            Foreground = ThemePalette.TextPrimaryBrush
        });

        grid.Children.Add(new TextBlock
        {
            Text = caption,
            FontSize = 12,
            FontWeight = FontWeight.SemiBold,
            Foreground = ThemePalette.TextMutedBrush,
            HorizontalAlignment = HorizontalAlignment.Right
        });
        Grid.SetColumn(grid.Children[1], 1);

        return new Border
        {
            Background = Brushes.Transparent,
            Child = grid
        };
    }

    public async Task LoadAsync()
    {
        await LoadUsersAsync();
        await LoadGamesAsync();
        await LoadGenresAsync();
        await LoadCategoryGamesAsync();
        await LoadCategoriesAsync();

        var users = await _users.GetAllAsync();
        var games = await _games.GetAllAsync();
        var genres = await _tags.GetAllAsync();
        var articles = await _articles.GetRecentAsync(1000);
        var categories = await _categories.GetAllAsync();

        _usersCount.Text = users.Count().ToString();
        _gamesCount.Text = games.Count().ToString();
        _genresCount.Text = genres.Count().ToString();
        _articlesCount.Text = articles.Count().ToString();
        _categoriesCount.Text = categories.Count().ToString();
        _statsText.Text = $"Loaded {users.Count()} users, {games.Count()} games, {genres.Count()} genres, {articles.Count()} articles and {categories.Count()} wiki categories.";
    }

    private async Task LoadUsersAsync()
    {
        _usersPanel.Children.Clear();
        var users = (await _users.GetAllAsync()).ToList();
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

    private async Task LoadGamesAsync()
    {
        _gamesPanel.Children.Clear();
        var games = (await _games.GetAllAsync()).ToList();
        if (games.Count == 0)
        {
            _gamesPanel.Children.Add(new TextBlock
            {
                Text = "No games found.",
                Foreground = ThemePalette.TextMutedBrush
            });
            return;
        }

        foreach (var game in games)
        {
            _gamesPanel.Children.Add(CreateGameCard(game));
        }
    }

    private async Task LoadGenresAsync()
    {
        _genresPanel.Children.Clear();
        var genres = (await _tags.GetAllAsync()).ToList();
        if (genres.Count == 0)
        {
            _genresPanel.Children.Add(new TextBlock
            {
                Text = "No genres found.",
                Foreground = ThemePalette.TextMutedBrush
            });
            return;
        }

        foreach (var genre in genres)
        {
            _genresPanel.Children.Add(CreateGenreCard(genre));
        }
    }

    private async Task LoadCategoryGamesAsync()
    {
        var games = (await _games.GetAllAsync()).ToList();
        _gameList.Clear();
        _gameList.AddRange(games);
        _categoryGameBox.ItemsSource = _gameList;

        if (_gameList.Count > 0)
        {
            _categoryGameBox.SelectedIndex = 0;
        }
    }

    private async Task LoadCategoriesAsync()
    {
        _categoriesPanel.Children.Clear();
        var categories = (await _categories.GetAllAsync()).ToList();
        if (categories.Count == 0)
        {
            _categoriesPanel.Children.Add(new TextBlock
            {
                Text = "No categories found.",
                Foreground = ThemePalette.TextMutedBrush
            });
            return;
        }

        foreach (var category in categories)
        {
            _categoriesPanel.Children.Add(CreateCategoryCard(category));
        }
    }

    private Control CreateUserCard(User user)
    {
        var card = new Border
        {
            Background = ThemePalette.BgCardBrush,
            BorderBrush = ThemePalette.BorderLightBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(16),
            Padding = new Thickness(16)
        };

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("2*,2*,Auto,Auto"),
            ColumnSpacing = 10
        };

        grid.Children.Add(BuildUserColumn(user.Username, user.Email, user.ThemePreference ?? "dark"));
        Grid.SetColumn(grid.Children[0], 0);

        grid.Children.Add(BuildUserBadge(user.RoleName ?? (user.RoleId == 1 ? "admin" : "user")));
        Grid.SetColumn(grid.Children[1], 1);

        var toggleRole = UiFactory.CreateSubtleButton(user.RoleId == 1 ? "Make user" : "Make admin", 110, "⇄");
        toggleRole.Click += async (_, __) => await ToggleRoleAsync(user);
        grid.Children.Add(toggleRole);
        Grid.SetColumn(toggleRole, 2);

        var delete = UiFactory.CreatePrimaryButton("Delete", 90, "✕");
        delete.Click += async (_, __) => await DeleteUserAsync(user);
        grid.Children.Add(delete);
        Grid.SetColumn(delete, 3);

        card.Child = grid;
        return card;
    }

    private static StackPanel BuildUserColumn(string username, string email, string theme)
    {
        var stack = new StackPanel
        {
            Spacing = 4
        };

        stack.Children.Add(new TextBlock
        {
            Text = username,
            FontSize = 13,
            FontWeight = FontWeight.Bold,
            Foreground = ThemePalette.TextPrimaryBrush
        });
        stack.Children.Add(new TextBlock
        {
            Text = email,
            FontSize = 11,
            Foreground = ThemePalette.TextSecondaryBrush
        });
        stack.Children.Add(new TextBlock
        {
            Text = $"Theme: {theme}",
            FontSize = 10,
            Foreground = ThemePalette.TextMutedBrush
        });
        return stack;
    }

    private static Border BuildUserBadge(string role)
    {
        return new Border
        {
            Background = ThemePalette.BgSecondaryBrush,
            BorderBrush = ThemePalette.BorderLightBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(14, 10),
            Child = new TextBlock
            {
                Text = role,
                Foreground = ThemePalette.AccentBrush,
                FontWeight = FontWeight.Bold,
                VerticalAlignment = VerticalAlignment.Center
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
            CornerRadius = new CornerRadius(16),
            Padding = new Thickness(16)
        };

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("2*,Auto"),
            ColumnSpacing = 10
        };

        var info = new StackPanel
        {
            Spacing = 4
        };
        info.Children.Add(new TextBlock
        {
            Text = game.Title,
            FontSize = 13,
            FontWeight = FontWeight.Bold,
            Foreground = ThemePalette.TextPrimaryBrush
        });
        info.Children.Add(new TextBlock
        {
            Text = $"{game.Slug} | {game.ArticleCount} articles | {game.PopularityScore} views",
            FontSize = 10.5,
            Foreground = ThemePalette.TextSecondaryBrush
        });
        grid.Children.Add(info);

        var edit = UiFactory.CreateSubtleButton("Edit", 90, "✎");
        edit.Click += async (_, __) => await OpenGameEditorAsync(game.GameId);
        grid.Children.Add(edit);
        Grid.SetColumn(edit, 1);

        var delete = UiFactory.CreatePrimaryButton("Delete", 90, "✕");
        delete.Click += async (_, __) => await DeleteGameAsync(game);
        grid.Children.Add(delete);
        Grid.SetColumn(delete, 2);

        card.Child = grid;
        return card;
    }

    private Control CreateGenreCard(GameTag genre)
    {
        var card = new Border
        {
            Background = ThemePalette.BgCardBrush,
            BorderBrush = ThemePalette.BorderLightBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(16),
            Padding = new Thickness(16)
        };

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("2*,Auto,Auto"),
            ColumnSpacing = 10
        };

        var info = new StackPanel
        {
            Spacing = 4
        };
        info.Children.Add(new TextBlock
        {
            Text = genre.TagName,
            FontSize = 13,
            FontWeight = FontWeight.Bold,
            Foreground = ThemePalette.TextPrimaryBrush
        });
        info.Children.Add(new TextBlock
        {
            Text = $"{genre.GameCount} games",
            FontSize = 10.5,
            Foreground = ThemePalette.TextSecondaryBrush
        });
        grid.Children.Add(info);

        var delete = UiFactory.CreatePrimaryButton("Delete", 90, "✕");
        delete.Click += async (_, __) => await DeleteGenreAsync(genre);
        grid.Children.Add(delete);
        Grid.SetColumn(delete, 1);

        card.Child = grid;
        return card;
    }

    private Control CreateCategoryCard(Category category)
    {
        var card = new Border
        {
            Background = ThemePalette.BgCardBrush,
            BorderBrush = ThemePalette.BorderLightBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(16),
            Padding = new Thickness(16)
        };

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("2*,Auto,Auto"),
            ColumnSpacing = 10
        };

        var info = new StackPanel
        {
            Spacing = 4
        };
        info.Children.Add(new TextBlock
        {
            Text = category.CategoryName,
            FontSize = 13,
            FontWeight = FontWeight.Bold,
            Foreground = ThemePalette.TextPrimaryBrush
        });
        info.Children.Add(new TextBlock
        {
            Text = $"{category.GameTitle ?? "Unknown game"} | {category.ArticleCount} articles",
            FontSize = 10.5,
            Foreground = ThemePalette.TextSecondaryBrush
        });
        if (!string.IsNullOrWhiteSpace(category.Description))
        {
            info.Children.Add(new TextBlock
            {
                Text = category.Description,
                FontSize = 10,
                Foreground = ThemePalette.TextMutedBrush,
                TextWrapping = TextWrapping.Wrap
            });
        }
        grid.Children.Add(info);

        var edit = UiFactory.CreateSubtleButton("Edit", 90, "✎");
        edit.Click += (_, __) => FillCategoryEditor(category);
        grid.Children.Add(edit);
        Grid.SetColumn(edit, 1);

        var delete = UiFactory.CreatePrimaryButton("Delete", 90, "✕");
        delete.Click += async (_, __) => await DeleteCategoryAsync(category);
        grid.Children.Add(delete);
        Grid.SetColumn(delete, 2);

        card.Child = grid;

        var shadow = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(28, 0, 0, 0)),
            CornerRadius = new CornerRadius(18),
            Opacity = 0.0,
            IsHitTestVisible = false,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        card.PointerEntered += (_, __) =>
        {
            _ = GameWikiApp.Helpers.UiAnimation.BackgroundColorToAsync(card, ThemePalette.BgTertiary, 160);
            _ = GameWikiApp.Helpers.UiAnimation.ScaleToAsync(card, 1.02, 150);
            _ = GameWikiApp.Helpers.UiAnimation.OpacityToAsync(shadow, 1.0, 150);
        };
        card.PointerExited += (_, __) =>
        {
            _ = GameWikiApp.Helpers.UiAnimation.BackgroundColorToAsync(card, ThemePalette.BgCard, 160);
            _ = GameWikiApp.Helpers.UiAnimation.ScaleToAsync(card, 1.0, 150);
            _ = GameWikiApp.Helpers.UiAnimation.OpacityToAsync(shadow, 0.0, 150);
        };

        var container = new Grid();
        container.Children.Add(shadow);
        container.Children.Add(card);
        return container;
    }

    private async Task AddGenreAsync()
    {
        var owner = TopLevel.GetTopLevel(this) as Window;
        if (owner == null)
        {
            return;
        }

        var name = _genreNameBox.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
        {
            await DialogHelper.ShowAsync(owner, "Missing genre", "Please enter a genre name.");
            return;
        }

        if (await _tags.GetByNameAsync(name) != null)
        {
            await DialogHelper.ShowAsync(owner, "Genre exists", "That genre already exists.");
            return;
        }

        if (await _tags.CreateAsync(name) > 0)
        {
            _genreNameBox.Text = string.Empty;
            await LoadAsync();
            return;
        }

        await DialogHelper.ShowAsync(owner, "Save failed", "The genre could not be created. Please try again.");
    }

    private async Task DeleteGenreAsync(GameTag genre)
    {
        var owner = TopLevel.GetTopLevel(this) as Window;
        if (owner == null)
        {
            return;
        }

        if (!await DialogHelper.ConfirmAsync(owner, "Delete genre", $"Delete genre {genre.TagName}?"))
        {
            return;
        }

        if (await _tags.DeleteAsync(genre.TagId))
        {
            await LoadAsync();
        }
    }

    private async Task ToggleRoleAsync(User user)
    {
        if (AppState.CurrentUser != null && user.UserId == AppState.CurrentUser.UserId)
        {
            return;
        }

        var roleId = user.RoleId == 1 ? 2 : 1;
        if (await _users.UpdateRoleAsync(user.UserId, roleId))
        {
            await LoadAsync();
        }
    }

    private async Task DeleteUserAsync(User user)
    {
        if (AppState.CurrentUser != null && user.UserId == AppState.CurrentUser.UserId)
        {
            return;
        }

        var owner = TopLevel.GetTopLevel(this) as Window;
        if (owner == null)
        {
            return;
        }

        if (!await DialogHelper.ConfirmAsync(owner, "Delete user", $"Delete user {user.Username}?"))
        {
            return;
        }

        if (await _users.DeleteAsync(user.UserId))
        {
            await LoadAsync();
        }
    }

    private async Task DeleteGameAsync(Game game)
    {
        var owner = TopLevel.GetTopLevel(this) as Window;
        if (owner == null)
        {
            return;
        }

        if (!await DialogHelper.ConfirmAsync(owner, "Delete game", $"Delete {game.Title} and all related content?"))
        {
            return;
        }

        if (await _games.DeleteAsync(game.GameId))
        {
            await LoadAsync();
        }
    }

    private async Task DeleteCategoryAsync(Category category)
    {
        var owner = TopLevel.GetTopLevel(this) as Window;
        if (owner == null)
        {
            return;
        }

        if (!await DialogHelper.ConfirmAsync(owner, "Delete category", $"Delete category {category.CategoryName}?"))
        {
            return;
        }

        if (await _categories.DeleteAsync(category.CategoryId))
        {
            await LoadAsync();
        }
    }

    private void FillCategoryEditor(Category category)
    {
        _selectedCategoryId = category.CategoryId;
        var game = _gameList.FirstOrDefault(x => x.GameId == category.GameId);
        if (game != null)
        {
            _categoryGameBox.SelectedItem = game;
        }

        _categoryNameBox.Text = category.CategoryName;
        _categoryDescriptionBox.Text = category.Description ?? string.Empty;
    }

    private void ClearCategoryEditor()
    {
        _selectedCategoryId = 0;
        if (_gameList.Count > 0)
        {
            _categoryGameBox.SelectedIndex = 0;
        }
        _categoryNameBox.Text = string.Empty;
        _categoryDescriptionBox.Text = string.Empty;
    }

    private async Task SaveCategoryAsync()
    {
        var owner = TopLevel.GetTopLevel(this) as Window;
        if (owner == null)
        {
            return;
        }

        if (_categoryGameBox.SelectedItem is not Game selectedGame)
        {
            await DialogHelper.ShowAsync(owner, "Missing game", "Please choose a game for the category.");
            return;
        }

        var name = _categoryNameBox.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
        {
            await DialogHelper.ShowAsync(owner, "Missing name", "Please enter a category name before saving.");
            return;
        }

        var category = new Category
        {
            CategoryId = _selectedCategoryId,
            GameId = selectedGame.GameId,
            CategoryName = name,
            Description = string.IsNullOrWhiteSpace(_categoryDescriptionBox.Text) ? null : _categoryDescriptionBox.Text.Trim()
        };

        if (_selectedCategoryId > 0)
        {
            if (!await _categories.UpdateAsync(category))
            {
                await DialogHelper.ShowAsync(owner, "Save failed", "The category could not be updated. Please try again.");
                return;
            }
        }
        else
        {
            if (await _categories.CreateAsync(category) <= 0)
            {
                await DialogHelper.ShowAsync(owner, "Save failed", "The category could not be created. Please try again.");
                return;
            }
        }

        ClearCategoryEditor();
        await LoadAsync();
    }

    private async Task OpenGameEditorAsync(int gameId)
    {
        var owner = TopLevel.GetTopLevel(this) as Window;
        if (owner == null)
        {
            return;
        }

        var editor = new GameEditorWindow(gameId);
        await editor.ShowDialog<bool>(owner);
        await LoadAsync();
    }
}
