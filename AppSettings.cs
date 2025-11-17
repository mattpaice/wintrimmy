using System.Text.Json;

namespace WinTrimmy;

public enum Aggressiveness
{
    Low = 0,
    Normal = 1,
    High = 2
}

public static class AggressivenessExtensions
{
    public static int ScoreThreshold(this Aggressiveness aggressiveness) => aggressiveness switch
    {
        Aggressiveness.Low => 8,
        Aggressiveness.Normal => 5,
        Aggressiveness.High => 2,
        _ => 5
    };

    public static string Title(this Aggressiveness aggressiveness) => aggressiveness switch
    {
        Aggressiveness.Low => "Low",
        Aggressiveness.Normal => "Normal",
        Aggressiveness.High => "High",
        _ => "Normal"
    };
}

public class AppSettings
{
    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WinTrimmy",
        "settings.json"
    );

    public Aggressiveness Aggressiveness { get; set; } = Aggressiveness.Normal;
    public bool PreserveBlankLines { get; set; } = false;
    public bool AutoTrimEnabled { get; set; } = true;
    public bool RemoveBoxDrawing { get; set; } = true;
    public bool LaunchAtLogin { get; set; } = false;

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch
        {
            // If loading fails, return defaults
        }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            var directory = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // Silently fail on save errors
        }
    }

    public void SetLaunchAtLogin(bool enabled)
    {
        LaunchAtLogin = enabled;
        Save();
        LaunchAtLoginManager.SetEnabled(enabled);
    }
}

public static class LaunchAtLoginManager
{
    private const string AppName = "WinTrimmy";

    public static void SetEnabled(bool enabled)
    {
        try
        {
            using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run", true);

            if (key == null) return;

            if (enabled)
            {
                var exePath = Application.ExecutablePath;
                key.SetValue(AppName, $"\"{exePath}\"");
            }
            else
            {
                key.DeleteValue(AppName, false);
            }
        }
        catch
        {
            // Silently fail on registry errors
        }
    }
}
