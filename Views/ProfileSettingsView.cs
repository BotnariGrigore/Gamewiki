using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using GameWikiApp.Helpers;
using GameWikiApp.Models;
using GameWikiApp.Services;

namespace GameWikiApp.Views;

public sealed class ProfileSettingsView : UserControl
{
    private readonly UserService _users = new();
    private readonly ImageService _images = new();
    private readonly Func<Task> _onLogout;
    private readonly Action? _onProfileChanged;
    private readonly DispatcherTimer _saveTimer = new();

    private readonly Border _avatarHost = new();
    private readonly TextBlock _accountTitle = new();
    private readonly TextBlock _accountSubtitle = new();
    private readonly TextBlock _accountMeta = new();
    private readonly TextBlock _statusText = new();
    private readonly TextBox _usernameBox = new();
    private readonly TextBox _emailBox = new();
    private readonly TextBox _bioBox = new();
    private readonly TextBox _currentPasswordBox = new();
    private readonly TextBox _newPasswordBox = new();
    private readonly TextBox _confirmPasswordBox = new();
    private readonly ToggleSwitch _themeSwitch = new();
    private readonly Button _saveButton = new();
    private readonly Button _changePasswordButton = new();
    private readonly Button _uploadButton = new();
    private readonly Button _removeImageButton = new();
    private readonly Button _logoutButton = new();
    private readonly TextBlock _themeHint = new();

    private bool _isLoading;
    private bool _isLoaded;
    private bool _suppressThemeEvents;
    private User? _profile;
    private string? _profileImagePath;
    private string _snapshotUsername = string.Empty;
    private string _snapshotEmail = string.Empty;
    private string _snapshotBio = string.Empty;
    private string _snapshotImage = string.Empty;
    private string _snapshotTheme = "light";

    public ProfileSettingsView(Func<Task> onLogout, Action? onProfileChanged = null)
    {
        _onLogout = onLogout;
        _onProfileChanged = onProfileChanged;
        _themeSwitch.Content = "Dark mode";

        _saveTimer.Interval = TimeSpan.FromMilliseconds(750);
        _saveTimer.Tick += async (_, __) =>
        {
            _saveTimer.Stop();
            if (!_isLoading)
            {
                await SaveProfileAsync();
            }
        };

        _usernameBox.TextChanged += (_, __) => MarkDirty();
        _emailBox.TextChanged += (_, __) => MarkDirty();
        _bioBox.TextChanged += (_, __) => MarkDirty();
        AppState.ThemeChanged += HandleThemeChanged;

        Content = BuildLayout();
        Loaded += async (_, __) =>
        {
            if (_isLoaded)
            {
                return;
            }

            _isLoaded = true;
            await LoadAsync();
        };
        Unloaded += (_, __) =>
        {
            AppState.ThemeChanged -= HandleThemeChanged;
        };
    }

    public async Task LoadAsync()
    {
        _isLoaded = true;

        if (AppState.CurrentUser == null)
        {
            ShowGuestState();
            return;
        }

        _isLoading = true;
        try
        {
            _profile = await _users.GetByIdAsync(AppState.CurrentUser.UserId) ?? AppState.CurrentUser;

            _snapshotUsername = _profile.Username ?? string.Empty;
            _snapshotEmail = _profile.Email ?? string.Empty;
            _snapshotBio = _profile.Bio ?? string.Empty;
            _snapshotImage = _profile.ProfileImage ?? string.Empty;
            _snapshotTheme = _profile.ThemePreference ?? "light";
            _profileImagePath = string.IsNullOrWhiteSpace(_snapshotImage) ? null : _snapshotImage;

            _usernameBox.Text = _snapshotUsername;
            _emailBox.Text = _snapshotEmail;
            _bioBox.Text = _snapshotBio;
            _themeSwitch.IsChecked = AppState.IsDark;
            _themeHint.Text = AppState.IsDark ? "Dark mode is enabled." : "Light mode is enabled.";

            UpdateAccountInfo();
            await RefreshAvatarAsync();
            _statusText.Foreground = ThemePalette.TextMutedBrush;
            _statusText.Text = "Autosave is enabled.";
        }
        finally
        {
            _isLoading = false;
        }
    }

    private Control BuildLayout()
    {
        var root = new ScrollViewer
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

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("320,*"),
            ColumnSpacing = 16
        };

