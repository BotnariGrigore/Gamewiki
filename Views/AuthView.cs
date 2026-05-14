using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
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

    public AuthView(Action<User> onAuthenticated)
    {
        _onAuthenticated = onAuthenticated;

        var root = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("1.35*,1*"),
            Background = ThemePalette.BgPrimaryBrush
        };

        var brandPanel = BuildBrandPanel();
        root.Children.Add(brandPanel);
        Grid.SetColumn(brandPanel, 0);

        var formCard = new Border
        {
            Background = ThemePalette.BgSecondaryBrush,
            BorderBrush = ThemePalette.BorderLightBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(24),
            Padding = new Thickness(28),
            Margin = new Thickness(28),
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        var formStack = new StackPanel
        {
            Spacing = 18
        };

        formStack.Children.Add(new TextBlock
        {
            Text = "Welcome back",
            FontSize = 28,
            FontWeight = FontWeight.Bold,
            Foreground = ThemePalette.TextPrimaryBrush
        });

        formStack.Children.Add(new TextBlock
        {
            Text = "Sign in or create a community account to continue.",
            FontSize = 13,
            Foreground = ThemePalette.TextSecondaryBrush
        });

        _tabs = new TabControl
        {
            Margin = new Thickness(0, 6, 0, 0)
        };

        _tabs.Items.Add(BuildLoginTab());
        _tabs.Items.Add(BuildRegisterTab());
        formStack.Children.Add(_tabs);

        formCard.Child = formStack;
        root.Children.Add(formCard);
        Grid.SetColumn(formCard, 1);

        Content = root;

        _ = LoadDatabaseStatusAsync();
    }

    private Control BuildBrandPanel()
    {
        var border = new Border
        {
            Background = ThemePalette.SurfaceBrush,
            Padding = new Thickness(56),
            Child = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                Spacing = 18,
                Children =
                {
                    new TextBlock
                    {
                        Text = "Sign in to continue",
                        FontSize = 38,
                        FontWeight = FontWeight.Bold,
                        Foreground = ThemePalette.TextPrimaryBrush
                    },
                    new TextBlock
                    {
                        Text = "Browse pages, manage games, organize genres, and moderate wiki categories from one place.",
                        FontSize = 14,
                        Foreground = ThemePalette.TextSecondaryBrush,
                        TextWrapping = TextWrapping.Wrap,
                        MaxWidth = 460
                    },
                    new StackPanel
                    {
                        Orientation = Orientation.Horizontal,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Spacing = 8,
                        Children =
                        {
                            new Button
                            {
                                Content = AppState.IsDark ? "Switch to light" : "Switch to dark",
                                Background = Brushes.Transparent,
                                BorderBrush = ThemePalette.BorderLightBrush,
                                Foreground = ThemePalette.TextPrimaryBrush,
                                CornerRadius = new CornerRadius(8),
                                Padding = new Thickness(8,4)
                            }
                        }
                    }
                }
            }
        };
        // Theme toggle handler: recreate the auth view so brushes refresh
        if (border.Child is StackPanel sp && sp.Children.Count > 2 && sp.Children[2] is StackPanel btnRow && btnRow.Children.Count > 0 && btnRow.Children[0] is Button themeBtn)
        {
            themeBtn.Click += (_, __) =>
            {
                var nextTheme = AppState.IsDark ? "light" : "dark";
                AppState.ApplyThemePreference(nextTheme);
                var owner = TopLevel.GetTopLevel(this) as Window;
                if (owner != null)
                {
                    owner.Content = new AuthView(_onAuthenticated);
                }
            };
        }
        return border;
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

        var button = UiFactory.CreatePrimaryButton("Sign In", 160);
        button.Margin = new Thickness(0, 8, 0, 0);
        button.Click += async (_, __) => await LoginAsync();

        var panel = new StackPanel
        {
            Spacing = 12,
            Margin = new Thickness(0, 10, 0, 0)
        };
        panel.Children.Add(Label("Username or Email"));
        panel.Children.Add(_loginIdentifier);
        panel.Children.Add(Label("Password"));
        panel.Children.Add(_loginPassword);
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

        var button = UiFactory.CreatePrimaryButton("Create Account", 180);
        button.Margin = new Thickness(0, 8, 0, 0);
        button.Click += async (_, __) => await RegisterAsync();

        var panel = new StackPanel
        {
            Spacing = 12,
            Margin = new Thickness(0, 10, 0, 0)
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
            _loginError.Foreground = ThemePalette.ErrorBrush;
            _loginError.Text = $"Database check failed: {error}";
        }
    }
}
