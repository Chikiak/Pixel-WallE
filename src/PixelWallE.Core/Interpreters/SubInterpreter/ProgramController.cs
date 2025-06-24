using PixelWallE.Core.Common;
using PixelWallE.Core.Errors;
using PixelWallE.Core.Interpreters.Interfaces;
using PixelWallE.Core.Parsers.AST;
using PixelWallE.Core.Parsers.AST.Stmts;

namespace PixelWallE.Core.Interpreters.SubInterpreter;

public class ProgramController : IProgramController
{
    private const int DEFAULT_MAX_STATEMENTS = 10000;
    private readonly Dictionary<string, int> _labelIndexMap = new();
    private IReadOnlyList<Stmt> _statements = new List<Stmt>().AsReadOnly();

    public ProgramController(int maxStatementsLimit = DEFAULT_MAX_STATEMENTS)
    {
        if (maxStatementsLimit <= 0)
            throw new ArgumentException("Max statements limit must be positive", nameof(maxStatementsLimit));

        MaxStatementsLimit = maxStatementsLimit;
    }

    public int TotalStatementsExecuted { get; private set; }

    public int MaxStatementsLimit { get; } = DEFAULT_MAX_STATEMENTS;

    public int CurrentStatementPointer { get; private set; }

    public void Initialize(ProgramStmt program)
    {
        if (program == null)
            throw new ArgumentNullException(nameof(program));

        _statements = program.Statements;
        _labelIndexMap.Clear();
        CurrentStatementPointer = 0;
        TotalStatementsExecuted = 0;

        for (var i = 0; i < _statements.Count; i++)
            if (_statements[i] is LabelStmt labelStmt)
            {
                if (_labelIndexMap.ContainsKey(labelStmt.Label))
                    throw new RuntimeErrorException(new RuntimeError(
                        labelStmt.Location,
                        $"Duplicate label '{labelStmt.Label}' found"));

                _labelIndexMap[labelStmt.Label] = i;
            }
    }

    public bool HasNextStatement()
    {
        return CurrentStatementPointer < _statements.Count;
    }

    public Stmt GetNextStatement()
    {
        if (!HasNextStatement()) throw new InvalidOperationException("No more statements to execute");

        var statement = _statements[CurrentStatementPointer];
        CurrentStatementPointer++;

        return statement;
    }

    public Stmt GetCurrentStatement()
    {
        var statement = _statements[CurrentStatementPointer];
        return statement;
    }

    public void JumpToLabel(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
            throw new ArgumentException("Label cannot be null or empty", nameof(label));

        if (!_labelIndexMap.TryGetValue(label, out var targetIndex))
            throw new RuntimeErrorException(new RuntimeError(
                new CodeLocation(0, 0),
                $"Label '{label}' not found"));

        CurrentStatementPointer = targetIndex;
    }

    public void IncrementStatementCount()
    {
        TotalStatementsExecuted++;
    }

    public bool HasReachedExecutionLimit()
    {
        return TotalStatementsExecuted >= MaxStatementsLimit;
    }

    public bool LabelExists(string label)
    {
        return !string.IsNullOrWhiteSpace(label) && _labelIndexMap.ContainsKey(label);
    }

    public IReadOnlyDictionary<string, int> GetLabels()
    {
        return _labelIndexMap.AsReadOnly();
    }

    public void Reset()
    {
        CurrentStatementPointer = 0;
        TotalStatementsExecuted = 0;
    }

    public ProgramStats GetStats()
    {
        return new ProgramStats
        {
            TotalStatements = _statements.Count,
            CurrentPointer = CurrentStatementPointer,
            StatementsExecuted = TotalStatementsExecuted,
            LabelsCount = _labelIndexMap.Count,
            MaxStatementsLimit = MaxStatementsLimit,
            ProgressPercentage = _statements.Count > 0 ? CurrentStatementPointer * 100.0 / _statements.Count : 0,
            ExecutionLimitReached = HasReachedExecutionLimit()
        };
    }

    public IEnumerable<Stmt> GetRemainingStatements()
    {
        return _statements.Skip(CurrentStatementPointer);
    }

    public bool IsCurrentStatement<T>() where T : Stmt
    {
        if (!HasNextStatement())
            return false;

        return _statements[CurrentStatementPointer] is T;
    }

    public Stmt? PeekCurrentStatement()
    {
        return HasNextStatement() ? _statements[CurrentStatementPointer] : null;
    }
}

public record ProgramStats
{
    public int TotalStatements { get; init; }
    public int CurrentPointer { get; init; }
    public int StatementsExecuted { get; init; }
    public int LabelsCount { get; init; }
    public int MaxStatementsLimit { get; init; }
    public double ProgressPercentage { get; init; }
    public bool ExecutionLimitReached { get; init; }
}