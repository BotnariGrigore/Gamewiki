using Avalonia.Media;

namespace GameWikiApp;

public static class ThemePalette
{
    public static bool IsDark => AppState.IsDark;

    public static Color BgPrimary => IsDark ? Color.FromRgb(16, 16, 16) : Color.FromRgb(244, 244, 244);
    public static Color BgSecondary => IsDark ? Color.FromRgb(23, 23, 23) : Color.FromRgb(255, 255, 255);
    public static Color BgTertiary => IsDark ? Color.FromRgb(32, 32, 32) : Color.FromRgb(233, 233, 233);
    public static Color BgCard => IsDark ? Color.FromRgb(27, 27, 27) : Color.FromRgb(255, 255, 255);
    public static Color BgInput => IsDark ? Color.FromRgb(23, 23, 23) : Color.FromRgb(255, 255, 255);
    public static Color BgHover => IsDark ? Color.FromRgb(42, 42, 42) : Color.FromRgb(226, 226, 226);
    public static Color Accent => IsDark ? Color.FromRgb(233, 233, 233) : Color.FromRgb(24, 24, 24);
    public static Color AccentHover => IsDark ? Color.FromRgb(255, 255, 255) : Color.FromRgb(0, 0, 0);
    public static Color AccentDim => IsDark ? Color.FromRgb(178, 178, 178) : Color.FromRgb(92, 92, 92);
    public static Color AccentForeground => IsDark ? Color.FromRgb(17, 17, 17) : Color.FromRgb(255, 255, 255);
    public static Color TextPrimary => IsDark ? Color.FromRgb(243, 243, 243) : Color.FromRgb(17, 17, 17);
    public static Color TextSecondary => IsDark ? Color.FromRgb(207, 207, 207) : Color.FromRgb(58, 58, 58);
    public static Color TextMuted => IsDark ? Color.FromRgb(141, 141, 141) : Color.FromRgb(108, 108, 108);
    public static Color Border => IsDark ? Color.FromRgb(49, 49, 49) : Color.FromRgb(215, 215, 215);
    public static Color BorderLight => IsDark ? Color.FromRgb(37, 37, 37) : Color.FromRgb(230, 230, 230);
    public static Color Sidebar => IsDark ? Color.FromRgb(13, 13, 13) : Color.FromRgb(255, 255, 255);
    public static Color Header => IsDark ? Color.FromRgb(18, 18, 18) : Color.FromRgb(255, 255, 255);
    public static Color Surface => IsDark ? Color.FromRgb(21, 21, 21) : Color.FromRgb(240, 240, 240);
    public static Color Error => Color.FromRgb(122, 122, 122);
    public static Color Success => Color.FromRgb(102, 102, 102);
    public static Color Warning => Color.FromRgb(146, 146, 146);

    public static IBrush BgPrimaryBrush => new SolidColorBrush(BgPrimary);
    public static IBrush BgSecondaryBrush => new SolidColorBrush(BgSecondary);
    public static IBrush BgTertiaryBrush => new SolidColorBrush(BgTertiary);
    public static IBrush BgCardBrush => new SolidColorBrush(BgCard);
    public static IBrush BgInputBrush => new SolidColorBrush(BgInput);
    public static IBrush BgHoverBrush => new SolidColorBrush(BgHover);
    public static IBrush AccentBrush => new SolidColorBrush(Accent);
    public static IBrush AccentHoverBrush => new SolidColorBrush(AccentHover);
    public static IBrush AccentDimBrush => new SolidColorBrush(AccentDim);
    public static IBrush AccentForegroundBrush => new SolidColorBrush(AccentForeground);
    public static IBrush TextPrimaryBrush => new SolidColorBrush(TextPrimary);
    public static IBrush TextSecondaryBrush => new SolidColorBrush(TextSecondary);
    public static IBrush TextMutedBrush => new SolidColorBrush(TextMuted);
    public static IBrush BorderBrush => new SolidColorBrush(Border);
    public static IBrush BorderLightBrush => new SolidColorBrush(BorderLight);
    public static IBrush SidebarBrush => new SolidColorBrush(Sidebar);
    public static IBrush HeaderBrush => new SolidColorBrush(Header);
    public static IBrush SurfaceBrush => new SolidColorBrush(Surface);
    public static IBrush ErrorBrush => new SolidColorBrush(Error);
    public static IBrush SuccessBrush => new SolidColorBrush(Success);
    public static IBrush WarningBrush => new SolidColorBrush(Warning);
}
