namespace WindowsITToolkit.Games.Minesweeper;

public sealed class MinesweeperCell
{
    internal MinesweeperCell(int row, int column)
    {
        Row = row;
        Column = column;
    }

    public int Row { get; }
    public int Column { get; }
    public bool IsMine { get; internal set; }
    public int AdjacentMines { get; internal set; }
    public bool IsRevealed { get; internal set; }
    public bool IsFlagged { get; internal set; }
    public bool IsIncorrectFlag { get; internal set; }
}
