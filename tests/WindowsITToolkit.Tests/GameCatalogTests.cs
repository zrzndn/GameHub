using Xunit;

namespace WindowsITToolkit.Tests;

public sealed class GameCatalogTests
{
    [Fact]
    public void AllImplementedGamesAreAvailable()
    {
        Assert.All(GameCatalog.AllGames, game => Assert.Equal(GameAvailability.Available, game.Availability));
    }
}
