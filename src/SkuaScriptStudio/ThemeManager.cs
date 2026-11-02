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

    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Palettes =
        new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            ["Potato Purple"] = new Dictionary<string, string>
            {
                ["AppBackgroundBrush"] = "#160D20", ["SurfaceBrush"] = "#24132F", ["SurfaceRaisedBrush"] = "#30183D",
                ["SurfaceInsetBrush"] = "#190F24", ["EditorBackgroundBrush"] = "#100B18", ["BorderBrush"] = "#713B83",
                ["AccentBorderBrush"] = "#D75BEA", ["TextPrimaryBrush"] = "#FFF4FF", ["TextSecondaryBrush"] = "#E2CBEA",
                ["TextMutedBrush"] = "#B99AC8", ["TextSubtleBrush"] = "#9574A8", ["EditorTextBrush"] = "#F9EDFF",
                ["AccentBrush"] = "#9828D8", ["AccentTextBrush"] = "#F2A5FF", ["AccentHoverBrush"] = "#C34BEE",
                ["PotatoGoldBrush"] = "#FFC45E", ["FooterBrush"] = "#2A1638", ["TitleBarStartBrush"] = "#542B68",
                ["TitleBarMiddleBrush"] = "#321B40", ["TitleBarEndBrush"] = "#24132F", ["ChromeForegroundBrush"] = "#FFF0C9"
            },
            ["Midnight Potato"] = new Dictionary<string, string>
            {
                ["AppBackgroundBrush"] = "#0B0A12", ["SurfaceBrush"] = "#15131F", ["SurfaceRaisedBrush"] = "#211B2D",
                ["SurfaceInsetBrush"] = "#100E18", ["EditorBackgroundBrush"] = "#08070D", ["BorderBrush"] = "#443653",
                ["AccentBorderBrush"] = "#9674B8", ["TextPrimaryBrush"] = "#F4F0FA", ["TextSecondaryBrush"] = "#C9BED8",
                ["TextMutedBrush"] = "#9B8DAF", ["TextSubtleBrush"] = "#766987", ["EditorTextBrush"] = "#F4F0FA",
                ["AccentBrush"] = "#69459A", ["AccentTextBrush"] = "#C7A7F0", ["AccentHoverBrush"] = "#8964B8",
                ["PotatoGoldBrush"] = "#D9B66F", ["FooterBrush"] = "#171321", ["TitleBarStartBrush"] = "#292036",
                ["TitleBarMiddleBrush"] = "#171321", ["TitleBarEndBrush"] = "#0D0B13", ["ChromeForegroundBrush"] = "#F4E8D0"
            },
            ["Golden Potato"] = new Dictionary<string, string>
            {
                ["AppBackgroundBrush"] = "#21180F", ["SurfaceBrush"] = "#302215", ["SurfaceRaisedBrush"] = "#44301B",
                ["SurfaceInsetBrush"] = "#1B140D", ["EditorBackgroundBrush"] = "#171109", ["BorderBrush"] = "#87633A",
                ["AccentBorderBrush"] = "#D6A34F", ["TextPrimaryBrush"] = "#FFF4DE", ["TextSecondaryBrush"] = "#E8D2A9",
                ["TextMutedBrush"] = "#C4A879", ["TextSubtleBrush"] = "#A18458", ["EditorTextBrush"] = "#FFF2D5",
                ["AccentBrush"] = "#A66B24", ["AccentTextBrush"] = "#FFD27A", ["AccentHoverBrush"] = "#C48735",
                ["PotatoGoldBrush"] = "#FFD27A", ["FooterBrush"] = "#352515", ["TitleBarStartBrush"] = "#76502A",
                ["TitleBarMiddleBrush"] = "#49301B", ["TitleBarEndBrush"] = "#2B1C11", ["ChromeForegroundBrush"] = "#FFF2D5"
            }
        };

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
            application.Resources[entry.Key] = new SolidColorBrush(Color.Parse(entry.Value));

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
