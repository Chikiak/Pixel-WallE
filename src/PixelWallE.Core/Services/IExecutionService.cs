// File: src\PixelWallE.Core\Services\IExecutionService.cs
// ==================================================

using PixelWallE.Core.Common;
using PixelWallE.Core.Drawing;
using PixelWallE.Core.Parsers.AST;
using SkiaSharp;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PixelWallE.Core.Services;

public interface IExecutionService
{
    Task ExecuteAsync(
        ProgramStmt program,
        SKBitmap? existingBitmap,
        int width,
        int height,
        IProgress<DrawingUpdate> progress,
        int executionDelay,
        ExecutionMode executionMode, // Parámetro añadido
        CancellationToken cancellationToken = default);
}