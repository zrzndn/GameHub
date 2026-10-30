using WindowsITToolkit.Games.Minesweeper;
using Xunit;

namespace WindowsITToolkit.Tests;

public sealed class MinesweeperEngineTests
{
    [Theory]
    [InlineData(MinesweeperDifficulty.Beginner, 9, 9, 10)]
    [InlineData(MinesweeperDifficulty.Intermediate, 16, 16, 40)]
    [InlineData(MinesweeperDifficulty.Expert, 30, 16, 99)]
    public void DifficultySetsDimensionsAndMineCount(MinesweeperDifficulty difficulty, int columns, int rows, int mines)
    {
        var engine = new MinesweeperEngine(difficulty, new FixedRandom());

        Assert.Equal(columns, engine.Settings.Columns);
        Assert.Equal(rows, engine.Settings.Rows);
        Assert.Equal(mines, engine.Settings.MineCount);
        Assert.Equal(rows, engine.Cells.GetLength(0));
        Assert.Equal(columns, engine.Cells.GetLength(1));
    }

    [Theory]
    [InlineData(MinesweeperDifficulty.Beginner, 10)]
    [InlineData(MinesweeperDifficulty.Intermediate, 40)]
    [InlineData(MinesweeperDifficulty.Expert, 99)]
    public void PlacesUniqueConfiguredMineCount(MinesweeperDifficulty difficulty, int mineCount)
    {
        var engine = new MinesweeperEngine(difficulty, new FixedRandom());
        engine.Reveal(0, 0);

        Assert.Equal(mineCount, engine.Cells.Cast<MinesweeperCell>().Count(cell => cell.IsMine));
    }

    [Fact]
    public void CalculatesAdjacentMineCounts()
    {
        var mines = CreateMines((0, 0), (0, 1), (0, 2), (1, 0), (1, 2), (2, 0), (2, 1), (2, 2), (8, 8), (8, 7));
        var engine = new MinesweeperEngine(MinesweeperDifficulty.Beginner, new FixedRandom(), mines);

        Assert.Equal(8, engine.GetCell(1, 1).AdjacentMines);
        Assert.Equal(3, engine.GetCell(1, 3).AdjacentMines);
    }

    [Fact]
    public void FirstRevealedCellIsAlwaysSafe()
    {
        var engine = new MinesweeperEngine(MinesweeperDifficulty.Beginner, new FixedRandom());

        Assert.True(engine.Reveal(4, 4));

        Assert.False(engine.GetCell(4, 4).IsMine);
        Assert.True(engine.GetCell(4, 4).IsRevealed);
    }

    [Fact]
    public void FirstRevealedCellIsSafeEvenWhenInjectedMinePositionsIncludeIt()
    {
        var mines = CreateMines((4, 4), (0, 0), (0, 1), (0, 2), (1, 0), (1, 2), (2, 0), (2, 1), (2, 2), (8, 8));
        var engine = new MinesweeperEngine(MinesweeperDifficulty.Beginner, new FixedRandom(), mines);

        Assert.True(engine.Reveal(4, 4));

        Assert.False(engine.GetCell(4, 4).IsMine);
        Assert.Equal(10, engine.Cells.Cast<MinesweeperCell>().Count(cell => cell.IsMine));
        Assert.Equal(MinesweeperResult.InProgress, engine.Result);
    }

    [Fact]
    public void RevealingZeroExpandsSafeAreaIteratively()
    {
        var mines = CreateMines(
            (0, 0), (0, 1), (0, 2), (0, 8), (1, 0), (1, 2), (2, 0), (2, 1), (2, 2), (8, 8));
        var engine = new MinesweeperEngine(MinesweeperDifficulty.Beginner, new FixedRandom(), mines);

        engine.Reveal(5, 5);

        Assert.True(engine.GetCell(5, 5).IsRevealed);
        Assert.True(engine.GetCell(7, 7).IsRevealed);
        Assert.False(engine.GetCell(1, 1).IsRevealed);
    }

    [Fact]
    public void FlagsCanBeToggledAndFlaggedCellsCannotBeRevealed()
    {
        var engine = new MinesweeperEngine(MinesweeperDifficulty.Beginner, new FixedRandom());

        Assert.True(engine.ToggleFlag(4, 4));
        Assert.Equal(1, engine.FlaggedCellCount);
        Assert.False(engine.Reveal(4, 4));
        Assert.True(engine.ToggleFlag(4, 4));
        Assert.Equal(0, engine.FlaggedCellCount);
        Assert.True(engine.Reveal(4, 4));
    }

    [Fact]
    public void WinsWhenEverySafeCellIsRevealed()
    {
        var mines = CreateMines((8, 8), (8, 7), (8, 6), (8, 5), (8, 4), (8, 3), (8, 2), (8, 1), (8, 0), (7, 8));
        var engine = new MinesweeperEngine(MinesweeperDifficulty.Beginner, new FixedRandom(), mines);
        for (var row = 0; row < 8; row++)
        {
            for (var column = 0; column < 9; column++)
            {
                engine.Reveal(row, column);
            }
        }

        Assert.Equal(MinesweeperResult.Won, engine.Result);
    }

    [Fact]
    public void RevealingMineLosesAndShowsMinesAndIncorrectFlags()
    {
        var mines = CreateMines((0, 0), (0, 1), (0, 2), (1, 0), (1, 2), (2, 0), (2, 1), (2, 2), (8, 8), (8, 7));
        var engine = new MinesweeperEngine(MinesweeperDifficulty.Beginner, new FixedRandom(), mines);
        engine.ToggleFlag(4, 4);
        engine.Reveal(3, 3);

        Assert.True(engine.Reveal(0, 0));

        Assert.Equal(MinesweeperResult.Lost, engine.Result);
        Assert.True(engine.GetCell(0, 0).IsRevealed);
        Assert.True(engine.GetCell(0, 1).IsRevealed);
        Assert.True(engine.GetCell(4, 4).IsIncorrectFlag);
    }

    [Fact]
    public void NoActionsAreAllowedAfterGameOver()
    {
        var mines = CreateMines((0, 0), (0, 1), (0, 2), (1, 0), (1, 2), (2, 0), (2, 1), (2, 2), (8, 8), (8, 7));
        var engine = new MinesweeperEngine(MinesweeperDifficulty.Beginner, new FixedRandom(), mines);
        engine.Reveal(3, 3);
        engine.Reveal(0, 0);

        Assert.False(engine.Reveal(4, 4));
        Assert.False(engine.ToggleFlag(4, 4));
    }

    [Fact]
    public void RejectsDuplicateMinePositions()
    {
        var mines = CreateMines((0, 0), (0, 0), (0, 2), (1, 0), (1, 2), (2, 0), (2, 1), (2, 2), (8, 8), (8, 7));

        Assert.Throws<ArgumentException>(() =>
            new MinesweeperEngine(MinesweeperDifficulty.Beginner, new FixedRandom(), mines));
    }

    private static (int Row, int Column)[] CreateMines(params (int Row, int Column)[] fixedPositions)
    {
        var positions = fixedPositions.ToList();
        for (var row = 0; positions.Count < 10; row++)
        {
            for (var column = 0; column < 9 && positions.Count < 10; column++)
            {
                if (!positions.Contains((row, column)))
                {
                    positions.Add((row, column));
                }
            }
        }

        return positions.ToArray();
    }

    private sealed class FixedRandom : Random
    {
        public override int Next(int maxValue) => 0;
    }
}
