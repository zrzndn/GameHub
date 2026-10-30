using System.IO;
using System.Security;
using System.Text.Json;

namespace WindowsITToolkit.Games.TwentyFortyEight;

public readonly record struct BestScoreLoadResult(int Score, bool Succeeded, string? Warning);
public readonly record struct BestScoreSaveResult(bool Succeeded, string? Warning);

public sealed class TwentyFortyEightBestScoreService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _filePath;

    public TwentyFortyEightBestScoreService()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WindowsArcadeHub",
            "2048-best-score.json"))
    {
    }

    public TwentyFortyEightBestScoreService(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        _filePath = filePath;
    }

    public BestScoreLoadResult Load()
    {
        try
        {
            var data = JsonSerializer.Deserialize<BestScoreData>(File.ReadAllText(_filePath));
            return data?.Score is null or < 0
                ? new BestScoreLoadResult(0, true, "The saved 2048 best score was invalid.")
                : new BestScoreLoadResult(data.Score.Value, true, null);
        }
        catch (FileNotFoundException)
        {
            return new BestScoreLoadResult(0, true, null);
        }
        catch (DirectoryNotFoundException)
        {
            return new BestScoreLoadResult(0, true, null);
        }
        catch (JsonException)
        {
            return new BestScoreLoadResult(0, true, "The saved 2048 best score was unreadable.");
        }
        catch (IOException)
        {
            return new BestScoreLoadResult(0, false, "The saved 2048 best score could not be read.");
        }
        catch (UnauthorizedAccessException)
        {
            return new BestScoreLoadResult(0, false, "The saved 2048 best score could not be read because access was denied.");
        }
        catch (SecurityException)
        {
            return new BestScoreLoadResult(0, false, "The saved 2048 best score could not be read because access was denied.");
        }
    }

    public BestScoreSaveResult SaveIfHigher(int score)
    {
        if (score < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(score));
        }

        var current = Load();
        if (!current.Succeeded)
        {
            return new BestScoreSaveResult(false, current.Warning);
        }

        if (score <= current.Score)
        {
            return new BestScoreSaveResult(true, current.Warning);
        }

        var temporaryPath = $"{_filePath}.{Guid.NewGuid():N}.tmp";
        BestScoreSaveResult result;
        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(new BestScoreData(score), JsonOptions));
            File.Move(temporaryPath, _filePath, overwrite: true);
            result = new BestScoreSaveResult(true, current.Warning);
        }
        catch (IOException)
        {
            result = new BestScoreSaveResult(false, "The 2048 best score could not be saved.");
        }
        catch (UnauthorizedAccessException)
        {
            result = new BestScoreSaveResult(false, "The 2048 best score could not be saved because access was denied.");
        }
        catch (SecurityException)
        {
            result = new BestScoreSaveResult(false, "The 2048 best score could not be saved because access was denied.");
        }

        if (File.Exists(temporaryPath))
        {
            try
            {
                File.Delete(temporaryPath);
            }
            catch (IOException)
            {
                result = result with { Warning = AppendWarning(result.Warning, "A temporary best-score file could not be removed.") };
            }
            catch (UnauthorizedAccessException)
            {
                result = result with { Warning = AppendWarning(result.Warning, "A temporary best-score file could not be removed because access was denied.") };
            }
            catch (SecurityException)
            {
                result = result with { Warning = AppendWarning(result.Warning, "A temporary best-score file could not be removed because access was denied.") };
            }
        }

        return result;
    }

    private static string AppendWarning(string? existingWarning, string warning) =>
        string.IsNullOrEmpty(existingWarning) ? warning : $"{existingWarning} {warning}";

    private sealed record BestScoreData(int? Score);
}
