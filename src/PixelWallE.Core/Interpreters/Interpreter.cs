using PixelWallE.Core.Common;
using PixelWallE.Core.Drawing;
using PixelWallE.Core.Errors;
using PixelWallE.Core.Interpreters.Interfaces;
using PixelWallE.Core.Interpreters.SubInterpreter;
using PixelWallE.Core.Parsers.AST;
using PixelWallE.Core.Parsers.AST.Exprs;
using PixelWallE.Core.Parsers.AST.Stmts;

namespace PixelWallE.Core.Interpreters;

public class Interpreter : IInterpreter, IVisitor<object?>
{
    #region Constructor

    public Interpreter(
        IWallEState wallEState,
        SkiaCanvas canvas,
        SkiaDrawingEngine drawingEngine,
        IProgramController programController,
        ProgressReporter progressReporter)
    {
        _wallEState = wallEState ?? throw new ArgumentNullException(nameof(wallEState));
        _canvas = canvas ?? throw new ArgumentNullException(nameof(canvas));
        _drawingEngine = drawingEngine ?? throw new ArgumentNullException(nameof(drawingEngine));
        _programController = programController ?? throw new ArgumentNullException(nameof(programController));
        _progressReporter = progressReporter ?? throw new ArgumentNullException(nameof(progressReporter));
    }

    #endregion

    #region Public Methods

    public async Task InterpretAsync(ProgramStmt program, IProgress<DrawingUpdate> progress,
        CancellationToken cancellationToken = default)
    {
        InitializeForRun(program);

        try
        {
            while (_programController.HasNextStatement())
            {
                cancellationToken.ThrowIfCancellationRequested();

                _programController.IncrementStatementCount();
                if (_programController.HasReachedExecutionLimit())
                {
                    var error = new RuntimeError(new CodeLocation(0, 0),
                        "Execution limit reached (possible infinite loop).");
                    await _progressReporter.ReportErrorProgress(_canvas, error.Message, new[] { error });
                    return;
                }

                var stmt = _programController.GetNextStatement();
                await ExecuteStatementAsync(stmt, cancellationToken);

                await Task.Yield();
            }

            await _progressReporter.ReportCompletionProgress(_canvas, _errors.AsReadOnly());
        }
        catch (RuntimeErrorException rex)
        {
            _errors.Add(rex.runtimeError);
            await _progressReporter.ReportErrorProgress(_canvas, rex.runtimeError.Message, _errors.AsReadOnly());
        }
        catch (OperationCanceledException)
        {
            await _progressReporter.ReportCancellationProgress(_canvas);
        }
        catch (Exception ex)
        {
            var loc = _programController.HasNextStatement()
                ? _programController.GetCurrentStatement().Location
                : new CodeLocation(1, 1);
            var error = new RuntimeError(loc, $"Unexpected error: {ex.Message}");
            _errors.Add(error);
            await _progressReporter.ReportErrorProgress(_canvas, ex.Message, _errors.AsReadOnly());
        }
    }

    #endregion

    #region Fields

    private readonly IWallEState _wallEState;
    private readonly SkiaCanvas _canvas;
    private readonly SkiaDrawingEngine _drawingEngine;
    private readonly IProgramController _programController;
    private readonly ProgressReporter _progressReporter;

    private readonly Dictionary<string, object> _environment = new();
    private readonly List<Error> _errors = new();

    #endregion

    #region Private Execution Logic

    private void InitializeForRun(ProgramStmt program)
    {
        _environment.Clear();
        _errors.Clear();
        _wallEState.Reset();
        _programController.Initialize(program);
        _progressReporter.Reset();
    }

