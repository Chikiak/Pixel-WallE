using PixelWallE.Core.Drawing;
using PixelWallE.Core.Interpreters;
using PixelWallE.Core.Parsers.AST;
using SkiaSharp;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PixelWallE.Core.Services{

    public interface IExecutionService
    {
        Task ExecuteAsync(
            ProgramStmt program, 
            SKBitmap? existingBitmap,
            int width, 
            int height,
            IProgress<DrawingUpdate> progress,
            CancellationToken cancellationToken = default);
    }

    public class ExecutionService : IExecutionService
    {
        public async Task ExecuteAsync(
            ProgramStmt program, 
            SKBitmap? existingBitmap,
            int width, 
            int height,
            IProgress<DrawingUpdate> progress,
            CancellationToken cancellationToken = default)
        {
            // Crear intérprete con bitmap existente o nuevo.
            // El intérprete ahora NO se debe desechar aquí, ya que el ViewModel puede querer reutilizar el bitmap.
            // Simplemente se crea, se usa y se deja. El ViewModel gestionará su ciclo de vida.
            var interpreter = existingBitmap != null 
                ? new Interpreter(existingBitmap) 
                : new Interpreter(width, height);
        
            // Ejecutar con el sistema de progreso
            await interpreter.InterpretAsync(program, progress, cancellationToken);
        }
    }
}