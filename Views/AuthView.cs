using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using GameWikiApp.Models;
using GameWikiApp.Services;

namespace GameWikiApp.Views;

public sealed class AuthView : UserControl
{
    private readonly AuthService _auth = new();
    private readonly Action<User> _onAuthenticated;

    private TabControl _tabs = null!;
    private TextBox _loginIdentifier = null!;
    private TextBox _loginPassword = null!;
    private TextBlock _loginError = null!;
    private TextBox _registerUsername = null!;
    private TextBox _registerEmail = null!;
    private TextBox _registerPassword = null!;
    private TextBox _registerConfirm = null!;
    private TextBlock _registerError = null!;
    private Button _themeButton = null!;

    public AuthView(Action<User> onAuthenticated)
    {
        _onAuthenticated = onAuthenticated;
        Content = BuildLayout();
        _ = LoadDatabaseStatusAsync();
    }

    private Control BuildLayout()
    {
        var scroll = new ScrollViewer
        {
            Background = ThemePalette.BgPrimaryBrush,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };

        var frame = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("1.05*,0.95*"),
            ColumnSpacing = 20,
            Margin = new Thickness(24),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Top,
            MaxWidth = 1280
        };

        var outer = new Grid
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        outer.Children.Add(frame);
        scroll.Content = outer;

        var brandPanel = BuildBrandPanel();
        var formPanel = BuildFormPanel();
        frame.Children.Add(brandPanel);
        frame.Children.Add(formPanel);
        Grid.SetColumn(formPanel, 1);

