namespace WindowsITToolkit.Games.Minesweeper;

public enum MinesweeperDifficulty
{
    Beginner,
    Intermediate,
    Expert
}

public enum MinesweeperResult
{
    InProgress,
    Won,
    Lost
}

public readonly record struct MinesweeperSettings(int Columns, int Rows, int MineCount)
{
    public static MinesweeperSettings For(MinesweeperDifficulty difficulty) =>
        difficulty switch
        {
            MinesweeperDifficulty.Beginner => new(9, 9, 10),
            MinesweeperDifficulty.Intermediate => new(16, 16, 40),
            MinesweeperDifficulty.Expert => new(30, 16, 99),
            _ => throw new ArgumentOutOfRangeException(nameof(difficulty))
        };
}

public sealed class MinesweeperEngine
{
    private readonly Random _random;
    private readonly MinesweeperCell[,] _cells;
    private bool _minesPlaced;
    private bool _firstRevealPending = true;

    public MinesweeperEngine(MinesweeperDifficulty difficulty, Random? random = null)
        : this(difficulty, random ?? Random.Shared, null)
    {
    }

    public MinesweeperEngine(
        MinesweeperDifficulty difficulty,
        Random random,
        IReadOnlyCollection<(int Row, int Column)>? minePositions)
    {
        ArgumentNullException.ThrowIfNull(random);
        Difficulty = difficulty;
        Settings = MinesweeperSettings.For(difficulty);
        if (Settings.MineCount >= Settings.Rows * Settings.Columns)
        {
            throw new ArgumentException("The board must have at least one safe cell.", nameof(difficulty));
        }

        _random = random;
        _cells = new MinesweeperCell[Settings.Rows, Settings.Columns];
        for (var row = 0; row < Settings.Rows; row++)
        {
            for (var column = 0; column < Settings.Columns; column++)
            {
                _cells[row, column] = new MinesweeperCell(row, column);
            }
        }

        if (minePositions is not null)
        {
            if (minePositions.Count != Settings.MineCount ||
                minePositions.Any(position => !IsInBounds(position.Row, position.Column)) ||
                minePositions.Distinct().Count() != Settings.MineCount)
            {
                throw new ArgumentException("Mine positions must be unique, in bounds, and match the difficulty.", nameof(minePositions));
            }

            foreach (var (row, column) in minePositions)
            {
                _cells[row, column].IsMine = true;
            }

            _minesPlaced = true;
            CalculateAdjacentMines();
        }
    }

    public MinesweeperDifficulty Difficulty { get; }
    public MinesweeperSettings Settings { get; }
    public MinesweeperResult Result { get; private set; } = MinesweeperResult.InProgress;
    public int RevealedSafeCellCount { get; private set; }
    public int FlaggedCellCount { get; private set; }
    public bool HasStarted => _minesPlaced;
    public MinesweeperCell[,] Cells => (MinesweeperCell[,])_cells.Clone();
    public MinesweeperCell GetCell(int row, int column) => _cells[row, column];

    public bool ToggleFlag(int row, int column)
    {
        if (Result != MinesweeperResult.InProgress || !IsInBounds(row, column))
        {
            return false;
        }

        var cell = _cells[row, column];
        if (cell.IsRevealed)
        {
            return false;
        }

        cell.IsFlagged = !cell.IsFlagged;
        FlaggedCellCount += cell.IsFlagged ? 1 : -1;
        return true;
    }

    public bool Reveal(int row, int column)
    {
        if (Result != MinesweeperResult.InProgress || !IsInBounds(row, column))
        {
            return false;
        }

        var selectedCell = _cells[row, column];
        if (selectedCell.IsRevealed || selectedCell.IsFlagged)
        {
            return false;
        }

        if (_firstRevealPending)
        {
            if (!_minesPlaced)
            {
                PlaceMines(row, column);
            }
            else if (selectedCell.IsMine)
            {
                RelocateFirstClickedMine(row, column);
            }

            _firstRevealPending = false;
        }

        if (selectedCell.IsMine)
        {
            selectedCell.IsRevealed = true;
            RevealMines();
            Result = MinesweeperResult.Lost;
            return true;
        }

        RevealSafeArea(selectedCell);
        if (RevealedSafeCellCount == Settings.Rows * Settings.Columns - Settings.MineCount)
        {
            Result = MinesweeperResult.Won;
        }

        return true;
    }

