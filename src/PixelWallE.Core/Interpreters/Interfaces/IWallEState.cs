using PixelWallE.Core.Common;

namespace PixelWallE.Core.Interpreters.Interfaces;

public interface IWallEState
{
    (int X, int Y) Position { get; }
    WallEColor Color { get; }
    int BrushSize { get; }
    bool IsFilling { get; }
    void Reset();

    void SetPosition(int x, int y);
    void SetColor(WallEColor color);
    void SetBrushSize(int brushSize);
    void SetFilling(bool isFilling);

    IWallEState Clone();
}