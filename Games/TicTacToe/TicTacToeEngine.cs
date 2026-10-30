namespace WindowsITToolkit.Games.TicTacToe;

public enum TicTacToePlayer
{
    X,
    O
}

public enum TicTacToeResult
{
    InProgress,
    XWon,
    OWon,
    Draw
}

public sealed class TicTacToeEngine
{
    private static readonly int[][] WinningCombinations =
    [
        [0, 1, 2],
        [3, 4, 5],
        [6, 7, 8],
        [0, 3, 6],
        [1, 4, 7],
        [2, 5, 8],
        [0, 4, 8],
        [2, 4, 6]
    ];

    private readonly TicTacToePlayer?[] _board = new TicTacToePlayer?[9];
    private readonly IReadOnlyList<TicTacToePlayer?> _readOnlyBoard;
    private int[] _winningCells = [];

    public TicTacToeEngine()
    {
        _readOnlyBoard = Array.AsReadOnly(_board);
    }

    public IReadOnlyList<TicTacToePlayer?> Board => _readOnlyBoard;
    public IReadOnlyList<int> WinningCells => Array.AsReadOnly(_winningCells);
    public TicTacToePlayer CurrentPlayer { get; private set; } = TicTacToePlayer.X;
    public TicTacToeResult Result { get; private set; } = TicTacToeResult.InProgress;
    public int XScore { get; private set; }
    public int OScore { get; private set; }
    public int DrawCount { get; private set; }

    public bool TryMakeMove(int cell)
    {
        if (cell is < 0 or >= 9 || Result != TicTacToeResult.InProgress || _board[cell].HasValue)
        {
            return false;
        }

        _board[cell] = CurrentPlayer;
        foreach (var combination in WinningCombinations)
        {
            if (combination.All(index => _board[index] == CurrentPlayer))
            {
                _winningCells = combination;
                Result = CurrentPlayer == TicTacToePlayer.X
                    ? TicTacToeResult.XWon
                    : TicTacToeResult.OWon;
                if (CurrentPlayer == TicTacToePlayer.X)
                {
                    XScore++;
                }
                else
                {
                    OScore++;
                }

                return true;
            }
        }

        if (_board.All(player => player.HasValue))
        {
            Result = TicTacToeResult.Draw;
            DrawCount++;
            return true;
        }

        CurrentPlayer = CurrentPlayer == TicTacToePlayer.X
            ? TicTacToePlayer.O
            : TicTacToePlayer.X;
        return true;
    }

    public void StartNewRound()
    {
        Array.Clear(_board);
        _winningCells = [];
        CurrentPlayer = TicTacToePlayer.X;
        Result = TicTacToeResult.InProgress;
    }

    public void ResetMatch()
    {
        XScore = 0;
        OScore = 0;
        DrawCount = 0;
        StartNewRound();
    }
}
