using PixelWallE.Core.Common;
using PixelWallE.Core.Drawing;
using PixelWallE.Core.Errors;
using PixelWallE.Core.Parsers.AST;
using PixelWallE.Core.Parsers.AST.Exprs;
using PixelWallE.Core.Parsers.AST.Stmts;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace PixelWallE.Core.Interpreters;

public class Interpreter : IInterpreter, IVisitor<object?>
{
    #region Fields and State
    private readonly List<Error> _errors = new();
    private Dictionary<string, object> _environment = new();
    private SKBitmap _skBitmap;
    private SKCanvas _skCanvas;
    private SKPaint _skPaint;
    private SKPoint _currentWallEPosition;
    private WallEColor _currentColor;
    private int _currentSize;
    private bool _isFilling = false;
    private readonly Dictionary<(int x, int y), SKColor> _currentDraw = new();
    private Dictionary<string, int> _labelIndexMap = new();
    private int _statementPointer = 0;
    private IReadOnlyList<Stmt> _programStatements = new List<Stmt>().AsReadOnly();

    private int _totalStatements = 0;
    private const int MAX_TOTAL_STATEMENTS = 50000;

    private readonly int _executionDelay;
    private readonly ExecutionMode _executionMode; // Campo añadido
    #endregion

    #region Constructors
    public Interpreter(int width, int height, int executionDelay = 0, ExecutionMode executionMode = ExecutionMode.Instant)
    {
        _executionDelay = executionDelay;
        _executionMode = executionMode; // Parámetro asignado
        _skBitmap = new SKBitmap(width, height);
        using (var tempCanvas = new SKCanvas(_skBitmap)) { tempCanvas.Clear(SKColors.White); }
        _skCanvas = new SKCanvas(_skBitmap);
        _skPaint = InitializePaint();
    }

    public Interpreter(SKBitmap existingBitmap, int executionDelay = 0, ExecutionMode executionMode = ExecutionMode.Instant)
    {
        _executionDelay = executionDelay;
        _executionMode = executionMode; // Parámetro asignado
        _skBitmap = existingBitmap.Copy();
        _skCanvas = new SKCanvas(_skBitmap);
        _skPaint = InitializePaint();
    }

    private SKPaint InitializePaint()
    {
        _currentWallEPosition = new SKPoint(0, 0);
        _currentColor = new WallEColor(0, 0, 0);
        _currentSize = 1;
        return new SKPaint { IsAntialias = false, StrokeWidth = _currentSize, Color = ToSkiaColor(_currentColor) };
    }
    #endregion

    public async Task InterpretAsync(ProgramStmt program, IProgress<DrawingUpdate> progress, CancellationToken cancellationToken = default)
    {
        InitializeForRun(program);

        try
        {
            _statementPointer = 0;
            _totalStatements = 0;

            while (_statementPointer < _programStatements.Count)
            {
                cancellationToken.ThrowIfCancellationRequested();

                _totalStatements++;
                if (_totalStatements > MAX_TOTAL_STATEMENTS)
                {
                    var error = new RuntimeError(new CodeLocation(0, 0), "Execution limit reached (possible infinite loop).");
                    progress.Report(new DrawingUpdate(
                        _skBitmap.Copy(),
                        DrawingUpdateType.Error,
                        error.Message, new[] { error }));
                    return;
                }

                var stmt = _programStatements[_statementPointer];
                _statementPointer++;

                await ExecuteStatementWithProgressAsync(stmt, progress, cancellationToken);

                await Task.Yield();
            }

            progress.Report(new DrawingUpdate(_skBitmap.Copy(), DrawingUpdateType.Complete, "Execution completed successfully", _errors.AsReadOnly()));
        }
        catch (RuntimeErrorException rex)
        {
            _errors.Add(rex.runtimeError);
            progress.Report(new DrawingUpdate(_skBitmap.Copy(), DrawingUpdateType.Error, rex.runtimeError.Message, _errors.AsReadOnly()));
        }
        catch (OperationCanceledException)
        {
            progress.Report(new DrawingUpdate(_skBitmap.Copy(), DrawingUpdateType.Complete, "Execution cancelled"));
        }
        catch (Exception ex)
        {
            var loc = _statementPointer > 0 ? _programStatements[_statementPointer - 1].Location : new CodeLocation(1, 1);
            var error = new RuntimeError(loc, $"Unexpected error: {ex.Message}");
            _errors.Add(error);
            progress.Report(new DrawingUpdate(_skBitmap.Copy(), DrawingUpdateType.Error, ex.Message, _errors.AsReadOnly()));
        }
    }

