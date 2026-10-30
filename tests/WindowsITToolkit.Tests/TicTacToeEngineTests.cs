using WindowsITToolkit.Games.TicTacToe;
using Xunit;

namespace WindowsITToolkit.Tests;

public sealed class TicTacToeEngineTests
{
    [Fact]
    public void StartsWithEmptyBoardAndXToMove()
    {
        var engine = new TicTacToeEngine();

        Assert.All(engine.Board, Assert.Null);
        Assert.Equal(TicTacToePlayer.X, engine.CurrentPlayer);
        Assert.Equal(TicTacToeResult.InProgress, engine.Result);
    }

    [Fact]
    public void ValidMovesAlternatePlayers()
    {
        var engine = new TicTacToeEngine();

        Assert.True(engine.TryMakeMove(0));
        Assert.Equal(TicTacToePlayer.O, engine.CurrentPlayer);
        Assert.True(engine.TryMakeMove(1));
        Assert.Equal(TicTacToePlayer.X, engine.CurrentPlayer);
    }

    [Fact]
    public void RejectsOccupiedCell()
    {
        var engine = new TicTacToeEngine();
        engine.TryMakeMove(0);

        Assert.False(engine.TryMakeMove(0));
        Assert.Equal(TicTacToePlayer.O, engine.CurrentPlayer);
    }

    [Theory]
    [InlineData(0, 3, 1, 4, 2)]
    [InlineData(0, 1, 3, 4, 6)]
    [InlineData(0, 1, 4, 2, 8)]
    public void DetectsWins(int x1, int o1, int x2, int o2, int x3)
    {
        var engine = new TicTacToeEngine();
        engine.TryMakeMove(x1);
        engine.TryMakeMove(o1);
        engine.TryMakeMove(x2);
        engine.TryMakeMove(o2);
        engine.TryMakeMove(x3);

        Assert.Equal(TicTacToeResult.XWon, engine.Result);
        Assert.Equal(1, engine.XScore);
        Assert.Equal(3, engine.WinningCells.Count);
    }

    [Fact]
    public void DetectsDraw()
    {
        var engine = new TicTacToeEngine();
        foreach (var cell in new[] { 0, 1, 2, 4, 3, 5, 7, 6, 8 })
        {
            engine.TryMakeMove(cell);
        }

        Assert.Equal(TicTacToeResult.Draw, engine.Result);
        Assert.Equal(1, engine.DrawCount);
    }

    [Fact]
    public void PlayerOWinsAndReceivesMatchPoint()
    {
        var engine = new TicTacToeEngine();
        foreach (var cell in new[] { 0, 3, 1, 4, 8, 5 })
        {
            engine.TryMakeMove(cell);
        }

        Assert.Equal(TicTacToeResult.OWon, engine.Result);
        Assert.Equal(1, engine.OScore);
    }

    [Fact]
    public void RejectsMovesAfterWinOrDraw()
    {
        var win = new TicTacToeEngine();
        foreach (var cell in new[] { 0, 3, 1, 4, 2 })
        {
            win.TryMakeMove(cell);
        }

        Assert.False(win.TryMakeMove(5));

        var draw = new TicTacToeEngine();
        foreach (var cell in new[] { 0, 1, 2, 4, 3, 5, 7, 6, 8 })
        {
            draw.TryMakeMove(cell);
        }

        Assert.False(draw.TryMakeMove(0));
    }

    [Fact]
    public void NewRoundClearsBoardAndPreservesMatchScores()
    {
        var engine = new TicTacToeEngine();
        foreach (var cell in new[] { 0, 3, 1, 4, 2 })
        {
            engine.TryMakeMove(cell);
        }

        engine.StartNewRound();

        Assert.All(engine.Board, Assert.Null);
        Assert.Equal(TicTacToePlayer.X, engine.CurrentPlayer);
        Assert.Equal(TicTacToeResult.InProgress, engine.Result);
        Assert.Equal(1, engine.XScore);
    }

    [Fact]
    public void ResetMatchClearsScoresAndRound()
    {
        var engine = new TicTacToeEngine();
        foreach (var cell in new[] { 0, 3, 1, 4, 2 })
        {
            engine.TryMakeMove(cell);
        }

        engine.ResetMatch();

        Assert.Equal(0, engine.XScore);
        Assert.Equal(0, engine.OScore);
        Assert.Equal(0, engine.DrawCount);
        Assert.All(engine.Board, Assert.Null);
    }
}
