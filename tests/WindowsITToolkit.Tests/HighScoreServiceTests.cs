using Xunit;

namespace WindowsITToolkit.Tests;

public sealed class HighScoreServiceTests
{
    [Fact]
    public void HighScorePersistsAndLowerScoresDoNotReplaceIt()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var filePath = Path.Combine(directory, "highscore.json");
            var service = new HighScoreService(filePath);

            Assert.True(service.SaveIfHigher(80).Succeeded);
            Assert.True(service.SaveIfHigher(30).Succeeded);

            var loaded = new HighScoreService(filePath).Load();
            Assert.True(loaded.Succeeded);
            Assert.Equal(80, loaded.Score);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void InvalidSaveDataLoadsSafelyAsZero()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var filePath = Path.Combine(directory, "highscore.json");
            File.WriteAllText(filePath, "not json");

            var result = new HighScoreService(filePath).Load();

            Assert.True(result.Succeeded);
            Assert.Equal(0, result.Score);
            Assert.NotNull(result.Warning);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void FailedReplacementPreservesExistingHighScore()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var filePath = Path.Combine(directory, "highscore.json");
            File.WriteAllText(filePath, """{"Score":80}""");
            var service = new HighScoreService(filePath);

            HighScoreResult result;
            using (new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                result = service.SaveIfHigher(120);
            }

            Assert.False(result.Succeeded);
            Assert.Equal(80, new HighScoreService(filePath).Load().Score);
            Assert.Single(Directory.GetFiles(directory));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string CreateTemporaryDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"NeonSnakeTests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }
}
