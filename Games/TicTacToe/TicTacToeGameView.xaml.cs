using System.Windows;
using System.Windows.Controls;
using WindowsITToolkit.Games.TicTacToe;

namespace WindowsITToolkit.Games.TicTacToe;

public partial class TicTacToeGameView : UserControl
{
    private readonly TicTacToeEngine _engine = new();
    private readonly Action _backToLibrary;

    public TicTacToeGameView(Action backToLibrary)
    {
        ArgumentNullException.ThrowIfNull(backToLibrary);
        InitializeComponent();
        _backToLibrary = backToLibrary;
        UpdateView();
    }

    private void CellButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string cellText } button &&
            int.TryParse(cellText, out var cell) &&
            _engine.TryMakeMove(cell))
        {
            UpdateView();
        }
    }

    private void NewRoundButton_Click(object sender, RoutedEventArgs e)
    {
        _engine.StartNewRound();
        UpdateView();
    }

    private void ResetMatchButton_Click(object sender, RoutedEventArgs e)
    {
        _engine.ResetMatch();
        UpdateView();
    }

    private void BackButton_Click(object sender, RoutedEventArgs e) => _backToLibrary();

    private void UpdateView()
    {
        for (var index = 0; index < BoardGrid.Children.Count; index++)
        {
            if (BoardGrid.Children[index] is not Button button)
            {
                continue;
            }

            var player = _engine.Board[index];
            button.Content = player?.ToString();
            button.Foreground = player switch
            {
                TicTacToePlayer.X => (System.Windows.Media.Brush)FindResource("HubAccentBrush"),
                TicTacToePlayer.O => (System.Windows.Media.Brush)FindResource("HubSecondaryAccentBrush"),
                _ => (System.Windows.Media.Brush)FindResource("HubTextBrush")
            };
            button.Background = _engine.WinningCells.Contains(index)
                ? (System.Windows.Media.Brush)FindResource("HubSuccessBrush")
                : (System.Windows.Media.Brush)FindResource("HubCardBrush");
            if (_engine.WinningCells.Contains(index))
            {
                button.Foreground = (System.Windows.Media.Brush)FindResource("HubCardBrush");
            }
        }

        TurnText.Text = _engine.Result switch
        {
            TicTacToeResult.XWon => "Player X wins!",
            TicTacToeResult.OWon => "Player O wins!",
            TicTacToeResult.Draw => "It's a draw!",
            _ => $"{_engine.CurrentPlayer}'s turn"
        };
        TurnText.Foreground = _engine.Result switch
        {
            TicTacToeResult.XWon => (System.Windows.Media.Brush)FindResource("HubAccentBrush"),
            TicTacToeResult.OWon => (System.Windows.Media.Brush)FindResource("HubSecondaryAccentBrush"),
            TicTacToeResult.Draw => (System.Windows.Media.Brush)FindResource("HubMutedBrush"),
            _ => (System.Windows.Media.Brush)FindResource("HubAccentBrush")
        };
        XScoreText.Text = _engine.XScore.ToString();
        OScoreText.Text = _engine.OScore.ToString();
        DrawCountText.Text = _engine.DrawCount.ToString();
    }
}
