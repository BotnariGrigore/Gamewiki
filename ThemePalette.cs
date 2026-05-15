using Avalonia.Media;

namespace GameWikiApp;

public static class ThemePalette
{
    private static Color _bgPrimary;
    private static Color _bgSecondary;
    private static Color _bgTertiary;
    private static Color _bgCard;
    private static Color _bgInput;
    private static Color _bgHover;
    private static Color _accent;
    private static Color _accentHover;
    private static Color _accentDim;
    private static Color _accentForeground;
    private static Color _textPrimary;
    private static Color _textSecondary;
    private static Color _textMuted;
    private static Color _border;
    private static Color _borderLight;
    private static Color _sidebar;
    private static Color _header;
    private static Color _surface;
    private static Color _surfaceElevated;
    private static Color _overlay;
    private static readonly Color _error = Color.FromRgb(183, 94, 94);
    private static readonly Color _success = Color.FromRgb(97, 138, 111);
    private static readonly Color _warning = Color.FromRgb(175, 149, 88);

    private static readonly SolidColorBrush _bgPrimaryBrush = new(_bgPrimary);
    private static readonly SolidColorBrush _bgSecondaryBrush = new(_bgSecondary);
    private static readonly SolidColorBrush _bgTertiaryBrush = new(_bgTertiary);
    private static readonly SolidColorBrush _bgCardBrush = new(_bgCard);
    private static readonly SolidColorBrush _bgInputBrush = new(_bgInput);
    private static readonly SolidColorBrush _bgHoverBrush = new(_bgHover);
    private static readonly SolidColorBrush _accentBrush = new(_accent);
    private static readonly SolidColorBrush _accentHoverBrush = new(_accentHover);
    private static readonly SolidColorBrush _accentDimBrush = new(_accentDim);
    private static readonly SolidColorBrush _accentForegroundBrush = new(_accentForeground);
    private static readonly SolidColorBrush _textPrimaryBrush = new(_textPrimary);
    private static readonly SolidColorBrush _textSecondaryBrush = new(_textSecondary);
    private static readonly SolidColorBrush _textMutedBrush = new(_textMuted);
    private static readonly SolidColorBrush _borderBrush = new(_border);
    private static readonly SolidColorBrush _borderLightBrush = new(_borderLight);
    private static readonly SolidColorBrush _sidebarBrush = new(_sidebar);
    private static readonly SolidColorBrush _headerBrush = new(_header);
    private static readonly SolidColorBrush _surfaceBrush = new(_surface);
    private static readonly SolidColorBrush _surfaceElevatedBrush = new(_surfaceElevated);
    private static readonly SolidColorBrush _overlayBrush = new(_overlay);
    private static readonly SolidColorBrush _errorBrush = new(_error);
    private static readonly SolidColorBrush _successBrush = new(_success);
    private static readonly SolidColorBrush _warningBrush = new(_warning);

    public static bool IsDark => AppState.IsDark;

    public static Color BgPrimary => _bgPrimary;
    public static Color BgSecondary => _bgSecondary;
    public static Color BgTertiary => _bgTertiary;
    public static Color BgCard => _bgCard;
    public static Color BgInput => _bgInput;
    public static Color BgHover => _bgHover;
    public static Color Accent => _accent;
    public static Color AccentHover => _accentHover;
    public static Color AccentDim => _accentDim;
    public static Color AccentForeground => _accentForeground;
    public static Color TextPrimary => _textPrimary;
    public static Color TextSecondary => _textSecondary;
    public static Color TextMuted => _textMuted;
    public static Color Border => _border;
    public static Color BorderLight => _borderLight;
    public static Color Sidebar => _sidebar;
    public static Color Header => _header;
    public static Color Surface => _surface;
    public static Color SurfaceElevated => _surfaceElevated;
    public static Color Overlay => _overlay;
    public static Color Error => _error;
    public static Color Success => _success;
    public static Color Warning => _warning;

