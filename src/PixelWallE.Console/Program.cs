using PixelWallE.Core.Drawing;
using PixelWallE.Core.Errors;
using PixelWallE.Core.Parsers.AST;
using PixelWallE.Core.Services;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using PixelWallE.Core.Common;

namespace ConsoleWall_e;

static class Program
{
    static List<Error> Errors = new List<Error>();

    // Main sigue siendo asíncrono
    static async Task Main(string[] args)
    {
        Errors.Clear();
        if (args.Length != 1)
        {
            Console.WriteLine("Uso: ConsoleWall-e.exe <ruta_del_archivo>");
            return;
        }
        string filePath = args[0];
        await RunFileAsync(filePath);

        if (Errors.Count > 0)
        {
            Console.WriteLine("\n--- Errors ---");
            foreach (var error in Errors)
            {
                Console.WriteLine(error.ToString());
            }
        }
    }

    private static async Task RunFileAsync(string filePath)
    {
        try
        {
            if (!File.Exists(filePath))
            {
                Errors.Add(new ImportError($"El archivo '{filePath}' no existe."));
                return;
            }
            if (!filePath.EndsWith(".pw"))
            {
                Errors.Add(new ImportError($"El archivo '{filePath}' no es compatible, debe ser .pw"));
                return;
            }
            string fileContent = await File.ReadAllTextAsync(filePath);
            await RunAsync(fileContent);
        }
        catch (Exception ex)
        {
            Errors.Add(new ImportError($"Error al procesar el archivo: {ex.Message}"));
        }
    }

    // ACTUALIZADO: RunAsync ahora usa IProgress<DrawingUpdate>
    private static async Task RunAsync(string code)
    {
        var compilerService = new CompilerService();
        var compileResult = compilerService.Compile(code);

        if (!compileResult.IsSuccess)
        {
            Errors.AddRange(compileResult.Errors);
            return;
        }

        var programAst = compileResult.Value;
        var executionService = new ExecutionService();
        SKBitmap? finalBitmap = null;

        // Seguimos usando TaskCompletionSource para esperar a que la ejecución asíncrona termine.
        var tcs = new TaskCompletionSource<bool>();

        Console.WriteLine("Executing...");

        // Se crea un manejador para IProgress<DrawingUpdate>.
        // Este se ejecutará cada vez que el intérprete reporte un progreso.
        var progressHandler = new Progress<DrawingUpdate>(update =>
        {
            // Para una app de consola, solo nos interesan los estados finales.
            // Opcional: mostrar actividad para los pasos intermedios.
            if (update.Type == DrawingUpdateType.Step)
            {
                Console.Write(".");
            }

            // Cuando la ejecución termina (ya sea por completarse o por un error),
            // capturamos el resultado y señalamos al TaskCompletionSource.
            if (update.Type == DrawingUpdateType.Complete || update.Type == DrawingUpdateType.Error)
            {
                Console.WriteLine(); // Salto de línea después de los puntos de progreso.
                finalBitmap = update.Bitmap?.Copy();
                if (update.Errors != null)
                {
                    Errors.AddRange(update.Errors);
                }

                // Si el update es de error y trae un mensaje, lo añadimos como error genérico.
                // Útil para errores como el límite de ejecución.
                if (update.Type == DrawingUpdateType.Error && !string.IsNullOrEmpty(update.Message) && (update.Errors == null || !update.Errors.Any()))
                {
                    Errors.Add(new RuntimeError(new PixelWallE.Core.Common.CodeLocation(0, 0), update.Message));
                }

                tcs.TrySetResult(true);
            }
        });

        // Inicia la ejecución con la nueva firma del método, pasando el progress handler,
        // el delay (0) y el modo de ejecución (Instant).
        await executionService.ExecuteAsync(
            programAst,
            null,                   // Sin bitmap inicial
            64,                     // Ancho por defecto
            64,                     // Alto por defecto
            progressHandler,
            0,                      // executionDelay: 0 para la consola
            ExecutionMode.Instant,  // executionMode: Instant para la consola
            CancellationToken.None);

        // Esperamos a que el progress handler nos avise que la ejecución ha terminado.
        await tcs.Task;

        Console.WriteLine("Execution finished.");

        // El guardado de la imagen permanece igual.
        if (finalBitmap != null)
        {
            string outputPath = "E:\\Proyectos\\PixelWallE\\src\\PixelWallE.Console\\CodigoPrueba\\output.png";
            try
            {
                using var image = SKImage.FromBitmap(finalBitmap);
                using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                using var stream = File.OpenWrite(outputPath);

                data.SaveTo(stream);

                Console.WriteLine($"Image saved to {outputPath}");
            }
            catch (Exception ex)
            {
                Errors.Add(new ImportError($"Failed to save image: {ex.Message}"));
            }
            finally
            {
                finalBitmap.Dispose();
            }
        }
        else if (!Errors.Any())
        {
            Console.WriteLine("Execution finished, but no image was generated.");
        }
    }
}