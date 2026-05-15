using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;

namespace GameWikiApp;

public static class UiFactory
{
    private const string IconFont = "Segoe UI Symbol";

    public static Border CreateCard(double? width = null, double? height = null, double margin = 10)
    {
        var card = new Border
        {
            Background = ThemePalette.BgCardBrush,
            BorderBrush = ThemePalette.BorderLightBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(18),
            Padding = new Thickness(16),
            Margin = new Thickness(margin),
            ClipToBounds = true,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Top
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

    public static TextBlock CreateIcon(string glyph, double size = 16, IBrush? brush = null)
    {
        var icon = new TextBlock
        {
            Text = glyph,
            FontFamily = new FontFamily(IconFont),
            FontSize = size,
            FontWeight = FontWeight.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(0)
        };

        if (brush != null)
        {
            icon.Foreground = brush;
        }

        return icon;
    }

    public static Border CreateIconBadge(string glyph, double size = 36)
    {
        return new Border
        {
            Width = size,
            Height = size,
            Background = ThemePalette.BgTertiaryBrush,
            BorderBrush = ThemePalette.BorderLightBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(Math.Max(12, size * 0.3)),
            Child = CreateIcon(glyph, Math.Max(14, size * 0.46), ThemePalette.TextPrimaryBrush)
        };
    }

    public static Button CreatePrimaryButton(string text, double width = double.NaN, string? icon = null)
    {
        var button = CreateButtonBase(
            ThemePalette.AccentBrush,
            ThemePalette.AccentForegroundBrush,
            Brushes.Transparent,
            new CornerRadius(14));
        button.FontWeight = FontWeight.SemiBold;
        button.Content = CreateButtonContent(text, icon);

        if (!double.IsNaN(width))
        {
            button.MinWidth = width;
        }

        return button;
    }

    public static Button CreateSubtleButton(string text, double width = double.NaN, string? icon = null)
    {
        var button = CreateButtonBase(
            ThemePalette.BgTertiaryBrush,
            ThemePalette.TextPrimaryBrush,
            ThemePalette.BorderLightBrush,
            new CornerRadius(14));
        button.FontWeight = FontWeight.SemiBold;
        button.Content = CreateButtonContent(text, icon);

        if (!double.IsNaN(width))
        {
            button.MinWidth = width;
        }

        return button;
    }

    public static Button CreateNavButton(string text, string? icon = null)
    {
        var button = CreateButtonBase(
            Brushes.Transparent,
            ThemePalette.TextSecondaryBrush,
            Brushes.Transparent,
            new CornerRadius(14));
        button.HorizontalAlignment = HorizontalAlignment.Stretch;
        button.HorizontalContentAlignment = HorizontalAlignment.Left;
        button.Padding = new Thickness(14, 10);
        button.Height = 44;
        button.Margin = new Thickness(0, 0, 0, 2);
        button.FontSize = 14;
        button.Content = CreateButtonContent(text, icon, 8);
        return button;
    }

    public static Button CreateIconButton(string glyph, double size = 42)
    {
        return new Button
        {
            Width = size,
            Height = size,
            Background = ThemePalette.BgSecondaryBrush,
            Foreground = ThemePalette.TextPrimaryBrush,
            BorderBrush = ThemePalette.BorderLightBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(0),
            Content = CreateIcon(glyph, Math.Max(14, size * 0.42))
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
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(14, 11),
            Height = 44,
            HorizontalAlignment = HorizontalAlignment.Stretch
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
            CornerRadius = new CornerRadius(14),
            Padding = new Thickness(14, 11),
            Height = 44,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };

        if (!double.IsNaN(width))
        {
            box.Width = width;
        }

        return box;
    }

    public static Border CreateSectionHeader(string title, string? caption = null)
    {
        var hasCaption = !string.IsNullOrWhiteSpace(caption);
        var grid = new Grid
        {
            ColumnDefinitions = hasCaption
                ? new ColumnDefinitions("*,Auto")
                : new ColumnDefinitions("*"),
            ColumnSpacing = 12,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 18, 0, 12)
        };

        grid.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 18,
            FontWeight = FontWeight.Bold,
            Foreground = ThemePalette.TextPrimaryBrush,
            VerticalAlignment = VerticalAlignment.Center
        });