    public static void ApplyTheme(bool isDark)
    {
        if (isDark)
        {
            _bgPrimary = Color.FromRgb(13, 15, 19);
            _bgSecondary = Color.FromRgb(17, 20, 26);
            _bgTertiary = Color.FromRgb(24, 28, 36);
            _bgCard = Color.FromRgb(20, 24, 31);
            _bgInput = Color.FromRgb(22, 27, 35);
            _bgHover = Color.FromRgb(31, 37, 47);
            _accent = Color.FromRgb(229, 234, 241);
            _accentHover = Color.FromRgb(255, 255, 255);
            _accentDim = Color.FromRgb(168, 178, 190);
            _accentForeground = Color.FromRgb(13, 15, 19);
            _textPrimary = Color.FromRgb(245, 247, 250);
            _textSecondary = Color.FromRgb(198, 205, 214);
            _textMuted = Color.FromRgb(136, 145, 157);
            _border = Color.FromRgb(40, 46, 58);
            _borderLight = Color.FromRgb(31, 37, 47);
            _sidebar = Color.FromRgb(10, 12, 16);
            _header = Color.FromRgb(14, 17, 22);
            _surface = Color.FromRgb(16, 19, 25);
            _surfaceElevated = Color.FromRgb(22, 26, 34);
            _overlay = Color.FromArgb(184, 8, 10, 14);
        }
        else
        {
            _bgPrimary = Color.FromRgb(244, 246, 248);
            _bgSecondary = Color.FromRgb(255, 255, 255);
            _bgTertiary = Color.FromRgb(233, 237, 242);
            _bgCard = Color.FromRgb(251, 252, 253);
            _bgInput = Color.FromRgb(255, 255, 255);
            _bgHover = Color.FromRgb(226, 231, 236);
            _accent = Color.FromRgb(31, 37, 48);
            _accentHover = Color.FromRgb(12, 15, 20);
            _accentDim = Color.FromRgb(92, 101, 112);
            _accentForeground = Color.FromRgb(255, 255, 255);
            _textPrimary = Color.FromRgb(16, 18, 22);
            _textSecondary = Color.FromRgb(58, 63, 71);
            _textMuted = Color.FromRgb(106, 114, 124);
            _border = Color.FromRgb(210, 216, 223);
            _borderLight = Color.FromRgb(230, 234, 238);
            _sidebar = Color.FromRgb(255, 255, 255);
            _header = Color.FromRgb(255, 255, 255);
            _surface = Color.FromRgb(247, 249, 251);
            _surfaceElevated = Color.FromRgb(255, 255, 255);
            _overlay = Color.FromArgb(178, 244, 246, 248);
        }

        _bgPrimaryBrush.Color = _bgPrimary;
        _bgSecondaryBrush.Color = _bgSecondary;
        _bgTertiaryBrush.Color = _bgTertiary;
        _bgCardBrush.Color = _bgCard;
        _bgInputBrush.Color = _bgInput;
        _bgHoverBrush.Color = _bgHover;
        _accentBrush.Color = _accent;
        _accentHoverBrush.Color = _accentHover;
        _accentDimBrush.Color = _accentDim;
        _accentForegroundBrush.Color = _accentForeground;
        _textPrimaryBrush.Color = _textPrimary;
        _textSecondaryBrush.Color = _textSecondary;
        _textMutedBrush.Color = _textMuted;
        _borderBrush.Color = _border;
        _borderLightBrush.Color = _borderLight;
        _sidebarBrush.Color = _sidebar;
        _headerBrush.Color = _header;
        _surfaceBrush.Color = _surface;
        _surfaceElevatedBrush.Color = _surfaceElevated;
        _overlayBrush.Color = _overlay;
        _errorBrush.Color = _error;
        _successBrush.Color = _success;
        _warningBrush.Color = _warning;
    }

    public static IBrush BgPrimaryBrush => _bgPrimaryBrush;
    public static IBrush BgSecondaryBrush => _bgSecondaryBrush;
    public static IBrush BgTertiaryBrush => _bgTertiaryBrush;
    public static IBrush BgCardBrush => _bgCardBrush;
    public static IBrush BgInputBrush => _bgInputBrush;
    public static IBrush BgHoverBrush => _bgHoverBrush;
    public static IBrush AccentBrush => _accentBrush;
    public static IBrush AccentHoverBrush => _accentHoverBrush;
    public static IBrush AccentDimBrush => _accentDimBrush;
    public static IBrush AccentForegroundBrush => _accentForegroundBrush;
    public static IBrush TextPrimaryBrush => _textPrimaryBrush;
    public static IBrush TextSecondaryBrush => _textSecondaryBrush;
    public static IBrush TextMutedBrush => _textMutedBrush;
    public static IBrush BorderBrush => _borderBrush;
    public static IBrush BorderLightBrush => _borderLightBrush;
    public static IBrush SidebarBrush => _sidebarBrush;
    public static IBrush HeaderBrush => _headerBrush;
    public static IBrush SurfaceBrush => _surfaceBrush;
    public static IBrush SurfaceElevatedBrush => _surfaceElevatedBrush;
    public static IBrush OverlayBrush => _overlayBrush;
    public static IBrush ErrorBrush => _errorBrush;
    public static IBrush SuccessBrush => _successBrush;
    public static IBrush WarningBrush => _warningBrush;
}
