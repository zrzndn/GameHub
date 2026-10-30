namespace WindowsITToolkit;

public readonly record struct GridPosition(int X, int Y);

public enum GameTickResult
{
    Moved,
    AteFood,
    GameOver
}

public readonly record struct DifficultySettings(
    int StartingIntervalMilliseconds,
    int MinimumIntervalMilliseconds,
    int SpeedUpPerFoodMilliseconds)
{
    public static DifficultySettings For(GameDifficulty difficulty) =>
        difficulty switch
        {
            GameDifficulty.Easy => new(180, 75, 4),
            GameDifficulty.Normal => new(130, 60, 4),
            GameDifficulty.Hard => new(90, 50, 3),
            _ => throw new ArgumentOutOfRangeException(nameof(difficulty))
        };
}

public sealed class GameEngine
{
    public const int BoardColumns = 25;
    public const int BoardRows = 25;
    public const int PointsPerFood = 10;

    private readonly Random _random;
    private readonly List<GridPosition> _snake = new();
    private Direction _direction;
    private Direction? _queuedDirection;

    public GameEngine(Random? random = null)
    {
        _random = random ?? Random.Shared;
    }

    public IReadOnlyList<GridPosition> Snake => _snake;
    public GridPosition Food { get; private set; }
    public int Score { get; private set; }
    public bool IsGameOver { get; private set; }
    public Direction CurrentDirection => _direction;

    public void StartNewGame()
    {
        _snake.Clear();
        _snake.Add(new GridPosition(BoardColumns / 2, BoardRows / 2));
        _snake.Add(new GridPosition(BoardColumns / 2 - 1, BoardRows / 2));
        _snake.Add(new GridPosition(BoardColumns / 2 - 2, BoardRows / 2));
        _direction = Direction.Right;
        _queuedDirection = null;
        Score = 0;
        IsGameOver = false;
        Food = ChooseFoodPosition();
    }

    public bool TryChangeDirection(Direction direction)
    {
        if (IsGameOver || _queuedDirection.HasValue ||
            direction == _direction || direction == OppositeOf(_direction))
        {
            return false;
        }

        _queuedDirection = direction;
        return true;
    }

    public GameTickResult Tick()
    {
        if (IsGameOver)
        {
            return GameTickResult.GameOver;
        }

        if (_queuedDirection.HasValue)
        {
            _direction = _queuedDirection.Value;
            _queuedDirection = null;
        }

        var head = _snake[0];
        var nextHead = _direction switch
        {
            Direction.Up => new GridPosition(head.X, head.Y - 1),
            Direction.Down => new GridPosition(head.X, head.Y + 1),
            Direction.Left => new GridPosition(head.X - 1, head.Y),
            Direction.Right => new GridPosition(head.X + 1, head.Y),
            _ => throw new ArgumentOutOfRangeException()
        };
        var eatingFood = nextHead == Food;

        if (nextHead.X < 0 || nextHead.X >= BoardColumns ||
            nextHead.Y < 0 || nextHead.Y >= BoardRows ||
            HitsSnake(nextHead, eatingFood))
        {
            IsGameOver = true;
            return GameTickResult.GameOver;
        }

        _snake.Insert(0, nextHead);
        if (eatingFood)
        {
            Score += PointsPerFood;
            Food = ChooseFoodPosition();
            return GameTickResult.AteFood;
        }

        _snake.RemoveAt(_snake.Count - 1);
        return GameTickResult.Moved;
    }

    public static int GetMovementIntervalMilliseconds(GameDifficulty difficulty, int score)
    {
        var settings = DifficultySettings.For(difficulty);
        var foodCount = Math.Max(0, score) / PointsPerFood;
        var speedIncrease = (long)foodCount * settings.SpeedUpPerFoodMilliseconds;
        return (int)Math.Max(
            settings.MinimumIntervalMilliseconds,
            settings.StartingIntervalMilliseconds - speedIncrease);
    }

    private bool HitsSnake(GridPosition position, bool eatingFood)
    {
        var bodyLengthToCheck = eatingFood ? _snake.Count : _snake.Count - 1;
        for (var i = 0; i < bodyLengthToCheck; i++)
        {
            if (_snake[i] == position)
            {
                return true;
            }
        }

        return false;
    }

    private GridPosition ChooseFoodPosition()
    {
        var availablePositions = new List<GridPosition>(
            BoardColumns * BoardRows - _snake.Count);

        for (var y = 0; y < BoardRows; y++)
        {
            for (var x = 0; x < BoardColumns; x++)
            {
                var position = new GridPosition(x, y);
                if (!_snake.Contains(position))
                {
                    availablePositions.Add(position);
                }
            }
        }

        if (availablePositions.Count == 0)
        {
            IsGameOver = true;
            return default;
        }

        return availablePositions[_random.Next(availablePositions.Count)];
    }

    private static Direction OppositeOf(Direction direction) =>
        direction switch
        {
            Direction.Up => Direction.Down,
            Direction.Down => Direction.Up,
            Direction.Left => Direction.Right,
            Direction.Right => Direction.Left,
            _ => throw new ArgumentOutOfRangeException(nameof(direction))
        };
}
