using PixelWallE.Core.Common;
using PixelWallE.Core.Errors;
using PixelWallE.Core.Lexing;
using PixelWallE.Core.Parser;
using PixelWallE.Core.Parser.AST;
using System;
using System.Threading.Tasks;

namespace PixelWallE.WPF.Services;

public interface ICompilerService
{
    Task<Result<ProgramStmt>> CompileAsync(string sourceCode);
    Result<ProgramStmt> Compile(string sourceCode);
}

public class CompilerService : ICompilerService
{
    public async Task<Result<ProgramStmt>> CompileAsync(string sourceCode)
    {
        return await Task.Run(() => Compile(sourceCode));
    }

    public Result<ProgramStmt> Compile(string sourceCode)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(sourceCode))
            {
                return Result<ProgramStmt>.Failure(
                    new SyntaxError(new CodeLocation(0, 0), "Source code cannot be empty"));
            }

            // 1. Lexical Analysis
            var lexer = new Lexer(sourceCode);
            var tokensResult = lexer.ScanTokens();
            if (!tokensResult.IsSuccess)
            {
                return Result<ProgramStmt>.Failure(tokensResult.Errors);
            }

            // 2. Syntax Analysis
            var parser = new Parser();
            var programResult = parser.Parse(tokensResult.Value);
            if (!programResult.IsSuccess)
            {
                return Result<ProgramStmt>.Failure(programResult.Errors);
            }

            // 3. Semantic Analysis
            var semanticChecker = new CheckSemantic();
            var semanticResult = semanticChecker.Analize(programResult.Value);
            if (!semanticResult.IsSuccess)
            {
                return Result<ProgramStmt>.Failure(semanticResult.Errors);
            }

            return Result<ProgramStmt>.Success(programResult.Value);
        }
        catch (Exception ex)
        {
            return Result<ProgramStmt>.Failure(
                new ImportError($"Unexpected compilation error: {ex.Message}"));
        }
    }
}