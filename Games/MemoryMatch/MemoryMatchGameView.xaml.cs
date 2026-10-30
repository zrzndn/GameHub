using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace WindowsITToolkit.Games.MemoryMatch;

public partial class MemoryMatchGameView : UserControl
{
    private static readonly string[] CardSymbols =
    [
        "●", "◆", "▲", "■", "★", "♥", "☀", "☂", "♣",
        "♦", "☯", "☘", "☕", "♫", "⚽", "☁", "⚓", "♠"
    ];

    private readonly Action _backToLibrary;
    private readonly DispatcherTimer _elapsedTimer;
    private readonly DispatcherTimer _mismatchTimer;
    private readonly LocalGameRecordStore _recordStore = new("memory-match");
    private readonly Dictionary<string, LocalGameRecord> _records;
    private MemoryMatchEngine _engine;
    private MemoryMatchDifficulty _difficulty = MemoryMatchDifficulty.Easy;
    private int _elapsedSeconds;
    private int _sessionVersion;
    private int _mismatchSessionVersion;
    private bool _isActive;
    private bool _isDeactivated;
    private string? _recordsWarning;

    public MemoryMatchGameView(Action backToLibrary)
    {
        ArgumentNullException.ThrowIfNull(backToLibrary);
        InitializeComponent();
        _backToLibrary = backToLibrary;
        var records = _recordStore.Load();
        _records = new Dictionary<string, LocalGameRecord>(records.Records, StringComparer.OrdinalIgnoreCase);
        _recordsWarning = records.Warning;
        _engine = new MemoryMatchEngine(_difficulty);
        _elapsedTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _elapsedTimer.Tick += ElapsedTimer_Tick;
        _mismatchTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(700) };
        _mismatchTimer.Tick += MismatchTimer_Tick;
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
        _sessionVersion++;
        _elapsedTimer.Stop();
        _mismatchTimer.Stop();
        _elapsedTimer.Tick -= ElapsedTimer_Tick;
        _mismatchTimer.Tick -= MismatchTimer_Tick;
        _engine.ResolveMismatch();
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

