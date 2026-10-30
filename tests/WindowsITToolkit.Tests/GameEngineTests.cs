using Xunit;

namespace WindowsITToolkit.Tests;

public sealed class GameEngineTests
{
    [Fact]
    public void StartNewGameCreatesCenteredSnakeAndValidFood()
    {
        var engine = new GameEngine(new Random(5));

        engine.StartNewGame();

        Assert.Equal(3, engine.Snake.Count);
        Assert.Equal(new GridPosition(12, 12), engine.Snake[0]);
        Assert.Equal(Direction.Right, engine.CurrentDirection);
        Assert.InRange(engine.Food.X, 0, GameEngine.BoardColumns - 1);
        Assert.InRange(engine.Food.Y, 0, GameEngine.BoardRows - 1);
        Assert.DoesNotContain(engine.Food, engine.Snake);
    }

    [Fact]
    public void DirectionChangesCannotReverseOrQueueMultipleTurns()
    {
        var engine = new GameEngine(new Random(6));
        engine.StartNewGame();

        Assert.False(engine.TryChangeDirection(Direction.Left));
        Assert.True(engine.TryChangeDirection(Direction.Up));
        Assert.False(engine.TryChangeDirection(Direction.Down));
        Assert.Equal(GameTickResult.Moved, engine.Tick());
        Assert.Equal(Direction.Up, engine.CurrentDirection);
        Assert.Equal(new GridPosition(12, 11), engine.Snake[0]);
    }

    [Fact]
    public void EatingFoodAwardsPointsAndGrowsSnake()
    {
        var engine = CreateEngineWithFood(new GridPosition(13, 12));
        var originalLength = engine.Snake.Count;

        var result = engine.Tick();

        Assert.Equal(GameTickResult.AteFood, result);
        Assert.Equal(GameEngine.PointsPerFood, engine.Score);
        Assert.Equal(originalLength + 1, engine.Snake.Count);
        Assert.DoesNotContain(engine.Food, engine.Snake);
    }

    [Fact]
    public void MovingIntoVacatingTailCellDoesNotCauseCollision()
    {
        var engine = CreateEngineWithFood(new GridPosition(13, 12), new[]
        {
            new GridPosition(14, 12),
            new GridPosition(14, 11),
            new GridPosition(13, 11)
        });

        Assert.Equal(GameTickResult.AteFood, engine.Tick());
        Assert.Equal(GameTickResult.Moved, engine.Tick());
        Assert.True(engine.TryChangeDirection(Direction.Up));
        Assert.Equal(GameTickResult.Moved, engine.Tick());
        Assert.True(engine.TryChangeDirection(Direction.Left));
        Assert.Equal(GameTickResult.Moved, engine.Tick());
        Assert.True(engine.TryChangeDirection(Direction.Down));

        var tailPosition = engine.Snake[^1];
        Assert.Equal(tailPosition, new GridPosition(13, 12));
        Assert.Equal(GameTickResult.Moved, engine.Tick());
        Assert.False(engine.IsGameOver);
        Assert.Equal(new GridPosition(13, 12), engine.Snake[0]);
    }

    [Fact]
    public void HittingSnakeBodyEndsGame()
    {
        var engine = CreateEngineWithFoodSequence(
            new GridPosition(13, 12),
            new GridPosition(14, 12),
            new[]
        {
            new GridPosition(14, 11),
            new GridPosition(13, 11)
        });

        Assert.Equal(GameTickResult.AteFood, engine.Tick());
        Assert.Equal(GameTickResult.AteFood, engine.Tick());
        Assert.True(engine.TryChangeDirection(Direction.Up));
        Assert.Equal(GameTickResult.Moved, engine.Tick());
        Assert.True(engine.TryChangeDirection(Direction.Left));
        Assert.Equal(GameTickResult.Moved, engine.Tick());
        Assert.True(engine.TryChangeDirection(Direction.Down));

        Assert.Equal(GameTickResult.GameOver, engine.Tick());
        Assert.True(engine.IsGameOver);
        Assert.Equal(GameEngine.PointsPerFood * 2, engine.Score);
    }

