using System.Windows;
using System.Windows.Media;

namespace WindowsITToolkit.Games.Pong;

public sealed class PongArena : FrameworkElement
{
    private static readonly Brush ArenaBrush = FrozenBrush("#0F172A");
    private static readonly Brush CourtLineBrush = FrozenBrush("#334155");
    private static readonly Brush PlayerBrush = FrozenBrush("#38BDF8");
    private static readonly Brush OpponentBrush = FrozenBrush("#2DD4BF");
    private static readonly Brush BallBrush = FrozenBrush("#F8FAFC");
    private static readonly Pen CourtLinePen = FrozenPen(CourtLineBrush, 2);
    private PongEngine? _engine;

    public PongEngine? Engine
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
        drawingContext.DrawRectangle(ArenaBrush, null, new Rect(0, 0, ActualWidth, ActualHeight));
        if (_engine is null || ActualWidth <= 0 || ActualHeight <= 0)
        {
            return;
        }

        var scale = Math.Min(ActualWidth / PongEngine.ArenaWidth, ActualHeight / PongEngine.ArenaHeight);
        var left = (ActualWidth - PongEngine.ArenaWidth * scale) / 2;
        var top = (ActualHeight - PongEngine.ArenaHeight * scale) / 2;
        drawingContext.PushTransform(new TranslateTransform(left, top));
        drawingContext.PushTransform(new ScaleTransform(scale, scale));

        for (var y = 12d; y < PongEngine.ArenaHeight; y += 26)
        {
            drawingContext.DrawLine(CourtLinePen, new Point(PongEngine.ArenaWidth / 2, y),
                new Point(PongEngine.ArenaWidth / 2, y + 13));
        }

        drawingContext.DrawRoundedRectangle(
            PlayerBrush,
            null,
            new Rect(36, _engine.PlayerPaddleY, PongEngine.PaddleWidth, PongEngine.PaddleHeight),
            7,
            7);
        drawingContext.DrawRoundedRectangle(
            OpponentBrush,
            null,
            new Rect(PongEngine.ArenaWidth - 36 - PongEngine.PaddleWidth, _engine.OpponentPaddleY,
                PongEngine.PaddleWidth, PongEngine.PaddleHeight),
            7,
            7);
        drawingContext.DrawEllipse(
            BallBrush,
            null,
            new Point(_engine.BallPosition.X, _engine.BallPosition.Y),
            PongEngine.BallRadius,
            PongEngine.BallRadius);

        drawingContext.Pop();
        drawingContext.Pop();
    }

    private static Brush FrozenBrush(string color)
    {
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(color));
        brush.Freeze();
        return brush;
    }

    private static Pen FrozenPen(Brush brush, double thickness)
    {
        var pen = new Pen(brush, thickness);
        pen.Freeze();
        return pen;
    }
}
