using PixelWallE.Core.Common;
using PixelWallE.Core.Drawing;
using PixelWallE.Core.Interpreters;
using PixelWallE.Core.Parsers.AST;
using SkiaSharp;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PixelWallE.Core.Services;

public class ExecutionService : IExecutionService
{
    public async Task ExecuteAsync(
        ProgramStmt program,
        SKBitmap? existingBitmap,
        int width,
        int height,
        IProgress<DrawingUpdate> progress,
        int executionDelay,
        ExecutionMode executionMode, // Parámetro añadido
        CancellationToken cancellationToken = default)
    {
        // Crear intérprete con bitmap existente o nuevo, pasando la demora y el modo.
        var interpreter = existingBitmap != null
            ? new Interpreter(existingBitmap, executionDelay, executionMode)
            : new Interpreter(width, height, executionDelay, executionMode);

        // Ejecutar con el sistema de progreso
        await interpreter.InterpretAsync(program, progress, cancellationToken);
    }
}