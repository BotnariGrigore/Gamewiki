using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;

namespace GameWikiApp;

public static class UiFactory
{
    public static Border CreateCard(double? width = null, double? height = null, double margin = 10)
    {
        var card = new Border
        {
            Background = ThemePalette.BgCardBrush,
            BorderBrush = ThemePalette.BorderLightBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(16),
            Padding = new Thickness(14),
            Margin = new Thickness(margin),
            ClipToBounds = true
        };

        if (width.HasValue)
        {
            card.Width = width.Value;
        }

        if (height.HasValue)
        {
            card.Height = height.Value;
        }

        return card;
    }

    public static TextBlock CreateText(string text, double size, FontWeight weight, IBrush? brush = null)
    {
        return new TextBlock
        {
            Text = text,
            FontSize = size,
            FontWeight = weight,
            Foreground = brush ?? ThemePalette.TextPrimaryBrush,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(0)
        };
    }

    public static Button CreatePrimaryButton(string text, double width = double.NaN)
    {
        var button = new Button
        {
            Content = text,
            Background = ThemePalette.AccentBrush,
            BorderBrush = Brushes.Transparent,
            Foreground = ThemePalette.AccentForegroundBrush,
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(14, 10),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            FontWeight = FontWeight.SemiBold
        };

        if (!double.IsNaN(width))
        {
            button.Width = width;
        }

        return button;
    }

    public static Button CreateSubtleButton(string text, double width = double.NaN)
    {
        var button = new Button
        {
            Content = text,
            Background = ThemePalette.BgTertiaryBrush,
            BorderBrush = ThemePalette.BorderLightBrush,
            BorderThickness = new Thickness(1),
            Foreground = ThemePalette.TextPrimaryBrush,
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(14, 10),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            FontWeight = FontWeight.SemiBold
        };

        if (!double.IsNaN(width))
        {
            button.Width = width;
        }

        return button;
    }

    public static Button CreateNavButton(string text)
    {
        return new Button
        {
            Content = text,
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            Foreground = ThemePalette.TextSecondaryBrush,
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(14, 0, 12, 0),
            Height = 42,
            HorizontalContentAlignment = HorizontalAlignment.Left,
            VerticalContentAlignment = VerticalAlignment.Center,
            FontSize = 14,
            FontWeight = FontWeight.SemiBold
        };
    }

    public static TextBox CreateTextBox(string watermark, double width = double.NaN)
    {
        var box = new TextBox
        {
            PlaceholderText = watermark,
            Background = ThemePalette.BgInputBrush,
            BorderBrush = ThemePalette.BorderBrush,
            Foreground = ThemePalette.TextPrimaryBrush,
            CaretBrush = ThemePalette.AccentBrush,
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(12, 10),
            Height = 42
        };

        if (!double.IsNaN(width))
        {
            box.Width = width;
        }

        return box;
    }

    public static TextBox CreatePasswordBox(string watermark, double width = double.NaN)
    {
        var box = new TextBox
        {
            PlaceholderText = watermark,
            PasswordChar = '*',
            Background = ThemePalette.BgInputBrush,
            BorderBrush = ThemePalette.BorderBrush,
            Foreground = ThemePalette.TextPrimaryBrush,
            CaretBrush = ThemePalette.AccentBrush,
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(12, 10),
            Height = 42
        };

        if (!double.IsNaN(width))
        {
            box.Width = width;
        }

        return box;
    }

    public static Border CreateSectionHeader(string title, string caption)
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 12, 0, 8)
        };

        grid.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 20,
            FontWeight = FontWeight.Bold,
            Foreground = ThemePalette.TextPrimaryBrush,
            VerticalAlignment = VerticalAlignment.Center
        });

        grid.Children.Add(new TextBlock
        {
            Text = caption,
            FontSize = 12,
            FontWeight = FontWeight.SemiBold,
            Foreground = ThemePalette.TextMutedBrush,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center
        });
        Grid.SetColumn(grid.Children[1], 1);

        return new Border
        {
            Background = Brushes.Transparent,
            Child = grid
        };
    }

    public static Border CreateSeparator(double height = 1)
    {
        return new Border
        {
            Background = ThemePalette.BorderLightBrush,
            Height = height,
            Margin = new Thickness(0)
        };
    }
}
