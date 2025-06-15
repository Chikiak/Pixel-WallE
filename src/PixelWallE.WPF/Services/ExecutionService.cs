using System.IO;
using PixelWallE.Core.Common;
using PixelWallE.Core.Errors;
using PixelWallE.Core.Interpreter;
using PixelWallE.Core.Parser.AST;
using SkiaSharp;

namespace PixelWallE.WPF.Services;

public interface IExecutionService
{
    Task<ExecutionOutcome> ExecuteAsync(ProgramStmt programAst, int width, int height, string? backgroundImagePath);
    ExecutionOutcome Execute(ProgramStmt programAst, int width, int height, string? backgroundImagePath);
}

// --- La implementación se adapta a la nueva lógica ---
public class ExecutionService : IExecutionService
{
    private readonly string _tempDirectory;

    public ExecutionService()
    {
        _tempDirectory = Path.GetTempPath();
    }

    public async Task<ExecutionOutcome> ExecuteAsync(ProgramStmt programAst, int width, int height, string? backgroundImagePath)
    {
        return await Task.Run(() => Execute(programAst, width, height, backgroundImagePath));
    }

    public ExecutionOutcome Execute(ProgramStmt programAst, int width, int height, string? backgroundImagePath)
    {
        if (programAst == null)
        {
            var error = new RuntimeError(new CodeLocation(0, 0), $"Argument '{nameof(programAst)}' cannot be null.");
            return new ExecutionOutcome(null, new List<Error> { error });
        }

        var tempImagePath = Path.Combine(_tempDirectory, $"pixelwalle_{Guid.NewGuid()}.png");
        SKBitmap? resultBitmap = null;

        try
        {
            var interpreter = new Interpreter(tempImagePath, backgroundImagePath, height, width);
            var interpretResult = interpreter.Interpret(programAst);

            // Intentamos cargar la imagen generada, incluso si hubo errores.
            // El intérprete guarda el archivo antes de devolver el resultado,
            // por lo que una imagen parcial podría existir.
            if (File.Exists(tempImagePath))
            {
                try
                {
                    resultBitmap = SKBitmap.Decode(tempImagePath);
                }
                catch (Exception ex)
                {
                    // Si la decodificación falla, agregamos un nuevo error y continuamos.
                    var decodeError = new RuntimeError(programAst.Location, $"Failed to decode the generated image: {ex.Message}");
                    var allErrors = interpretResult.Errors.ToList();
                    allErrors.Add(decodeError);
                    return new ExecutionOutcome(null, allErrors);
                }
            }

            // --- LÓGICA CLAVE MODIFICADA ---
            if (!interpretResult.IsSuccess)
            {
                // ¡Falló! Pero devolvemos un ExecutionOutcome que contiene
                // tanto la imagen parcial (si se cargó) como los errores.
                // El consumidor (ViewModel) decidirá qué hacer con ambos.
                return new ExecutionOutcome(resultBitmap, interpretResult.Errors);
            }

            if (resultBitmap == null)
            {
                var noImageError = new RuntimeError(programAst.Location, "Execution completed successfully, but no image was generated.");
                return new ExecutionOutcome(null, new List<Error> { noImageError });
            }

            // ¡Éxito! Devolvemos la imagen completa sin errores.
            return new ExecutionOutcome(resultBitmap, Array.Empty<Error>());
        }
        catch (Exception ex)
        {
            resultBitmap?.Dispose(); // Limpiar si una excepción ocurrió antes de devolver.
            var unhandledError = new RuntimeError(programAst.Location, $"Execution failed with an unhandled exception: {ex.Message}");
            return new ExecutionOutcome(null, new List<Error> { unhandledError });
        }
        finally
        {
            // La limpieza del archivo temporal no cambia.
            try
            {
                if (File.Exists(tempImagePath))
                {
                    File.Delete(tempImagePath);
                }
            }
            catch { /* Ignorar errores de limpieza */ }
        }
    }
}