using ConsoleWall_e.Errors;
using ConsoleWall_e.Lexing;
using ConsoleWall_e.Tokens;

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
        List<Token> tokens = lexer.ScanTokens();

        if (Errors.Count > 0)
        {
            Console.WriteLine($"Se encontraron {Errors.Count} errores:");
            foreach (var error in Errors)
            {
                Console.WriteLine(error.ToString());
            }
            return;
        }

        foreach (var token in tokens)
        {
            Console.WriteLine(token.ToString());
        }
    }
}