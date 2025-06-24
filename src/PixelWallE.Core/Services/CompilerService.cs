using PixelWallE.Core.Common;
using PixelWallE.Core.Errors;
using PixelWallE.Core.Lexers;
using PixelWallE.Core.Parsers;
using PixelWallE.Core.Parsers.AST;

namespace PixelWallE.Core.Services;

public interface ICompilerService
{
    public Result<ProgramStmt> Compile(string code);
}

public class CompilerService : ICompilerService
{
    public Result<ProgramStmt> Compile(string sourceCode)
    {
        var errors = new List<Error>();

        // 1. Lexer
        var lexer = new Lexer(sourceCode);
        var tokensResult = lexer.ScanTokens();
        if (!tokensResult.IsSuccess) return Result<ProgramStmt>.Failure(tokensResult.Errors);

        // 2. Parser
        var parser = new Parser();
        var programResult = parser.Parse(tokensResult.Value);
        if (!programResult.IsSuccess) return Result<ProgramStmt>.Failure(programResult.Errors);

        var programAst = programResult.Value;

        // 3. Semantic Checker
        var semanticChecker = new CheckSemantic();
        var semanticResult = semanticChecker.Analize(programAst);
        if (!semanticResult.IsSuccess) return Result<ProgramStmt>.Failure(semanticResult.Errors);

        return Result<ProgramStmt>.Success(programAst);
    }
}