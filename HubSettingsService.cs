using System.IO;
using System.Security;
using System.Text.Json;

namespace WindowsITToolkit;

public sealed record HubSettings
{
    public bool ReduceAnimations { get; init; }
    public bool ConfirmExit { get; init; }
    public double WindowWidth { get; init; } = 1280;
    public double WindowHeight { get; init; } = 900;
    public List<string> FavoriteGameIds { get; init; } = [];
    public List<string> RecentlyPlayedGameIds { get; init; } = [];
    public Dictionary<string, int> GameLaunchCounts { get; init; } = new(StringComparer.OrdinalIgnoreCase);
}

public readonly record struct HubSettingsLoadResult(HubSettings Settings, bool Succeeded, string? Warning);
public readonly record struct HubSettingsSaveResult(bool Succeeded, string? Warning);

public sealed class HubSettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _filePath;

    public HubSettingsService()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WindowsArcadeHub",
            "settings.json"))
    {
    }

    public HubSettingsService(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        _filePath = filePath;
    }

    public HubSettingsLoadResult Load()
    {
        try
        {
            var settings = JsonSerializer.Deserialize<HubSettings>(File.ReadAllText(_filePath));
            if (settings is null)
            {
                return new HubSettingsLoadResult(new HubSettings(), true, "Saved settings were invalid; defaults are in use.");
            }

            return new HubSettingsLoadResult(Normalize(settings), true, null);
        }
        catch (FileNotFoundException)
        {
            return new HubSettingsLoadResult(new HubSettings(), true, null);
        }
        catch (DirectoryNotFoundException)
        {
            return new HubSettingsLoadResult(new HubSettings(), true, null);
        }
        catch (JsonException)
        {
            return new HubSettingsLoadResult(new HubSettings(), true, "Saved settings could not be read; defaults are in use.");
        }
        catch (IOException)
        {
            return new HubSettingsLoadResult(new HubSettings(), false, "Settings could not be read.");
        }
        catch (UnauthorizedAccessException)
        {
            return new HubSettingsLoadResult(new HubSettings(), false, "Settings could not be read because access was denied.");
        }
        catch (SecurityException)
        {
            return new HubSettingsLoadResult(new HubSettings(), false, "Settings could not be read because access was denied.");
        }
    }

    public HubSettingsSaveResult Save(HubSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var temporaryPath = $"{_filePath}.{Guid.NewGuid():N}.tmp";
        HubSettingsSaveResult result;
        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(Normalize(settings), JsonOptions));
            File.Move(temporaryPath, _filePath, overwrite: true);
            result = new HubSettingsSaveResult(true, null);
        }
        catch (IOException)
        {
            result = new HubSettingsSaveResult(false, "Settings could not be saved.");
        }
        catch (UnauthorizedAccessException)
        {
            result = new HubSettingsSaveResult(false, "Settings could not be saved because access was denied.");
        }
        catch (SecurityException)
        {
            result = new HubSettingsSaveResult(false, "Settings could not be saved because access was denied.");
        }

        if (File.Exists(temporaryPath))
        {
            try
            {
                File.Delete(temporaryPath);
            }
            catch (IOException)
            {
                result = result with { Warning = "A temporary settings file could not be removed." };
            }
            catch (UnauthorizedAccessException)
            {
                result = result with { Warning = "A temporary settings file could not be removed because access was denied." };
            }
            catch (SecurityException)
            {
                result = result with { Warning = "A temporary settings file could not be removed because access was denied." };
            }
        }

        return result;
    }

    private static HubSettings Normalize(HubSettings settings)
    {
        var favoriteIds = (settings.FavoriteGameIds ?? [])
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var recentIds = (settings.RecentlyPlayedGameIds ?? [])
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToList();
        var launchCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        if (settings.GameLaunchCounts is not null)
        {
            foreach (var entry in settings.GameLaunchCounts)
            {
                if (!string.IsNullOrWhiteSpace(entry.Key) && entry.Value >= 0)
                {
                    launchCounts[entry.Key.Trim()] = Math.Max(
                        entry.Value,
                        launchCounts.GetValueOrDefault(entry.Key.Trim()));
                }
            }
        }

        return settings with
        {
            WindowWidth = double.IsFinite(settings.WindowWidth) && settings.WindowWidth >= 800 ? settings.WindowWidth : 1280,
            WindowHeight = double.IsFinite(settings.WindowHeight) && settings.WindowHeight >= 620 ? settings.WindowHeight : 900,
            FavoriteGameIds = favoriteIds,
            RecentlyPlayedGameIds = recentIds,
            GameLaunchCounts = launchCounts
        };
    }
}
