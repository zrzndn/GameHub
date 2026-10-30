using WindowsITToolkit.Games.TwentyFortyEight;
using Xunit;

namespace WindowsITToolkit.Tests;

public sealed class TwentyFortyEightBestScoreServiceTests
{
    [Fact]
    public void BestScorePersistsAndLowerScoresDoNotReplaceIt()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var path = Path.Combine(directory, "best.json");
            var service = new TwentyFortyEightBestScoreService(path);

            Assert.True(service.SaveIfHigher(1200).Succeeded);
            Assert.True(service.SaveIfHigher(800).Succeeded);
            Assert.Equal(1200, new TwentyFortyEightBestScoreService(path).Load().Score);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void MissingOrCorruptedSaveLoadsSafely()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var path = Path.Combine(directory, "best.json");
            var service = new TwentyFortyEightBestScoreService(path);
            Assert.Equal(0, service.Load().Score);
            File.WriteAllText(path, "not json");
            Assert.Equal(0, service.Load().Score);
            Assert.NotNull(service.Load().Warning);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string CreateTemporaryDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"TwentyFortyEightTests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }
}
