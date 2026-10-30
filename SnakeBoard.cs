using System.Windows;
using System.Windows.Media;

namespace WindowsITToolkit;

public sealed class SnakeBoard : FrameworkElement
{
    private static readonly Brush BoardBrush = CreateFrozenBrush("#0B1424");
    private static readonly Brush GridBrush = CreateFrozenBrush("#17263A");
    private static readonly Brush SnakeBrush = CreateFrozenBrush("#22C55E");
    private static readonly Brush HeadBrush = CreateFrozenBrush("#22D3EE");
    private static readonly Brush FoodBrush = CreateFrozenBrush("#F43F5E");
    private static readonly Pen GridPen = CreateFrozenPen(GridBrush, 1);
    private GameEngine? _engine;

    public GameEngine? Engine
    {
        get => _engine;
        set
        {
            _engine = value;
            InvalidateVisual();
        }
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        var width = ActualWidth;
        var height = ActualHeight;
        drawingContext.DrawRectangle(BoardBrush, null, new Rect(0, 0, width, height));

        var cellWidth = width / GameEngine.BoardColumns;
        var cellHeight = height / GameEngine.BoardRows;

        for (var x = 0; x <= GameEngine.BoardColumns; x++)
        {
            var xPosition = x * cellWidth;
            drawingContext.DrawLine(GridPen, new Point(xPosition, 0), new Point(xPosition, height));
        }

        for (var y = 0; y <= GameEngine.BoardRows; y++)
        {
            var yPosition = y * cellHeight;
            drawingContext.DrawLine(GridPen, new Point(0, yPosition), new Point(width, yPosition));
        }

        if (_engine is null)
        {
            return;
        }

        DrawCell(
            drawingContext,
            _engine.Food,
            cellWidth,
            cellHeight,
            FoodBrush,
            Math.Min(cellWidth, cellHeight) * 0.3);

        for (var i = 0; i < _engine.Snake.Count; i++)
        {
            DrawCell(
                drawingContext,
                _engine.Snake[i],
                cellWidth,
                cellHeight,
                i == 0 ? HeadBrush : SnakeBrush,
                Math.Min(cellWidth, cellHeight) * 0.18);
        }
    }

    private static void DrawCell(
        DrawingContext drawingContext,
        GridPosition position,
        double cellWidth,
        double cellHeight,
        Brush brush,
        double inset)
    {
        var rectangle = new Rect(
            position.X * cellWidth + inset,
            position.Y * cellHeight + inset,
            cellWidth - 2 * inset,
            cellHeight - 2 * inset);
        drawingContext.DrawRoundedRectangle(brush, null, rectangle, inset, inset);
    }

    private static Brush CreateFrozenBrush(string color)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        brush.Freeze();
        return brush;
    }

    private static Pen CreateFrozenPen(Brush brush, double thickness)
    {
        var pen = new Pen(brush, thickness);
        pen.Freeze();
        return pen;
    }
}
