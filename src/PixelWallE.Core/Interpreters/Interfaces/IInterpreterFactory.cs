using PixelWallE.Core.Common;
using PixelWallE.Core.Drawing;
using SkiaSharp;

namespace PixelWallE.Core.Interpreters.Interfaces;

public interface IInterpreterFactory
{
    IInterpreter CreateInterpreter(
        int width,
        int height,
        int executionDelay,
        ExecutionMode executionMode,
        IProgress<DrawingUpdate> progress);

    IInterpreter CreateInterpreter(
        SKBitmap existingBitmap,
        int executionDelay,
        ExecutionMode executionMode,
        IProgress<DrawingUpdate> progress);
}