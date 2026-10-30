namespace WindowsITToolkit.Games.MemoryMatch;

public sealed class MemoryCard
{
    internal MemoryCard(int index, int pairId)
    {
        Index = index;
        PairId = pairId;
    }

    public int Index { get; }
    public int PairId { get; }
    public bool IsRevealed { get; internal set; }
    public bool IsMatched { get; internal set; }
}
