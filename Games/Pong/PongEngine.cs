using System.Numerics;

namespace WindowsITToolkit.Games.Pong;

public sealed class PongEngine
{
    public const double ArenaWidth = 900;
    public const double ArenaHeight = 520;
    public const double PaddleWidth = 14;
    public const double PaddleHeight = 96;
    public const double BallRadius = 9;
    public const int WinningScore = 5;
    public const double ServeDurationSeconds = 2;
    private const double AiErrorRefreshSeconds = 0.9;

    private readonly Random _random;
    private readonly PongDifficultySettings _settings;
    private readonly double _playerPaddleX = 36;
    private readonly double _opponentPaddleX = ArenaWidth - 36 - PaddleWidth;
    private double _serveDirection;
    private double _playerInput;
    private double _aiThinkTime;
    private double _aiTargetY;
    private double _serveRemaining;
    private double _ballSpeed;
    private PongGameState _stateBeforePause;

    public PongEngine(PongDifficulty difficulty = PongDifficulty.Normal, Random? random = null)
    {
        Difficulty = difficulty;
        _settings = PongDifficultySettings.For(difficulty);
        _random = random ?? Random.Shared;
        _serveDirection = _random.Next(2) == 0 ? -1 : 1;
        PlayerPaddleY = (ArenaHeight - PaddleHeight) / 2;
        OpponentPaddleY = PlayerPaddleY;
        _aiTargetY = ArenaHeight / 2;
        BallPosition = new Vector2((float)(ArenaWidth / 2), (float)(ArenaHeight / 2));
        BallVelocity = Vector2.Zero;
    }

    internal PongEngine(
        PongDifficulty difficulty,
        Random random,
        Vector2 ballPosition,
        Vector2 ballVelocity,
        double playerPaddleY,
        double opponentPaddleY,
        int playerScore = 0,
        int opponentScore = 0)
        : this(difficulty, random)
    {
        BallPosition = ballPosition;
        BallVelocity = ballVelocity;
        PlayerPaddleY = ClampPaddle(playerPaddleY);
        OpponentPaddleY = ClampPaddle(opponentPaddleY);
        PlayerScore = playerScore;
        OpponentScore = opponentScore;
        RallyHits = 0;
        State = PongGameState.Playing;
        _ballSpeed = ballVelocity.Length();
    }

    public PongDifficulty Difficulty { get; }
    public PongGameState State { get; private set; } = PongGameState.Ready;
    public Vector2 BallPosition { get; private set; }
    public Vector2 BallVelocity { get; private set; }
    public double PlayerPaddleY { get; private set; }
    public double OpponentPaddleY { get; private set; }
    public int PlayerScore { get; private set; }
    public int OpponentScore { get; private set; }
    public int RallyHits { get; private set; }
    public int BestRallyHits { get; private set; }
    public double ServeRemainingSeconds => _serveRemaining;
    public double AiPaddleSpeed => _settings.AiPaddleSpeed;
    public double AiReactionDelaySeconds => _settings.AiReactionDelaySeconds;
    public double AiPositioningError => _settings.AiPositioningError;
    public double PlayerPaddleSpeed => _settings.PlayerPaddleSpeed;
    public double BallSpeed => _ballSpeed;

    public void StartNewMatch()
    {
        PlayerScore = 0;
        OpponentScore = 0;
        RallyHits = 0;
        BestRallyHits = 0;
        _playerInput = 0;
        BeginServe(_serveDirection);
    }

    public void SetPlayerMovement(double direction)
    {
        _playerInput = Math.Clamp(direction, -1, 1);
    }

    public void Pause()
    {
        if (State is PongGameState.Playing or PongGameState.Serving or PongGameState.Ready)
        {
            _stateBeforePause = State;
            State = PongGameState.Paused;
        }
    }

    public void Resume()
    {
        if (State == PongGameState.Paused)
        {
            State = _stateBeforePause;
        }
    }

    public void Update(double elapsedSeconds)
    {
        if (!double.IsFinite(elapsedSeconds) || elapsedSeconds <= 0 ||
            State is PongGameState.Paused or PongGameState.MatchOver)
        {
            return;
        }

        var remaining = Math.Min(elapsedSeconds, 5);
        while (remaining > 0)
        {
            var step = Math.Min(remaining, 1d / 120);
            UpdateStep(step);
            remaining -= step;
        }
    }

    private void UpdateStep(double deltaTime)
    {
        MovePlayerPaddle(deltaTime);
        UpdateAi(deltaTime);

        if (State == PongGameState.Ready)
        {
            return;
        }

        if (State == PongGameState.Serving)
        {
            _serveRemaining -= deltaTime;
            if (_serveRemaining <= 0)
            {
                State = PongGameState.Playing;
                BallVelocity = new Vector2((float)(_serveDirection * _ballSpeed), 0);
            }

            return;
        }

        if (State != PongGameState.Playing)
        {
            return;
        }

        BallPosition += BallVelocity * (float)deltaTime;
        var y = (double)BallPosition.Y;
        if (y - BallRadius < 0)
        {
            y = BallRadius;
            BallVelocity = new Vector2(BallVelocity.X, Math.Abs(BallVelocity.Y));
        }
        else if (y + BallRadius > ArenaHeight)
        {
            y = ArenaHeight - BallRadius;
            BallVelocity = new Vector2(BallVelocity.X, -Math.Abs(BallVelocity.Y));
        }

        BallPosition = new Vector2(BallPosition.X, (float)y);
        CheckPaddleCollision();
        CheckScore();
    }

