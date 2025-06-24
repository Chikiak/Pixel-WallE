using PixelWallE.Core.Common;
using PixelWallE.Core.Drawing;
using PixelWallE.Core.Errors;

namespace PixelWallE.Core.Interpreters.SubInterpreter;

public class ProgressReporter
{
    private int _fillCount;

    public ProgressReporter(ExecutionMode executionMode, int executionDelay, IProgress<DrawingUpdate> progress)
    {
        if (executionDelay < 0)
            throw new ArgumentException("Execution delay cannot be negative", nameof(executionDelay));

        ExecutionMode = executionMode;
        ExecutionDelay = executionDelay;
        Progress = progress ?? throw new ArgumentNullException(nameof(progress));
    }

    public ExecutionMode ExecutionMode { get; }
    public int ExecutionDelay { get; }
    public IProgress<DrawingUpdate> Progress { get; }

    public async Task ReportPixelProgress(SkiaCanvas canvas, CancellationToken cancellationToken)
    {
        if (ExecutionMode != ExecutionMode.PixelByPixel) return;
        Progress.Report(new DrawingUpdate(canvas.GetBitmap(), DrawingUpdateType.Pixel));
        await ApplyExecutionDelay(DrawingUpdateType.Pixel, cancellationToken);
    }

    public async Task ReportStepProgress(SkiaCanvas canvas, CancellationToken cancellationToken)
    {
        if (ExecutionMode != ExecutionMode.StepByStep) return;
        Progress.Report(new DrawingUpdate(canvas.GetBitmap(), DrawingUpdateType.Step));
        await ApplyExecutionDelay(DrawingUpdateType.Step, cancellationToken);
    }

    public async Task ReportFillProgress(SkiaCanvas canvas, int pixelsFilled, CancellationToken cancellationToken)
    {
        _fillCount += pixelsFilled;

        switch (ExecutionMode)
        {
            case ExecutionMode.PixelByPixel:
                // Reportar cada 5 píxeles rellenados para optimizar performance
                if (_fillCount % 5 == 0)
                {
                    Progress.Report(new DrawingUpdate(canvas.GetBitmap(), DrawingUpdateType.Pixel));
                    await ApplyExecutionDelay(DrawingUpdateType.Pixel, cancellationToken);
                }
                else
                {
                    await Task.Yield();
                }

                break;

            case ExecutionMode.StepByStep:
                // Reportar cada 50 píxeles para mejor performance
                if (_fillCount % 20 == 0)
                {
                    Progress.Report(new DrawingUpdate(canvas.GetBitmap(), DrawingUpdateType.Step));
                    await Task.Yield();
                }

                break;

            case ExecutionMode.Instant:
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(ExecutionMode), ExecutionMode, "Unknown execution mode");
        }

        Progress.Report(new DrawingUpdate(canvas.GetBitmap(), DrawingUpdateType.Step));
        if (ExecutionMode == ExecutionMode.StepByStep)
            await ApplyExecutionDelay(DrawingUpdateType.Step, cancellationToken);
        _fillCount = 0;
    }

    public async Task ReportErrorProgress(SkiaCanvas canvas, string message, IReadOnlyList<Error> errors)
    {
        var drawingUpdate = new DrawingUpdate(
            canvas.GetBitmap(),
            DrawingUpdateType.Error,
            message,
            errors);

        Progress.Report(drawingUpdate);
        await Task.CompletedTask;
    }

    public async Task ReportCompletionProgress(SkiaCanvas canvas, IReadOnlyList<Error> errors)
    {
        var drawingUpdate = new DrawingUpdate(
            canvas.GetBitmap(),
            DrawingUpdateType.Complete,
            "Execution completed successfully",
            errors);

        Progress.Report(drawingUpdate);
        await Task.CompletedTask;
    }

    public async Task ReportCancellationProgress(SkiaCanvas canvas)
    {
        var drawingUpdate = new DrawingUpdate(
            canvas.GetBitmap(),
            DrawingUpdateType.Complete,
            "Execution cancelled");

        Progress.Report(drawingUpdate);
        await Task.CompletedTask;
    }

    private async Task ApplyExecutionDelay(DrawingUpdateType delayType, CancellationToken cancellationToken)
    {
        if (ExecutionDelay <= 0)
        {
            await Task.Yield();
            return;
        }

        await Task.Delay(ExecutionDelay, cancellationToken);
    }

    public void Reset()
    {
        _fillCount = 0;
    }
}