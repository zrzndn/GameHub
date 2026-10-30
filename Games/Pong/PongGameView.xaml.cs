using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace WindowsITToolkit.Games.Pong;

public partial class PongGameView : UserControl
{
    private readonly Action _backToLibrary;
    private readonly DispatcherTimer _gameTimer;
    private readonly PongStatisticsService _statisticsService = new();
    private readonly Stopwatch _stopwatch = new();
    private PongStatistics _statistics;
    private PongEngine _engine;
    private Window? _activeWindow;
    private bool _isActive;
    private bool _isDeactivated;
    private bool _pausedByWindowDeactivation;
    private bool _matchRecorded;
    private string? _statisticsWarning;

    public PongGameView(Action backToLibrary)
    {
        ArgumentNullException.ThrowIfNull(backToLibrary);
        InitializeComponent();
        _backToLibrary = backToLibrary;

        var statistics = _statisticsService.Load();
        _statistics = statistics.Statistics;
        _statisticsWarning = statistics.Warning;
        _engine = new PongEngine();
        _engine.StartNewMatch();
        Arena.Engine = _engine;

        _gameTimer = new DispatcherTimer(DispatcherPriority.Render)
        {
            Interval = TimeSpan.FromMilliseconds(16)
        };
        _gameTimer.Tick += GameTimer_Tick;
        Loaded += GameView_Loaded;
        Unloaded += GameView_Unloaded;
        UpdateView();
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
        _stopwatch.Stop();
        _gameTimer.Tick -= GameTimer_Tick;
        if (_activeWindow is not null)
        {
            _activeWindow.PreviewKeyDown -= Window_PreviewKeyDown;
            _activeWindow.PreviewKeyUp -= Window_PreviewKeyUp;
            _activeWindow.Deactivated -= Window_Deactivated;
            _activeWindow.Activated -= Window_Activated;
            _activeWindow = null;
        }

        Loaded -= GameView_Loaded;
        Unloaded -= GameView_Unloaded;
    }

    private void GameView_Loaded(object sender, RoutedEventArgs e)
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
            _activeWindow.PreviewKeyUp += Window_PreviewKeyUp;
            _activeWindow.Deactivated += Window_Deactivated;
            _activeWindow.Activated += Window_Activated;
        }

        Keyboard.Focus(this);
        _stopwatch.Restart();
        _gameTimer.Start();
    }

    private void GameView_Unloaded(object sender, RoutedEventArgs e) => Deactivate();

    private void GameTimer_Tick(object? sender, EventArgs e)
    {
        if (!_isActive)
        {
            return;
        }

        var elapsed = _stopwatch.Elapsed.TotalSeconds;
        _stopwatch.Restart();
        _engine.Update(elapsed);
        Arena.InvalidateVisual();
        UpdateView();
        if (_engine.State == PongGameState.MatchOver && !_matchRecorded)
        {
            _gameTimer.Stop();
            _stopwatch.Stop();
            RecordMatch();
            UpdateView();
        }
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!_isActive)
        {
            return;
        }

        if (e.Key == Key.Space && !e.IsRepeat)
        {
            TogglePause();
            e.Handled = true;
        }
        else if (e.Key is Key.W or Key.Up)
        {
            _engine.SetPlayerMovement(-1);
            e.Handled = true;
        }
        else if (e.Key is Key.S or Key.Down)
        {
            _engine.SetPlayerMovement(1);
            e.Handled = true;
        }
    }

    private void Window_PreviewKeyUp(object sender, KeyEventArgs e)
    {
        if (_isActive && e.Key is Key.W or Key.Up or Key.S or Key.Down)
        {
            _engine.SetPlayerMovement(0);
            e.Handled = true;
        }
    }

    private void Window_Deactivated(object? sender, EventArgs e)
    {
        if (_engine.State is PongGameState.Playing or PongGameState.Serving)
        {
            _engine.Pause();
            _engine.SetPlayerMovement(0);
            _pausedByWindowDeactivation = true;
            UpdateView();
        }
    }

    private void Window_Activated(object? sender, EventArgs e)
    {
        if (!_isActive)
        {
            return;
        }

        Keyboard.Focus(this);
        if (_pausedByWindowDeactivation)
        {
            _pausedByWindowDeactivation = false;
            _engine.Resume();
            UpdateView();
        }
    }

    private void DifficultyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isActive || DifficultyComboBox.SelectedIndex < 0)
        {
            return;
        }

        StartNewMatch((PongDifficulty)DifficultyComboBox.SelectedIndex);
    }

    private void PauseButton_Click(object sender, RoutedEventArgs e) => TogglePause();

    private void NewMatchButton_Click(object sender, RoutedEventArgs e) =>
        StartNewMatch((PongDifficulty)DifficultyComboBox.SelectedIndex);

    private void BackButton_Click(object sender, RoutedEventArgs e) => _backToLibrary();

    private void TogglePause()
    {
        if (_engine.State == PongGameState.Paused)
        {
            _engine.Resume();
            _pausedByWindowDeactivation = false;
        }
        else
        {
            _engine.Pause();
            _engine.SetPlayerMovement(0);
        }

        UpdateView();
    }

    private void StartNewMatch(PongDifficulty difficulty)
    {
        _engine = new PongEngine(difficulty);
        _engine.StartNewMatch();
        Arena.Engine = _engine;
        _matchRecorded = false;
        _pausedByWindowDeactivation = false;
        _stopwatch.Restart();
        if (_isActive)
        {
            _gameTimer.Start();
        }

        UpdateView();
    }

    private void RecordMatch()
    {
        _matchRecorded = true;
        var result = _statisticsService.RecordMatch(
            _engine.PlayerScore >= PongEngine.WinningScore,
            _engine.BestRallyHits);
        if (result.Succeeded)
        {
            _statistics = result.Statistics;
        }

        _statisticsWarning = result.Warning;
    }

    private void UpdateView()
    {
        PlayerScoreText.Text = _engine.PlayerScore.ToString();
        OpponentScoreText.Text = _engine.OpponentScore.ToString();
        PauseButton.Content = _engine.State == PongGameState.Paused ? "RESUME" : "PAUSE";
        PauseButton.IsEnabled = _engine.State != PongGameState.MatchOver;
        StatusText.Text = _engine.State switch
        {
            PongGameState.Ready => "Ready",
            PongGameState.Serving => $"Serve in {Math.Max(1, (int)Math.Ceiling(_engine.ServeRemainingSeconds))}",
            PongGameState.Playing => $"{DifficultyComboBox.Text} · Rally {_engine.RallyHits}",
            PongGameState.Paused => "Paused · press Space or Resume",
            PongGameState.MatchOver => _engine.PlayerScore >= PongEngine.WinningScore
                ? "You win! Start a new match to play again."
                : "Computer wins. Start a new match to try again.",
            _ => string.Empty
        };
        StatusText.Foreground = _engine.State == PongGameState.MatchOver
            ? (Brush)FindResource(_engine.PlayerScore >= PongEngine.WinningScore ? "HubSuccessBrush" : "HubSecondaryAccentBrush")
            : Brushes.White;
        StatisticsText.Text =
            $"Matches {_statistics.MatchesPlayed}  ·  Wins {_statistics.PlayerWins}  ·  Losses {_statistics.ComputerWins}  ·  Best rally {_statistics.BestRallyHits}";
        if (!string.IsNullOrEmpty(_statisticsWarning))
        {
            StatisticsText.Text += $"  ·  {_statisticsWarning}";
        }

        Arena.InvalidateVisual();
    }
}
