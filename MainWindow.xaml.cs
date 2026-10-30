using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.ComponentModel;
using WindowsITToolkit.Games.Snake;
using WindowsITToolkit.Games.TicTacToe;
using WindowsITToolkit.Games.TwentyFortyEight;
using WindowsITToolkit.Games.Minesweeper;
using WindowsITToolkit.Games.MemoryMatch;
using WindowsITToolkit.Games.Pong;

namespace WindowsITToolkit;

public partial class MainWindow : Window
{
    private readonly GameLibraryView _gameLibraryView;
    private readonly HubSettingsService _settingsService = new();
    private HubSettings _settings;
    private string _currentSection = "library";
    private SnakeGameView? _activeSnakeGame;
    private TwentyFortyEightGameView? _activeTwentyFortyEightGame;
    private MinesweeperGameView? _activeMinesweeperGame;
    private MemoryMatchGameView? _activeMemoryMatchGame;
    private PongGameView? _activePongGame;

    public MainWindow()
    {
        InitializeComponent();

        var settingsLoad = _settingsService.Load();
        _settings = settingsLoad.Settings;
        Width = _settings.WindowWidth;
        Height = _settings.WindowHeight;
        _gameLibraryView = new GameLibraryView(_settings);
        _gameLibraryView.PlayRequested += GameLibraryView_PlayRequested;
        _gameLibraryView.FavoriteToggled += GameLibraryView_FavoriteToggled;
        ShowLibrary();
        if (settingsLoad.Warning is not null)
        {
            Loaded += (_, _) => MessageBox.Show(this, settingsLoad.Warning, "Windows Arcade Hub", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    public void NavigateTo(UIElement content)
    {
        ArgumentNullException.ThrowIfNull(content);
        ContentHost.Content = content;
    }

    public void ShowLibrary()
    {
        DeactivateActiveGame();
        _currentSection = "library";
        NavigateTo(_gameLibraryView);
        UpdateNavigationState();
    }

    private void GameLibraryView_PlayRequested(GameCatalogItem game)
    {
        LaunchGame(game);
    }

    private void LaunchGame(GameCatalogItem game)
    {
        if (game.Availability != GameAvailability.Available)
        {
            return;
        }

        _settings = _settings with
        {
            RecentlyPlayedGameIds = [game.GameId, .. _settings.RecentlyPlayedGameIds
                .Where(id => !string.Equals(id, game.GameId, StringComparison.OrdinalIgnoreCase))
                .Take(9)],
            GameLaunchCounts = new Dictionary<string, int>(_settings.GameLaunchCounts, StringComparer.OrdinalIgnoreCase)
            {
                [game.GameId] = _settings.GameLaunchCounts.GetValueOrDefault(game.GameId) < int.MaxValue
                    ? _settings.GameLaunchCounts.GetValueOrDefault(game.GameId) + 1
                    : int.MaxValue
            }
        };
        SaveSettings();
        _gameLibraryView.UpdateSettings(_settings);
        DeactivateActiveGame();
        UIElement? gameView = game.GameId switch
        {
            "snake" => _activeSnakeGame = new SnakeGameView(ShowLibrary),
            "tic-tac-toe" => new TicTacToeGameView(ShowLibrary),
            "2048" => _activeTwentyFortyEightGame = new TwentyFortyEightGameView(ShowLibrary),
            "minesweeper" => _activeMinesweeperGame = new MinesweeperGameView(ShowLibrary),
            "memory-match" => _activeMemoryMatchGame = new MemoryMatchGameView(ShowLibrary),
            "pong" => _activePongGame = new PongGameView(ShowLibrary),
            _ => null
        };
        if (gameView is not null)
        {
            _currentSection = "library";
            UpdateNavigationState();
            NavigateTo(gameView);
        }
    }

    private void GameLibraryView_FavoriteToggled(string gameId)
    {
        var favorites = new List<string>(_settings.FavoriteGameIds);
        var existingId = favorites.FirstOrDefault(id => string.Equals(id, gameId, StringComparison.OrdinalIgnoreCase));
        if (existingId is null)
        {
            favorites.Add(gameId);
        }
        else
        {
            favorites.Remove(existingId);
        }

        _settings = _settings with { FavoriteGameIds = favorites };
        SaveSettings();
        _gameLibraryView.UpdateSettings(_settings);
    }

    private void NavigationButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string section })
        {
            NavigateToSection(section);
        }
    }

    private void NavigateToSection(string section)
    {
        DeactivateActiveGame();
        _currentSection = section;
        if (section == "library")
        {
            NavigateTo(_gameLibraryView);
        }
        else
        {
            NavigateTo(new HubSectionView(section, _settings, UpdateSettings, LaunchGame));
        }

        UpdateNavigationState();
    }

    private void UpdateSettings(HubSettings settings)
    {
        _settings = settings;
        SaveSettings();
        _gameLibraryView.UpdateSettings(_settings);
    }

    private void SaveSettings()
    {
        var result = _settingsService.Save(_settings);
        if (!result.Succeeded || result.Warning is not null)
        {
            MessageBox.Show(
                this,
                result.Warning ?? "Your settings could not be saved.",
                "Windows Arcade Hub",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void UpdateNavigationState()
    {
        foreach (var button in NavigationPanel.Children.OfType<Button>())
        {
            var isSelected = string.Equals(button.Tag?.ToString(), _currentSection, StringComparison.OrdinalIgnoreCase);
            button.Background = isSelected
                ? (Brush)FindResource("HubHoverBrush")
                : (Brush)FindResource("HubCardBrush");
            button.Foreground = isSelected
                ? (Brush)FindResource("HubAccentBrush")
                : (Brush)FindResource("HubTextBrush");
            button.BorderBrush = isSelected
                ? (Brush)FindResource("HubAccentBrush")
                : (Brush)FindResource("HubBorderBrush");
        }
    }

    private void DeactivateActiveGame()
    {
        _activeSnakeGame?.Deactivate();
        _activeSnakeGame = null;
        _activeTwentyFortyEightGame?.Deactivate();
        _activeTwentyFortyEightGame = null;
        _activeMinesweeperGame?.Deactivate();
        _activeMinesweeperGame = null;
        _activeMemoryMatchGame?.Deactivate();
        _activeMemoryMatchGame = null;
        _activePongGame?.Deactivate();
        _activePongGame = null;
    }

    protected override void OnClosed(EventArgs e)
    {
        _gameLibraryView.PlayRequested -= GameLibraryView_PlayRequested;
        _gameLibraryView.FavoriteToggled -= GameLibraryView_FavoriteToggled;
        DeactivateActiveGame();
        base.OnClosed(e);
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (_settings.ConfirmExit &&
            MessageBox.Show(this, "Exit Windows Arcade Hub?", "Confirm exit", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            e.Cancel = true;
            return;
        }

        _settings = _settings with { WindowWidth = Width, WindowHeight = Height };
        SaveSettings();
        base.OnClosing(e);
    }
}
