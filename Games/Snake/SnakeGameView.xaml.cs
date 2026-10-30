using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;

namespace WindowsITToolkit.Games.Snake;

public partial class SnakeGameView : UserControl
{
    private readonly GameEngine _gameEngine = new();
    private readonly HighScoreService _highScoreService = new();
    private readonly DispatcherTimer _gameTimer;
    private readonly Action _backToLibrary;
    private GameState _gameState = GameState.MainMenu;
    private GameDifficulty _difficulty = GameDifficulty.Normal;
    private int _highScore;
    private Window? _activeWindow;
    private bool _isActive;
    private bool _isDeactivated;
    private string? _startupWarning;

    public SnakeGameView(Action backToLibrary)
    {
        ArgumentNullException.ThrowIfNull(backToLibrary);
        InitializeComponent();
        _backToLibrary = backToLibrary;

        _gameTimer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(
                DifficultySettings.For(_difficulty).StartingIntervalMilliseconds)
        };
        _gameTimer.Tick += GameTimer_Tick;
        Loaded += SnakeGameView_Loaded;
        Unloaded += SnakeGameView_Unloaded;

        var highScoreResult = _highScoreService.Load();
        _highScore = highScoreResult.Score;
        _startupWarning = highScoreResult.Warning;
        UpdateScoreDisplays();
    }

    public void Deactivate()
    {
        if (_isDeactivated)
        {
            return;
        }

        _isDeactivated = true;
        _isActive = false;
        _gameTimer.Stop();
        if (_activeWindow is not null)
        {
            _activeWindow.PreviewKeyDown -= Window_PreviewKeyDown;
            _activeWindow.Deactivated -= Window_Deactivated;
            _activeWindow.Activated -= Window_Activated;
            _activeWindow = null;
        }

        Loaded -= SnakeGameView_Loaded;
        Unloaded -= SnakeGameView_Unloaded;
        _gameTimer.Tick -= GameTimer_Tick;
    }

    private void SnakeGameView_Loaded(object sender, RoutedEventArgs e)
    {
        if (_isActive || _isDeactivated)
        {
            return;
        }

        _isActive = true;
        _activeWindow = Window.GetWindow(this);
        if (_activeWindow is not null)
        {
            _activeWindow.PreviewKeyDown += Window_PreviewKeyDown;
            _activeWindow.Deactivated += Window_Deactivated;
            _activeWindow.Activated += Window_Activated;
        }

        Keyboard.Focus(this);
        StartNewGame();
        ShowFeedback(_startupWarning);
        _startupWarning = null;
    }

    private void SnakeGameView_Unloaded(object sender, RoutedEventArgs e)
    {
        Deactivate();
    }

    private void PauseButton_Click(object sender, RoutedEventArgs e)
    {
        if (_gameState == GameState.Playing)
        {
            SetGameState(GameState.Paused);
        }
        else if (_gameState == GameState.Paused)
        {
            SetGameState(GameState.Playing);
        }
    }

    private void RestartButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isActive)
        {
            StartNewGame();
        }
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        _backToLibrary();
    }

    private void OverlayPrimaryButton_Click(object sender, RoutedEventArgs e)
    {
        switch (_gameState)
        {
            case GameState.Paused:
                SetGameState(GameState.Playing);
                break;
            case GameState.GameOver:
                StartNewGame();
                break;
        }
    }

    private void GameTimer_Tick(object? sender, EventArgs e)
    {
        if (!_isActive || _gameState != GameState.Playing)
        {
            return;
        }

        var result = _gameEngine.Tick();
        GameBoard.InvalidateVisual();
        UpdateScoreDisplays();

        if (result == GameTickResult.AteFood)
        {
            UpdateHighScoreIfNeeded();
            UpdateTimerInterval();
        }

        if (result == GameTickResult.GameOver || _gameEngine.IsGameOver)
        {
            ShowGameOver();
        }
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!_isActive)
        {
            return;
        }

        if (DifficultyComboBox.IsKeyboardFocusWithin)
        {
            return;
        }

        if (_gameState == GameState.Playing)
        {
            var direction = e.Key switch
            {
                Key.Up or Key.W => Direction.Up,
                Key.Down or Key.S => Direction.Down,
                Key.Left or Key.A => Direction.Left,
                Key.Right or Key.D => Direction.Right,
                _ => (Direction?)null
            };

            if (direction.HasValue)
            {
                _gameEngine.TryChangeDirection(direction.Value);
                e.Handled = true;
                return;
            }

            if (e.Key is Key.Space or Key.Escape)
            {
                SetGameState(GameState.Paused);
                e.Handled = true;
            }

            return;
        }

        if (_gameState == GameState.Paused)
        {
            if (e.Key is Key.Space or Key.Enter)
            {
                SetGameState(GameState.Playing);
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                _backToLibrary();
                e.Handled = true;
            }

            return;
        }

        if (_gameState == GameState.GameOver)
        {
            if (e.Key == Key.Enter)
            {
                StartNewGame();
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                _backToLibrary();
                e.Handled = true;
            }
        }
    }

    private void Window_Deactivated(object? sender, EventArgs e)
    {
        if (_gameState == GameState.Playing)
        {
            SetGameState(GameState.Paused);
        }
    }

    private void Window_Activated(object? sender, EventArgs e)
    {
        if (_isActive && _gameState is GameState.Playing or GameState.Paused)
        {
            Keyboard.Focus(this);
        }
    }

    private void StartNewGame()
    {
        _difficulty = GetSelectedDifficulty();
        _gameEngine.StartNewGame();
        GameBoard.Engine = _gameEngine;
        UpdateScoreDisplays();
        UpdateTimerInterval();
        SetGameState(GameState.Playing);
    }

    private void DifficultyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isActive)
        {
            return;
        }

        _difficulty = GetSelectedDifficulty();
        if (_gameState == GameState.Playing)
        {
            UpdateTimerInterval();
        }
    }

    private GameDifficulty GetSelectedDifficulty()
    {
        if (DifficultyComboBox.SelectedItem is ComboBoxItem item &&
            Enum.TryParse<GameDifficulty>(item.Content?.ToString(), true, out var difficulty))
        {
            return difficulty;
        }

        throw new InvalidOperationException("The selected game difficulty is not valid.");
    }

    private void SetGameState(GameState gameState)
    {
        _gameState = gameState;
        switch (gameState)
        {
            case GameState.Playing:
                OverlayPanel.Visibility = Visibility.Collapsed;
                PauseButton.Content = "PAUSE";
                StatusText.Text = "Use arrow keys or WASD to move.";
                UpdateTimerInterval();
                if (_isActive)
                {
                    _gameTimer.Start();
                }
                break;
            case GameState.Paused:
                _gameTimer.Stop();
                PauseButton.Content = "RESUME";
                ShowOverlay("PAUSED", "Your run is waiting.", "RESUME");
                break;
            case GameState.GameOver:
                _gameTimer.Stop();
                PauseButton.Content = "PAUSE";
                ShowOverlay("GAME OVER", $"Final score: {_gameEngine.Score}", "PLAY AGAIN");
                break;
            case GameState.MainMenu:
                _gameTimer.Stop();
                OverlayPanel.Visibility = Visibility.Collapsed;
                break;
        }
    }

    private void ShowOverlay(string title, string message, string primaryAction)
    {
        OverlayTitleText.Text = title;
        OverlayMessageText.Text = message;
        OverlayPrimaryButton.Content = primaryAction;
        OverlayPanel.Visibility = Visibility.Visible;
    }

    private void ShowGameOver()
    {
        UpdateHighScoreIfNeeded();
        SetGameState(GameState.GameOver);
    }

    private void UpdateHighScoreIfNeeded()
    {
        if (_gameEngine.Score <= _highScore)
        {
            return;
        }

        var result = _highScoreService.SaveIfHigher(_gameEngine.Score);
        if (result.Succeeded)
        {
            _highScore = result.Score;
            UpdateScoreDisplays();
        }

        if (!result.Succeeded || result.Warning is not null)
        {
            ShowFeedback(result.Warning);
        }
    }

    private void UpdateScoreDisplays()
    {
        ScoreText.Text = _gameEngine.Score.ToString();
        HighScoreText.Text = _highScore.ToString();
    }

    private void UpdateTimerInterval()
    {
        _gameTimer.Interval = TimeSpan.FromMilliseconds(
            GameEngine.GetMovementIntervalMilliseconds(_difficulty, _gameEngine.Score));
    }

    private void ShowFeedback(string? message)
    {
        if (!string.IsNullOrEmpty(message))
        {
            StatusText.Text = message;
        }
    }
}