    [Fact]
    public void HittingWallEndsGame()
    {
        var engine = new GameEngine(new Random(7));
        engine.StartNewGame();

        for (var i = 0; i < 12; i++)
        {
            Assert.Equal(GameTickResult.Moved, engine.Tick());
        }

        Assert.Equal(GameTickResult.GameOver, engine.Tick());
        Assert.True(engine.IsGameOver);
    }

    [Fact]
    public void RestartResetsSnakeScoreAndDirection()
    {
        var engine = CreateEngineWithFood(new GridPosition(13, 12));
        Assert.Equal(GameTickResult.AteFood, engine.Tick());
        Assert.True(engine.TryChangeDirection(Direction.Up));
        engine.Tick();

        engine.StartNewGame();

        Assert.Equal(0, engine.Score);
        Assert.False(engine.IsGameOver);
        Assert.Equal(3, engine.Snake.Count);
        Assert.Equal(new GridPosition(12, 12), engine.Snake[0]);
        Assert.Equal(Direction.Right, engine.CurrentDirection);
        Assert.DoesNotContain(engine.Food, engine.Snake);
    }

    [Theory]
    [InlineData(GameDifficulty.Easy, 180, 75)]
    [InlineData(GameDifficulty.Normal, 130, 60)]
    [InlineData(GameDifficulty.Hard, 90, 50)]
    public void DifficultyControlsStartingAndMinimumSpeed(
        GameDifficulty difficulty,
        int startingInterval,
        int minimumInterval)
    {
        Assert.Equal(startingInterval, GameEngine.GetMovementIntervalMilliseconds(difficulty, 0));
        Assert.Equal(
            minimumInterval,
            GameEngine.GetMovementIntervalMilliseconds(difficulty, int.MaxValue));
    }

    private static GameEngine CreateEngineWithFood(
        GridPosition firstFood,
        IReadOnlyCollection<GridPosition>? avoidAfterEating = null)
    {
        for (var seed = 0; seed < 100_000; seed++)
        {
            var probe = new Random(seed);
            if (probe.Next(GameEngine.BoardColumns * GameEngine.BoardRows - 3) !=
                GetInitialFoodIndex(firstFood))
            {
                continue;
            }

            var engine = new GameEngine(new Random(seed));
            engine.StartNewGame();
            if (avoidAfterEating is null)
            {
                return engine;
            }

            if (engine.Tick() != GameTickResult.AteFood ||
                avoidAfterEating.Contains(engine.Food))
            {
                continue;
            }

            var candidate = new GameEngine(new Random(seed));
            candidate.StartNewGame();
            return candidate;
        }

        throw new InvalidOperationException($"Could not find a deterministic seed for food at {firstFood}.");
    }

    private static GameEngine CreateEngineWithFoodSequence(
        GridPosition firstFood,
        GridPosition secondFood,
        IReadOnlyCollection<GridPosition> avoidAfterSecondFood)
    {
        for (var seed = 0; seed < 1_000_000; seed++)
        {
            var probe = new Random(seed);
            if (probe.Next(GameEngine.BoardColumns * GameEngine.BoardRows - 3) !=
                GetInitialFoodIndex(firstFood))
            {
                continue;
            }

            var candidate = new GameEngine(new Random(seed));
            candidate.StartNewGame();
            if (candidate.Tick() != GameTickResult.AteFood ||
                candidate.Food != secondFood ||
                candidate.Tick() != GameTickResult.AteFood ||
                avoidAfterSecondFood.Contains(candidate.Food))
            {
                continue;
            }

            var engine = new GameEngine(new Random(seed));
            engine.StartNewGame();
            return engine;
        }

        throw new InvalidOperationException(
            $"Could not find a deterministic seed for food at {firstFood}, then {secondFood}.");
    }

    private static int GetInitialFoodIndex(GridPosition position)
    {
        var availableIndex = 0;
        for (var y = 0; y < GameEngine.BoardRows; y++)
        {
            for (var x = 0; x < GameEngine.BoardColumns; x++)
            {
                var cell = new GridPosition(x, y);
                if (cell is { X: 12, Y: 12 } or { X: 11, Y: 12 } or { X: 10, Y: 12 })
                {
                    continue;
                }

                if (cell == position)
                {
                    return availableIndex;
                }

                availableIndex++;
            }
        }

        throw new ArgumentOutOfRangeException(nameof(position));
    }
}