        var sidebar = BuildSidebarCard();
        var profile = BuildProfileCard();
        grid.Children.Add(sidebar);
        grid.Children.Add(profile);
        Grid.SetColumn(profile, 1);

        stack.Children.Add(grid);
        root.Content = stack;
        return root;
    }

    private Control BuildHero()
    {
        return new Border
        {
            Background = ThemePalette.SurfaceBrush,
            BorderBrush = ThemePalette.BorderLightBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(22),
            Padding = new Thickness(24),
            Child = new StackPanel
            {
                Spacing = 6,
                Children =
                {
                    new TextBlock
                    {
                        Text = "Profile & Settings",
                        FontSize = 28,
                        FontWeight = FontWeight.Bold,
                        Foreground = ThemePalette.TextPrimaryBrush
                    },
                    new TextBlock
                    {
                        Text = "Manage your account details, avatar, password and application preferences.",
                        FontSize = 13,
                        Foreground = ThemePalette.TextSecondaryBrush,
                        TextWrapping = TextWrapping.Wrap
                    }
                }
            }
        };
    }

    private Control BuildSidebarCard()
    {
        var card = new Border
        {
            Background = ThemePalette.BgSecondaryBrush,
            BorderBrush = ThemePalette.BorderLightBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(22),
            Padding = new Thickness(20)
        };

        var stack = new StackPanel
        {
            Spacing = 14
        };

        stack.Children.Add(_avatarHost);

        _accountTitle.Text = "Account";
        _accountTitle.FontSize = 22;
        _accountTitle.FontWeight = FontWeight.Bold;
        _accountTitle.Foreground = ThemePalette.TextPrimaryBrush;
        stack.Children.Add(_accountTitle);

        _accountSubtitle.FontSize = 12;
        _accountSubtitle.Foreground = ThemePalette.TextSecondaryBrush;
        stack.Children.Add(_accountSubtitle);

        _accountMeta.FontSize = 11;
        _accountMeta.Foreground = ThemePalette.TextMutedBrush;
        _accountMeta.TextWrapping = TextWrapping.Wrap;
        stack.Children.Add(_accountMeta);

        var themeRow = new StackPanel
        {
            Spacing = 10
        };

        themeRow.Children.Add(new TextBlock
        {
            Text = "Dark / Light mode",
            FontSize = 11,
            FontWeight = FontWeight.Bold,
            Foreground = ThemePalette.TextMutedBrush
        });

        _themeSwitch.PropertyChanged += (_, e) =>
        {
            if (!_suppressThemeEvents && e.Property == ToggleButton.IsCheckedProperty)
            {
                _ = ToggleThemeAsync();
            }
        };
        themeRow.Children.Add(_themeSwitch);

        _themeHint.FontSize = 11;
        _themeHint.Foreground = ThemePalette.TextMutedBrush;
        themeRow.Children.Add(_themeHint);
        stack.Children.Add(themeRow);

        var imageRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10
        };

        _uploadButton.Content = UiFactory.CreateButtonContent("Upload image", "↑");
        _uploadButton.Background = ThemePalette.BgTertiaryBrush;
        _uploadButton.BorderBrush = ThemePalette.BorderLightBrush;
        _uploadButton.BorderThickness = new Thickness(1);
        _uploadButton.Foreground = ThemePalette.TextPrimaryBrush;
        _uploadButton.CornerRadius = new CornerRadius(14);
        _uploadButton.Padding = new Thickness(14, 11);
        _uploadButton.MinHeight = 44;
        _uploadButton.Click += async (_, __) => await PickImageAsync();

        _removeImageButton.Content = UiFactory.CreateButtonContent("Remove", "×");
        _removeImageButton.Background = ThemePalette.BgTertiaryBrush;
        _removeImageButton.BorderBrush = ThemePalette.BorderLightBrush;
        _removeImageButton.BorderThickness = new Thickness(1);
        _removeImageButton.Foreground = ThemePalette.TextPrimaryBrush;
        _removeImageButton.CornerRadius = new CornerRadius(14);
        _removeImageButton.Padding = new Thickness(14, 11);
        _removeImageButton.MinHeight = 44;
        _removeImageButton.Click += async (_, __) =>
        {
            _profileImagePath = null;
            await RefreshAvatarAsync();
            await SaveProfileAsync();
        };

        imageRow.Children.Add(_uploadButton);
        imageRow.Children.Add(_removeImageButton);
        stack.Children.Add(imageRow);

        _logoutButton.Content = UiFactory.CreateButtonContent("Logout", "⏻");
        _logoutButton.Background = ThemePalette.BgTertiaryBrush;
        _logoutButton.BorderBrush = ThemePalette.BorderLightBrush;
        _logoutButton.BorderThickness = new Thickness(1);
        _logoutButton.Foreground = ThemePalette.TextPrimaryBrush;
        _logoutButton.CornerRadius = new CornerRadius(14);
        _logoutButton.Padding = new Thickness(14, 11);
        _logoutButton.MinHeight = 44;
        _logoutButton.Click += async (_, __) => await _onLogout();
        stack.Children.Add(_logoutButton);

        card.Child = stack;
        return card;
    }

    private Control BuildProfileCard()
    {
        var card = new Border
        {
            Background = ThemePalette.BgSecondaryBrush,
            BorderBrush = ThemePalette.BorderLightBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(22),
            Padding = new Thickness(22)
        };

        var stack = new StackPanel
        {
            Spacing = 14
        };

        stack.Children.Add(new TextBlock
        {
            Text = "Edit Profile",
            FontSize = 20,
            FontWeight = FontWeight.Bold,
            Foreground = ThemePalette.TextPrimaryBrush
        });

        stack.Children.Add(BuildField("Username", _usernameBox));
        stack.Children.Add(BuildField("Email", _emailBox));
        stack.Children.Add(BuildField("Bio", _bioBox, true));

        var saveRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 10
        };

        _saveButton.Content = "Save changes";
        _saveButton.Background = ThemePalette.AccentBrush;
        _saveButton.Foreground = ThemePalette.AccentForegroundBrush;
        _saveButton.CornerRadius = new CornerRadius(14);
        _saveButton.Padding = new Thickness(16, 11);
        _saveButton.MinHeight = 44;
        _saveButton.Click += async (_, __) => await SaveProfileAsync();
        saveRow.Children.Add(_saveButton);

        _statusText.FontSize = 11;
        _statusText.Foreground = ThemePalette.TextMutedBrush;
        saveRow.Children.Add(_statusText);
        stack.Children.Add(saveRow);

        stack.Children.Add(new Border
        {
            Background = ThemePalette.BorderLightBrush,
            Height = 1,
            Margin = new Thickness(0, 6, 0, 2)
        });

        stack.Children.Add(new TextBlock
        {
            Text = "Change Password",
            FontSize = 18,
            FontWeight = FontWeight.Bold,
            Foreground = ThemePalette.TextPrimaryBrush
        });

        stack.Children.Add(BuildField("Current password", _currentPasswordBox, false, true));
        stack.Children.Add(BuildField("New password", _newPasswordBox, false, true));
        stack.Children.Add(BuildField("Confirm new password", _confirmPasswordBox, false, true));

        _changePasswordButton.Content = "Update password";
        _changePasswordButton.Background = ThemePalette.BgTertiaryBrush;
        _changePasswordButton.BorderBrush = ThemePalette.BorderLightBrush;
        _changePasswordButton.BorderThickness = new Thickness(1);
        _changePasswordButton.Foreground = ThemePalette.TextPrimaryBrush;
        _changePasswordButton.CornerRadius = new CornerRadius(14);
        _changePasswordButton.Padding = new Thickness(16, 11);
        _changePasswordButton.MinHeight = 44;
        _changePasswordButton.Click += async (_, __) => await ChangePasswordAsync();
        stack.Children.Add(_changePasswordButton);

        card.Child = stack;
        return card;
    }

    private static Control BuildField(string label, TextBox editor, bool multiline = false, bool password = false)
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

        editor.Background = ThemePalette.BgInputBrush;
        editor.BorderBrush = ThemePalette.BorderBrush;
        editor.Foreground = ThemePalette.TextPrimaryBrush;
        editor.CaretBrush = ThemePalette.AccentBrush;
        editor.CornerRadius = new CornerRadius(12);
        editor.Padding = new Thickness(12, 10);
        editor.Height = multiline ? 96 : 42;
        if (multiline)
        {
            editor.AcceptsReturn = true;
            editor.TextWrapping = TextWrapping.Wrap;
        }

        if (password)
        {
            editor.PasswordChar = '*';
        }

        stack.Children.Add(editor);
        return stack;
    }

    private void ShowGuestState()
    {
        _statusText.Foreground = ThemePalette.WarningBrush;
        _statusText.Text = "You need to sign in to edit your profile.";
    }

    private void UpdateAccountInfo()
    {
        if (_profile == null)
        {
            _accountSubtitle.Text = string.Empty;
            _accountMeta.Text = string.Empty;
            return;
        }

        _accountSubtitle.Text = $"{_profile.Username} • {(_profile.RoleId == 1 ? "Administrator" : "Member")}";

        var lastSeen = _profile.IsOnline
            ? "Online now"
            : (_profile.LastSeen.HasValue ? $"Last seen {_profile.LastSeen.Value:g}" : "Offline");

        _accountSubtitle.Text = $"{_profile.Username} · {(_profile.RoleId == 1 ? "Administrator" : "Member")}";

        _accountMeta.Text = string.Join(Environment.NewLine, new[]
        {
            $"Email: {_profile.Email}",
            $"Status: {lastSeen}",
            $"Joined: {_profile.CreatedAt:g}"
        });
    }

    private async Task RefreshAvatarAsync()
    {
        try
        {
            var username = _profile?.Username ?? AppState.CurrentUser?.Username ?? "U";
            var path = _profileImagePath;

            if (!string.IsNullOrWhiteSpace(path))
            {
                var bitmap = await ImageLoader.LoadAsync(path);
                if (bitmap != null)
                {
                    _avatarHost.Width = 120;
                    _avatarHost.Height = 120;
                    _avatarHost.Background = ThemePalette.BgTertiaryBrush;
                    _avatarHost.BorderBrush = ThemePalette.BorderLightBrush;
                    _avatarHost.BorderThickness = new Thickness(1);
                    _avatarHost.CornerRadius = new CornerRadius(30);
                    _avatarHost.ClipToBounds = true;
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

            _avatarHost.Width = 120;
            _avatarHost.Height = 120;
            _avatarHost.Background = ThemePalette.AccentBrush;
            _avatarHost.BorderBrush = ThemePalette.BorderLightBrush;
            _avatarHost.BorderThickness = new Thickness(1);
            _avatarHost.CornerRadius = new CornerRadius(30);
            _avatarHost.Child = new TextBlock
            {
                Text = GetInitials(username),
                FontSize = 32,
                FontWeight = FontWeight.Bold,
                Foreground = ThemePalette.AccentForegroundBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }
        catch
        {
            var username = _profile?.Username ?? AppState.CurrentUser?.Username ?? "U";
            _avatarHost.Width = 120;
            _avatarHost.Height = 120;
            _avatarHost.Background = ThemePalette.AccentBrush;
            _avatarHost.BorderBrush = ThemePalette.BorderLightBrush;
            _avatarHost.BorderThickness = new Thickness(1);
            _avatarHost.CornerRadius = new CornerRadius(30);
            _avatarHost.Child = new TextBlock
            {
                Text = GetInitials(username),
                FontSize = 32,
                FontWeight = FontWeight.Bold,
                Foreground = ThemePalette.AccentForegroundBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }
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

    private void MarkDirty()
    {
        if (_isLoading)
        {
            return;
        }

        _statusText.Foreground = ThemePalette.TextMutedBrush;
        _statusText.Text = "Unsaved changes...";
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    private async Task SaveProfileAsync()
    {
        if (AppState.CurrentUser == null || _isLoading)
        {
            return;
        }

        var username = _usernameBox.Text?.Trim() ?? string.Empty;
        var email = _emailBox.Text?.Trim() ?? string.Empty;
        var bio = _bioBox.Text?.Trim();

        if (username == _snapshotUsername &&
            email == _snapshotEmail &&
            string.Equals(bio ?? string.Empty, _snapshotBio, StringComparison.Ordinal) &&
            string.Equals(_profileImagePath ?? string.Empty, _snapshotImage, StringComparison.Ordinal))
        {
            _statusText.Foreground = ThemePalette.TextMutedBrush;
            _statusText.Text = "No profile changes to save.";
            return;
        }

        try
        {
            var result = await _users.UpdateProfileAsync(
                AppState.CurrentUser.UserId,
                username,
                email,
                bio,
                _profileImagePath,
                AppState.PreferredTheme);

            if (!result.success || result.user == null)
            {
                _statusText.Foreground = ThemePalette.ErrorBrush;
                _statusText.Text = result.message;
                return;
            }

            AppState.CurrentUser.Username = result.user.Username;
            AppState.CurrentUser.Email = result.user.Email;
            AppState.CurrentUser.Bio = result.user.Bio;
            AppState.CurrentUser.ProfileImage = result.user.ProfileImage;
            AppState.CurrentUser.ThemePreference = result.user.ThemePreference;

            _profile = result.user;
            _snapshotUsername = username;
            _snapshotEmail = email;
            _snapshotBio = bio ?? string.Empty;
            _snapshotImage = _profileImagePath ?? string.Empty;

            UpdateAccountInfo();
            _statusText.Foreground = ThemePalette.SuccessBrush;
            _statusText.Text = result.message;
            _onProfileChanged?.Invoke();
        }
        catch (Exception ex)
        {
            try { File.AppendAllText("profile_errors.log", DateTime.UtcNow + " SAVE PROFILE ERROR: " + ex + Environment.NewLine); } catch { }
            _statusText.Foreground = ThemePalette.ErrorBrush;
            _statusText.Text = "An unexpected error occurred while saving the profile.";
            return;
        }
    }

    private async Task ChangePasswordAsync()
    {
        if (AppState.CurrentUser == null)
        {
            return;
        }

        var current = _currentPasswordBox.Text ?? string.Empty;
        var next = _newPasswordBox.Text ?? string.Empty;
        var confirm = _confirmPasswordBox.Text ?? string.Empty;

        if (!string.Equals(next, confirm, StringComparison.Ordinal))
        {
            _statusText.Foreground = ThemePalette.ErrorBrush;
            _statusText.Text = "The new passwords do not match.";
            return;
        }

        var result = await _users.ChangePasswordAsync(AppState.CurrentUser.UserId, current, next);
        if (!result.success)
        {
            _statusText.Foreground = ThemePalette.ErrorBrush;
            _statusText.Text = result.message;
            return;
        }

        _currentPasswordBox.Text = string.Empty;
        _newPasswordBox.Text = string.Empty;
        _confirmPasswordBox.Text = string.Empty;
        _statusText.Foreground = ThemePalette.SuccessBrush;
        _statusText.Text = result.message;
    }

    private async Task ToggleThemeAsync()
    {
        if (_isLoading)
        {
            return;
        }

        var nextTheme = _themeSwitch.IsChecked == true ? "dark" : "light";
        AppState.ApplyThemePreference(nextTheme);

        if (AppState.CurrentUser != null)
        {
            await _users.UpdateThemePreferenceAsync(AppState.CurrentUser.UserId, nextTheme);
            AppState.CurrentUser.ThemePreference = nextTheme;
            _snapshotTheme = nextTheme;
            _themeHint.Text = AppState.IsDark ? "Dark mode is enabled." : "Light mode is enabled.";
            _onProfileChanged?.Invoke();
        }
    }

    private void HandleThemeChanged()
    {
        if (_isLoading)
        {
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            _suppressThemeEvents = true;
            try
            {
                _themeSwitch.IsChecked = AppState.IsDark;
                _themeHint.Text = AppState.IsDark ? "Dark mode is enabled." : "Light mode is enabled.";
            }
            finally
            {
                _suppressThemeEvents = false;
            }
        });
    }

    private async Task PickImageAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null || AppState.CurrentUser == null)
        {
            return;
        }

        try
        {
            var files = await topLevel.StorageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
            {
                Title = "Select profile image",
                AllowMultiple = false,
                FileTypeFilter =
                [
                    new Avalonia.Platform.Storage.FilePickerFileType("Images")
                    {
                        Patterns = ["*.jpg", "*.jpeg", "*.png", "*.gif", "*.bmp", "*.webp"]
                    }
                ]
            });

            if (files.Count == 0)
            {
                return;
            }

            var localPath = files[0].Path.LocalPath;
            var validation = await ImageValidation.ValidateLocalImageAsync(localPath);
            if (!validation.Success)
            {
                _statusText.Foreground = ThemePalette.ErrorBrush;
                _statusText.Text = validation.Error ?? "The selected image could not be used.";
                return;
            }

            var saved = await _images.SaveProfileImageAsync(localPath, AppState.CurrentUser.UserId);
            if (string.IsNullOrWhiteSpace(saved))
            {
                _statusText.Foreground = ThemePalette.ErrorBrush;
                _statusText.Text = "The selected image could not be saved.";
                return;
            }

            _profileImagePath = saved;
            await RefreshAvatarAsync();
            await SaveProfileAsync();
        }
        catch
        {
            _statusText.Foreground = ThemePalette.ErrorBrush;
            _statusText.Text = "The selected image could not be processed right now.";
        }
    }

}
