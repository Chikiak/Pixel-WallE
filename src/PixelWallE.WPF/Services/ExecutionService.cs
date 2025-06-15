using PixelWallE.Core.Common;
using PixelWallE.Core.Interpreter;
using PixelWallE.Core.Parser.AST;
using PixelWallE.Core.Errors;
using SkiaSharp;
using System;
using System.IO;
using System.Threading.Tasks;

namespace PixelWallE.WPF.Services;

public interface IExecutionService
{
    Task<Result<SKBitmap>> ExecuteAsync(ProgramStmt programAst, int width = 500, int height = 500);
    Result<SKBitmap> Execute(ProgramStmt programAst, int width = 500, int height = 500);
}

public class ExecutionService : IExecutionService
{
    private readonly string _tempDirectory;

    public ExecutionService()
    {
        _tempDirectory = Path.GetTempPath();
    }

    public async Task<Result<SKBitmap>> ExecuteAsync(ProgramStmt programAst, int width = 500, int height = 500)
    {
        return await Task.Run(() => Execute(programAst, width, height));
    }

    public Result<SKBitmap> Execute(ProgramStmt programAst, int width = 500, int height = 500)
    {
        if (programAst == null)
        {
            return Result<SKBitmap>.Failure(
                new RuntimeError(new CodeLocation(0, 0), "Program AST cannot be null"));
        }

        var tempImagePath = Path.Combine(_tempDirectory, $"pixelwalle_{Guid.NewGuid()}.png");
        SKBitmap? resultBitmap = null;

        try
        {
            var interpreter = new Interpreter(tempImagePath, null,height, width);
            var interpretResult = interpreter.Interpret(programAst);

            if (File.Exists(tempImagePath))
            {
                try
                {
                    resultBitmap = SKBitmap.Decode(tempImagePath);
                }
                catch (Exception ex)
                {
                    return Result<SKBitmap>.Failure(
                        new RuntimeError(programAst.Location, $"Failed to decode generated image: {ex.Message}"));
                }
            }

            // Manejar errores de interpretación
            if (!interpretResult.IsSuccess)
            {
                // Si hay una imagen parcial, la devolvemos junto con los errores
                if (resultBitmap != null)
                {
                    // En una implementación más avanzada, podríamos devolver tanto la imagen como los errores
                    // Por ahora, priorizamos los errores
                    resultBitmap?.Dispose();
                    return Result<SKBitmap>.Failure(interpretResult.Errors);
                }
                return Result<SKBitmap>.Failure(interpretResult.Errors);
            }

            if (resultBitmap == null)
            {
                return Result<SKBitmap>.Failure(
                    new RuntimeError(programAst.Location, "Execution completed but no image was generated"));
            }

            return Result<SKBitmap>.Success(resultBitmap);
        }
        catch (Exception ex)
        {
            resultBitmap?.Dispose();
            return Result<SKBitmap>.Failure(
                new RuntimeError(programAst.Location, $"Execution failed with exception: {ex.Message}"));
        }
        finally
        {
            // Limpiar archivo temporal
            try
            {
                if (File.Exists(tempImagePath))
                {
                    File.Delete(tempImagePath);
                }
            }
            catch
            {
                // Ignorar errores de limpieza
            }
        }
    }
}