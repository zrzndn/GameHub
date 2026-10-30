using System.Numerics;
using WindowsITToolkit.Games.Pong;
using Xunit;

namespace WindowsITToolkit.Tests;

public sealed class PongEngineTests
{
    [Fact]
    public void StartsReadyWithCenteredBallAndZeroScores()
    {
        var engine = new PongEngine(PongDifficulty.Normal, new FixedRandom());

        Assert.Equal(PongGameState.Ready, engine.State);
        Assert.Equal(0, engine.PlayerScore);
        Assert.Equal(0, engine.OpponentScore);
        Assert.Equal(new Vector2(450, 260), engine.BallPosition);
        Assert.Equal(Vector2.Zero, engine.BallVelocity);
    }

    [Fact]
    public void PlayerPaddleMovesAndStaysInsideArena()
    {
        var engine = new PongEngine(PongDifficulty.Normal, new FixedRandom());
        engine.SetPlayerMovement(-1);
        engine.Update(2);
        Assert.Equal(0, engine.PlayerPaddleY);

        engine.SetPlayerMovement(1);
        engine.Update(2);
        Assert.Equal(PongEngine.ArenaHeight - PongEngine.PaddleHeight, engine.PlayerPaddleY);
    }

    [Fact]
    public void BallMovesByVelocityAndReflectsFromBothWalls()
    {
        var moving = CreatePlayingEngine(new Vector2(450, 260), new Vector2(100, 0), 200, 200);
        moving.Update(0.1);
        Assert.InRange(moving.BallPosition.X, 459.9f, 460.1f);

        var top = CreatePlayingEngine(new Vector2(450, 10), new Vector2(0, -300), 200, 200);
        top.Update(0.05);
        Assert.True(top.BallVelocity.Y > 0);
        Assert.True(top.BallPosition.Y >= PongEngine.BallRadius);

        var bottom = CreatePlayingEngine(new Vector2(450, 510), new Vector2(0, 300), 200, 200);
        bottom.Update(0.05);
        Assert.True(bottom.BallVelocity.Y < 0);
        Assert.True(bottom.BallPosition.Y <= PongEngine.ArenaHeight - PongEngine.BallRadius);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void BallReflectsFromBothPaddles(bool opponentPaddle)
    {
        var paddleX = opponentPaddle
            ? PongEngine.ArenaWidth - 36 - PongEngine.PaddleWidth
            : 36;
        var velocity = opponentPaddle ? new Vector2(300, 0) : new Vector2(-300, 0);
        var ballX = opponentPaddle ? paddleX - PongEngine.BallRadius - 1 : paddleX + PongEngine.PaddleWidth + PongEngine.BallRadius + 1;
        var engine = CreatePlayingEngine(
            new Vector2((float)ballX, (float)(PongEngine.ArenaHeight / 2)),
            velocity,
            (PongEngine.ArenaHeight - PongEngine.PaddleHeight) / 2,
            (PongEngine.ArenaHeight - PongEngine.PaddleHeight) / 2);

        engine.Update(1d / 120);

        Assert.Equal(opponentPaddle ? -1 : 1, Math.Sign(engine.BallVelocity.X));
        Assert.Equal(1, engine.RallyHits);
        Assert.True(engine.BallSpeed > velocity.Length());
    }

    [Fact]
    public void PaddleImpactPositionChangesBallAngle()
    {
        var engine = CreatePlayingEngine(
            new Vector2(52, 220),
            new Vector2(-300, 0),
            212,
            200);

        engine.Update(1d / 120);

        Assert.True(engine.BallVelocity.Y < 0);
    }

    [Fact]
    public void BallExitingEitherSideAwardsPointAndStartsServe()
    {
        var playerScores = CreatePlayingEngine(
            new Vector2((float)(PongEngine.ArenaWidth - 20), 260),
            new Vector2(500, 0),
            0,
            0);
        playerScores.Update(0.1);

        Assert.Equal(1, playerScores.PlayerScore);
        Assert.Equal(0, playerScores.OpponentScore);
        Assert.Equal(PongGameState.Serving, playerScores.State);
        Assert.Equal(Vector2.Zero, playerScores.BallVelocity);
        Assert.Equal(new Vector2(450, 260), playerScores.BallPosition);

        var opponentScores = CreatePlayingEngine(
            new Vector2(20, 260),
            new Vector2(-500, 0),
            0,
            0);
        opponentScores.Update(0.1);

        Assert.Equal(0, opponentScores.PlayerScore);
        Assert.Equal(1, opponentScores.OpponentScore);
        Assert.Equal(PongGameState.Serving, opponentScores.State);
    }

    [Fact]
    public void ServeCountdownStartsBallAndMatchEndsAtFive()
    {
        var engine = new PongEngine(PongDifficulty.Normal, new FixedRandom());
        engine.StartNewMatch();
        Assert.Equal(PongGameState.Serving, engine.State);
        Assert.Equal(PongEngine.ServeDurationSeconds, engine.ServeRemainingSeconds);
        engine.Update(PongEngine.ServeDurationSeconds + 0.01);
        Assert.Equal(PongGameState.Playing, engine.State);
        Assert.NotEqual(Vector2.Zero, engine.BallVelocity);

        engine = CreatePlayingEngine(
            new Vector2((float)(PongEngine.ArenaWidth - 20), 260),
            new Vector2(500, 0),
            0,
            0,
            playerScore: PongEngine.WinningScore - 1);
        engine.Update(0.1);
        Assert.Equal(PongGameState.MatchOver, engine.State);
        Assert.Equal(PongEngine.WinningScore, engine.PlayerScore);
        var position = engine.BallPosition;
        engine.Update(0.1);
        Assert.Equal(position, engine.BallPosition);
    }

    [Fact]
    public void PauseAndResumePreserveGameStateAndFreezeBall()
    {
        var engine = CreatePlayingEngine(new Vector2(450, 260), new Vector2(100, 0), 200, 200);
        engine.Pause();
        var position = engine.BallPosition;
        engine.Update(1);
        Assert.Equal(PongGameState.Paused, engine.State);
        Assert.Equal(position, engine.BallPosition);

        engine.Resume();
        engine.Update(0.1);
        Assert.Equal(PongGameState.Playing, engine.State);
        Assert.NotEqual(position, engine.BallPosition);
    }

    [Theory]
    [InlineData(PongDifficulty.Easy)]
    [InlineData(PongDifficulty.Normal)]
    [InlineData(PongDifficulty.Hard)]
    public void AiSpeedIsBoundedByConfiguredMaximum(PongDifficulty difficulty)
    {
        var engine = CreatePlayingEngine(
            new Vector2(600, 50),
            new Vector2(400, -80),
            200,
            100,
            difficulty);
        var startingY = engine.OpponentPaddleY;

        engine.Update(0.2);

        Assert.True(Math.Abs(engine.OpponentPaddleY - startingY) <=
            engine.AiPaddleSpeed * 0.2 + 0.01);
    }

    [Fact]
    public void DifficultySettingsTradeReactionAndAccuracy()
    {
        var easy = PongDifficultySettings.For(PongDifficulty.Easy);
        var normal = PongDifficultySettings.For(PongDifficulty.Normal);
        var hard = PongDifficultySettings.For(PongDifficulty.Hard);

        Assert.True(easy.AiReactionDelaySeconds > normal.AiReactionDelaySeconds);
        Assert.True(normal.AiReactionDelaySeconds > hard.AiReactionDelaySeconds);
        Assert.True(easy.AiPositioningError > normal.AiPositioningError);
        Assert.True(normal.AiPositioningError > hard.AiPositioningError);
        Assert.True(easy.AiPaddleSpeed < normal.AiPaddleSpeed);
        Assert.True(normal.AiPaddleSpeed < hard.AiPaddleSpeed);
    }

    [Fact]
    public void RallySpeedIncreasesButNeverExceedsConfiguredMaximum()
    {
        var maximumSpeed = PongDifficultySettings.For(PongDifficulty.Normal).MaximumBallSpeed;
        var engine = CreatePlayingEngine(
            new Vector2(52, 260),
            new Vector2(-700, 0),
            212,
            212);

        engine.Update(1d / 120);

        Assert.Equal(maximumSpeed, engine.BallSpeed);
    }

    [Fact]
    public void NewMatchResetsScoreAndServesAgain()
    {
        var engine = CreatePlayingEngine(
            new Vector2((float)(PongEngine.ArenaWidth - 20), 260),
            new Vector2(500, 0),
            0,
            2);
        engine.StartNewMatch();

        Assert.Equal(0, engine.PlayerScore);
        Assert.Equal(0, engine.OpponentScore);
        Assert.Equal(PongGameState.Serving, engine.State);
        Assert.Equal(0, engine.RallyHits);
    }

    private static PongEngine CreatePlayingEngine(
        Vector2 ballPosition,
        Vector2 ballVelocity,
        double playerPaddleY,
        double opponentPaddleY,
        PongDifficulty difficulty = PongDifficulty.Normal,
        int playerScore = 0,
        int opponentScore = 0) =>
        new(
            difficulty,
            new FixedRandom(),
            ballPosition,
            ballVelocity,
            playerPaddleY,
            opponentPaddleY,
            playerScore,
            opponentScore);

    private sealed class FixedRandom : Random
    {
        public override int Next(int maxValue) => 0;
        public override double NextDouble() => 0.5;
    }
}
