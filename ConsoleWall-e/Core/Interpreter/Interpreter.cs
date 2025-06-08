using ConsoleWall_e.Core.Common;
using ConsoleWall_e.Core.Errors;
using ConsoleWall_e.Core.Parser;
using ConsoleWall_e.Core.Parser.AST;
using ConsoleWall_e.Core.Parser.AST.Exprs;
using ConsoleWall_e.Core.Parser.AST.Stmts;
using SkiaSharp;
using System.Drawing;

namespace ConsoleWall_e.Core.Interpreter;

public class RuntimeErrorException(RuntimeError error) : Exception(error.Message)
{
    public RuntimeError runtimeError { get; } = error;
}

public class Interpreter : IInterpreter, IVisitor<object?>
{
    private readonly List<Error> _errors = new();
    private Dictionary<string, object> _environment = new();

    private SKBitmap _skBitmap;
    private SKCanvas _skCanvas;
    private SKPaint _skPaint;

    private Point _currentWallEPosition;
    private WallEColor _currentColor = new(0, 0, 0);
    private int _currentSize = 1;
    private string _outputFilePath;
    private bool _isFilling = false;

    private Dictionary<string, int> _labelIndexMap = new();
    private int _statementPointer = 0;
    private IReadOnlyList<Stmt> _programStatements = new List<Stmt>().AsReadOnly();

    public Interpreter(string outputFilePath = "output.png", string? loadImagePath = null, int defaultWidth = 500,
        int defaultHeight = 500)
    {
        _outputFilePath = outputFilePath;
        var imageLoadedSuccessfully = false;

        if (!string.IsNullOrEmpty(loadImagePath) && File.Exists(loadImagePath))
        {
            try
            {
                _skBitmap = SKBitmap.Decode(loadImagePath);
                if (_skBitmap == null) throw new Exception($"Failed to decode image '{loadImagePath}'.");
                //Console.WriteLine($"Image loaded '{loadImagePath}' ({_skBitmap.Width}x{_skBitmap.Height}).");
                imageLoadedSuccessfully = true;
            }
            catch (Exception ex)
            {
                _errors.Add(new ImportError(
                    $"Error loading image '{loadImagePath}': {ex.Message}. Using default canvas."));
                _skBitmap = new SKBitmap(defaultWidth, defaultHeight);
            }
        }
        else
        {
            _skBitmap = new SKBitmap(defaultWidth, defaultHeight);
        }

        _skCanvas = new SKCanvas(_skBitmap);
        _skPaint = new SKPaint
        {
            IsAntialias = true,
            StrokeWidth = _currentSize,
            Color = ToSkiaColor(_currentColor)
        };

        if (!imageLoadedSuccessfully) _skCanvas.Clear(SKColors.White);
        _currentWallEPosition = new Point(0, 0);
    }

    public Result<object> Interpret(ProgramStmt program)
    {
        _environment.Clear();
        _currentColor = new WallEColor(0, 0, 0);
        _currentSize = 1;
        _isFilling = false; // Reset fill mode


        _skPaint.Color = ToSkiaColor(_currentColor);
        _skPaint.StrokeWidth = _currentSize;


        _labelIndexMap.Clear();
        _programStatements = program.Statements;

        for (var i = 0; i < _programStatements.Count; i++)
            if (_programStatements[i] is LabelStmt labelStmt)
                _labelIndexMap.Add(labelStmt.Label, i);

        _statementPointer = 0;
        try
        {
            while (_statementPointer < _programStatements.Count)
            {
                if (_errors.Any(e => e.Type == ErrorType.Runtime)) break;
                var currentStmt = _programStatements[_statementPointer];
                _statementPointer++;
                Execute(currentStmt);
            }
        }
        catch (RuntimeErrorException ex)
        {
            _errors.Add(ex.runtimeError);
        }
        catch (Exception ex)
        {
            var errorLocation = _statementPointer > 0 && _statementPointer <= _programStatements.Count
                ? _programStatements[_statementPointer - 1].Location
                : new CodeLocation(0, 0);
            _errors.Add(new RuntimeError(errorLocation,
                $"Unexpected runtime error: {ex.Message}\nStackTrace: {ex.StackTrace}"));
        }

        var hadRTErrors = _errors.Any(e => e.Type == ErrorType.Runtime);
        var hadIErrors = _errors.Any(e => e.Type == ErrorType.Import);
        var hadErrors = _errors.Any();


        try
        {
            // Save image using SkiaSharp
            using (var image = SKImage.FromBitmap(_skBitmap))
            using (var data = image.Encode(SKEncodedImageFormat.Png, 100)) // 100 is quality for PNG
            using (var stream = File.OpenWrite(_outputFilePath))
            {
                data.SaveTo(stream);
            }

            if (hadRTErrors)
                Console.WriteLine(
                    $"Interpretation completed with errors. Image saved with progress up to the error in '{_outputFilePath}'.");
            else if (hadIErrors)
                Console.WriteLine(
                    $"Interpretation completed. Image saved in '{_outputFilePath}'. Note: There was an error loading the initial image, a default canvas was used.");
            else
                Console.WriteLine($"Interpretation completed successfully. Image saved in '{_outputFilePath}'.");
        }
        catch (Exception ex)
        {
            _errors.Add(new ImportError($"Failed to save image to '{_outputFilePath}': {ex.Message}"));
        }
        finally
        {
            _skCanvas?.Dispose();
            _skBitmap?.Dispose();
            _skPaint?.Dispose();
        }

        if (_errors.Any()) return Result<object>.Failure(_errors);
        return Result<object>.Success(new object());
    }