        _difficulty = (MemoryMatchDifficulty)DifficultyComboBox.SelectedIndex;
        StartNewGame();
    }

    private void NewGameButton_Click(object sender, RoutedEventArgs e) => StartNewGame();

    private void BackButton_Click(object sender, RoutedEventArgs e) => _backToLibrary();

    private void ElapsedTimer_Tick(object? sender, EventArgs e)
    {
        if (!_isActive || _engine.IsComplete)
        {
            _elapsedTimer.Stop();
            return;
        }

        _elapsedSeconds++;
        UpdateElapsedText();
    }

    private void MismatchTimer_Tick(object? sender, EventArgs e)
    {
        _mismatchTimer.Stop();
        if (!_isActive || _mismatchSessionVersion != _sessionVersion)
        {
            return;
        }

        if (_engine.ResolveMismatch())
        {
            UpdateView();
        }
    }

    private void StartNewGame()
    {
        _sessionVersion++;
        _mismatchTimer.Stop();
        _elapsedTimer.Stop();
        _elapsedSeconds = 0;
        _engine = new MemoryMatchEngine(_difficulty);
        CreateBoard();
        UpdateView();
    }

    private void CreateBoard()
    {
        DetachBoardHandlers();
        BoardGrid.Children.Clear();
        BoardGrid.Rows = _engine.Settings.Rows;
        BoardGrid.Columns = _engine.Settings.Columns;
        var cardWidth = _difficulty == MemoryMatchDifficulty.Hard ? 68 : 82;
        var cardHeight = _difficulty == MemoryMatchDifficulty.Hard ? 62 : 74;
        foreach (var card in _engine.Cards)
        {
            var button = new Button
            {
                Tag = card.Index,
                Width = cardWidth,
                Height = cardHeight,
                Margin = _difficulty == MemoryMatchDifficulty.Hard ? new Thickness(3) : new Thickness(5),
                Padding = new Thickness(2),
                FontSize = 29,
                FontWeight = FontWeights.Bold,
                Style = (Style)FindResource("HubButtonStyle"),
                Background = (Brush)FindResource("MemoryBackBrush")
            };
            button.Click += CardButton_Click;
            BoardGrid.Children.Add(button);
        }
    }

    private void DetachBoardHandlers()
    {
        foreach (var button in BoardGrid?.Children.OfType<Button>() ?? [])
        {
            button.Click -= CardButton_Click;
        }
    }

    private void CardButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_isActive || sender is not Button { Tag: int index })
        {
            return;
        }

        var result = _engine.Reveal(index);
        if (result == CardRevealResult.Rejected)
        {
            return;
        }

        if (_elapsedSeconds == 0 && !_elapsedTimer.IsEnabled && result == CardRevealResult.FirstCard)
        {
            _elapsedTimer.Start();
        }

        if (result == CardRevealResult.Mismatched)
        {
            _mismatchSessionVersion = _sessionVersion;
            _mismatchTimer.Stop();
            _mismatchTimer.Start();
        }
        else if (result == CardRevealResult.Completed)
        {
            _elapsedTimer.Stop();
            SaveCompletionRecords();
        }

        UpdateView();
    }

    private void SaveCompletionRecords()
    {
        var timeResult = _recordStore.SaveBestTimeIfLower(_difficulty.ToString(), _elapsedSeconds);
        var movesResult = _recordStore.SaveFewestMovesIfLower(_difficulty.ToString(), _engine.Moves);
        var warning = timeResult.Warning ?? movesResult.Warning;
        if (!timeResult.Succeeded || !movesResult.Succeeded)
        {
            _recordsWarning = warning ?? "One or more Memory Match records could not be saved.";
            return;
        }

        var current = _records.GetValueOrDefault(_difficulty.ToString()) ?? new LocalGameRecord(null, null);
        _records[_difficulty.ToString()] = current with
        {
            BestTimeSeconds = current.BestTimeSeconds is null
                ? _elapsedSeconds
                : Math.Min(current.BestTimeSeconds.Value, _elapsedSeconds),
            FewestMoves = current.FewestMoves is null
                ? _engine.Moves
                : Math.Min(current.FewestMoves.Value, _engine.Moves)
        };
        _recordsWarning = warning;
    }

    private void UpdateView()
    {
        foreach (var button in BoardGrid.Children.OfType<Button>())
        {
            var card = _engine.Cards[(int)button.Tag];
            button.Content = card.IsRevealed || card.IsMatched ? CardSymbols[card.PairId % CardSymbols.Length] : "✦";
            button.Background = (Brush)FindResource(card.IsMatched
                ? "MemoryMatchedBrush"
                : card.IsRevealed ? "HubCardBrush" : "MemoryBackBrush");
            button.Foreground = (Brush)FindResource(card.IsMatched
                ? "HubSuccessBrush"
                : card.IsRevealed ? "HubAccentBrush" : "HubCardBrush");
            button.IsEnabled = !card.IsMatched && !_engine.IsWaitingForMismatch;
        }

        MovesText.Text = _engine.Moves.ToString();
        PairsText.Text = $"{_engine.MatchedPairCount} / {_engine.Settings.PairCount}";
        UpdateElapsedText();
        BestRecordText.Text = _records.TryGetValue(_difficulty.ToString(), out var record)
            ? $"Best: {FormatRecordTime(record.BestTimeSeconds)} · Fewest: {record.FewestMoves?.ToString() ?? "--"}"
            : "Best: --:-- · Fewest: --";
        if (_engine.IsComplete)
        {
            StatusText.Text = $"All pairs matched in {_engine.Moves} moves and {FormatTime(_elapsedSeconds)}!";
            if (!string.IsNullOrEmpty(_recordsWarning))
            {
                StatusText.Text += $" {_recordsWarning}";
            }

            StatusText.Foreground = (Brush)FindResource("HubSuccessBrush");
        }
        else
        {
            StatusText.Text = _recordsWarning ?? "Reveal a card to start.";
            StatusText.Foreground = (Brush)FindResource(_recordsWarning is null ? "HubAccentBrush" : "HubMutedBrush");
        }
    }

    private void UpdateElapsedText() => ElapsedText.Text = FormatTime(_elapsedSeconds);

    private static string FormatRecordTime(int? seconds) =>
        seconds.HasValue ? FormatTime(seconds.Value) : "--:--";

    private static string FormatTime(int seconds) => $"{seconds / 60:00}:{seconds % 60:00}";
}