    private void PlaceMines(int safeRow, int safeColumn)
    {
        var candidates = new List<(int Row, int Column)>(
            Settings.Rows * Settings.Columns - 1);
        for (var row = 0; row < Settings.Rows; row++)
        {
            for (var column = 0; column < Settings.Columns; column++)
            {
                if (row != safeRow || column != safeColumn)
                {
                    candidates.Add((row, column));
                }
            }
        }

        for (var index = 0; index < Settings.MineCount; index++)
        {
            var selected = index + _random.Next(candidates.Count - index);
            (candidates[index], candidates[selected]) = (candidates[selected], candidates[index]);
            var (row, column) = candidates[index];
            _cells[row, column].IsMine = true;
        }

        _minesPlaced = true;
        CalculateAdjacentMines();
    }

    private void CalculateAdjacentMines()
    {
        for (var row = 0; row < Settings.Rows; row++)
        {
            for (var column = 0; column < Settings.Columns; column++)
            {
                if (_cells[row, column].IsMine)
                {
                    continue;
                }

                _cells[row, column].AdjacentMines = GetNeighbors(row, column)
                    .Count(position => _cells[position.Row, position.Column].IsMine);
            }
        }
    }

    private void RelocateFirstClickedMine(int safeRow, int safeColumn)
    {
        _cells[safeRow, safeColumn].IsMine = false;
        for (var row = 0; row < Settings.Rows; row++)
        {
            for (var column = 0; column < Settings.Columns; column++)
            {
                if (!_cells[row, column].IsMine && (row != safeRow || column != safeColumn))
                {
                    _cells[row, column].IsMine = true;
                    CalculateAdjacentMines();
                    return;
                }
            }
        }

        throw new InvalidOperationException("The board does not contain a safe cell for the first move.");
    }

    private void RevealSafeArea(MinesweeperCell first)
    {
        var pending = new Queue<MinesweeperCell>();
        pending.Enqueue(first);
        while (pending.TryDequeue(out var cell))
        {
            if (cell.IsMine || cell.IsFlagged || cell.IsRevealed)
            {
                continue;
            }

            cell.IsRevealed = true;
            RevealedSafeCellCount++;
            if (cell.AdjacentMines == 0)
            {
                foreach (var neighbor in GetNeighbors(cell.Row, cell.Column))
                {
                    pending.Enqueue(_cells[neighbor.Row, neighbor.Column]);
                }
            }
        }
    }

    private void RevealMines()
    {
        for (var row = 0; row < Settings.Rows; row++)
        {
            for (var column = 0; column < Settings.Columns; column++)
            {
                var cell = _cells[row, column];
                if (cell.IsMine)
                {
                    cell.IsRevealed = true;
                }

                cell.IsIncorrectFlag = cell.IsFlagged && !cell.IsMine;
            }
        }
    }

    private IEnumerable<(int Row, int Column)> GetNeighbors(int row, int column)
    {
        for (var adjacentRow = Math.Max(0, row - 1); adjacentRow <= Math.Min(Settings.Rows - 1, row + 1); adjacentRow++)
        {
            for (var adjacentColumn = Math.Max(0, column - 1); adjacentColumn <= Math.Min(Settings.Columns - 1, column + 1); adjacentColumn++)
            {
                if (adjacentRow != row || adjacentColumn != column)
                {
                    yield return (adjacentRow, adjacentColumn);
                }
            }
        }
    }

    private bool IsInBounds(int row, int column) =>
        row >= 0 && row < Settings.Rows && column >= 0 && column < Settings.Columns;
}
