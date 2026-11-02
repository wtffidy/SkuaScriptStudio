using System.Text.Json;
using Avalonia;
using Avalonia.Media;

namespace SkuaScriptStudio;

public static class ThemeManager
{
    private const string SettingsDirectory = "SkuaScriptStudio";
    private const string SettingsFileName = "settings.json";
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        SettingsDirectory,
        SettingsFileName);

    private static readonly Dictionary<string, IReadOnlyDictionary<string, string>> Palettes =
        new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Potato Purple"] = new Dictionary<string, string>
            {
                ["AppBackgroundBrush"] = "#160D20", ["SurfaceBrush"] = "#24132F", ["SurfaceRaisedBrush"] = "#30183D",
                ["SurfaceInsetBrush"] = "#190F24", ["EditorBackgroundBrush"] = "#100B18", ["BorderBrush"] = "#713B83",
                ["AccentBorderBrush"] = "#D75BEA", ["TextPrimaryBrush"] = "#FFF4FF", ["TextSecondaryBrush"] = "#E2CBEA",
                ["TextMutedBrush"] = "#B99AC8", ["TextSubtleBrush"] = "#9574A8", ["EditorTextBrush"] = "#F9EDFF",
                ["AccentBrush"] = "#9828D8", ["AccentTextBrush"] = "#F2A5FF", ["AccentHoverBrush"] = "#C34BEE",
                ["PotatoGoldBrush"] = "#FFC45E", ["FooterBrush"] = "#2A1638", ["TitleBarStartColor"] = "#542B68",
                ["TitleBarMiddleColor"] = "#321B40", ["TitleBarEndColor"] = "#24132F", ["ChromeForegroundBrush"] = "#FFF0C9"
            },
            ["Midnight Potato"] = new Dictionary<string, string>
            {
                ["AppBackgroundBrush"] = "#0B0A12", ["SurfaceBrush"] = "#15131F", ["SurfaceRaisedBrush"] = "#211B2D",
                ["SurfaceInsetBrush"] = "#100E18", ["EditorBackgroundBrush"] = "#08070D", ["BorderBrush"] = "#443653",
                ["AccentBorderBrush"] = "#9674B8", ["TextPrimaryBrush"] = "#F4F0FA", ["TextSecondaryBrush"] = "#C9BED8",
                ["TextMutedBrush"] = "#9B8DAF", ["TextSubtleBrush"] = "#766987", ["EditorTextBrush"] = "#F4F0FA",
                ["AccentBrush"] = "#69459A", ["AccentTextBrush"] = "#C7A7F0", ["AccentHoverBrush"] = "#8964B8",
                ["PotatoGoldBrush"] = "#D9B66F", ["FooterBrush"] = "#171321", ["TitleBarStartColor"] = "#292036",
                ["TitleBarMiddleColor"] = "#171321", ["TitleBarEndColor"] = "#0D0B13", ["ChromeForegroundBrush"] = "#F4E8D0"
            },
            ["Golden Potato"] = new Dictionary<string, string>
            {
                ["AppBackgroundBrush"] = "#21180F", ["SurfaceBrush"] = "#302215", ["SurfaceRaisedBrush"] = "#44301B",
                ["SurfaceInsetBrush"] = "#1B140D", ["EditorBackgroundBrush"] = "#171109", ["BorderBrush"] = "#87633A",
                ["AccentBorderBrush"] = "#D6A34F", ["TextPrimaryBrush"] = "#FFF4DE", ["TextSecondaryBrush"] = "#E8D2A9",
                ["TextMutedBrush"] = "#C4A879", ["TextSubtleBrush"] = "#A18458", ["EditorTextBrush"] = "#FFF2D5",
                ["AccentBrush"] = "#A66B24", ["AccentTextBrush"] = "#FFD27A", ["AccentHoverBrush"] = "#C48735",
                ["PotatoGoldBrush"] = "#FFD27A", ["FooterBrush"] = "#352515", ["TitleBarStartColor"] = "#76502A",
                ["TitleBarMiddleColor"] = "#49301B", ["TitleBarEndColor"] = "#2B1C11", ["ChromeForegroundBrush"] = "#FFF2D5"
            }
        };

    static ThemeManager()
    {
        Palettes["Russet Roast"] = CreateVariant("Golden Potato", "#301812", "#48251B", "#5B3022", "#1F100D", "#170B09", "#9B5940", "#E18A65", "#FFF0E8", "#E8C6B8", "#C69A8A", "#A87A6C", "#FFF0E8", "#B94E35", "#FFC0A2", "#D66A4D", "#E7A66D", "#51291E", "#8B4935", "#613021", "#3C1B15", "#FFF0E8");
        Palettes["Yukon Gold"] = CreateVariant("Golden Potato", "#211B0D", "#332A13", "#463719", "#191407", "#120F06", "#8C7130", "#E2BD55", "#FFF8DC", "#E9DDAF", "#C7B77D", "#A59662", "#FFF6D7", "#B58A20", "#FFE28A", "#D4A832", "#FFE28A", "#3A3017", "#806526", "#59451B", "#33270F", "#FFF6D7");
        Palettes["Purple Majesty"] = CreateVariant("Potato Purple", "#160D22", "#251333", "#351C49", "#190F27", "#100A19", "#65418A", "#B58BEA", "#F8F0FF", "#DDC8F2", "#B59ACF", "#9276AD", "#F8F0FF", "#7440B8", "#D0A7FF", "#9860D5", "#E0B9FF", "#2D1940", "#9A73C6", "#68468B", "#321C49", "#F5E9FF");
        Palettes["Sweet Potato"] = CreateVariant("Golden Potato", "#25131C", "#38202B", "#4B2937", "#1D1018", "#160B12", "#8A465B", "#D77A91", "#FFF1F3", "#E9CBD1", "#C9A1AC", "#A77E89", "#FFF1F3", "#A94368", "#F3A8B9", "#C85D7C", "#F6C2A0", "#402331", "#9B5369", "#6C394B", "#3D1E2A", "#FFF1F3");
        Palettes["Red Bliss"] = CreateVariant("Golden Potato", "#211214", "#321A1D", "#452326", "#190D10", "#12080A", "#87424A", "#D66B72", "#FFF0EB", "#E8C6C1", "#C69B98", "#A67B79", "#FFF0EB", "#A63842", "#F29A91", "#C94D55", "#FFD18A", "#3A1B20", "#994C51", "#653137", "#35171B", "#FFF0EB");
        Palettes["Tater Tot"] = CreateVariant("Golden Potato", "#1D1710", "#302316", "#46321D", "#181109", "#110C07", "#8B633B", "#D7A45D", "#FFF5E5", "#E8D5B7", "#C6AC86", "#A58A66", "#FFF5E5", "#A66B24", "#F5C875", "#C58A3A", "#FFD27A", "#352515", "#956A3E", "#624526", "#382515", "#FFF5E5");
    }

    private static IReadOnlyDictionary<string, string> CreateVariant(string baseTheme, string appBackground, string surface, string surfaceRaised, string surfaceInset, string editorBackground, string border, string accentBorder, string textPrimary, string textSecondary, string textMuted, string textSubtle, string editorText, string accent, string accentText, string accentHover, string potatoGold, string footer, string titleStart, string titleMiddle, string titleEnd, string chromeForeground)
    {
        Dictionary<string, string> palette = new(Palettes[baseTheme], StringComparer.OrdinalIgnoreCase)
        {
            ["AppBackgroundBrush"] = appBackground, ["SurfaceBrush"] = surface, ["SurfaceRaisedBrush"] = surfaceRaised,
            ["SurfaceInsetBrush"] = surfaceInset, ["EditorBackgroundBrush"] = editorBackground, ["BorderBrush"] = border,
            ["AccentBorderBrush"] = accentBorder, ["TextPrimaryBrush"] = textPrimary, ["TextSecondaryBrush"] = textSecondary,
            ["TextMutedBrush"] = textMuted, ["TextSubtleBrush"] = textSubtle, ["EditorTextBrush"] = editorText,
            ["AccentBrush"] = accent, ["AccentTextBrush"] = accentText, ["AccentHoverBrush"] = accentHover,
            ["PotatoGoldBrush"] = potatoGold, ["FooterBrush"] = footer, ["TitleBarStartColor"] = titleStart,
            ["TitleBarMiddleColor"] = titleMiddle, ["TitleBarEndColor"] = titleEnd, ["ChromeForegroundBrush"] = chromeForeground
        };

        return palette;
    }

    public static string[] AvailableThemes => Palettes.Keys.ToArray();

    public static string CurrentTheme { get; private set; } = "Potato Purple";

    public static void Initialize()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                ThemeSettings? settings = JsonSerializer.Deserialize<ThemeSettings>(File.ReadAllText(SettingsPath));
                if (!string.IsNullOrWhiteSpace(settings?.Theme) && Palettes.ContainsKey(settings.Theme))
                    CurrentTheme = settings.Theme;
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            // A damaged or inaccessible settings file should never prevent the Studio from launching.
        }

        Apply(CurrentTheme, false);
    }

    public static void Apply(string theme, bool save = true)
    {
        if (!Palettes.TryGetValue(theme, out IReadOnlyDictionary<string, string>? palette))
            return;

        Application? application = Application.Current;
        if (application is null)
            return;

        foreach (KeyValuePair<string, string> entry in palette)
            application.Resources[entry.Key] = entry.Key.EndsWith("Color", StringComparison.Ordinal)
                ? Color.Parse(entry.Value)
                : new SolidColorBrush(Color.Parse(entry.Value));

        CurrentTheme = theme;
        if (!save)
            return;

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(new ThemeSettings(theme)));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Theme changes still apply for this session when persistence is unavailable.
        }
    }

    private sealed record ThemeSettings(string Theme);
}
