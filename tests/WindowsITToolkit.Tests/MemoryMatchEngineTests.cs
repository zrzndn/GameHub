using WindowsITToolkit.Games.MemoryMatch;
using Xunit;

namespace WindowsITToolkit.Tests;

public sealed class MemoryMatchEngineTests
{
    [Theory]
    [InlineData(MemoryMatchDifficulty.Easy, 16, 8)]
    [InlineData(MemoryMatchDifficulty.Medium, 24, 12)]
    [InlineData(MemoryMatchDifficulty.Hard, 36, 18)]
    public void CreatesConfiguredPairs(MemoryMatchDifficulty difficulty, int cardCount, int pairCount)
    {
        var engine = new MemoryMatchEngine(difficulty, new FixedRandom());

        Assert.Equal(cardCount, engine.Cards.Count);
        Assert.Equal(pairCount, engine.Settings.PairCount);
        Assert.All(engine.Cards.GroupBy(card => card.PairId), group => Assert.Equal(2, group.Count()));
        Assert.All(engine.Cards, card => Assert.False(card.IsRevealed));
    }

    [Fact]
    public void MatchingCardsStayVisibleAndCountAsOneMoveAndPair()
    {
        var engine = NewEngine();
        var pair = FindPair(engine, 0);

        Assert.Equal(CardRevealResult.FirstCard, engine.Reveal(pair.First));
        Assert.Equal(CardRevealResult.Matched, engine.Reveal(pair.Second));

        Assert.True(engine.Cards[pair.First].IsMatched);
        Assert.True(engine.Cards[pair.Second].IsMatched);
        Assert.Equal(1, engine.MatchedPairCount);
        Assert.Equal(1, engine.Moves);
    }

    [Fact]
    public void RejectsInvalidAndMatchedSelections()
    {
        var engine = NewEngine();
        Assert.Equal(CardRevealResult.Rejected, engine.Reveal(-1));
        Assert.Equal(CardRevealResult.Rejected, engine.Reveal(16));
        var pair = FindPair(engine, 0);
        engine.Reveal(pair.First);
        engine.Reveal(pair.Second);

        Assert.Equal(CardRevealResult.Rejected, engine.Reveal(pair.First));
    }

    [Fact]
    public void MismatchBlocksAdditionalSelectionsUntilResolved()
    {
        var engine = NewEngine();
        var firstPair = FindPair(engine, 0);
        var secondPair = FindPair(engine, 1);
        engine.Reveal(firstPair.First);

        Assert.Equal(CardRevealResult.Mismatched, engine.Reveal(secondPair.First));
        Assert.True(engine.IsWaitingForMismatch);
        Assert.Equal(CardRevealResult.Rejected, engine.Reveal(firstPair.Second));
        Assert.True(engine.Cards[firstPair.First].IsRevealed);
        Assert.True(engine.Cards[secondPair.First].IsRevealed);

        Assert.True(engine.ResolveMismatch());
        Assert.False(engine.Cards[firstPair.First].IsRevealed);
        Assert.False(engine.Cards[secondPair.First].IsRevealed);
        Assert.False(engine.IsWaitingForMismatch);
        Assert.Equal(1, engine.Moves);
    }

    [Fact]
    public void DetectsCompletionWhenAllPairsAreMatched()
    {
        var engine = NewEngine();
        for (var pairId = 0; pairId < engine.Settings.PairCount; pairId++)
        {
            var pair = FindPair(engine, pairId);
            engine.Reveal(pair.First);
            var result = engine.Reveal(pair.Second);
            if (pairId == engine.Settings.PairCount - 1)
            {
                Assert.Equal(CardRevealResult.Completed, result);
            }
        }

        Assert.True(engine.IsComplete);
        Assert.Equal(engine.Settings.PairCount, engine.MatchedPairCount);
        Assert.Equal(engine.Settings.PairCount, engine.Moves);
        Assert.Equal(CardRevealResult.Rejected, engine.Reveal(0));
    }

    [Fact]
    public void RestartCancelsMismatchAndResetsBoardAndCounters()
    {
        var engine = NewEngine();
        var pair = FindPair(engine, 0);
        var otherPair = FindPair(engine, 1);
        engine.Reveal(pair.First);
        engine.Reveal(otherPair.First);
        Assert.True(engine.IsWaitingForMismatch);

        engine.StartNewGame();

        Assert.Equal(0, engine.Moves);
        Assert.Equal(0, engine.MatchedPairCount);
        Assert.All(engine.Cards, card => Assert.False(card.IsRevealed || card.IsMatched));
    }

    [Fact]
    public void MismatchedCardsCannotBeRevealedAgainBeforeResolution()
    {
        var engine = NewEngine();
        var firstPair = FindPair(engine, 0);
        var secondPair = FindPair(engine, 1);
        engine.Reveal(firstPair.First);
        engine.Reveal(secondPair.First);

        Assert.Equal(CardRevealResult.Rejected, engine.Reveal(secondPair.Second));
        Assert.True(engine.ResolveMismatch());
        Assert.Equal(CardRevealResult.FirstCard, engine.Reveal(secondPair.Second));
    }

    private static MemoryMatchEngine NewEngine() =>
        new(MemoryMatchDifficulty.Easy, new FixedRandom(), Enumerable.Range(0, 8).SelectMany(pair => new[] { pair, pair }).ToArray());

    private static (int First, int Second) FindPair(MemoryMatchEngine engine, int pairId)
    {
        var matches = engine.Cards.Where(card => card.PairId == pairId).Select(card => card.Index).ToArray();
        return (matches[0], matches[1]);
    }

    private sealed class FixedRandom : Random
    {
        public override int Next(int maxValue) => 0;
    }
}
