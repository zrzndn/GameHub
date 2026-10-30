using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace WindowsITToolkit.Games.Minesweeper;

public partial class MinesweeperGameView : UserControl
{
    private readonly Action _backToLibrary;
    private readonly DispatcherTimer _elapsedTimer;
    private readonly LocalGameRecordStore _recordStore = new("minesweeper");
    private readonly Dictionary<string, LocalGameRecord> _records;
    private MinesweeperEngine _engine;
    private MinesweeperDifficulty _difficulty = MinesweeperDifficulty.Beginner;
    private int _elapsedSeconds;
    private bool _isActive;
    private bool _isDeactivated;
    private string? _recordsWarning;

    public MinesweeperGameView(Action backToLibrary)
    {
        ArgumentNullException.ThrowIfNull(backToLibrary);
        InitializeComponent();
        _backToLibrary = backToLibrary;
        var records = _recordStore.Load();
        _records = new Dictionary<string, LocalGameRecord>(records.Records, StringComparer.OrdinalIgnoreCase);
        _recordsWarning = records.Warning;
        _engine = new MinesweeperEngine(_difficulty);
        _elapsedTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _elapsedTimer.Tick += ElapsedTimer_Tick;
        Loaded += GameView_Loaded;
        Unloaded += GameView_Unloaded;
        CreateBoard();
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
        _elapsedTimer.Stop();
        _elapsedTimer.Tick -= ElapsedTimer_Tick;
        Loaded -= GameView_Loaded;
        Unloaded -= GameView_Unloaded;
        DetachBoardHandlers();
    }

    private void GameView_Loaded(object sender, RoutedEventArgs e)
    {
        if (_isActive || _isDeactivated)
        {
            return;
        }

        _isActive = true;
    }

    private void GameView_Unloaded(object sender, RoutedEventArgs e) => Deactivate();

    private void DifficultyComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (!_isActive || DifficultyComboBox.SelectedIndex < 0)
        {
            return;
        }