    private void Execute(Stmt stmt)
    {
        stmt.Accept(this);
    }

    private object Evaluate(Expr expr)
    {
        return expr.Accept(this);
    }

    private SKColor ToSkiaColor(WallEColor c)
    {
        return new SKColor(c.Red, c.Green, c.Blue, c.Alpha);
    }

    private int ConvertToInt(object value, CodeLocation loc, string context = "valor")
    {
        if (value is IntegerOrBool iob) return iob;
        throw new RuntimeErrorException(new RuntimeError(loc,
            $"Expected integer for {context}, got {value?.GetType().Name ?? "null"}."));
    }

    private bool ConvertToBool(object value, CodeLocation loc, string context = "valor")
    {
        if (value is IntegerOrBool iob) return iob;
        throw new RuntimeErrorException(new RuntimeError(loc,
            $"Expected boolean for {context}, got {value?.GetType().Name ?? "null"}."));
    }

    private string ConvertToString(object value, CodeLocation loc, string context = "valor")
    {
        if (value is string s) return s;
        throw new RuntimeErrorException(new RuntimeError(loc,
            $"Expected string for {context}, got {value?.GetType().Name ?? "null"}."));
    }

    private string ConvertValToString(object val, CodeLocation loc)
    {
        if (val is string s) return s;
        if (val is IntegerOrBool iob)
        {
            if (iob.Value is int i) return i.ToString();
            if (iob.Value is bool b) return b.ToString().ToLowerInvariant();
        }

        throw new RuntimeErrorException(new RuntimeError(loc,
            $"Cannot convert {val?.GetType().Name ?? "null"} to string for concatenation."));
    }

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
        if (x < 0 || x >= _skBitmap.Width || y < 0 || y >= _skBitmap.Height)
            throw new RuntimeErrorException(new RuntimeError(stmt.Location, $"Spawn coordinates out of canvas range."));
        _currentWallEPosition = new Point(x, y);
        return null;
    }

    public object? VisitColorStmt(ColorStmt stmt)
    {
        var colorStr = ConvertToString(Evaluate(stmt.ColorExpr), stmt.ColorExpr.Location, "Color argument");
        if (WallEColor.TryParse(colorStr, out var newColor))
        {
            _currentColor = newColor;
            _skPaint.Color = ToSkiaColor(_currentColor);
        }
        else
        {
            throw new RuntimeErrorException(new RuntimeError(stmt.ColorExpr.Location,
                $"Invalid color string '{colorStr}'."));
        }

        return null;
    }

    public object? VisitSizeStmt(SizeStmt stmt)
    {
        _currentSize = ConvertToInt(Evaluate(stmt.SizeExpr), stmt.SizeExpr.Location, "Size argument");
        if (_currentSize % 2 == 0) _currentSize--;
        if (_currentSize <= 0)
            throw new RuntimeErrorException(
                new RuntimeError(stmt.SizeExpr.Location, "Size must be a positive integer."));
        _skPaint.StrokeWidth = _currentSize;
        return null;
    }

    public object? VisitDrawLineStmt(DrawLineStmt stmt)
    {
        var dirX = ConvertToInt(Evaluate(stmt.DirX), stmt.DirX.Location, "DirX");
        var dirY = ConvertToInt(Evaluate(stmt.DirY), stmt.DirY.Location, "DirY");
        var distance = ConvertToInt(Evaluate(stmt.Distance), stmt.Distance.Location, "Distance");

        if (Math.Abs(dirX) > 1 || Math.Abs(dirY) > 1 || (dirX == 0 && dirY == 0 && distance != 0))
            throw new RuntimeErrorException(new RuntimeError(stmt.Location, "Invalid direction for DrawLine."));

        var startSkPoint = new SKPoint(_currentWallEPosition.X, _currentWallEPosition.Y);
        var endSkPoint = new SKPoint(_currentWallEPosition.X + dirX * distance,
            _currentWallEPosition.Y + dirY * distance);

        _skPaint.Style = SKPaintStyle.Stroke;
        _skCanvas.DrawLine(startSkPoint, endSkPoint, _skPaint);

        _currentWallEPosition = new Point((int)endSkPoint.X, (int)endSkPoint.Y);
        return null;
    }

    public object? VisitDrawCircleStmt(DrawCircleStmt stmt)
    {
        var dirX = ConvertToInt(Evaluate(stmt.DirX), stmt.DirX.Location, "DirX for circle center");
        var dirY = ConvertToInt(Evaluate(stmt.DirY), stmt.DirY.Location, "DirY for circle center");
        var radius = ConvertToInt(Evaluate(stmt.Radius), stmt.Radius.Location, "Radius");

        if (radius <= 0)
            throw new RuntimeErrorException(new RuntimeError(stmt.Radius.Location, "Radius must be positive."));
        if (Math.Abs(dirX) > 1 || Math.Abs(dirY) > 1)
            throw new RuntimeErrorException(new RuntimeError(stmt.DirX.Location,
                "DirX/DirY for circle center offset must be -1, 0, or 1."));

        var circleCenter = new SKPoint(_currentWallEPosition.X + dirX, _currentWallEPosition.Y + dirY);

        _skPaint.Style = _isFilling ? SKPaintStyle.Fill : SKPaintStyle.Stroke;
        _skCanvas.DrawOval(circleCenter.X, circleCenter.Y, radius, radius, _skPaint);

        _currentWallEPosition = new Point((int)circleCenter.X, (int)circleCenter.Y);
        return null;
    }

    public object? VisitDrawRectangleStmt(DrawRectangleStmt stmt)
    {
        var dirX = ConvertToInt(Evaluate(stmt.DirX), stmt.DirX.Location, "DirX");
        var dirY = ConvertToInt(Evaluate(stmt.DirY), stmt.DirY.Location, "DirY");
        var distance = ConvertToInt(Evaluate(stmt.Distance), stmt.Distance.Location, "Distance");
        var width = ConvertToInt(Evaluate(stmt.Width), stmt.Width.Location, "Width");
        var height = ConvertToInt(Evaluate(stmt.Height), stmt.Height.Location, "Height");

        if (width <= 0 || height <= 0)
            throw new RuntimeErrorException(new RuntimeError(stmt.Location,
                "Width and Height for DrawRectangle must be positive."));
        if (Math.Abs(dirX) > 1 || Math.Abs(dirY) > 1 || (dirX == 0 && dirY == 0 && distance != 0))
            throw new RuntimeErrorException(new RuntimeError(stmt.Location, "Invalid direction for DrawRectangle."));

        var topLeft = new SKPoint(_currentWallEPosition.X + dirX * distance, _currentWallEPosition.Y + dirY * distance);
        var skRect = SKRect.Create(topLeft, new SKSize(width, height));

        _skPaint.Style = _isFilling ? SKPaintStyle.Fill : SKPaintStyle.Stroke;
        _skCanvas.DrawRect(skRect, _skPaint);

        _currentWallEPosition = new Point((int)(topLeft.X + width / 2), (int)(topLeft.Y + height / 2));
        return null;
    }

    public object? VisitFillStmt(FillStmt stmt)
    {
        var targetSkColor = _skBitmap.GetPixel(_currentWallEPosition.X, _currentWallEPosition.Y);

        var fillSkColor = ToSkiaColor(_currentColor);
        if (targetSkColor == fillSkColor) return null;

        var pixelsToProcess = new Queue<Point>();
        pixelsToProcess.Enqueue(_currentWallEPosition);

        var visited = new HashSet<Point>();
        while (pixelsToProcess.Count > 0)
        {
            var currentPixel = pixelsToProcess.Dequeue();

            if (!visited.Add(currentPixel)) continue;

            var x = currentPixel.X;
            var y = currentPixel.Y;

            if (x < 0 || x >= _skBitmap.Width || y < 0 || y >= _skBitmap.Height) continue;

            if (_skBitmap.GetPixel(x, y) == targetSkColor)
            {
                _skBitmap.SetPixel(x, y, fillSkColor);
                pixelsToProcess.Enqueue(new Point(x, y - 1));
                pixelsToProcess.Enqueue(new Point(x, y + 1));
                pixelsToProcess.Enqueue(new Point(x - 1, y));
                pixelsToProcess.Enqueue(new Point(x + 1, y));
            }
        }

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
        if (!condition) return null;
        if (_labelIndexMap.TryGetValue(stmt.Label, out var targetIndex)) _statementPointer = targetIndex;
        return null;
    }

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
            var count = ConvertToInt(rightVal, expr.Right.Location, "multiplier for string (right)");
            if (count < 0)
                throw new RuntimeErrorException(new RuntimeError(expr.Right.Location,
                    "Cannot repeat a string a negative number of times."));
            return string.Concat(Enumerable.Repeat(sVal, count));
        }

        if (rightVal is string sRVal)
        {
            var count = ConvertToInt(leftVal, expr.Left.Location, "multiplier for string (left)");
            if (count < 0)
                throw new RuntimeErrorException(new RuntimeError(expr.Left.Location,
                    "Cannot repeat a string a negative number of times."));
            return string.Concat(Enumerable.Repeat(sRVal, count));
        }

        return new IntegerOrBool(ConvertToInt(leftVal, expr.Left.Location, "left operand of *") *
                                 ConvertToInt(rightVal, expr.Right.Location, "right operand of *"));
    }

    public object VisitDivideExpr(DivideExpr expr)
    {
        var left = ConvertToInt(Evaluate(expr.Left), expr.Left.Location, "dividend");
        var right = ConvertToInt(Evaluate(expr.Right), expr.Right.Location, "divisor");
        if (right == 0) throw new RuntimeErrorException(new RuntimeError(expr.Right.Location, "Division by zero."));
        return new IntegerOrBool(left / right);
    }

    public object VisitPowerExpr(PowerExpr expr)
    {
        var numBase = ConvertToInt(Evaluate(expr.Left), expr.Left.Location, "base of power");
        var exponent = ConvertToInt(Evaluate(expr.Right), expr.Right.Location, "exponent of power");
        if (exponent < 0)
            throw new RuntimeErrorException(new RuntimeError(expr.Right.Location,
                "Exponent must be non-negative for integer power."));
        try
        {
            return new IntegerOrBool((int)Math.Pow(numBase, exponent));
        }
        catch (OverflowException)
        {
            throw new RuntimeErrorException(new RuntimeError(expr.Location,
                "Result of power operation out of range for an integer."));
        }
    }

    public object VisitModuloExpr(ModuloExpr expr)
    {
        var left = ConvertToInt(Evaluate(expr.Left), expr.Left.Location, "dividend of %");
        var right = ConvertToInt(Evaluate(expr.Right), expr.Right.Location, "divisor of %");
        if (right == 0) throw new RuntimeErrorException(new RuntimeError(expr.Right.Location, "Modulo by zero."));
        return new IntegerOrBool(left % right);
    }

    public object VisitEqualEqualExpr(EqualEqualExpr expr)
    {
        var left = Evaluate(expr.Left);
        var right = Evaluate(expr.Right);
        if (left is IntegerOrBool liob && right is IntegerOrBool riob) return new IntegerOrBool((int)liob == (int)riob);
        if (left is string sl && right is string sr) return new IntegerOrBool(sl == sr);
        throw new RuntimeErrorException(new RuntimeError(expr.Location,
            $"Incompatible types for '==': {left?.GetType()} and {right?.GetType()}"));
    }

    public object VisitBangEqualExpr(BangEqualExpr expr)
    {
        var isEqualResult =
            (IntegerOrBool)VisitEqualEqualExpr(new EqualEqualExpr(expr.Left, expr.Right, expr.Location));
        return new IntegerOrBool(!isEqualResult);
    }

    private IntegerOrBool PerformNumericComparison(BinaryExpr expr, Func<int, int, bool> comparison, string ope)
    {
        var left = ConvertToInt(Evaluate(expr.Left), expr.Left.Location, $"left operand of '{ope}'");
        var right = ConvertToInt(Evaluate(expr.Right), expr.Right.Location, $"right operand of '{ope}'");
        return new IntegerOrBool(comparison(left, right));
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
        var left = ConvertToBool(Evaluate(expr.Left), expr.Left.Location, "left operand of 'and'");
        if (!left) return new IntegerOrBool(false); // Short-circuit
        return new IntegerOrBool(ConvertToBool(Evaluate(expr.Right), expr.Right.Location, "right operand of 'and'"));
    }

    public object VisitOrExpr(OrExpr expr)
    {
        var left = ConvertToBool(Evaluate(expr.Left), expr.Left.Location, "left operand of 'or'");
        if (left) return new IntegerOrBool(true); // Short-circuit
        return new IntegerOrBool(ConvertToBool(Evaluate(expr.Right), expr.Right.Location, "right operand of 'or'"));
    }

    public object VisitGroupExpr(GroupExpr expr)
    {
        return Evaluate(expr.Expr);
    }

    public object VisitVariableExpr(VariableExpr expr)
    {
        if (_environment.TryGetValue(expr.Name, out var value)) return value!;
        throw new RuntimeErrorException(new RuntimeError(expr.Location, $"Variable '{expr.Name}' not defined."));
    }

    public object VisitCallExpr(CallExpr expr)
    {
        var evaluatedArgs = new List<object>();
        foreach (var argExpr in expr.Arguments) evaluatedArgs.Add(Evaluate(argExpr));

        if (expr.CalledFunction == "GetActualX") return new IntegerOrBool(_currentWallEPosition.X);

        if (expr.CalledFunction == "GetActualY") return new IntegerOrBool(_currentWallEPosition.Y);

        if (expr.CalledFunction == "GetCanvasSize") return new IntegerOrBool(_skBitmap.Width);

        if (expr.CalledFunction == "IsBrushColor")
        {
            var brushColorStr =
                ConvertToString(evaluatedArgs[0], expr.Arguments[0].Location, "color for IsBrushColor");
            if (WallEColor.TryParse(brushColorStr, out var checkColor))
                return new IntegerOrBool(_currentColor == checkColor);
            throw new RuntimeErrorException(new RuntimeError(expr.Arguments[0].Location,
                $"Invalid color string '{brushColorStr}' for IsBrushColor."));
        }

        if (expr.CalledFunction == "IsBrushSize")
            return new IntegerOrBool(_currentSize ==
                                     ConvertToInt(evaluatedArgs[0], expr.Arguments[0].Location,
                                         "size for IsBrushSize"));

        if (expr.CalledFunction == "IsCanvasColor")
        {
            var canvasColorStr = ConvertToString(evaluatedArgs[0], expr.Arguments[0].Location,
                "color for IsCanvasColor");
            var cx = ConvertToInt(evaluatedArgs[1], expr.Arguments[1].Location, "x for IsCanvasColor");
            var cy = ConvertToInt(evaluatedArgs[2], expr.Arguments[2].Location, "y for IsCanvasColor");

            if (!WallEColor.TryParse(canvasColorStr, out var targetWallEColor))
                throw new RuntimeErrorException(new RuntimeError(expr.Arguments[0].Location,
                    $"Invalid color string '{canvasColorStr}' for IsCanvasColor."));

            if (cx < 0 || cx >= _skBitmap.Width || cy < 0 || cy >= _skBitmap.Height)
                return new IntegerOrBool(false);

            var pixelSkColor = _skBitmap.GetPixel(cx, cy);
            var pixelWallEColor = new WallEColor(pixelSkColor.Red, pixelSkColor.Green, pixelSkColor.Blue,
                pixelSkColor.Alpha);
            return new IntegerOrBool(pixelWallEColor == targetWallEColor);
        }

        if (expr.CalledFunction == "GetColorCount")
        {
            var searchColorStr = ConvertToString(evaluatedArgs[0], expr.Arguments[0].Location,
                "color for GetColorCount");
            var rX = ConvertToInt(evaluatedArgs[1], expr.Arguments[1].Location, "x for GetColorCount");
            var rY = ConvertToInt(evaluatedArgs[2], expr.Arguments[2].Location, "y for GetColorCount");
            var rW = ConvertToInt(evaluatedArgs[3], expr.Arguments[3].Location, "width for GetColorCount");
            var rH = ConvertToInt(evaluatedArgs[4], expr.Arguments[4].Location, "height for GetColorCount");

            if (!WallEColor.TryParse(searchColorStr, out var targetSColor))
                throw new RuntimeErrorException(new RuntimeError(expr.Arguments[0].Location,
                    $"Invalid color string '{searchColorStr}' for GetColorCount."));

            var targetSkSColor = ToSkiaColor(targetSColor);

            if (rW <= 0 || rH <= 0) return new IntegerOrBool(0);
            var count = 0;
            var startX = Math.Max(0, rX);
            var startY = Math.Max(0, rY);
            var endX = Math.Min(_skBitmap.Width, rX + rW);
            var endY = Math.Min(_skBitmap.Height, rY + rH);

            for (var ix = startX; ix < endX; ix++)
            for (var iy = startY; iy < endY; iy++)
                if (_skBitmap.GetPixel(ix, iy) == targetSkSColor)
                    count++;
            return new IntegerOrBool(count);
        }

        throw new RuntimeErrorException(new RuntimeError(expr.Location,
            $"Function '{expr.CalledFunction}' not defined."));
    }
}