    private void InitializeForRun(ProgramStmt program)
    {
        _environment.Clear();
        _errors.Clear();
        _labelIndexMap.Clear();
        _programStatements = program.Statements;

        for (var i = 0; i < _programStatements.Count; i++)
        {
            if (_programStatements[i] is LabelStmt labelStmt)
            {
                _labelIndexMap.TryAdd(labelStmt.Label, i);
            }
        }
    }

    #region Execution and Drawing Logic

    private async Task ExecuteStatementWithProgressAsync(Stmt stmt, IProgress<DrawingUpdate> progress, CancellationToken cancellationToken)
    {
        stmt.Accept(this);

        if (stmt is DrawLineStmt or DrawCircleStmt or DrawRectangleStmt)
        {
            await EmitDrawingProgressAsync(progress, cancellationToken);
        }
        else if (stmt is FillStmt fillStmt)
        {
            await EmitFillProgressAsync(fillStmt, progress, cancellationToken);
        }
    }

    private async Task EmitDrawingProgressAsync(IProgress<DrawingUpdate> progress, CancellationToken cancellationToken)
    {
        int pixelCount = 0;
        foreach (var pixel in _currentDraw)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var (pos, color) = (pixel.Key, pixel.Value);
            if (IsInBounds(pos.x, pos.y))
            {
                DrawPixel(pos.x, pos.y, color);
                pixelCount++;

                // Lógica de actualización y delay por modo de ejecución
                if (_executionMode == ExecutionMode.PixelByPixel)
                {
                    progress.Report(new DrawingUpdate(_skBitmap.Copy(), DrawingUpdateType.Pixel));
                    if (_executionDelay > 0)
                    {
                        await Task.Delay(_executionDelay, cancellationToken);
                    }
                }
                else if (_executionMode == ExecutionMode.StepByStep)
                {
                    if (pixelCount % 10 == 0) // Actualizar visualmente cada 10 píxeles para performance
                    {
                        progress.Report(new DrawingUpdate(_skBitmap.Copy(), DrawingUpdateType.Pixel));
                        await Task.Yield();
                    }
                }
            }
        }

        // Reportar que el paso (comando) se ha completado
        progress.Report(new DrawingUpdate(_skBitmap.Copy(), DrawingUpdateType.Step));

        // Aplicar delay al final del paso solo en modo StepByStep
        if (_executionMode == ExecutionMode.StepByStep && _executionDelay > 0)
        {
            await Task.Delay(_executionDelay, cancellationToken);
        }

