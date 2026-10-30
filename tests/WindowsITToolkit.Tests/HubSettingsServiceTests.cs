using WindowsITToolkit;
using Xunit;

namespace WindowsITToolkit.Tests;

public sealed class HubSettingsServiceTests
{
    [Fact]
    public void MissingSettingsUseDefaults()
    {
        var service = new HubSettingsService(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "settings.json"));

        var result = service.Load();

        Assert.True(result.Succeeded);
        Assert.Equal(1280, result.Settings.WindowWidth);
        Assert.Empty(result.Settings.FavoriteGameIds);
    }

    [Fact]
    public void SettingsPersistAndNormalizeRecentAndFavoriteIds()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var service = new HubSettingsService(Path.Combine(directory, "settings.json"));
        var settings = new HubSettings
        {
            ReduceAnimations = true,
            ConfirmExit = true,
            FavoriteGameIds = ["snake", "snake"],
            RecentlyPlayedGameIds = ["2048", "snake"],
            GameLaunchCounts = new Dictionary<string, int> { ["snake"] = 4 }
        };

        Assert.True(service.Save(settings).Succeeded);
        var loaded = service.Load();

        Assert.True(loaded.Succeeded);
        Assert.True(loaded.Settings.ReduceAnimations);
        Assert.True(loaded.Settings.ConfirmExit);
        Assert.Equal(["snake"], loaded.Settings.FavoriteGameIds);
        Assert.Equal(["2048", "snake"], loaded.Settings.RecentlyPlayedGameIds);
        Assert.Equal(4, loaded.Settings.GameLaunchCounts["snake"]);
        Directory.Delete(directory, recursive: true);
    }

    [Fact]
    public void CorruptSettingsFallBackToDefaults()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "settings.json");
        File.WriteAllText(path, "{ invalid");
        var service = new HubSettingsService(path);

        var loaded = service.Load();

        Assert.True(loaded.Succeeded);
        Assert.NotNull(loaded.Warning);
        Assert.Empty(loaded.Settings.FavoriteGameIds);
        Directory.Delete(directory, recursive: true);
    }
}
