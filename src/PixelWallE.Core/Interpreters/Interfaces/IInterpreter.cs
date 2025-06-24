using PixelWallE.Core.Drawing;
using PixelWallE.Core.Parsers.AST;

namespace PixelWallE.Core.Interpreters.Interfaces;

public interface IInterpreter
{
    Task InterpretAsync(
        ProgramStmt program,
        IProgress<DrawingUpdate> progress,
        CancellationToken cancellationToken = default);
}