        _difficulty = (MinesweeperDifficulty)DifficultyComboBox.SelectedIndex;
        StartNewGame();
    }

    private void NewGameButton_Click(object sender, RoutedEventArgs e) => StartNewGame();

    private void BackButton_Click(object sender, RoutedEventArgs e) => _backToLibrary();

    private void ElapsedTimer_Tick(object? sender, EventArgs e)
    {
        if (!_isActive || _engine.Result != MinesweeperResult.InProgress)
        {
            _elapsedTimer.Stop();
            return;
        }

        _elapsedSeconds++;
        UpdateElapsedText();
    }

    private void StartNewGame()
    {
        _elapsedTimer.Stop();
        _elapsedSeconds = 0;
        _engine = new MinesweeperEngine(_difficulty);
        CreateBoard();
        UpdateView();
    }

    private void CreateBoard()
    {
        DetachBoardHandlers();
        BoardGrid.Children.Clear();
        BoardGrid.Rows = _engine.Settings.Rows;
        BoardGrid.Columns = _engine.Settings.Columns;
        var cellSize = _difficulty == MinesweeperDifficulty.Expert ? 27 : 36;
        for (var index = 0; index < _engine.Settings.Rows * _engine.Settings.Columns; index++)
        {
            var button = new Button
            {
                Tag = index,
                Width = cellSize,
                Height = cellSize,
                Padding = new Thickness(0),
                Margin = _difficulty == MinesweeperDifficulty.Expert ? new Thickness(1) : new Thickness(2),
                FontSize = _difficulty == MinesweeperDifficulty.Expert ? 11 : 14,
                FontWeight = FontWeights.Bold,
                Style = (Style)FindResource("HubButtonStyle"),
                Background = (Brush)FindResource("MineHiddenBrush")
            };
            button.Click += CellButton_Click;
            button.PreviewMouseRightButtonDown += CellButton_RightClick;
            BoardGrid.Children.Add(button);
        }
    }

    private void DetachBoardHandlers()
    {
        foreach (var button in BoardGrid?.Children.OfType<Button>() ?? [])
        {
            button.Click -= CellButton_Click;
            button.PreviewMouseRightButtonDown -= CellButton_RightClick;
        }
    }

    private void CellButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_isActive || sender is not Button { Tag: int index })
        {
            return;
        }

        var row = index / _engine.Settings.Columns;
        var column = index % _engine.Settings.Columns;
        if (!_engine.HasStarted && !_engine.GetCell(row, column).IsFlagged)
        {
            _elapsedSeconds = 0;
            _elapsedTimer.Start();
        }

        if (_engine.Reveal(row, column))
        {
            if (_engine.Result != MinesweeperResult.InProgress)
            {
                _elapsedTimer.Stop();
                SaveBestTimeIfWon();
            }

            UpdateView();
        }
    }

    private void CellButton_RightClick(object sender, MouseButtonEventArgs e)
    {
        if (!_isActive || sender is not Button { Tag: int index })
        {
            return;
        }

        _engine.ToggleFlag(index / _engine.Settings.Columns, index % _engine.Settings.Columns);
        UpdateView();
        e.Handled = true;
    }

    private void UpdateView()
    {
        foreach (var button in BoardGrid.Children.OfType<Button>())
        {
            var index = (int)button.Tag;
            var cell = _engine.GetCell(index / _engine.Settings.Columns, index % _engine.Settings.Columns);
            button.Content = GetCellContent(cell);
            button.IsEnabled = _engine.Result == MinesweeperResult.InProgress && !cell.IsRevealed;
            button.Background = GetCellBackground(cell);
            button.Foreground = cell.IsRevealed && !cell.IsMine
                ? (Brush)FindResource(GetCountBrushKey(cell.AdjacentMines))
                : (Brush)FindResource("HubTextBrush");
            button.BorderBrush = (Brush)FindResource("HubBorderBrush");
            button.FontSize = cell.AdjacentMines > 0 && cell.IsRevealed ? 14 : button.FontSize;
        }

        MineCountText.Text = (_engine.Settings.MineCount - _engine.FlaggedCellCount).ToString();
        UpdateElapsedText();
        BestTimeText.Text = _records.TryGetValue(_difficulty.ToString(), out var record) &&
            record.BestTimeSeconds.HasValue
            ? $"Best: {FormatTime(record.BestTimeSeconds.Value)}"
            : "Best: --:--";

        if (_engine.Result == MinesweeperResult.Won)
        {
            StatusText.Text = $"Field cleared in {FormatTime(_elapsedSeconds)}!";
            if (!string.IsNullOrEmpty(_recordsWarning))
            {
                StatusText.Text += $" {_recordsWarning}";
            }

            StatusText.Foreground = (Brush)FindResource("HubSuccessBrush");
        }
        else if (_engine.Result == MinesweeperResult.Lost)
        {
            StatusText.Text = "Mine hit — start a new game to try again.";
            StatusText.Foreground = (Brush)FindResource("HubSecondaryAccentBrush");
        }
        else
        {
            StatusText.Text = _recordsWarning ?? "Reveal a cell to begin. Right-click to flag a mine.";
            StatusText.Foreground = (Brush)FindResource(_recordsWarning is null ? "HubAccentBrush" : "HubMutedBrush");
        }
    }

    private void SaveBestTimeIfWon()
    {
        if (_engine.Result != MinesweeperResult.Won)
        {
            return;
        }

        var save = _recordStore.SaveBestTimeIfLower(_difficulty.ToString(), _elapsedSeconds);
        if (save.Succeeded)
        {
            _records[_difficulty.ToString()] = (_records.GetValueOrDefault(_difficulty.ToString()) ??
                new LocalGameRecord(null, null)) with { BestTimeSeconds = _elapsedSeconds };
        }

        if (!save.Succeeded || save.Warning is not null)
        {
            _recordsWarning = save.Warning ?? "The best completion time could not be saved.";
        }
    }

    private void UpdateElapsedText() => ElapsedText.Text = FormatTime(_elapsedSeconds);

    private static string FormatTime(int seconds) => $"{seconds / 60:00}:{seconds % 60:00}";

    private static object? GetCellContent(MinesweeperCell cell)
    {
        if (cell.IsIncorrectFlag)
        {
            return "×";
        }

        if (cell.IsFlagged)
        {
            return "⚑";
        }

        if (!cell.IsRevealed)
        {
            return null;
        }

        if (cell.IsMine)
        {
            return "●";
        }

        return cell.AdjacentMines == 0 ? null : cell.AdjacentMines.ToString();
    }

    private Brush GetCellBackground(MinesweeperCell cell)
    {
        if (cell.IsIncorrectFlag || cell.IsMine && cell.IsRevealed)
        {
            return (Brush)FindResource("MineHitBrush");
        }

        if (cell.IsFlagged)
        {
            return (Brush)FindResource("MineFlagBrush");
        }

        return (Brush)FindResource(cell.IsRevealed ? "MineRevealedBrush" : "MineHiddenBrush");
    }

    private static string GetCountBrushKey(int count) =>
        count is >= 1 and <= 8 ? $"MineCount{count}Brush" : "HubTextBrush";
}
