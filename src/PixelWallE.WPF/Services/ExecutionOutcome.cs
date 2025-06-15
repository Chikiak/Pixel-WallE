using PixelWallE.Core.Errors;
using SkiaSharp;
using System.Linq;


namespace PixelWallE.WPF.Services;

public record ExecutionOutcome(SKBitmap? Bitmap, IReadOnlyList<Error> Errors)
{
    public bool IsSuccess => !Errors.Any();
}