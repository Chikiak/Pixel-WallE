using PixelWallE.Core.Errors;
using SkiaSharp;
using System.Collections.Generic;

namespace PixelWallE.Core.Drawing;

public record DrawingUpdate(
    SKBitmap? Bitmap,
    DrawingUpdateType Type,
    string? Message = null,
    IReadOnlyList<Error>? Errors = null)
{
    public bool IsComplete => Type == DrawingUpdateType.Complete;
    public bool IsError => Type == DrawingUpdateType.Error;
    public bool IsPixel => Type == DrawingUpdateType.Pixel;
    public bool IsStep => Type == DrawingUpdateType.Step;
}