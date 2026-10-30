using WindowsITToolkit.Games.TwentyFortyEight;
using Xunit;

namespace WindowsITToolkit.Tests;

public sealed class TwentyFortyEightEngineTests
{
    [Fact]
    public void NewGameStartsWithTwoTilesAndZeroScore()
    {
        var engine = new TwentyFortyEightEngine(new FixedRandom());

        Assert.Equal(2, engine.Board.Cast<int>().Count(value => value != 0));
        Assert.All(engine.Board.Cast<int>().Where(value => value != 0), value => Assert.Contains(value, new[] { 2, 4 }));
        Assert.Equal(0, engine.Score);
        Assert.False(engine.IsGameOver);
    }

    [Theory]
    [InlineData(TwentyFortyEightDirection.Left)]
    [InlineData(TwentyFortyEightDirection.Right)]
    [InlineData(TwentyFortyEightDirection.Up)]
    [InlineData(TwentyFortyEightDirection.Down)]
    public void MergesFourEqualTilesOnceInEveryDirection(TwentyFortyEightDirection direction)
    {
        var board = EmptyBoard();
        if (direction is TwentyFortyEightDirection.Left or TwentyFortyEightDirection.Right)
        {
            for (var column = 0; column < 4; column++)
            {
                board[0, column] = 2;
            }
        }
        else
        {
            for (var row = 0; row < 4; row++)
            {
                board[row, 0] = 2;
            }
        }

        var engine = CreateEngine(board);

        Assert.True(engine.Move(direction));
        if (direction == TwentyFortyEightDirection.Left)
        {
            Assert.Equal(new[] { 4, 4 }, Enumerable.Range(0, 2).Select(column => engine.GetCell(0, column)));
        }
        else if (direction == TwentyFortyEightDirection.Right)
        {
            Assert.Equal(new[] { 4, 4 }, Enumerable.Range(2, 2).Select(column => engine.GetCell(0, column)));
        }
        else if (direction == TwentyFortyEightDirection.Up)
        {
            Assert.Equal(new[] { 4, 4 }, Enumerable.Range(0, 2).Select(row => engine.GetCell(row, 0)));
        }
        else
        {
            Assert.Equal(new[] { 4, 4 }, Enumerable.Range(2, 2).Select(row => engine.GetCell(row, 0)));
        }

        Assert.Equal(8, engine.Score);
    }

    [Fact]
    public void CompressesEmptyCellsBeforeMerging()
    {
        var board = EmptyBoard();
        board[0, 0] = 2;
        board[0, 3] = 2;
        var engine = CreateEngine(board);

        Assert.True(engine.Move(TwentyFortyEightDirection.Left));

        Assert.Equal(4, engine.GetCell(0, 0));
        Assert.Equal(4, engine.Score);
    }

    [Fact]
    public void ScoresOnlyCreatedTilesAndDoesNotSpawnForInvalidMove()
    {
        var board = EmptyBoard();
        board[0, 0] = 2;
        var engine = CreateEngine(board);
        var before = engine.Board;

        Assert.False(engine.Move(TwentyFortyEightDirection.Left));
        Assert.Equal(0, engine.Score);
        Assert.Equal(before, engine.Board);

        board[0, 0] = 2;
        board[0, 1] = 2;
        engine = CreateEngine(board);
        Assert.True(engine.Move(TwentyFortyEightDirection.Left));
        Assert.Equal(4, engine.Score);
    }

    [Fact]
    public void DetectsWinAndAllowsContinuing()
    {
        var board = EmptyBoard();
        board[0, 0] = 1024;
        board[0, 1] = 1024;
        var engine = CreateEngine(board);

        Assert.True(engine.Move(TwentyFortyEightDirection.Left));
        Assert.True(engine.HasWon);
        Assert.False(engine.IsGameOver);
        Assert.True(engine.ContinueGame());
        Assert.True(engine.HasContinuedAfterWin);
        Assert.True(engine.Move(TwentyFortyEightDirection.Right));
    }

    [Fact]
    public void DetectsGameOverWhenNoMovesRemain()
    {
        var board = new[,]
        {
            { 2, 4, 2, 4 },
            { 4, 2, 4, 2 },
            { 2, 4, 2, 4 },
            { 4, 2, 4, 2 }
        };
        var engine = CreateEngine(board);

        Assert.True(engine.IsGameOver);
        Assert.False(engine.Move(TwentyFortyEightDirection.Left));
    }

    [Fact]
    public void RestartResetsBoardScoreAndWinState()
    {
        var board = EmptyBoard();
        board[0, 0] = 2;
        board[0, 1] = 2;
        var engine = CreateEngine(board);
        engine.Move(TwentyFortyEightDirection.Left);

        engine.StartNewGame();

        Assert.Equal(0, engine.Score);
        Assert.False(engine.HasWon);
        Assert.False(engine.IsGameOver);
        Assert.Equal(2, engine.Board.Cast<int>().Count(value => value != 0));
    }

    private static TwentyFortyEightEngine CreateEngine(int[,] board) =>
        new(new FixedRandom(), board);

    private static int[,] EmptyBoard() => new int[4, 4];

    private sealed class FixedRandom : Random
    {
        public override int Next(int maxValue) => 0;
    }
}
