namespace WindowsITToolkit.Games.TwentyFortyEight;

public enum TwentyFortyEightDirection
{
    Left,
    Right,
    Up,
    Down
}

public sealed class TwentyFortyEightEngine
{
    public const int BoardSize = 4;
    public const int TargetTile = 2048;

    private readonly Random _random;
    private readonly int[,] _board = new int[BoardSize, BoardSize];

    public TwentyFortyEightEngine(Random? random = null)
    {
        _random = random ?? Random.Shared;
        StartNewGame();
    }

    internal TwentyFortyEightEngine(Random random, int[,] startingBoard, int score = 0)
    {
        ArgumentNullException.ThrowIfNull(random);
        ArgumentNullException.ThrowIfNull(startingBoard);
        if (startingBoard.GetLength(0) != BoardSize || startingBoard.GetLength(1) != BoardSize ||
            startingBoard.Cast<int>().Any(value => value < 0 || (value != 0 && (value & (value - 1)) != 0)) ||
            score < 0)
        {
            throw new ArgumentException("The starting board or score is invalid.");
        }

        _random = random;
        Array.Copy(startingBoard, _board, startingBoard.Length);
        Score = score;
        HasWon = _board.Cast<int>().Any(value => value >= TargetTile);
        UpdateGameOver();
    }

    public int Score { get; private set; }
    public bool HasWon { get; private set; }
    public bool HasContinuedAfterWin { get; private set; }
    public bool IsGameOver { get; private set; }
    public int[,] Board => (int[,])_board.Clone();

    public int GetCell(int row, int column) => _board[row, column];

    public void StartNewGame()
    {
        Array.Clear(_board);
        Score = 0;
        HasWon = false;
        HasContinuedAfterWin = false;
        IsGameOver = false;
        SpawnTile();
        SpawnTile();
    }

    public bool ContinueGame()
    {
        if (!HasWon || IsGameOver)
        {
            return false;
        }

        HasContinuedAfterWin = true;
        return true;
    }

    public bool Move(TwentyFortyEightDirection direction)
    {
        if (IsGameOver)
        {
            return false;
        }

        var changed = false;
        var scoreIncrease = 0;
        for (var lineIndex = 0; lineIndex < BoardSize; lineIndex++)
        {
            var line = new int[BoardSize];
            for (var position = 0; position < BoardSize; position++)
            {
                var (row, column) = GetPosition(direction, lineIndex, position);
                line[position] = _board[row, column];
            }

            var mergedLine = MergeLine(line, ref scoreIncrease);
            for (var position = 0; position < BoardSize; position++)
            {
                var (row, column) = GetPosition(direction, lineIndex, position);
                if (_board[row, column] != mergedLine[position])
                {
                    changed = true;
                    _board[row, column] = mergedLine[position];
                }
            }
        }

        if (!changed)
        {
            return false;
        }

        Score += scoreIncrease;
        SpawnTile();
        HasWon |= _board.Cast<int>().Any(value => value >= TargetTile);
        UpdateGameOver();
        return true;
    }

    private void SpawnTile()
    {
        var emptyCount = _board.Cast<int>().Count(value => value == 0);
        if (emptyCount == 0)
        {
            return;
        }

        var target = _random.Next(emptyCount);
        var index = 0;
        for (var row = 0; row < BoardSize; row++)
        {
            for (var column = 0; column < BoardSize; column++)
            {
                if (_board[row, column] != 0)
                {
                    continue;
                }

                if (index++ == target)
                {
                    _board[row, column] = _random.Next(10) == 0 ? 4 : 2;
                    return;
                }
            }
        }
    }

    private void UpdateGameOver()
    {
        IsGameOver = true;
        for (var row = 0; row < BoardSize; row++)
        {
            for (var column = 0; column < BoardSize; column++)
            {
                if (_board[row, column] == 0 ||
                    (row + 1 < BoardSize && _board[row, column] == _board[row + 1, column]) ||
                    (column + 1 < BoardSize && _board[row, column] == _board[row, column + 1]))
                {
                    IsGameOver = false;
                    return;
                }
            }
        }
    }

    private static int[] MergeLine(IReadOnlyList<int> line, ref int scoreIncrease)
    {
        var compacted = line.Where(value => value != 0).ToArray();
        var merged = new List<int>(BoardSize);
        for (var index = 0; index < compacted.Length; index++)
        {
            if (index + 1 < compacted.Length && compacted[index] == compacted[index + 1])
            {
                var value = checked(compacted[index] * 2);
                merged.Add(value);
                scoreIncrease = checked(scoreIncrease + value);
                index++;
            }
            else
            {
                merged.Add(compacted[index]);
            }
        }

        return merged.Concat(Enumerable.Repeat(0, BoardSize - merged.Count)).ToArray();
    }

    private static (int Row, int Column) GetPosition(
        TwentyFortyEightDirection direction,
        int lineIndex,
        int position) =>
        direction switch
        {
            TwentyFortyEightDirection.Left => (lineIndex, position),
            TwentyFortyEightDirection.Right => (lineIndex, BoardSize - 1 - position),
            TwentyFortyEightDirection.Up => (position, lineIndex),
            TwentyFortyEightDirection.Down => (BoardSize - 1 - position, lineIndex),
            _ => throw new ArgumentOutOfRangeException(nameof(direction))
        };
}