    private void MovePlayerPaddle(double deltaTime) =>
        PlayerPaddleY = ClampPaddle(PlayerPaddleY + _playerInput * _settings.PlayerPaddleSpeed * deltaTime);

    private void UpdateAi(double deltaTime)
    {
        _aiThinkTime += deltaTime;
        if (_aiThinkTime >= _settings.AiReactionDelaySeconds)
        {
            _aiThinkTime = 0;
            var projectedY = BallVelocity.X > 0
                ? ProjectBallYAtOpponent()
                : ArenaHeight / 2;
            _aiTargetY = projectedY + RandomOffset(_settings.AiPositioningError);
        }

        var targetPaddleY = Math.Clamp(
            _aiTargetY - PaddleHeight / 2,
            0,
            ArenaHeight - PaddleHeight);
        OpponentPaddleY = MoveTowards(
            OpponentPaddleY,
            targetPaddleY,
            _settings.AiPaddleSpeed * deltaTime);
    }

    private double ProjectBallYAtOpponent()
    {
        if (BallVelocity.X <= 0)
        {
            return BallPosition.Y;
        }

        var seconds = (_opponentPaddleX - BallPosition.X) / BallVelocity.X;
        var projected = BallPosition.Y + BallVelocity.Y * seconds;
        var reflected = Math.Abs(projected % (ArenaHeight * 2));
        return reflected > ArenaHeight ? ArenaHeight * 2 - reflected : reflected;
    }

    private void CheckPaddleCollision()
    {
        var ballX = (double)BallPosition.X;
        var ballY = (double)BallPosition.Y;
        if (BallVelocity.X < 0 &&
            ballX - BallRadius <= _playerPaddleX + PaddleWidth &&
            ballX + BallRadius >= _playerPaddleX &&
            ballY + BallRadius >= PlayerPaddleY &&
            ballY - BallRadius <= PlayerPaddleY + PaddleHeight)
        {
            BounceFromPaddle(_playerPaddleX + PaddleWidth, PlayerPaddleY);
        }
        else if (BallVelocity.X > 0 &&
                 ballX + BallRadius >= _opponentPaddleX &&
                 ballX - BallRadius <= _opponentPaddleX + PaddleWidth &&
                 ballY + BallRadius >= OpponentPaddleY &&
                 ballY - BallRadius <= OpponentPaddleY + PaddleHeight)
        {
            BounceFromPaddle(_opponentPaddleX, OpponentPaddleY);
        }
    }

    private void BounceFromPaddle(double paddleFaceX, double paddleY)
    {
        var offset = Math.Clamp(
            (BallPosition.Y - (paddleY + PaddleHeight / 2)) / (PaddleHeight / 2),
            -1,
            1);
        _ballSpeed = Math.Min(_settings.MaximumBallSpeed, _ballSpeed + _settings.RallySpeedIncrease);
        var angle = offset * Math.PI / 3.2;
        var directionX = paddleFaceX < ArenaWidth / 2 ? 1 : -1;
        BallPosition = new Vector2(
            (float)(paddleFaceX + directionX * (BallRadius + 0.5)),
            BallPosition.Y);
        BallVelocity = new Vector2(
            (float)(Math.Cos(angle) * directionX * _ballSpeed),
            (float)(Math.Sin(angle) * _ballSpeed));
        RallyHits++;
        BestRallyHits = Math.Max(BestRallyHits, RallyHits);
        _aiTargetY += RandomOffset(_settings.AiPositioningError);
    }

    private void CheckScore()
    {
        if (BallPosition.X + BallRadius < 0)
        {
            OpponentScore++;
            PointScored(_serveDirection);
        }
        else if (BallPosition.X - BallRadius > ArenaWidth)
        {
            PlayerScore++;
            PointScored(-_serveDirection);
        }
    }

    private void PointScored(double nextServeDirection)
    {
        RallyHits = 0;
        if (PlayerScore >= WinningScore || OpponentScore >= WinningScore)
        {
            State = PongGameState.MatchOver;
            BallVelocity = Vector2.Zero;
            _playerInput = 0;
            return;
        }

        BeginServe(nextServeDirection);
    }

    private void BeginServe(double direction)
    {
        _serveRemaining = ServeDurationSeconds;
        _ballSpeed = _settings.InitialBallSpeed;
        BallPosition = new Vector2((float)(ArenaWidth / 2), (float)(ArenaHeight / 2));
        BallVelocity = Vector2.Zero;
        PlayerPaddleY = (ArenaHeight - PaddleHeight) / 2;
        OpponentPaddleY = PlayerPaddleY;
        _aiTargetY = ArenaHeight / 2;
        _aiThinkTime = 0;
        _serveDirection = direction;
        State = PongGameState.Serving;
    }

    private double RandomOffset(double maximum) =>
        (_random.NextDouble() * 2 - 1) * maximum;

    private static double ClampPaddle(double position) =>
        Math.Clamp(position, 0, ArenaHeight - PaddleHeight);

    private static double MoveTowards(double current, double target, double maximumDelta) =>
        Math.Abs(target - current) <= maximumDelta
            ? target
            : current + Math.Sign(target - current) * maximumDelta;
}
