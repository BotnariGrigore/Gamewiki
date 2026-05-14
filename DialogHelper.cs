using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace GameWikiApp;

public static class DialogHelper
{
    public static async Task ShowAsync(Window owner, string title, string message)
    {
        var dialog = BuildWindow(title, message, confirm: false);
        await dialog.ShowDialog(owner);
    }

    public static async Task<bool> ConfirmAsync(Window owner, string title, string message)
    {
        var dialog = BuildWindow(title, message, confirm: true);
        return await dialog.ShowDialog<bool>(owner);
    }

    private static Window BuildWindow(string title, string message, bool confirm)
    {
        var window = new Window
        {
            Title = title,
            Width = 460,
            Height = 220,
            CanResize = false,
            Background = ThemePalette.BgPrimaryBrush,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        var shell = new Border
        {
            Background = ThemePalette.BgSecondaryBrush,
            BorderBrush = ThemePalette.BorderLightBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(18),
            Padding = new Thickness(20),
            Margin = new Thickness(0)
        };

        var stack = new StackPanel
        {
            Spacing = 16
        };

        stack.Children.Add(new TextBlock
        {
            Text = message,
            FontSize = 14,
            Foreground = ThemePalette.TextPrimaryBrush,
            TextWrapping = TextWrapping.Wrap
        });

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 10
        };

        if (confirm)
        {
            var cancelButton = UiFactory.CreateSubtleButton("No", 90);
            cancelButton.Click += (_, __) => window.Close(false);

            var okButton = UiFactory.CreatePrimaryButton("Yes", 90);
            okButton.Click += (_, __) => window.Close(true);

            buttons.Children.Add(cancelButton);
            buttons.Children.Add(okButton);
        }
        else
        {
            var okButton = UiFactory.CreatePrimaryButton("OK", 90);
            okButton.Click += (_, __) => window.Close(false);
            buttons.Children.Add(okButton);
        }

        stack.Children.Add(buttons);
        shell.Child = stack;
        window.Content = shell;
        return window;
    }
}
