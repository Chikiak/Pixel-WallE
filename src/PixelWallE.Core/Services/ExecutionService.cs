using PixelWallE.Core.Common;
using PixelWallE.Core.Drawing;
using PixelWallE.Core.Interpreters;
using PixelWallE.Core.Interpreters.Interfaces;
using PixelWallE.Core.Parsers.AST;
using SkiaSharp;

namespace PixelWallE.Core.Services;

public class ExecutionService(IInterpreterFactory? interpreterFactory = null) : IExecutionService
{
    private readonly IInterpreterFactory _interpreterFactory = interpreterFactory ?? new InterpreterFactory();

    public async Task ExecuteAsync(
        ProgramStmt program,
        SKBitmap? existingBitmap,
        int width,
        int height,
        IProgress<DrawingUpdate> progress,
        int executionDelay,
        ExecutionMode executionMode,
        CancellationToken cancellationToken = default)
    {
        ValidateParameters(program, progress, width, height, executionDelay);


        var interpreter = existingBitmap != null
            ? _interpreterFactory.CreateInterpreter(existingBitmap, executionDelay, executionMode, progress)
            : _interpreterFactory.CreateInterpreter(width, height, executionDelay, executionMode, progress);

        try
        {
            await interpreter.InterpretAsync(program, progress, cancellationToken);
        }
        finally
        {
            if (interpreter is IDisposable disposableInterpreter) disposableInterpreter.Dispose();
        }
    }

    private void ValidateParameters(ProgramStmt program, IProgress<DrawingUpdate> progress, int width, int height,
        int executionDelay)
    {
        if (program == null)
            throw new ArgumentNullException(nameof(program));

        if (progress == null)
            throw new ArgumentNullException(nameof(progress));

        if (width <= 0)
            throw new ArgumentException("Width must be positive", nameof(width));

        if (height <= 0)
            throw new ArgumentException("Height must be positive", nameof(height));

        if (executionDelay < 0)
            throw new ArgumentException("Execution delay cannot be negative", nameof(executionDelay));
    }
}