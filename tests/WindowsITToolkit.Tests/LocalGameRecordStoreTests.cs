using WindowsITToolkit;
using Xunit;

namespace WindowsITToolkit.Tests;

public sealed class LocalGameRecordStoreTests
{
    [Fact]
    public void SavesFastestTimeAndFewestMovesIndependently()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var path = Path.Combine(directory, "records.json");
            var store = LocalGameRecordStore.ForFile(path);
            Assert.True(store.SaveBestTimeIfLower("Easy", 90).Succeeded);
            Assert.True(store.SaveBestTimeIfLower("Easy", 120).Succeeded);
            Assert.True(store.SaveFewestMovesIfLower("Easy", 14).Succeeded);
            Assert.True(store.SaveFewestMovesIfLower("Easy", 20).Succeeded);

            var record = LocalGameRecordStore.ForFile(path).Load().Records["Easy"];
            Assert.Equal(90, record.BestTimeSeconds);
            Assert.Equal(14, record.FewestMoves);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void MissingAndCorruptRecordsLoadSafely()
    {
        var directory = CreateTemporaryDirectory();
        try
        {
            var path = Path.Combine(directory, "records.json");
            Assert.Empty(LocalGameRecordStore.ForFile(path).Load().Records);
            File.WriteAllText(path, "invalid json");

            var result = LocalGameRecordStore.ForFile(path).Load();
            Assert.Empty(result.Records);
            Assert.NotNull(result.Warning);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private static string CreateTemporaryDirectory()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"ArcadeRecords-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }
}
