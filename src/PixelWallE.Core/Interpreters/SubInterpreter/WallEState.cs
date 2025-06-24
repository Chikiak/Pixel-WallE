using PixelWallE.Core.Common;
using PixelWallE.Core.Interpreters.Interfaces;

namespace PixelWallE.Core.Interpreters.SubInterpreter;

/// <summary>
///     Mantiene la posición, color, tamaño del pincel y estado de relleno
/// </summary>
public class WallEState : IWallEState
{
    public WallEState()
    {
        Reset();
    }

    public WallEState(IWallEState other)
    {
        Position = other.Position;
        Color = other.Color;
        BrushSize = other.BrushSize;
        IsFilling = other.IsFilling;
    }

    public (int X, int Y) Position { get; private set; }

    public WallEColor Color { get; private set; }

    public int BrushSize { get; private set; }

    public bool IsFilling { get; private set; }

    public void SetPosition(int x, int y)
    {
        Position = (x, y);
    }

    public void SetColor(WallEColor color)
    {
        Color = color;
    }

    public void SetBrushSize(int brushSize)
    {
        if (brushSize <= 0)
            BrushSize = 1;
        else if (brushSize % 2 == 0)
            BrushSize = brushSize - 1;
        else
            BrushSize = brushSize;
    }

    public void SetFilling(bool isFilling)
    {
        IsFilling = isFilling;
    }

    public void Reset()
    {
        Position = (0, 0);
        Color = new WallEColor(0, 0, 0);
        BrushSize = 1;
        IsFilling = false;
    }

    public IWallEState Clone()
    {
        return new WallEState(this);
    }

    public override bool Equals(object? obj)
    {
        if (obj is not IWallEState other) return false;

        return Position.X == other.Position.X &&
               Position.Y == other.Position.Y &&
               Color.Equals(other.Color) &&
               BrushSize == other.BrushSize &&
               IsFilling == other.IsFilling;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Position.X, Position.Y, Color, BrushSize, IsFilling);
    }
}