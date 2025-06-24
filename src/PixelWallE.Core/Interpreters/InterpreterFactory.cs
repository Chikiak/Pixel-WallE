using PixelWallE.Core.Common;
using PixelWallE.Core.Drawing;
using PixelWallE.Core.Interpreters.Interfaces;
using PixelWallE.Core.Interpreters.SubInterpreter;
using SkiaSharp;

namespace PixelWallE.Core.Interpreters;

public class InterpreterFactory : IInterpreterFactory
{
    public IInterpreter CreateInterpreter(
        int width,
        int height,
        int executionDelay,
        ExecutionMode executionMode,
        IProgress<DrawingUpdate> progress)
    {
        var canvas = CreateCanvas(width, height);
        return CreateInterpreterInternal(canvas, executionDelay, executionMode, progress);
    }

    public IInterpreter CreateInterpreter(
        SKBitmap existingBitmap,
        int executionDelay,
        ExecutionMode executionMode,
        IProgress<DrawingUpdate> progress)
    {
        var canvas = CreateCanvas(existingBitmap);
        return CreateInterpreterInternal(canvas, executionDelay, executionMode, progress);
    }

    private IWallEState CreateRobotState()
    {
        return new WallEState();
    }

    private SkiaCanvas CreateCanvas(int width, int height)
    {
        return new SkiaCanvas(width, height);
    }

    private SkiaCanvas CreateCanvas(SKBitmap existingBitmap)
    {
        return new SkiaCanvas(existingBitmap);
    }

    private SkiaDrawingEngine CreateDrawingEngine()
    {
        return new SkiaDrawingEngine();
    }

    private IProgramController CreateProgramController()
    {
        return new ProgramController();
    }

    private ProgressReporter CreateProgressReporter(ExecutionMode mode, int delay, IProgress<DrawingUpdate> progress)
    {
        return new ProgressReporter(mode, delay, progress);
    }

    private IInterpreter CreateInterpreterInternal(
        SkiaCanvas canvas,
        int executionDelay,
        ExecutionMode executionMode,
        IProgress<DrawingUpdate> progress)
    {
        var wallEState = CreateRobotState();
        var drawingEngine = CreateDrawingEngine();
        var programController = CreateProgramController();
        var progressReporter = CreateProgressReporter(executionMode, executionDelay, progress);

        return new Interpreter(
            wallEState,
            canvas,
            drawingEngine,
            programController,
            progressReporter);
    }

    private void ValidateParameters(int executionDelay, ExecutionMode executionMode, IProgress<DrawingUpdate> progress)
    {
        if (executionDelay < 0)
            throw new ArgumentException("Execution delay cannot be negative", nameof(executionDelay));

        if (!Enum.IsDefined(typeof(ExecutionMode), executionMode))
            throw new ArgumentException("Invalid execution mode", nameof(executionMode));

        if (progress == null)
            throw new ArgumentNullException(nameof(progress));
    }

    public override string ToString()
    {
        return "InterpreterFactory[Modular Implementation]";
    }
}