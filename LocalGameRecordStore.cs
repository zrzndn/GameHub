using System.IO;
using System.Security;
using System.Text.Json;

namespace WindowsITToolkit;

public sealed record LocalGameRecord(int? BestTimeSeconds, int? FewestMoves);
public readonly record struct GameRecordsLoadResult(
    IReadOnlyDictionary<string, LocalGameRecord> Records,
    bool Succeeded,
    string? Warning);
public readonly record struct GameRecordSaveResult(bool Succeeded, string? Warning);

public sealed class LocalGameRecordStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _filePath;

    public LocalGameRecordStore(string gameId)
        : this(new FileInfo(GetGamePath(gameId)))
    {
    }

    private LocalGameRecordStore(FileInfo file)
    {
        ArgumentNullException.ThrowIfNull(file);
        _filePath = file.FullName;
    }

    public static LocalGameRecordStore ForFile(string filePath) =>
        new(new FileInfo(ValidateFilePath(filePath)));

    private static string GetGamePath(string gameId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(gameId);
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "WindowsArcadeHub",
            $"{gameId}-records.json");
    }

    private static string ValidateFilePath(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        return filePath;
    }

    public GameRecordsLoadResult Load()
    {
        try
        {
            var records = JsonSerializer.Deserialize<Dictionary<string, LocalGameRecord>>(
                File.ReadAllText(_filePath));
            if (records is null || records.Any(entry =>
                string.IsNullOrWhiteSpace(entry.Key) ||
                entry.Value is null ||
                entry.Value.BestTimeSeconds < 0 ||
                entry.Value.FewestMoves < 0))
            {
                return new GameRecordsLoadResult(
                    new Dictionary<string, LocalGameRecord>(),
                    true,
                    "Saved game records were invalid.");
            }

            return new GameRecordsLoadResult(records, true, null);
        }
        catch (FileNotFoundException)
        {
            return new GameRecordsLoadResult(new Dictionary<string, LocalGameRecord>(), true, null);
        }
        catch (DirectoryNotFoundException)
        {
            return new GameRecordsLoadResult(new Dictionary<string, LocalGameRecord>(), true, null);
        }
        catch (JsonException)
        {
            return new GameRecordsLoadResult(
                new Dictionary<string, LocalGameRecord>(),
                true,
                "Saved game records were unreadable.");
        }
        catch (IOException)
        {
            return new GameRecordsLoadResult(
                new Dictionary<string, LocalGameRecord>(),
                false,
                "Saved game records could not be read.");
        }
        catch (UnauthorizedAccessException)
        {
            return new GameRecordsLoadResult(
                new Dictionary<string, LocalGameRecord>(),
                false,
                "Saved game records could not be read because access was denied.");
        }
        catch (SecurityException)
        {
            return new GameRecordsLoadResult(
                new Dictionary<string, LocalGameRecord>(),
                false,
                "Saved game records could not be read because access was denied.");
        }
    }

    public GameRecordSaveResult SaveBestTimeIfLower(string difficulty, int seconds)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(difficulty);
        ArgumentOutOfRangeException.ThrowIfNegative(seconds);
        return Update(difficulty, record =>
            record with
            {
                BestTimeSeconds = record.BestTimeSeconds is null
                    ? seconds
                    : Math.Min(record.BestTimeSeconds.Value, seconds)
            },
            record => record.BestTimeSeconds is null || seconds < record.BestTimeSeconds.Value);
    }

    public GameRecordSaveResult SaveFewestMovesIfLower(string difficulty, int moves)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(difficulty);
        ArgumentOutOfRangeException.ThrowIfNegative(moves);
        return Update(difficulty, record =>
            record with
            {
                FewestMoves = record.FewestMoves is null
                    ? moves
                    : Math.Min(record.FewestMoves.Value, moves)
            },
            record => record.FewestMoves is null || moves < record.FewestMoves.Value);
    }

    private GameRecordSaveResult Update(
        string difficulty,
        Func<LocalGameRecord, LocalGameRecord> update,
        Func<LocalGameRecord, bool> isImprovement)
    {
        var loaded = Load();
        if (!loaded.Succeeded)
        {
            return new GameRecordSaveResult(false, loaded.Warning);
        }

        var records = new Dictionary<string, LocalGameRecord>(loaded.Records, StringComparer.OrdinalIgnoreCase);
        var existing = records.GetValueOrDefault(difficulty) ?? new LocalGameRecord(null, null);
        if (!isImprovement(existing))
        {
            return new GameRecordSaveResult(true, loaded.Warning);
        }

        records[difficulty] = update(existing);
        var temporaryPath = $"{_filePath}.{Guid.NewGuid():N}.tmp";
        GameRecordSaveResult result;
        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(temporaryPath, JsonSerializer.Serialize(records, JsonOptions));
            File.Move(temporaryPath, _filePath, overwrite: true);
            result = new GameRecordSaveResult(true, loaded.Warning);
        }
        catch (IOException)
        {
            result = new GameRecordSaveResult(false, "The new game record could not be saved.");
        }
        catch (UnauthorizedAccessException)
        {
            result = new GameRecordSaveResult(false, "The new game record could not be saved because access was denied.");
        }
        catch (SecurityException)
        {
            result = new GameRecordSaveResult(false, "The new game record could not be saved because access was denied.");
        }

        if (File.Exists(temporaryPath))
        {
            try
            {
                File.Delete(temporaryPath);
            }
            catch (IOException)
            {
                result = result with { Warning = AppendWarning(result.Warning, "A temporary records file could not be removed.") };
            }
            catch (UnauthorizedAccessException)
            {
                result = result with { Warning = AppendWarning(result.Warning, "A temporary records file could not be removed because access was denied.") };
            }
            catch (SecurityException)
            {
                result = result with { Warning = AppendWarning(result.Warning, "A temporary records file could not be removed because access was denied.") };
            }
        }

        return result;
    }

    private static string AppendWarning(string? existingWarning, string warning) =>
        string.IsNullOrEmpty(existingWarning) ? warning : $"{existingWarning} {warning}";
}
