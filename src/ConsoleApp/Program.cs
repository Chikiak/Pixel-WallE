using Core.DevUtils;
using Core.Errors;
using Core.Interpreter;
using Core.Lexing;
using Core.Parser;

namespace ConsoleWall_e;

static class Program
{
    static List<Error> Errors = [];
    static void Main(string[] args)
    {
        Errors.Clear();
        // Verificar si se proporcionó un argumento
        if (args.Length != 1)
        {
            Console.WriteLine("Uso: ConsoleWall-e.exe <ruta_del_archivo>");
            return;
        }
        string filePath = args[0];
        RunFile(filePath);
        foreach (var error in Errors)
        {
            Console.WriteLine(error.ToString());
        }
            
    }
        
    private static void RunFile(string filePath)
    {
        try
        {
            // Comprobar si el archivo existe
            if (!File.Exists(filePath))
            {
                Errors.Add(new ImportError($"El archivo {filePath} no existe."));
                return;
            }

            if (!filePath.EndsWith(".pw"))
            {
                Errors.Add(new ImportError($"El archivo {filePath} no es compatible, debe ser .pw"));
                return;
            }
            string fileContent = File.ReadAllText(filePath);
            Run(fileContent);
            return;
        }
        catch (Exception ex)
        {
            Errors.Add(new ImportError($"Error al procesar el archivo: {ex.Message}"));
        }
    }

    private static void Run(string code)
    {
        var lexer = new Lexer(code);
        var tokensResult = lexer.ScanTokens();

        if (!tokensResult.IsSuccess)
        {
            Errors.AddRange(tokensResult.Errors);
            return;
        }


        var parser = new Parser();
        var programResult = parser.Parse(tokensResult.Value);
        Console.WriteLine("Parser terminó");


        if (!programResult.IsSuccess)
        {
            Errors.AddRange(programResult.Errors);
            return;
        }

        var semanticChecker = new CheckSemantic();
        var semanticResult = semanticChecker.Analize(programResult.Value);

        if (!semanticResult.IsSuccess)
        {
            Errors.AddRange(semanticResult.Errors);
            return;
        }

        // Imprimir el AST
        var printer = new ASTPrinter();
        Console.WriteLine("=== AST ===");
        Console.WriteLine(printer.Print(programResult.Value));

        var interpreter = new Interpreter("E:\\Proyectos\\PixelWallE\\src\\ConsoleApp\\CodigoPrueba\\output.png",
            defaultHeight: 1000, defaultWidth: 1000);
        var result = interpreter.Interpret(programResult.Value);

        if (!result.IsSuccess)
        {
            Errors.AddRange(result.Errors);
            return;
        }
    }
}