        _currentDraw.Clear();
    }

    private async Task EmitFillProgressAsync(FillStmt stmt, IProgress<DrawingUpdate> progress, CancellationToken cancellationToken)
    {
        if (!IsInBounds((int)_currentWallEPosition.X, (int)_currentWallEPosition.Y)) return;

        var targetSkColor = _skBitmap.GetPixel((int)_currentWallEPosition.X, (int)_currentWallEPosition.Y);
        var fillSkColor = BlendColors(targetSkColor, ToSkiaColor(_currentColor));

        if (targetSkColor == fillSkColor)
        {
            progress.Report(new DrawingUpdate(_skBitmap.Copy(), DrawingUpdateType.Step));
            return;
        }

        var pixelsToProcess = new Queue<SKPoint>();
        pixelsToProcess.Enqueue(_currentWallEPosition);
        var visited = new HashSet<SKPoint>();

        int processedCount = 0;

        while (pixelsToProcess.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var currentPixel = pixelsToProcess.Dequeue();
            if (!visited.Add(currentPixel)) continue;

            var (x, y) = ((int)currentPixel.X, (int)currentPixel.Y);

            if (IsInBounds(x, y) && _skBitmap.GetPixel(x, y) == targetSkColor)
            {
                _skBitmap.SetPixel(x, y, fillSkColor);

                processedCount++;
                // Lógica de actualización y delay por modo de ejecución
                if (_executionMode == ExecutionMode.PixelByPixel)
                {
                    if (processedCount % 5 == 0) // Optimización para no ahogar la UI
                    {
                        progress.Report(new DrawingUpdate(_skBitmap.Copy(), DrawingUpdateType.Pixel));
                        if (_executionDelay > 0)
                        {
                            await Task.Delay(_executionDelay, cancellationToken);
                        }
                        else
                        {
                            await Task.Yield();
                        }
                    }
                }
                else if (_executionMode == ExecutionMode.StepByStep)
                {
                    if (processedCount % 50 == 0) // Actualización menos frecuente
                    {
                        progress.Report(new DrawingUpdate(_skBitmap.Copy(), DrawingUpdateType.Pixel));
                        await Task.Yield();
                    }
                }

                pixelsToProcess.Enqueue(new SKPoint(x, y - 1));
                pixelsToProcess.Enqueue(new SKPoint(x, y + 1));
                pixelsToProcess.Enqueue(new SKPoint(x - 1, y));
                pixelsToProcess.Enqueue(new SKPoint(x + 1, y));
            }
        }

        progress.Report(new DrawingUpdate(_skBitmap.Copy(), DrawingUpdateType.Step));

        // Aplicar delay al final del paso solo en modo StepByStep
        if (_executionMode == ExecutionMode.StepByStep && _executionDelay > 0)
        {
            await Task.Delay(_executionDelay, cancellationToken);
        }
    }
    #endregion

    #region Statement and Expression Visitors (IVisitor Implementation)
    // ESTA SECCIÓN PERMANECE IGUAL
    public object? VisitProgramStmt(ProgramStmt stmt) => null;
    public object? VisitExpressionStmt(ExpressionStmt stmt) { Evaluate(stmt.Expr); return null; }
    public object? VisitSpawnStmt(SpawnStmt stmt)
    {
        var x = ConvertToInt(Evaluate(stmt.X), stmt.X.Location, nameof(stmt.X));
        var y = ConvertToInt(Evaluate(stmt.Y), stmt.Y.Location, nameof(stmt.Y));
        if (!IsInBounds(x, y)) throw new RuntimeErrorException(new RuntimeError(stmt.Location, $"Spawn coordinates ({x},{y}) out of canvas range ({_skBitmap.Width}x{_skBitmap.Height})."));
        _currentWallEPosition = new SKPoint(x, y);
        return null;
    }
    public object? VisitColorStmt(ColorStmt stmt)
    {
        var colorStr = ConvertToString(Evaluate(stmt.ColorExpr), stmt.ColorExpr.Location, "Color argument");
        if (WallEColor.TryParse(colorStr, out var newColor)) { _currentColor = newColor; _skPaint.Color = ToSkiaColor(_currentColor); }
        else { throw new RuntimeErrorException(new RuntimeError(stmt.ColorExpr.Location, $"Invalid color string '{colorStr}'.")); }
        return null;
    }
    public object? VisitSizeStmt(SizeStmt stmt)
    {
        _currentSize = ConvertToInt(Evaluate(stmt.SizeExpr), stmt.SizeExpr.Location, "Size argument");
        if (_currentSize % 2 == 0) _currentSize--;
        if (_currentSize <= 0) _currentSize = 1;
        _skPaint.StrokeWidth = _currentSize;
        return null;
    }
    public object? VisitDrawLineStmt(DrawLineStmt stmt)
    {
        var dirX = ConvertToInt(Evaluate(stmt.DirX), stmt.DirX.Location, "DirX");
        var dirY = ConvertToInt(Evaluate(stmt.DirY), stmt.DirY.Location, "DirY");
        var distance = ConvertToInt(Evaluate(stmt.Distance), stmt.Distance.Location, "Distance");
        if (Math.Abs(dirX) > 1 || Math.Abs(dirY) > 1 || (dirX == 0 && dirY == 0 && distance != 0)) throw new RuntimeErrorException(new RuntimeError(stmt.Location, "Invalid direction for DrawLine."));
        var color = ToSkiaColor(_currentColor);
        var lastX = (int)_currentWallEPosition.X;
        var lastY = (int)_currentWallEPosition.Y;
        for (var i = 0; i < distance; i++)
        {
            lastX = (int)_currentWallEPosition.X + dirX * i;
            lastY = (int)_currentWallEPosition.Y + dirY * i;
            DrawBrush(lastX, lastY, color);
        }
        _currentWallEPosition = new SKPoint(lastX, lastY);
        return null;
    }
    public object? VisitDrawCircleStmt(DrawCircleStmt stmt)
    {
        var dirX = ConvertToInt(Evaluate(stmt.DirX), stmt.DirX.Location, "DirX");
        var dirY = ConvertToInt(Evaluate(stmt.DirY), stmt.DirY.Location, "DirY");
        var radius = ConvertToInt(Evaluate(stmt.Radius), stmt.Radius.Location, "Radius");
        if (radius <= 0) throw new RuntimeErrorException(new RuntimeError(stmt.Radius.Location, "Radius must be positive."));
        if (Math.Abs(dirX) > 1 || Math.Abs(dirY) > 1) throw new RuntimeErrorException(new RuntimeError(stmt.DirX.Location, "DirX/DirY offset must be -1, 0, or 1."));
        var circleCenter = new SKPoint(_currentWallEPosition.X + dirX * radius, _currentWallEPosition.Y + dirY * radius);
        if (_isFilling) FillCircle((int)circleCenter.X, (int)circleCenter.Y, radius); else DrawCircle((int)circleCenter.X, (int)circleCenter.Y, radius);
        _currentWallEPosition = circleCenter;
        return null;
    }
    public object? VisitDrawRectangleStmt(DrawRectangleStmt stmt)
    {
        var dirX = ConvertToInt(Evaluate(stmt.DirX), stmt.DirX.Location, "DirX");
        var dirY = ConvertToInt(Evaluate(stmt.DirY), stmt.DirY.Location, "DirY");
        var distance = ConvertToInt(Evaluate(stmt.Distance), stmt.Distance.Location, "Distance");
        var width = ConvertToInt(Evaluate(stmt.Width), stmt.Width.Location, "Width");
        var height = ConvertToInt(Evaluate(stmt.Height), stmt.Height.Location, "Height");
        if (width <= 0 || height <= 0) throw new RuntimeErrorException(new RuntimeError(stmt.Location, "Width and Height must be positive."));
        if (Math.Abs(dirX) > 1 || Math.Abs(dirY) > 1 || (dirX == 0 && dirY == 0 && distance != 0)) throw new RuntimeErrorException(new RuntimeError(stmt.Location, "Invalid direction."));
        var topLeft = new SKPoint(_currentWallEPosition.X + dirX * distance, _currentWallEPosition.Y + dirY * distance);
        var startX = (int)topLeft.X;
        var startY = (int)topLeft.Y;
        if (_isFilling) FilledRectangle(startX, startY, width, height); else DrawRectangle(startX, startY, width, height);
        _currentWallEPosition = new SKPoint(startX + width / 2, startY + height / 2);
        return null;
    }
    public object? VisitFillStmt(FillStmt stmt) => null;
    public object? VisitAssignStmt(AssignStmt stmt) { _environment[stmt.Name] = Evaluate(stmt.Value); return null; }
    public object? VisitLabelStmt(LabelStmt stmt) => null;
    public object? VisitGoToStmt(GoToStmt stmt)
    {
        var condition = ConvertToBool(Evaluate(stmt.Condition), stmt.Condition.Location, "GoTo condition");
        if (condition && _labelIndexMap.TryGetValue(stmt.Label, out var targetIndex)) _statementPointer = targetIndex;
        return null;
    }
    public object VisitLiteralExpr(LiteralExpr expr) => expr.Value.Value ?? throw new RuntimeErrorException(new RuntimeError(expr.Location, "Literal value is null."));
    public object VisitBangExpr(BangExpr expr) => new IntegerOrBool(!ConvertToBool(Evaluate(expr.Right), expr.Right.Location, "operand of !"));
    public object VisitMinusExpr(MinusExpr expr) => new IntegerOrBool(-ConvertToInt(Evaluate(expr.Right), expr.Right.Location, "operand of negation -"));
    public object VisitAddExpr(AddExpr expr)
    {
        var leftVal = Evaluate(expr.Left); var rightVal = Evaluate(expr.Right);
        if (leftVal is string || rightVal is string) return ConvertValToString(leftVal, expr.Left.Location) + ConvertValToString(rightVal, expr.Right.Location);
        return new IntegerOrBool(ConvertToInt(leftVal, expr.Left.Location, "left operand of +") + ConvertToInt(rightVal, expr.Right.Location, "right operand of +"));
    }
    public object VisitSubtractExpr(SubtractExpr expr) => new IntegerOrBool(ConvertToInt(Evaluate(expr.Left), expr.Left.Location, "left operand of -") - ConvertToInt(Evaluate(expr.Right), expr.Right.Location, "right operand of -"));
    public object VisitMultiplyExpr(MultiplyExpr expr)
    {
        var leftVal = Evaluate(expr.Left); var rightVal = Evaluate(expr.Right);
        if (leftVal is string sVal) { var count = ConvertToInt(rightVal, expr.Right.Location, "multiplier"); if (count < 0) throw new RuntimeErrorException(new RuntimeError(expr.Right.Location, "Negative repetition count.")); return string.Concat(Enumerable.Repeat(sVal, count)); }
        if (rightVal is string sRVal) { var count = ConvertToInt(leftVal, expr.Left.Location, "multiplier"); if (count < 0) throw new RuntimeErrorException(new RuntimeError(expr.Left.Location, "Negative repetition count.")); return string.Concat(Enumerable.Repeat(sRVal, count)); }
        return new IntegerOrBool(ConvertToInt(leftVal, expr.Left.Location, "left operand of *") * ConvertToInt(rightVal, expr.Right.Location, "right operand of *"));
    }
    public object VisitDivideExpr(DivideExpr expr)
    {
        var right = ConvertToInt(Evaluate(expr.Right), expr.Right.Location, "divisor"); if (right == 0) throw new RuntimeErrorException(new RuntimeError(expr.Right.Location, "Division by zero."));
        var left = ConvertToInt(Evaluate(expr.Left), expr.Left.Location, "dividend"); return new IntegerOrBool(left / right);
    }
    public object VisitPowerExpr(PowerExpr expr)
    {
        var numBase = ConvertToInt(Evaluate(expr.Left), expr.Left.Location, "base"); var exponent = ConvertToInt(Evaluate(expr.Right), expr.Right.Location, "exponent");
        if (exponent < 0) throw new RuntimeErrorException(new RuntimeError(expr.Right.Location, "Exponent must be non-negative."));
        try { return new IntegerOrBool((int)Math.Pow(numBase, exponent)); } catch (OverflowException) { throw new RuntimeErrorException(new RuntimeError(expr.Location, "Power operation result too large.")); }
    }
    public object VisitModuloExpr(ModuloExpr expr)
    {
        var right = ConvertToInt(Evaluate(expr.Right), expr.Right.Location, "divisor"); if (right == 0) throw new RuntimeErrorException(new RuntimeError(expr.Right.Location, "Modulo by zero."));
        var left = ConvertToInt(Evaluate(expr.Left), expr.Left.Location, "dividend"); return new IntegerOrBool(left % right);
    }
    public object VisitEqualEqualExpr(EqualEqualExpr expr)
    {
        var left = Evaluate(expr.Left); var right = Evaluate(expr.Right);
        if (left is IntegerOrBool liob && right is IntegerOrBool riob) return new IntegerOrBool((int)liob == (int)riob);
        if (left is string sl && right is string sr) return new IntegerOrBool(sl == sr);
        throw new RuntimeErrorException(new RuntimeError(expr.Location, $"Incompatible types for '==': {left?.GetType()} and {right?.GetType()}"));
    }
    public object VisitBangEqualExpr(BangEqualExpr expr) => new IntegerOrBool(!((IntegerOrBool)VisitEqualEqualExpr(new EqualEqualExpr(expr.Left, expr.Right, expr.Location))));
    private IntegerOrBool PerformNumericComparison(BinaryExpr expr, Func<int, int, bool> op, string s) => new IntegerOrBool(op(ConvertToInt(Evaluate(expr.Left), expr.Left.Location, $"left op of '{s}'"), ConvertToInt(Evaluate(expr.Right), expr.Right.Location, $"right op of '{s}'")));
    public object VisitGreaterExpr(GreaterExpr expr) => PerformNumericComparison(expr, (l, r) => l > r, ">");
    public object VisitGreaterEqualExpr(GreaterEqualExpr expr) => PerformNumericComparison(expr, (l, r) => l >= r, ">=");
    public object VisitLessExpr(LessExpr expr) => PerformNumericComparison(expr, (l, r) => l < r, "<");
    public object VisitLessEqualExpr(LessEqualExpr expr) => PerformNumericComparison(expr, (l, r) => l <= r, "<=");
    public object VisitAndExpr(AndExpr expr) { var left = ConvertToBool(Evaluate(expr.Left), expr.Left.Location, "left op of 'and'"); if (!left) return new IntegerOrBool(false); return new IntegerOrBool(ConvertToBool(Evaluate(expr.Right), expr.Right.Location, "right op of 'and'")); }
    public object VisitOrExpr(OrExpr expr) { var left = ConvertToBool(Evaluate(expr.Left), expr.Left.Location, "left op of 'or'"); if (left) return new IntegerOrBool(true); return new IntegerOrBool(ConvertToBool(Evaluate(expr.Right), expr.Right.Location, "right op of 'or'")); }
    public object VisitGroupExpr(GroupExpr expr) => Evaluate(expr.Expr);
    public object VisitVariableExpr(VariableExpr expr) { if (_environment.TryGetValue(expr.Name, out var v)) return v; throw new RuntimeErrorException(new RuntimeError(expr.Location, $"Variable '{expr.Name}' not defined.")); }
    public object VisitCallExpr(CallExpr expr)
    {
        var args = expr.Arguments.Select(arg => Evaluate(arg)).ToList();
        return expr.CalledFunction switch { "GetActualX" => new IntegerOrBool((int)_currentWallEPosition.X), "GetActualY" => new IntegerOrBool((int)_currentWallEPosition.Y), "GetCanvasSize" => new IntegerOrBool(_skBitmap.Width), _ => CallComplexFunction(expr, args) };
    }
    private object CallComplexFunction(CallExpr e, List<object> a) => e.CalledFunction switch
    {
        "IsBrushColor" => WallEColor.TryParse(ConvertToString(a[0], e.Arguments[0].Location), out var c) ? new IntegerOrBool(_currentColor == c) : throw new RuntimeErrorException(new RuntimeError(e.Arguments[0].Location, "Invalid color string.")),
        "IsBrushSize" => new IntegerOrBool(_currentSize == ConvertToInt(a[0], e.Arguments[0].Location)),
        "IsCanvasColor" => IsCanvasColorImpl(e, a),
        "GetColorCount" => GetColorCountImpl(e, a),
        _ => throw new RuntimeErrorException(new RuntimeError(e.Location, $"Function '{e.CalledFunction}' not defined."))
    };
    private object IsCanvasColorImpl(CallExpr e, List<object> a)
    {
        if (!WallEColor.TryParse(ConvertToString(a[0], e.Arguments[0].Location), out var c)) throw new RuntimeErrorException(new RuntimeError(e.Arguments[0].Location, "Invalid color string."));
        var (x, y) = (ConvertToInt(a[1], e.Arguments[1].Location), ConvertToInt(a[2], e.Arguments[2].Location));
        if (!IsInBounds(x, y)) return new IntegerOrBool(false);
        var p = _skBitmap.GetPixel(x, y); return new IntegerOrBool(new WallEColor(p.Red, p.Green, p.Blue, p.Alpha) == c);
    }
    private object GetColorCountImpl(CallExpr e, List<object> a)
    {
        if (!WallEColor.TryParse(ConvertToString(a[0], e.Arguments[0].Location), out var c)) throw new RuntimeErrorException(new RuntimeError(e.Arguments[0].Location, "Invalid color string."));
        var (x1, y1, x2, y2) = (ConvertToInt(a[1], e.Arguments[1].Location), ConvertToInt(a[2], e.Arguments[2].Location), ConvertToInt(a[3], e.Arguments[3].Location), ConvertToInt(a[4], e.Arguments[4].Location));
        var (startX, endX) = (Math.Min(x1, x2), Math.Max(x1, x2)); var (startY, endY) = (Math.Min(y1, y2), Math.Max(y1, y2));
        int count = 0; var tc = ToSkiaColor(c);
        for (int ix = startX; ix <= endX; ix++) for (int iy = startY; iy <= endY; iy++) if (IsInBounds(ix, iy) && _skBitmap.GetPixel(ix, iy) == tc) count++;
        return new IntegerOrBool(count);
    }
    #endregion

    #region Drawing and Conversion Helpers
    private void DrawPixel(int x, int y, SKColor color) => _skBitmap.SetPixel(x, y, color);
    private void DrawBrush(int cx, int cy, SKColor color) { for (int x = cx - _currentSize / 2; x <= cx + _currentSize / 2; x++) for (int y = cy - _currentSize / 2; y <= cy + _currentSize / 2; y++) if (IsInBounds(x, y)) _currentDraw.TryAdd((x, y), BlendColors(_skBitmap.GetPixel(x, y), color)); }
    private void DrawCircle(int cX, int cY, int r) { var x = 0; var y = r; var d = 3 - 2 * r; var c = ToSkiaColor(_currentColor); while (y >= x) { DrawBrush(cX + x, cY + y, c); DrawBrush(cX - x, cY + y, c); DrawBrush(cX + x, cY - y, c); DrawBrush(cX - x, cY - y, c); DrawBrush(cX + y, cY + x, c); DrawBrush(cX - y, cY + x, c); DrawBrush(cX + y, cY - x, c); DrawBrush(cX - y, cY - x, c); x++; if (d > 0) { y--; d = d + 4 * (x - y) + 10; } else { d = d + 4 * x + 6; } } }
    private void DrawRectangle(int x, int y, int w, int h) { var c = ToSkiaColor(_currentColor); for (var i = 0; i < w; i++) { DrawBrush(x + i, y, c); DrawBrush(x + i, y + h - 1, c); } for (var i = 1; i < h - 1; i++) { DrawBrush(x, y + i, c); DrawBrush(x + w - 1, y + i, c); } }
    private void FilledRectangle(int x, int y, int w, int h) { var c = ToSkiaColor(_currentColor); for (var j = 0; j < h; j++) for (var i = 0; i < w; i++) DrawBrush(x + i, y + j, c); }
    private void FillCircle(int cX, int cY, int r) { var c = ToSkiaColor(_currentColor); for (int y = -r; y <= r; y++) for (int x = -r; x <= r; x++) if (x * x + y * y <= r * r) DrawBrush(cX + x, cY + y, c); }
    private object Evaluate(Expr expr) => expr.Accept(this) ?? throw new InvalidOperationException("Evaluation resulted in null.");
    private SKColor ToSkiaColor(WallEColor c) => new SKColor(c.Red, c.Green, c.Blue, c.Alpha);
    private bool IsInBounds(int x, int y) => x >= 0 && x < _skBitmap.Width && y >= 0 && y < _skBitmap.Height;
    private SKColor BlendColors(SKColor canvas, SKColor brush) { if (brush.Alpha == 255) return brush; if (brush.Alpha == 0) return canvas; var r = (brush.Red * brush.Alpha + canvas.Red * (255 - brush.Alpha)) / 255; var g = (brush.Green * brush.Alpha + canvas.Green * (255 - brush.Alpha)) / 255; var b = (brush.Blue * brush.Alpha + canvas.Blue * (255 - brush.Alpha)) / 255; return new SKColor((byte)r, (byte)g, (byte)b); }
    private int ConvertToInt(object v, CodeLocation l, string c = "v") => v is IntegerOrBool iob ? iob : throw new RuntimeErrorException(new RuntimeError(l, $"Expected integer for {c}, got {v?.GetType().Name ?? "null"}."));
    private bool ConvertToBool(object v, CodeLocation l, string c = "v") => v is IntegerOrBool iob ? iob : throw new RuntimeErrorException(new RuntimeError(l, $"Expected boolean for {c}, got {v?.GetType().Name ?? "null"}."));
    private string ConvertToString(object v, CodeLocation l, string c = "v") => v is string s ? s : throw new RuntimeErrorException(new RuntimeError(l, $"Expected string for {c}, got {v?.GetType().Name ?? "null"}."));
    private string ConvertValToString(object val, CodeLocation loc) { if (val is string s) return s; if (val is IntegerOrBool iob) { if (iob.Value is int i) return i.ToString(); if (iob.Value is bool b) return b.ToString().ToLowerInvariant(); } throw new RuntimeErrorException(new RuntimeError(loc, $"Cannot convert {val?.GetType().Name ?? "null"} to string.")); }
    #endregion
}