    private async Task ExecuteStatementAsync(Stmt stmt, CancellationToken cancellationToken)
    {
        stmt.Accept(this);

        Dictionary<(int x, int y), WallEColor>? pixelsToDraw = null;
        var pixelsFilled = 0;

        switch (stmt)
        {
            case DrawLineStmt s:
                pixelsToDraw = _drawingEngine.DrawLine(_wallEState, _canvas, ConvertToInt(Evaluate(s.DirX), s.Location),
                    ConvertToInt(Evaluate(s.DirY), s.Location), ConvertToInt(Evaluate(s.Distance), s.Location));
                break;
            case DrawCircleStmt s:
                pixelsToDraw = _drawingEngine.DrawCircle(_wallEState, _canvas,
                    ConvertToInt(Evaluate(s.DirX), s.Location), ConvertToInt(Evaluate(s.DirY), s.Location),
                    ConvertToInt(Evaluate(s.Radius), s.Location));
                break;
            case DrawRectangleStmt s:
                pixelsToDraw = _drawingEngine.DrawRectangle(_wallEState, _canvas,
                    ConvertToInt(Evaluate(s.DirX), s.Location), ConvertToInt(Evaluate(s.DirY), s.Location),
                    ConvertToInt(Evaluate(s.Distance), s.Location), ConvertToInt(Evaluate(s.Width), s.Location),
                    ConvertToInt(Evaluate(s.Height), s.Location));
                break;
            case FillStmt _:
                pixelsToDraw = _drawingEngine.Fill(_wallEState, _canvas);
                break;
        }

        if (pixelsToDraw != null) await ApplyDrawingChangesAsync(pixelsToDraw, cancellationToken);

        if (pixelsFilled > 0)
            await _progressReporter.ReportFillProgress(_canvas, pixelsFilled, cancellationToken);
        else if (stmt is DrawLineStmt or DrawCircleStmt or DrawRectangleStmt or FillStmt)
            await _progressReporter.ReportStepProgress(_canvas, cancellationToken);
    }

    private async Task ApplyDrawingChangesAsync(Dictionary<(int x, int y), WallEColor> pixelsToDraw,
        CancellationToken cancellationToken)
    {
        foreach (var pixel in pixelsToDraw)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var (pos, color) = (pixel.Key, pixel.Value);
            var blendedColor = _drawingEngine.BlendColors(_canvas.GetPixel(pos.x, pos.y), color);
            _canvas.SetPixel(pos.x, pos.y, blendedColor);

            await _progressReporter.ReportPixelProgress(_canvas, cancellationToken);
        }

