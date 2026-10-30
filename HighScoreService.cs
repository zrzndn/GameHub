using System.IO;
using System.Security;
using System.Text.Json;

namespace WindowsITToolkit;

public readonly record struct HighScoreResult(int Score, bool Succeeded, string? Warning);

public sealed class HighScoreService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _filePath;

    public HighScoreService()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NeonSnake",
            "highscore.json"))
    {
    }

    public HighScoreService(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        _filePath = filePath;
    }

    public HighScoreResult Load()
    {
        try
        {
            var json = File.ReadAllText(_filePath);
            var data = JsonSerializer.Deserialize<HighScoreData>(json);
            if (data is null || data.Score < 0)
            {
                return new HighScoreResult(0, true, "Saved high score data was invalid and has been reset.");
            }

            return new HighScoreResult(data.Score, true, null);
        }
        catch (FileNotFoundException)
        {
            return new HighScoreResult(0, true, null);
        }
        catch (DirectoryNotFoundException)
        {
            return new HighScoreResult(0, true, null);
        }
        catch (JsonException)
        {
            return new HighScoreResult(0, true, "Saved high score data was unreadable and will be replaced when a new record is saved.");
        }
        catch (IOException)
        {
            return new HighScoreResult(0, false, "The saved high score could not be read.");
        }
        catch (UnauthorizedAccessException)
        {
            return new HighScoreResult(0, false, "The saved high score could not be read because access was denied.");
        }
        catch (SecurityException)
        {
            return new HighScoreResult(0, false, "The saved high score could not be read because access was denied.");
        }
    }

    public HighScoreResult SaveIfHigher(int score)
    {
        if (score < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(score), "High score cannot be negative.");
        }

        var current = Load();
        if (!current.Succeeded)
        {
            return current;
        }

        if (score <= current.Score)
        {
            return current;
        }

        var temporaryFilePath = $"{_filePath}.{Guid.NewGuid():N}.tmp";
        HighScoreResult result;
        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(new HighScoreData(score), JsonOptions);
            File.WriteAllText(temporaryFilePath, json);
            File.Move(temporaryFilePath, _filePath, overwrite: true);
            result = new HighScoreResult(score, true, current.Warning);
        }
        catch (IOException)
        {
            result = new HighScoreResult(current.Score, false, "The new high score could not be saved.");
        }
        catch (UnauthorizedAccessException)
        {
            result = new HighScoreResult(current.Score, false, "The new high score could not be saved because access was denied.");
        }
        catch (SecurityException)
        {
            result = new HighScoreResult(current.Score, false, "The new high score could not be saved because access was denied.");
        }

        if (File.Exists(temporaryFilePath))
        {
            try
            {
                File.Delete(temporaryFilePath);
            }
            catch (IOException)
            {
                result = result with
                {
                    Warning = AppendWarning(result.Warning, "A temporary high score file could not be removed.")
                };
            }
            catch (UnauthorizedAccessException)
            {
                result = result with
                {
                    Warning = AppendWarning(result.Warning, "A temporary high score file could not be removed because access was denied.")
                };
            }
            catch (SecurityException)
            {
                result = result with
                {
                    Warning = AppendWarning(result.Warning, "A temporary high score file could not be removed because access was denied.")
                };
            }
        }

        return result;
    }

    private static string AppendWarning(string? existingWarning, string warning) =>
        string.IsNullOrEmpty(existingWarning) ? warning : $"{existingWarning} {warning}";

    private sealed record HighScoreData(int Score);
}
