using WindowsITToolkit.Games.Pong;
using Xunit;

namespace WindowsITToolkit.Tests;

public sealed class PongStatisticsServiceTests
{
    [Fact]
    public void PersistsMatchStatisticsAndBestRally()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var path = Path.Combine(directory, "pong.json");
            var service = new PongStatisticsService(path);

            var first = service.RecordMatch(playerWon: true, bestRallyHits: 8);
            var second = service.RecordMatch(playerWon: false, bestRallyHits: 4);
            var loaded = new PongStatisticsService(path).Load();

            Assert.True(first.Succeeded);
            Assert.True(second.Succeeded);
            Assert.True(loaded.Succeeded);
            Assert.Equal(new PongStatistics(2, 1, 1, 8), loaded.Statistics);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void MissingAndCorruptStatisticsLoadSafely()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var path = Path.Combine(directory, "pong.json");
            Assert.Equal(0, new PongStatisticsService(path).Load().Statistics.MatchesPlayed);
            File.WriteAllText(path, "invalid json");

            var loaded = new PongStatisticsService(path).Load();
            Assert.Equal(0, loaded.Statistics.MatchesPlayed);
            Assert.NotNull(loaded.Warning);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string CreateTemporaryDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"PongTests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }
}
