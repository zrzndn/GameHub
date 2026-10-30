namespace WindowsITToolkit.Games.Pong;

public enum PongDifficulty
{
    Easy,
    Normal,
    Hard
}

public readonly record struct PongDifficultySettings(
    double PlayerPaddleSpeed,
    double AiPaddleSpeed,
    double AiReactionDelaySeconds,
    double AiPositioningError,
    double InitialBallSpeed,
    double RallySpeedIncrease,
    double MaximumBallSpeed)
{
    public static PongDifficultySettings For(PongDifficulty difficulty) =>
        difficulty switch
        {
            PongDifficulty.Easy => new(500, 310, 0.34, 86, 330, 12, 540),
            PongDifficulty.Normal => new(500, 430, 0.22, 52, 390, 14, 650),
            PongDifficulty.Hard => new(500, 540, 0.14, 28, 440, 16, 740),
            _ => throw new ArgumentOutOfRangeException(nameof(difficulty))
        };
}
