namespace WindowsITToolkit.Games.MemoryMatch;

public enum MemoryMatchDifficulty
{
    Easy,
    Medium,
    Hard
}

public readonly record struct MemoryMatchSettings(int Columns, int Rows, int PairCount)
{
    public static MemoryMatchSettings For(MemoryMatchDifficulty difficulty) =>
        difficulty switch
        {
            MemoryMatchDifficulty.Easy => new(4, 4, 8),
            MemoryMatchDifficulty.Medium => new(6, 4, 12),
            MemoryMatchDifficulty.Hard => new(6, 6, 18),
            _ => throw new ArgumentOutOfRangeException(nameof(difficulty))
        };
}

public enum CardRevealResult
{
    Rejected,
    FirstCard,
    Matched,
    Mismatched,
    Completed
}

public sealed class MemoryMatchEngine
{
    private readonly Random _random;
    private MemoryCard[] _cards = [];
    private int? _firstSelected;
    private int? _secondSelected;

    public MemoryMatchEngine(MemoryMatchDifficulty difficulty, Random? random = null)
        : this(difficulty, random ?? Random.Shared, null)
    {
    }

    public MemoryMatchEngine(
        MemoryMatchDifficulty difficulty,
        Random random,
        IReadOnlyList<int>? pairLayout)
    {
        ArgumentNullException.ThrowIfNull(random);
        Difficulty = difficulty;
        Settings = MemoryMatchSettings.For(difficulty);
        _random = random;
        if (pairLayout is not null &&
            (pairLayout.Count != Settings.PairCount * 2 ||
             pairLayout.Any(pair => pair < 0 || pair >= Settings.PairCount) ||
             pairLayout.GroupBy(pair => pair).Any(group => group.Count() != 2)))
        {
            throw new ArgumentException("The card layout must contain exactly two cards per pair.", nameof(pairLayout));
        }

        StartNewGame(pairLayout);
    }

    public MemoryMatchDifficulty Difficulty { get; }
    public MemoryMatchSettings Settings { get; }
    public IReadOnlyList<MemoryCard> Cards => Array.AsReadOnly(_cards);
    public int Moves { get; private set; }
    public int MatchedPairCount { get; private set; }
    public bool IsWaitingForMismatch => _secondSelected.HasValue;
    public bool IsComplete => MatchedPairCount == Settings.PairCount;

    public CardRevealResult Reveal(int index)
    {
        if (index < 0 || index >= _cards.Length || IsWaitingForMismatch || IsComplete)
        {
            return CardRevealResult.Rejected;
        }

        var card = _cards[index];
        if (card.IsRevealed || card.IsMatched)
        {
            return CardRevealResult.Rejected;
        }

        card.IsRevealed = true;
        if (!_firstSelected.HasValue)
        {
            _firstSelected = index;
            return CardRevealResult.FirstCard;
        }

        _secondSelected = index;
        Moves++;
        var firstCard = _cards[_firstSelected.Value];
        if (firstCard.PairId == card.PairId)
        {
            firstCard.IsMatched = true;
            card.IsMatched = true;
            MatchedPairCount++;
            _firstSelected = null;
            _secondSelected = null;
            return IsComplete ? CardRevealResult.Completed : CardRevealResult.Matched;
        }

        return CardRevealResult.Mismatched;
    }

    public bool ResolveMismatch()
    {
        if (!_firstSelected.HasValue || !_secondSelected.HasValue)
        {
            return false;
        }

        _cards[_firstSelected.Value].IsRevealed = false;
        _cards[_secondSelected.Value].IsRevealed = false;
        _firstSelected = null;
        _secondSelected = null;
        return true;
    }

    public void StartNewGame(IReadOnlyList<int>? pairLayout = null)
    {
        _firstSelected = null;
        _secondSelected = null;
        Moves = 0;
        MatchedPairCount = 0;
        var pairs = pairLayout is null
            ? Enumerable.Range(0, Settings.PairCount).SelectMany(pair => new[] { pair, pair }).ToArray()
            : pairLayout.ToArray();
        if (pairs.Length != Settings.PairCount * 2 ||
            pairs.Any(pair => pair < 0 || pair >= Settings.PairCount) ||
            pairs.GroupBy(pair => pair).Any(group => group.Count() != 2))
        {
            throw new ArgumentException("The card layout must contain exactly two cards per pair.", nameof(pairLayout));
        }

        Shuffle(pairs);
        _cards = pairs.Select((pair, index) => new MemoryCard(index, pair)).ToArray();
    }

    private void Shuffle(int[] pairs)
    {
        for (var index = pairs.Length - 1; index > 0; index--)
        {
            var selected = _random.Next(index + 1);
            (pairs[index], pairs[selected]) = (pairs[selected], pairs[index]);
        }
    }
}
