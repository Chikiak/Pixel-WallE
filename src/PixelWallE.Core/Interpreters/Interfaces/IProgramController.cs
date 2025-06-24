using PixelWallE.Core.Parsers.AST;
using PixelWallE.Core.Parsers.AST.Stmts;

namespace PixelWallE.Core.Interpreters.Interfaces;

public interface IProgramController
{
    int TotalStatementsExecuted { get; }
    int MaxStatementsLimit { get; }
    int CurrentStatementPointer { get; }

    void Initialize(ProgramStmt program);
    bool HasNextStatement();
    Stmt GetNextStatement();
    Stmt GetCurrentStatement();
    void IncrementStatementCount();
    bool HasReachedExecutionLimit();

    void JumpToLabel(string label);
    bool LabelExists(string label);
    IReadOnlyDictionary<string, int> GetLabels();

    void Reset();
}