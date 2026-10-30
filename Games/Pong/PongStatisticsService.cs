using System.IO;
using System.Security;
using System.Text.Json;

namespace WindowsITToolkit.Games.Pong;

public sealed record PongStatistics(int MatchesPlayed, int PlayerWins, int ComputerWins, int BestRallyHits);
public readonly record struct PongStatisticsResult(PongStatistics Statistics, bool Succeeded, string? Warning);

public sealed class PongStatisticsService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _filePath;

    public PongStatisticsService()
        : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WindowsArcadeHub",
            "pong-statistics.json"))
    {
    }

    public PongStatisticsService(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        _filePath = filePath;
    }

    public PongStatisticsResult Load()
    {
        try
        {
            var statistics = JsonSerializer.Deserialize<PongStatistics>(File.ReadAllText(_filePath));
            if (statistics is null || statistics.MatchesPlayed < 0 ||
                statistics.PlayerWins < 0 || statistics.ComputerWins < 0 ||
                statistics.BestRallyHits < 0 ||
                statistics.PlayerWins + statistics.ComputerWins > statistics.MatchesPlayed)
            {
                return new PongStatisticsResult(new PongStatistics(0, 0, 0, 0), true, "Saved Pong statistics were invalid.");
            }

            return new PongStatisticsResult(statistics, true, null);
        }
        catch (FileNotFoundException)
        {
            return new PongStatisticsResult(new PongStatistics(0, 0, 0, 0), true, null);
        }
        catch (DirectoryNotFoundException)
        {
            return new PongStatisticsResult(new PongStatistics(0, 0, 0, 0), true, null);
        }
        catch (JsonException)
        {
            return new PongStatisticsResult(new PongStatistics(0, 0, 0, 0), true, "Saved Pong statistics were unreadable.");
        }
        catch (IOException)
        {
            return new PongStatisticsResult(new PongStatistics(0, 0, 0, 0), false, "Pong statistics could not be read.");
        }
        catch (UnauthorizedAccessException)
        {
            return new PongStatisticsResult(new PongStatistics(0, 0, 0, 0), false, "Pong statistics could not be read because access was denied.");
        }
        catch (SecurityException)
        {
            return new PongStatisticsResult(new PongStatistics(0, 0, 0, 0), false, "Pong statistics could not be read because access was denied.");
        }
    }

    public PongStatisticsResult RecordMatch(bool playerWon, int bestRallyHits)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(bestRallyHits);
        var current = Load();
        if (!current.Succeeded)
        {
            return current;
        }

        var updated = current.Statistics with
        {
            MatchesPlayed = current.Statistics.MatchesPlayed + 1,
            PlayerWins = current.Statistics.PlayerWins + (playerWon ? 1 : 0),
            ComputerWins = current.Statistics.ComputerWins + (playerWon ? 0 : 1),
            BestRallyHits = Math.Max(current.Statistics.BestRallyHits, bestRallyHits)
        };
        var temporaryPath = $"{_filePath}.{Guid.NewGuid():N}.tmp";
        PongStatisticsResult result;
        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(updated, JsonOptions));
            File.Move(temporaryPath, _filePath, overwrite: true);
            result = new PongStatisticsResult(updated, true, current.Warning);
        }
        catch (IOException)
        {
            result = new PongStatisticsResult(current.Statistics, false, "Pong statistics could not be saved.");
        }
        catch (UnauthorizedAccessException)
        {
            result = new PongStatisticsResult(current.Statistics, false, "Pong statistics could not be saved because access was denied.");
        }
        catch (SecurityException)
        {
            result = new PongStatisticsResult(current.Statistics, false, "Pong statistics could not be saved because access was denied.");
        }

        if (File.Exists(temporaryPath))
        {
            try
            {
                File.Delete(temporaryPath);
            }
            catch (IOException)
            {
                result = result with { Warning = AppendWarning(result.Warning, "A temporary statistics file could not be removed.") };
            }
            catch (UnauthorizedAccessException)
            {
                result = result with { Warning = AppendWarning(result.Warning, "A temporary statistics file could not be removed because access was denied.") };
            }
            catch (SecurityException)
            {
                result = result with { Warning = AppendWarning(result.Warning, "A temporary statistics file could not be removed because access was denied.") };
            }
        }

        return result;
    }

    private static string AppendWarning(string? existingWarning, string warning) =>
        string.IsNullOrEmpty(existingWarning) ? warning : $"{existingWarning} {warning}";
}
