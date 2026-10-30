namespace WindowsITToolkit;

public enum GameAvailability
{
    ComingSoon,
    Available
}

public sealed record GameCatalogItem(
    string GameId,
    string DisplayName,
    string Description,
    string Category,
    string? ArtworkPath,
    GameAvailability Availability);

public static class GameCatalog
{
    public static IReadOnlyList<GameCatalogItem> AllGames { get; } =
    [
        new(
            "snake",
            "Snake",
            "Guide the growing snake, collect food, and chase a new high score.",
            "Arcade",
            null,
            GameAvailability.Available),
        new(
            "tic-tac-toe",
            "Tic-Tac-Toe",
            "Outthink a friend and claim three spaces in a row.",
            "Strategy",
            null,
            GameAvailability.Available),
        new(
            "minesweeper",
            "Minesweeper",
            "Use the clues to uncover a safe path through the minefield.",
            "Puzzle",
            null,
            GameAvailability.Available),
        new(
            "2048",
            "2048",
            "Slide and combine matching tiles to reach the target number.",
            "Puzzle",
            null,
            GameAvailability.Available),
        new(
            "pong",
            "Pong",
            "Return the ball, outplay your opponent, and score the point.",
            "Arcade",
            null,
            GameAvailability.Available),
        new(
            "memory-match",
            "Memory Match",
            "Flip the cards, find matching pairs, and test your memory.",
            "Puzzle",
            null,
            GameAvailability.Available)
    ];
}