        if (hasCaption)
        {
            grid.Children.Add(new TextBlock
            {
                Text = caption,
                FontSize = 12,
                FontWeight = FontWeight.SemiBold,
                Foreground = ThemePalette.TextMutedBrush,
                TextWrapping = TextWrapping.NoWrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center
            });
            Grid.SetColumn(grid.Children[1], 1);
        }

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
            Margin = new Thickness(0),
            Opacity = 0.9
        };
    }

    public static Border CreateAvatarFrame(Bitmap? bitmap, string initials, double size)
    {
        var shell = new Border
        {
            Width = size,
            Height = size,
            CornerRadius = new CornerRadius(size / 2),
            Background = ThemePalette.BgTertiaryBrush,
            BorderBrush = ThemePalette.BorderLightBrush,
            BorderThickness = new Thickness(1),
            ClipToBounds = true
        };

        if (bitmap != null)
        {
            shell.Child = new Image
            {
                Source = bitmap,
                Stretch = Stretch.UniformToFill,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }
        else
        {
            shell.Background = ThemePalette.AccentBrush;
            shell.Child = new TextBlock
            {
                Text = initials,
                FontSize = Math.Max(10, size * 0.32),
                FontWeight = FontWeight.Bold,
                Foreground = ThemePalette.AccentForegroundBrush,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
        }

        return shell;
    }

    public static Border CreateMediaFrame(
        Bitmap? bitmap,
        string placeholder,
        double width,
        double height,
        bool crop = false,
        double cornerRadius = 14)
    {
        var shell = new Border
        {
            Width = width,
            Height = height,
            CornerRadius = new CornerRadius(cornerRadius),
            Background = ThemePalette.BgTertiaryBrush,
            BorderBrush = ThemePalette.BorderLightBrush,
            BorderThickness = new Thickness(1),
            ClipToBounds = true
        };

        if (bitmap != null)
        {
            shell.Child = new Image
            {
                Source = bitmap,
                Stretch = crop ? Stretch.UniformToFill : Stretch.Uniform,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            };
            return shell;
        }

        shell.Child = new TextBlock
        {
            Text = placeholder,
            FontSize = Math.Max(11, height * 0.11),
            FontWeight = FontWeight.Bold,
            Foreground = ThemePalette.TextSecondaryBrush,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        };

        return shell;
    }

    private static Button CreateButtonBase(
        IBrush background,
        IBrush foreground,
        IBrush borderBrush,
        CornerRadius cornerRadius)
    {
        return new Button
        {
            Background = background,
            Foreground = foreground,
            BorderBrush = borderBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = cornerRadius,
            Padding = new Thickness(16, 11),
            HorizontalContentAlignment = HorizontalAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            MinHeight = 44
        };
    }

    public static Control CreateButtonContent(string text, string? icon, double spacing = 10)
    {
        if (string.IsNullOrWhiteSpace(icon))
        {
            return new TextBlock
            {
                Text = text,
                FontWeight = FontWeight.SemiBold,
                TextWrapping = TextWrapping.NoWrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                TextAlignment = TextAlignment.Center
            };
        }

        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("Auto,Auto"),
            ColumnSpacing = spacing,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center
        };

        grid.Children.Add(CreateIcon(icon, 14));
        grid.Children.Add(new TextBlock
        {
            Text = text,
            FontWeight = FontWeight.SemiBold,
            TextWrapping = TextWrapping.NoWrap,
            TextTrimming = TextTrimming.CharacterEllipsis,
            VerticalAlignment = VerticalAlignment.Center
        });
        Grid.SetColumn(grid.Children[1], 1);
        return grid;
    }
}