        await _progressReporter.ReportStepProgress(_canvas, cancellationToken);
    }

    #endregion

    #region Statement Visitors

    public object? VisitProgramStmt(ProgramStmt stmt)
    {
        return null;
    }

    public object? VisitExpressionStmt(ExpressionStmt stmt)
    {
        Evaluate(stmt.Expr);
        return null;
    }

    public object? VisitSpawnStmt(SpawnStmt stmt)
    {
        var x = ConvertToInt(Evaluate(stmt.X), stmt.X.Location, nameof(stmt.X));
        var y = ConvertToInt(Evaluate(stmt.Y), stmt.Y.Location, nameof(stmt.Y));
        if (!_canvas.IsInBounds(x, y))
            throw new RuntimeErrorException(new RuntimeError(stmt.Location,
                $"Spawn coordinates ({x},{y}) out of canvas range ({_canvas.Width}x{_canvas.Height})."));
        _wallEState.SetPosition(x, y);
        return null;
    }

    public object? VisitColorStmt(ColorStmt stmt)
    {
        var colorStr = ConvertToString(Evaluate(stmt.ColorExpr), stmt.ColorExpr.Location, "Color argument");
        if (WallEColor.TryParse(colorStr, out var newColor))
            _wallEState.SetColor(newColor);
        else
            throw new RuntimeErrorException(new RuntimeError(stmt.ColorExpr.Location,
                $"Invalid color string '{colorStr}'."));
        return null;
    }

    public object? VisitSizeStmt(SizeStmt stmt)
    {
        var brushSize = ConvertToInt(Evaluate(stmt.SizeExpr), stmt.SizeExpr.Location, "Size argument");
        _wallEState.SetBrushSize(brushSize);
        return null;
    }

    public object? VisitDrawLineStmt(DrawLineStmt stmt)
    {
        return null;
    }

    public object? VisitDrawCircleStmt(DrawCircleStmt stmt)
    {
        return null;
    }

    public object? VisitDrawRectangleStmt(DrawRectangleStmt stmt)
    {
        return null;
    }

    public object? VisitFillStmt(FillStmt stmt)
    {
        return null;
    }
    
    public object? VisitFillingStmt(FillingStmt stmt)
    {
        var boolVal = ConvertToBool(Evaluate(stmt.BoolExpr), stmt.BoolExpr.Location, "Filling argument");
        _wallEState.SetFilling(boolVal);
        return null;
    }

    public object? VisitRespawnStmt(RespawnStmt stmt)
    {
        var x = ConvertToInt(Evaluate(stmt.X), stmt.X.Location, nameof(stmt.X));
        var y = ConvertToInt(Evaluate(stmt.Y), stmt.Y.Location, nameof(stmt.Y));
        if (!_canvas.IsInBounds(x, y))
            throw new RuntimeErrorException(new RuntimeError(stmt.Location,
                $"Respawn coordinates ({x},{y}) out of canvas range ({_canvas.Width}x{_canvas.Height})."));
        _wallEState.SetPosition(x, y);
        return null;
    }

    public object? VisitAssignStmt(AssignStmt stmt)
    {
        _environment[stmt.Name] = Evaluate(stmt.Value);
        return null;
    }

    public object? VisitLabelStmt(LabelStmt stmt)
    {
        return null;
    }

    public object? VisitGoToStmt(GoToStmt stmt)
    {
        var condition = ConvertToBool(Evaluate(stmt.Condition), stmt.Condition.Location, "GoTo condition");
        if (condition)
        {
            if (_programController.LabelExists(stmt.Label))
                _programController.JumpToLabel(stmt.Label);
            else
                throw new RuntimeErrorException(new RuntimeError(stmt.Location, $"Label '{stmt.Label}' not found."));
        }

        return null;
    }

    #endregion

    #region Expression Visitors

    public object VisitLiteralExpr(LiteralExpr expr)
    {
        return expr.Value.Value ??
               throw new RuntimeErrorException(new RuntimeError(expr.Location, "Literal value is null."));
    }

    public object VisitBangExpr(BangExpr expr)
    {
        return new IntegerOrBool(!ConvertToBool(Evaluate(expr.Right), expr.Right.Location, "operand of !"));
    }

    public object VisitMinusExpr(MinusExpr expr)
    {
        return new IntegerOrBool(-ConvertToInt(Evaluate(expr.Right), expr.Right.Location, "operand of negation -"));
    }

    public object VisitAddExpr(AddExpr expr)
    {
        var leftVal = Evaluate(expr.Left);
        var rightVal = Evaluate(expr.Right);
        if (leftVal is string || rightVal is string)
            return ConvertValToString(leftVal, expr.Left.Location) + ConvertValToString(rightVal, expr.Right.Location);
        return new IntegerOrBool(ConvertToInt(leftVal, expr.Left.Location, "left operand of +") +
                                 ConvertToInt(rightVal, expr.Right.Location, "right operand of +"));
    }

    public object VisitSubtractExpr(SubtractExpr expr)
    {
        return new IntegerOrBool(ConvertToInt(Evaluate(expr.Left), expr.Left.Location, "left operand of -") -
                                 ConvertToInt(Evaluate(expr.Right), expr.Right.Location, "right operand of -"));
    }

    public object VisitMultiplyExpr(MultiplyExpr expr)
    {
        var leftVal = Evaluate(expr.Left);
        var rightVal = Evaluate(expr.Right);
        if (leftVal is string sVal)
        {
            var count = ConvertToInt(rightVal, expr.Right.Location, "multiplier");
            if (count < 0)
                throw new RuntimeErrorException(new RuntimeError(expr.Right.Location, "Negative repetition count."));
            return string.Concat(Enumerable.Repeat(sVal, count));
        }

        if (rightVal is string sRVal)
        {
            var count = ConvertToInt(leftVal, expr.Left.Location, "multiplier");
            if (count < 0)
                throw new RuntimeErrorException(new RuntimeError(expr.Left.Location, "Negative repetition count."));
            return string.Concat(Enumerable.Repeat(sRVal, count));
        }

        return new IntegerOrBool(ConvertToInt(leftVal, expr.Left.Location, "left operand of *") *
                                 ConvertToInt(rightVal, expr.Right.Location, "right operand of *"));
    }

    public object VisitDivideExpr(DivideExpr expr)
    {
        var right = ConvertToInt(Evaluate(expr.Right), expr.Right.Location, "divisor");
        if (right == 0)
            throw new RuntimeErrorException(new RuntimeError(expr.Right.Location, "Division by zero."));
        var left = ConvertToInt(Evaluate(expr.Left), expr.Left.Location, "dividend");
        return new IntegerOrBool(left / right);
    }

    public object VisitPowerExpr(PowerExpr expr)
    {
        var numBase = ConvertToInt(Evaluate(expr.Left), expr.Left.Location, "base");
        var exponent = ConvertToInt(Evaluate(expr.Right), expr.Right.Location, "exponent");
        if (exponent < 0)
            throw new RuntimeErrorException(new RuntimeError(expr.Right.Location, "Exponent must be non-negative."));
        try
        {
            return new IntegerOrBool((int)Math.Pow(numBase, exponent));
        }
        catch (OverflowException)
        {
            throw new RuntimeErrorException(new RuntimeError(expr.Location, "Power operation result too large."));
        }
    }

    public object VisitModuloExpr(ModuloExpr expr)
    {
        var right = ConvertToInt(Evaluate(expr.Right), expr.Right.Location, "divisor");
        if (right == 0)
            throw new RuntimeErrorException(new RuntimeError(expr.Right.Location, "Modulo by zero."));
        var left = ConvertToInt(Evaluate(expr.Left), expr.Left.Location, "dividend");
        return new IntegerOrBool(left % right);
    }

    public object VisitEqualEqualExpr(EqualEqualExpr expr)
    {
        var left = Evaluate(expr.Left);
        var right = Evaluate(expr.Right);
        if (left is IntegerOrBool liob && right is IntegerOrBool riob)
            return new IntegerOrBool((int)liob == (int)riob);
        if (left is string sl && right is string sr)
            return new IntegerOrBool(sl == sr);
        throw new RuntimeErrorException(new RuntimeError(expr.Location,
            $"Incompatible types for '==': {left?.GetType()} and {right?.GetType()}"));
    }

    public object VisitBangEqualExpr(BangEqualExpr expr)
    {
        return new IntegerOrBool(
            !((IntegerOrBool)VisitEqualEqualExpr(new EqualEqualExpr(expr.Left, expr.Right, expr.Location))));
    }

    private IntegerOrBool PerformNumericComparison(BinaryExpr expr, Func<int, int, bool> op, string s)
    {
        return new IntegerOrBool(op(ConvertToInt(Evaluate(expr.Left), expr.Left.Location, $"left op of '{s}'"),
            ConvertToInt(Evaluate(expr.Right), expr.Right.Location, $"right op of '{s}'")));
    }

    public object VisitGreaterExpr(GreaterExpr expr)
    {
        return PerformNumericComparison(expr, (l, r) => l > r, ">");
    }

    public object VisitGreaterEqualExpr(GreaterEqualExpr expr)
    {
        return PerformNumericComparison(expr, (l, r) => l >= r, ">=");
    }

    public object VisitLessExpr(LessExpr expr)
    {
        return PerformNumericComparison(expr, (l, r) => l < r, "<");
    }

    public object VisitLessEqualExpr(LessEqualExpr expr)
    {
        return PerformNumericComparison(expr, (l, r) => l <= r, "<=");
    }

    public object VisitAndExpr(AndExpr expr)
    {
        var left = ConvertToBool(Evaluate(expr.Left), expr.Left.Location, "left op of 'and'");
        if (!left) return new IntegerOrBool(false);
        return new IntegerOrBool(ConvertToBool(Evaluate(expr.Right), expr.Right.Location, "right op of 'and'"));
    }

    public object VisitOrExpr(OrExpr expr)
    {
        var left = ConvertToBool(Evaluate(expr.Left), expr.Left.Location, "left op of 'or'");
        if (left)
            return new IntegerOrBool(true);
        return new IntegerOrBool(ConvertToBool(Evaluate(expr.Right), expr.Right.Location, "right op of 'or'"));
    }

    public object VisitGroupExpr(GroupExpr expr)
    {
        return Evaluate(expr.Expr);
    }

    public object VisitVariableExpr(VariableExpr expr)
    {
        if (_environment.TryGetValue(expr.Name, out var v)) return v;
        throw new RuntimeErrorException(new RuntimeError(expr.Location, $"Variable '{expr.Name}' not defined."));
    }

    public object VisitCallExpr(CallExpr expr)
    {
        var args = expr.Arguments.Select(arg => Evaluate(arg)).ToList();
        return expr.CalledFunction switch
        {
            "GetActualX" => new IntegerOrBool(_wallEState.Position.X),
            "GetActualY" => new IntegerOrBool(_wallEState.Position.Y),
            "GetCanvasSize" => new IntegerOrBool(_canvas.Width),
            _ => CallComplexFunction(expr, args)
        };
    }

    private object CallComplexFunction(CallExpr e, List<object> a)
    {
        return e.CalledFunction switch
        {
            "IsBrushColor" => WallEColor.TryParse(ConvertToString(a[0], e.Arguments[0].Location), out var c)
                ? new IntegerOrBool(_wallEState.Color == c)
                : throw new RuntimeErrorException(new RuntimeError(e.Arguments[0].Location, "Invalid color string.")),
            "IsBrushSize" => new IntegerOrBool(_wallEState.BrushSize == ConvertToInt(a[0], e.Arguments[0].Location)),
            "IsCanvasColor" => IsCanvasColorImpl(e, a),
            "GetColorCount" => GetColorCountImpl(e, a), 
            "GetRandomInt" => GetRandomIntImpl(e, a),
            _ => throw new RuntimeErrorException(new RuntimeError(e.Location,
                $"Function '{e.CalledFunction}' not defined."))
        };
    }

    private object GetRandomIntImpl(CallExpr callExpr, List<object> args)
    {
        var min = ConvertToInt(args[0], callExpr.Arguments[0].Location);
        var max = ConvertToInt(args[1], callExpr.Arguments[1].Location);
        if (min > max)
            throw new RuntimeErrorException(new RuntimeError(callExpr.Arguments[0].Location,
                "Minimum value must be less than maximum value."));
        return new IntegerOrBool(new Random().Next(min, max + 1));
    }

    private object IsCanvasColorImpl(CallExpr callExpr, List<object> args)
    {
        if (!WallEColor.TryParse(ConvertToString(args[0], callExpr.Arguments[0].Location), out var c))
            throw new RuntimeErrorException(new RuntimeError(callExpr.Arguments[0].Location, "Invalid color string."));
        var (x, y) = (ConvertToInt(args[1], callExpr.Arguments[1].Location), ConvertToInt(args[2], callExpr.Arguments[2].Location));
        if (!_canvas.IsInBounds(x, y)) return new IntegerOrBool(false);
        var pixelColor = _canvas.GetPixel(x, y);
        return new IntegerOrBool(pixelColor == c);
    }

    private object GetColorCountImpl(CallExpr callExpr, List<object> args)
    {
        if (!WallEColor.TryParse(ConvertToString(args[0], callExpr.Arguments[0].Location), out var c))
            throw new RuntimeErrorException(new RuntimeError(callExpr.Arguments[0].Location, "Invalid color string."));
        var (x1, y1, x2, y2) = (ConvertToInt(args[1], callExpr.Arguments[1].Location),
            ConvertToInt(args[2], callExpr.Arguments[2].Location), ConvertToInt(args[3], callExpr.Arguments[3].Location),
            ConvertToInt(args[4], callExpr.Arguments[4].Location));
        var (startX, endX) = (Math.Min(x1, x2), Math.Max(x1, x2));
        var (startY, endY) = (Math.Min(y1, y2), Math.Max(y1, y2));
        var count = 0;
        for (var ix = startX; ix <= endX; ix++)
        for (var iy = startY; iy <= endY; iy++)
            if (_canvas.IsInBounds(ix, iy) && _canvas.GetPixel(ix, iy) == c)
                count++;

        return new IntegerOrBool(count);
    }

    #endregion

    #region Helpers

    private object Evaluate(Expr expr)
    {
        return expr.Accept(this) ?? throw new InvalidOperationException("Evaluation resulted in null.");
    }

    private int ConvertToInt(object v, CodeLocation l, string c = "value")
    {
        return v is IntegerOrBool iob
            ? iob
            : throw new RuntimeErrorException(new RuntimeError(l!,
                $"Expected integer for {c}, got {v?.GetType().Name ?? "null"}."));
    }

    private bool ConvertToBool(object v, CodeLocation l, string c = "value")
    {
        return v is IntegerOrBool iob
            ? iob
            : throw new RuntimeErrorException(new RuntimeError(l,
                $"Expected boolean for {c}, got {v?.GetType().Name ?? "null"}."));
    }

    private string ConvertToString(object v, CodeLocation l, string c = "value")
    {
        return v is string s
            ? s
            : throw new RuntimeErrorException(new RuntimeError(l,
                $"Expected string for {c}, got {v?.GetType().Name ?? "null"}."));
    }

    private string ConvertValToString(object val, CodeLocation l)
    {
        if (val is string s) return s;
        if (val is IntegerOrBool iob)
        {
            if (iob.Value is int i) return i.ToString();
            if (iob.Value is bool b) return b.ToString().ToLowerInvariant();
        }

        throw new RuntimeErrorException(new RuntimeError(l,
            $"Cannot convert {val?.GetType().Name ?? "null"} to string."));
    }

    #endregion
}