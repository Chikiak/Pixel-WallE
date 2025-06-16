using PixelWallE.Core.Drawing;
using PixelWallE.Core.Parsers.AST;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace PixelWallE.Core.Interpreters;

public interface IInterpreter
{
    Task InterpretAsync(
        ProgramStmt program, 
        IProgress<DrawingUpdate> progress, 
        CancellationToken cancellationToken = default);
}