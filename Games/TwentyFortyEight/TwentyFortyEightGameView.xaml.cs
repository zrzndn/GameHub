using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WindowsITToolkit.Games.TwentyFortyEight;

namespace WindowsITToolkit.Games.TwentyFortyEight;

public partial class TwentyFortyEightGameView : UserControl
{
    private readonly TwentyFortyEightEngine _engine = new();
    private readonly TwentyFortyEightBestScoreService _bestScoreService = new();
    private readonly Action _backToLibrary;
    private Window? _activeWindow;
    private bool _isActive;
    private bool _isDeactivated;
    private int _bestScore;
    private string? _startupWarning;

    public TwentyFortyEightGameView(Action backToLibrary)
    {
        ArgumentNullException.ThrowIfNull(backToLibrary);
        InitializeComponent();
        _backToLibrary = backToLibrary;
        var savedScore = _bestScoreService.Load();
        _bestScore = savedScore.Score;
        _startupWarning = savedScore.Warning;

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
        if (_activeWindow is not null)
        {
            _activeWindow.PreviewKeyDown -= Window_PreviewKeyDown;
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
            _activeWindow.Activated += Window_Activated;
        }

        Keyboard.Focus(this);
        ShowStartupWarning();
    }

    private void GameView_Unloaded(object sender, RoutedEventArgs e) => Deactivate();

    private void Window_Activated(object? sender, EventArgs e)
    {
        if (_isActive)
        {
            Keyboard.Focus(this);
        }
    }

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (!_isActive)
        {
            return;
        }

        var direction = e.Key switch
        {
            Key.Left or Key.A => TwentyFortyEightDirection.Left,
            Key.Right or Key.D => TwentyFortyEightDirection.Right,
            Key.Up or Key.W => TwentyFortyEightDirection.Up,
            Key.Down or Key.S => TwentyFortyEightDirection.Down,
            _ => (TwentyFortyEightDirection?)null
        };

        if (direction.HasValue)
        {
            Move(direction.Value);
            e.Handled = true;
        }
    }

    private void NewGameButton_Click(object sender, RoutedEventArgs e)
    {
        _engine.StartNewGame();
        UpdateView();
    }

    private void ContinueButton_Click(object sender, RoutedEventArgs e)
    {
        if (_engine.ContinueGame())
        {
            StatusText.Text = "Keep going! You can still build beyond 2048.";
            ContinueButton.Visibility = Visibility.Collapsed;
        }
    }

    private void BackButton_Click(object sender, RoutedEventArgs e) => _backToLibrary();

    private void Move(TwentyFortyEightDirection direction)
    {
        if (!_engine.Move(direction))
        {
            return;
        }

        if (_engine.Score > _bestScore)
        {
            var result = _bestScoreService.SaveIfHigher(_engine.Score);
            if (result.Succeeded)
            {
                _bestScore = _engine.Score;
            }

            if (!result.Succeeded || result.Warning is not null)
            {
                _startupWarning = result.Warning;
            }
        }

        UpdateView();
    }

    private void UpdateView()
    {
        for (var index = 0; index < BoardGrid.Children.Count; index++)
        {
            if (BoardGrid.Children[index] is not Button button)
            {
                continue;
            }

            var value = _engine.GetCell(index / TwentyFortyEightEngine.BoardSize, index % TwentyFortyEightEngine.BoardSize);
            button.Content = value == 0 ? null : value.ToString();
            button.Background = (Brush)FindResource(GetTileBrushKey(value));
            button.Foreground = (Brush)FindResource(value >= 64 ? "HubCardBrush" : "HubTextBrush");
            button.FontSize = value >= 1024 ? 25 : value >= 128 ? 28 : 32;
        }

        ScoreText.Text = _engine.Score.ToString();
        BestScoreText.Text = _bestScore.ToString();
        ContinueButton.Visibility = _engine.HasWon && !_engine.HasContinuedAfterWin && !_engine.IsGameOver
            ? Visibility.Visible
            : Visibility.Collapsed;

        if (_engine.IsGameOver)
        {
            StatusText.Text = "Game over — start a new game to play again.";
            StatusText.Foreground = (Brush)FindResource("HubSecondaryAccentBrush");
        }
        else if (_engine.HasWon && !_engine.HasContinuedAfterWin)
        {
            StatusText.Text = "2048 reached! Continue playing or start a new game.";
            StatusText.Foreground = (Brush)FindResource("HubSuccessBrush");
        }
        else if (!string.IsNullOrEmpty(_startupWarning))
        {
            StatusText.Text = _startupWarning;
            StatusText.Foreground = (Brush)FindResource("HubMutedBrush");
        }
        else
        {
            StatusText.Text = "Combine matching tiles to reach 2048.";
            StatusText.Foreground = (Brush)FindResource("HubAccentBrush");
        }
    }

    private void ShowStartupWarning()
    {
        if (!string.IsNullOrEmpty(_startupWarning))
        {
            StatusText.Text = _startupWarning;
            StatusText.Foreground = (Brush)FindResource("HubMutedBrush");
        }
    }

    private static string GetTileBrushKey(int value) =>
        value switch
        {
            0 => "Tile0Brush",
            2 => "Tile2Brush",
            4 => "Tile4Brush",
            8 => "Tile8Brush",
            16 => "Tile16Brush",
            32 => "Tile32Brush",
            64 => "Tile64Brush",
            128 => "Tile128Brush",
            256 => "Tile256Brush",
            512 => "Tile512Brush",
            1024 => "Tile1024Brush",
            2048 => "Tile2048Brush",
            _ => "TileHighBrush"
        };

}
