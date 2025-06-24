using PixelWallE.Core.Common;
using SkiaSharp;

namespace PixelWallE.Core.Interpreters.SubInterpreter;

public class SkiaCanvas : IDisposable
{
    private readonly SKBitmap _bitmap;
    private bool _disposed;

    public SkiaCanvas(int width, int height)
    {
        if (width <= 0) throw new ArgumentException("Width must be positive", nameof(width));
        if (height <= 0) throw new ArgumentException("Height must be positive", nameof(height));

        _bitmap = new SKBitmap(width, height);
        Clear(new WallEColor(255, 255, 255));
    }

    public SkiaCanvas(SKBitmap existingBitmap)
    {
        if (existingBitmap == null) throw new ArgumentNullException(nameof(existingBitmap));

        _bitmap = existingBitmap.Copy();
    }

    public int Width => _bitmap.Width;
    public int Height => _bitmap.Height;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public SKBitmap GetBitmap()
    {
        ThrowIfDisposed();
        return _bitmap.Copy();
    }

    public void SetPixel(int x, int y, WallEColor color)
    {
        ThrowIfDisposed();

        if (!IsInBounds(x, y)) return;
        var newColor = ToSkiaColor(color);
        _bitmap.SetPixel(x, y, newColor);
    }

    private SKColor ToSkiaColor(WallEColor c)
    {
        return new SKColor(c.Red, c.Green, c.Blue, c.Alpha);
    }

    public WallEColor GetPixel(int x, int y)
    {
        ThrowIfDisposed();

        if (IsInBounds(x, y))
        {
            var color = _bitmap.GetPixel(x, y);
            return ToWallEColor(color);
        }

        return new WallEColor(0, 0, 0, 0);
    }

    private WallEColor ToWallEColor(SKColor c)
    {
        return new WallEColor(c.Red, c.Green, c.Blue, c.Alpha);
    }

    public void Clear(WallEColor color)
    {
        ThrowIfDisposed();

        using var canvas = new SKCanvas(_bitmap);
        var sColor = ToSkiaColor(color);
        canvas.Clear(sColor);
    }

    public bool IsInBounds(int x, int y)
    {
        return x >= 0 && x < Width && y >= 0 && y < Height;
    }

    public SkiaCanvas Clone()
    {
        ThrowIfDisposed();
        return new SkiaCanvas(_bitmap);
    }

    private void ThrowIfDisposed()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(SkiaCanvas));
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _bitmap?.Dispose();
            _disposed = true;
        }
    }

    ~SkiaCanvas()
    {
        Dispose(false);
    }
}