        return scroll;
    }

    private Border BuildBrandPanel()
    {
        var panel = new Border
        {
            Background = ThemePalette.SurfaceBrush,
            BorderBrush = ThemePalette.BorderLightBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(28),
            Padding = new Thickness(28),
            ClipToBounds = true
        };

        var stack = new StackPanel
        {
            Spacing = 18
        };

        var topRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto")
        };

        var brand = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 14
        };

        brand.Children.Add(new Border
        {
            Width = 56,
            Height = 56,
            Background = ThemePalette.AccentBrush,
            CornerRadius = new CornerRadius(18),
            Child = new TextBlock
            {
                Text = "NX",
                FontSize = 20,
                FontWeight = FontWeight.Bold,
                Foreground = ThemePalette.AccentForegroundBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        });

        var brandText = new StackPanel
        {
            Spacing = 4,
            VerticalAlignment = VerticalAlignment.Center
        };
        brandText.Children.Add(new TextBlock
        {
            Text = "Nexoria",
            FontSize = 26,
            FontWeight = FontWeight.Bold,
            Foreground = ThemePalette.TextPrimaryBrush
        });
        brandText.Children.Add(new TextBlock
        {
            Text = "Game Wiki Platform",
            FontSize = 12,
            Foreground = ThemePalette.TextMutedBrush
        });
        brand.Children.Add(brandText);
        topRow.Children.Add(brand);

        _themeButton = UiFactory.CreateSubtleButton(AppState.IsDark ? "Light mode" : "Dark mode", 128, "◐");
        _themeButton.HorizontalAlignment = HorizontalAlignment.Right;
        _themeButton.Click += (_, __) =>
        {
            var nextTheme = AppState.IsDark ? "light" : "dark";
            AppState.ApplyThemePreference(nextTheme);
            UpdateThemeButton();
        };
        topRow.Children.Add(_themeButton);
        Grid.SetColumn(_themeButton, 1);
        stack.Children.Add(topRow);

        stack.Children.Add(new TextBlock
        {
            Text = "A cleaner community workspace for games, articles, chat, and moderation.",
            FontSize = 14,
            Foreground = ThemePalette.TextSecondaryBrush,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 430
        });

        stack.Children.Add(BuildFeature("Browse wiki pages", "⌕", "Search games, pages, and categories from one place."));
        stack.Children.Add(BuildFeature("Private chat", "✉", "Talk with friends, send images, and keep conversations organized."));
        stack.Children.Add(BuildFeature("Moderation tools", "⚙", "Manage content, games, and categories without leaving the platform."));

        stack.Children.Add(new Border
        {
            Background = ThemePalette.BgTertiaryBrush,
            BorderBrush = ThemePalette.BorderLightBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(22),
            Padding = new Thickness(18),
            Child = new StackPanel
            {
                Spacing = 6,
                Children =
                {
                    new TextBlock
                    {
                        Text = "NX community hub",
                        FontSize = 12,
                        FontWeight = FontWeight.Bold,
                        Foreground = ThemePalette.TextMutedBrush
                    },
                    new TextBlock
                    {
                        Text = "Modern, minimal, and tuned for fast browsing.",
                        FontSize = 13,
                        Foreground = ThemePalette.TextPrimaryBrush,
                        TextWrapping = TextWrapping.Wrap
                    }
                }
            }
        });

        panel.Child = stack;
        return panel;
    }

    private Control BuildFormPanel()
    {
        var panel = new Border
        {
            Background = ThemePalette.BgSecondaryBrush,
            BorderBrush = ThemePalette.BorderLightBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(28),
            Padding = new Thickness(28),
            ClipToBounds = true
        };

        var stack = new StackPanel
        {
            Spacing = 16
        };

        stack.Children.Add(new TextBlock
        {
            Text = "Welcome back",
            FontSize = 24,
            FontWeight = FontWeight.Bold,
            Foreground = ThemePalette.TextPrimaryBrush
        });

        stack.Children.Add(new TextBlock
        {
            Text = "Sign in or create a new account to continue.",
            FontSize = 13,
            Foreground = ThemePalette.TextSecondaryBrush,
            TextWrapping = TextWrapping.Wrap
        });

        _tabs = new TabControl
        {
            Margin = new Thickness(0, 6, 0, 0)
        };
        _tabs.Items.Add(BuildLoginTab());
        _tabs.Items.Add(BuildRegisterTab());
        stack.Children.Add(_tabs);

        panel.Child = stack;
        return panel;
    }

    private Border BuildFeature(string title, string icon, string description)
    {
        var shell = new Border
        {
            Background = ThemePalette.BgSecondaryBrush,
            BorderBrush = ThemePalette.BorderLightBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(20),
            Padding = new Thickness(16)
        };

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,*"),
            ColumnSpacing = 12
        };

        grid.Children.Add(UiFactory.CreateIconBadge(icon, 36));

        var stack = new StackPanel
        {
            Spacing = 4
        };
        stack.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 13.5,
            FontWeight = FontWeight.Bold,
            Foreground = ThemePalette.TextPrimaryBrush
        });
        stack.Children.Add(new TextBlock
        {
            Text = description,
            FontSize = 11.5,
            Foreground = ThemePalette.TextSecondaryBrush,
            TextWrapping = TextWrapping.Wrap
        });

        grid.Children.Add(stack);
        Grid.SetColumn(stack, 1);
        shell.Child = grid;
        return shell;
    }

    private TabItem BuildLoginTab()
    {
        _loginIdentifier = UiFactory.CreateTextBox("Username or email", 360);
        _loginPassword = UiFactory.CreatePasswordBox("Password", 360);
        _loginError = new TextBlock
        {
            Foreground = ThemePalette.ErrorBrush,
            TextWrapping = TextWrapping.Wrap
        };

        var button = UiFactory.CreatePrimaryButton("Sign In", 170, "↩");
        button.Click += async (_, __) => await LoginAsync();

        var forgot = new TextBlock
        {
            Text = "Forgot password?",
            FontSize = 12,
            Foreground = ThemePalette.AccentBrush,
            Cursor = new Cursor(StandardCursorType.Hand)
        };
        forgot.PointerPressed += (_, __) =>
        {
            _loginError.Foreground = ThemePalette.TextSecondaryBrush;
            _loginError.Text = "Password reset is not implemented yet.";
        };

        var panel = new StackPanel
        {
            Spacing = 12,
            Margin = new Thickness(0, 8, 0, 0)
        };

        panel.Children.Add(Label("Username or Email"));
        panel.Children.Add(_loginIdentifier);
        panel.Children.Add(Label("Password"));
        panel.Children.Add(_loginPassword);
        panel.Children.Add(forgot);
        panel.Children.Add(button);
        panel.Children.Add(_loginError);

        return new TabItem
        {
            Header = "Sign In",
            Content = panel
        };
    }

    private TabItem BuildRegisterTab()
    {
        _registerUsername = UiFactory.CreateTextBox("Username", 360);
        _registerEmail = UiFactory.CreateTextBox("Email", 360);
        _registerPassword = UiFactory.CreatePasswordBox("Password", 360);
        _registerConfirm = UiFactory.CreatePasswordBox("Confirm password", 360);
        _registerError = new TextBlock
        {
            Foreground = ThemePalette.ErrorBrush,
            TextWrapping = TextWrapping.Wrap
        };

        var button = UiFactory.CreatePrimaryButton("Create Account", 190, "＋");
        button.Click += async (_, __) => await RegisterAsync();

        var panel = new StackPanel
        {
            Spacing = 12,
            Margin = new Thickness(0, 8, 0, 0)
        };

        panel.Children.Add(Label("Username"));
        panel.Children.Add(_registerUsername);
        panel.Children.Add(Label("Email"));
        panel.Children.Add(_registerEmail);
        panel.Children.Add(Label("Password"));
        panel.Children.Add(_registerPassword);
        panel.Children.Add(Label("Confirm Password"));
        panel.Children.Add(_registerConfirm);
        panel.Children.Add(button);
        panel.Children.Add(_registerError);

        return new TabItem
        {
            Header = "Register",
            Content = panel
        };
    }

    private static TextBlock Label(string text)
    {
        return new TextBlock
        {
            Text = text,
            FontSize = 12,
            FontWeight = FontWeight.SemiBold,
            Foreground = ThemePalette.TextPrimaryBrush
        };
    }

    private async Task LoginAsync()
    {
        try
        {
            _loginError.Foreground = ThemePalette.ErrorBrush;
            _loginError.Text = string.Empty;

            var identifier = _loginIdentifier.Text?.Trim() ?? string.Empty;
            var password = _loginPassword.Text ?? string.Empty;

            var result = await _auth.AuthenticateAsync(identifier, password);
            if (result.user == null)
            {
                _loginError.Text = result.error ?? "Login failed.";
                return;
            }

            AppState.StartSession(result.user, Guid.NewGuid().ToString(), result.user.ThemePreference ?? "light");
            _onAuthenticated(result.user);
        }
        catch
        {
            _loginError.Foreground = ThemePalette.ErrorBrush;
            _loginError.Text = "Login failed unexpectedly. Please try again.";
        }
    }

    private async Task RegisterAsync()
    {
        _registerError.Foreground = ThemePalette.ErrorBrush;
        _registerError.Text = string.Empty;

        var username = _registerUsername.Text?.Trim() ?? string.Empty;
        var email = _registerEmail.Text?.Trim() ?? string.Empty;
        var password = _registerPassword.Text ?? string.Empty;
        var confirm = _registerConfirm.Text ?? string.Empty;

        if (!string.Equals(password, confirm, StringComparison.Ordinal))
        {
            _registerError.Text = "Passwords do not match.";
            return;
        }

        var result = await _auth.RegisterAsync(username, email, password);
        if (!result.success)
        {
            _registerError.Text = result.error ?? "Registration failed.";
            return;
        }

        _tabs.SelectedIndex = 0;
        _loginIdentifier.Text = username;
        _loginPassword.Text = string.Empty;
        _loginError.Foreground = ThemePalette.SuccessBrush;
        _loginError.Text = "Account created. You can sign in now.";
    }

    private async Task LoadDatabaseStatusAsync()
    {
        var error = await _auth.CheckDatabaseAsync();
        if (error != null)
        {
            _loginError.Foreground = ThemePalette.WarningBrush;
            _loginError.Text = $"Database check failed: {error}";
        }
    }

    private void UpdateThemeButton()
    {
        if (_themeButton != null)
        {
            _themeButton.Content = UiFactory.CreateButtonContent(AppState.IsDark ? "Light mode" : "Dark mode", "◐");
        }
    }

    public Task RefreshThemeAsync()
    {
        UpdateThemeButton();
        return Task.CompletedTask;
    }
}
