using ConsoleWall_e.Core.Common;
using ConsoleWall_e.Core.Errors;
using ConsoleWall_e.Core.Parser;
using ConsoleWall_e.Core.Parser.AST;
using ConsoleWall_e.Core.Parser.AST.Exprs;
using ConsoleWall_e.Core.Parser.AST.Stmts;
using System.Drawing;
using System.Drawing.Imaging;

namespace ConsoleWall_e.Core.Interpreter;

public class RuntimeErrorException(RuntimeError error) : Exception(error.Message)
{
    public RuntimeError runtimeError { get; } = error;
}

public class Interpreter : IInterpreter, IVisitor<object?>
{
    private readonly List<Error> _errors = new();
    private Dictionary<string, object> _environment = new();
    private Bitmap _canvas;
    private Graphics _graphics;

    private Point _currentWallEPosition;
    private WallEColor _currentColor = new(0, 0, 0); // Negro por defecto
    private int _currentSize = 1;
    private string _outputFilePath;
    private bool _isFilling = false;

    private Dictionary<string, int> _labelIndexMap = new();
    private int _statementPointer = 0; // Puntero a la instrucción actual
    private IReadOnlyList<Stmt> _programStatements = new List<Stmt>().AsReadOnly();

    public Interpreter(string outputFilePath = "output.png", string? loadImagePath = null, int defaultWidth = 500,
        int defaultHeight = 500)
    {
        _outputFilePath = outputFilePath;
        var imageLoadedSuccessfully = false;
        if (!string.IsNullOrEmpty(loadImagePath) && File.Exists(loadImagePath))
            try
            {
                // Carga la imagen manteniendo su información de píxeles.
                // Bitmap(string) crea una copia en un formato estándar (usualmente 32bppArgb).
                using (var bmpTemp = new Bitmap(loadImagePath))
                {
                    _canvas = new Bitmap(bmpTemp); // Crea una copia editable
                }

                //Console.WriteLine($"Imagen cargada '{loadImagePath}' ({_canvas.Width}x{_canvas.Height}).");
                imageLoadedSuccessfully = true;
            }
            catch (Exception ex)
            {
                _errors.Add(new ImportError(
                    $"Error al cargar la imagen '{loadImagePath}': {ex.Message}. Usando lienzo por defecto."));
                _canvas = new Bitmap(defaultWidth, defaultHeight);
            }
        else
            // No se especificó imagen o no existe, crea un lienzo nuevo.
            _canvas = new Bitmap(defaultWidth, defaultHeight);

        _graphics = Graphics.FromImage(_canvas);
        if (!imageLoadedSuccessfully) _graphics.Clear(Color.White);
        _currentWallEPosition = new Point(0, 0);
    }

    public Result<object> Interpret(ProgramStmt program)
    {
        //Falta manejar errores anteriores pero se puede seguir ya que lo que hacen es dejar el canvas por defecto
        _environment.Clear();
        _currentColor = new WallEColor(0, 0, 0);
        _currentSize = 1;
        _isFilling = false;

        _labelIndexMap.Clear();
        _programStatements = program.Statements;

        // Pre-escaneo para encontrar todas las etiquetas y sus índices.
        for (var i = 0; i < _programStatements.Count; i++)
        {
            if (_programStatements[i] is not LabelStmt labelStmt) continue;
            _labelIndexMap.Add(labelStmt.Label, i);
        }

        _statementPointer = 0;
        try
        {
            while (_statementPointer < _programStatements.Count)
            {
                if (_errors.Any(e => e.Type == ErrorType.Runtime)) break;
                var currentStmt = _programStatements[_statementPointer];
                _statementPointer++; // Avanza el puntero ANTES de ejecutar, GoTo lo ajustará.
                Execute(currentStmt);
            }
        }
        catch (RuntimeErrorException ex) // Captura errores en Runtime
        {
            _errors.Add(ex.runtimeError);
        }
        catch (Exception ex) // Captura otros errores inesperados
        {
            var errorLocation = _statementPointer > 0 && _statementPointer <= _programStatements.Count
                ? _programStatements[_statementPointer - 1].Location
                : new CodeLocation(0, 0); // Localización por defecto si no se puede determinar.
            _errors.Add(new RuntimeError(errorLocation, $"Error inesperado en tiempo de ejecución: {ex.Message}"));
        }

        var hadRTErrors = _errors.Any(e => e.Type == ErrorType.Runtime);
        var hadErrors = _errors.Any();

        try
        {
            _canvas.Save(_outputFilePath, ImageFormat.Png);
            if (hadErrors)
            {
                if (!hadRTErrors && _errors.Any(e => e.Type == ErrorType.Import))
                    Console.WriteLine(
                        $"Interpretación del script completada. Imagen guardada en '{_outputFilePath}'. Nota: Hubo errores de importación de imagen inicial.");
                else
                    Console.WriteLine(
                        $"Interpretación del script con errores. Imagen guardada con el progreso hasta el error en '{_outputFilePath}'.");
            }
            else
            {
                Console.WriteLine($"Interpretación completada exitosamente. Imagen guardada en '{_outputFilePath}'.");
            }
        }
        catch (Exception ex)
        {
            // Posible error AL GUARDAR la imagen
            _errors.Add(new ImportError($"Fallo al guardar la imagen en '{_outputFilePath}': {ex.Message}"));
        }
        finally
        {
            _graphics.Dispose();
            _canvas.Dispose();
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

    private Color ToSystemColor(WallEColor c)
    {
        return Color.FromArgb(c.Alpha, c.Red, c.Green, c.Blue);
    }

    private int ConvertToInt(object value, CodeLocation loc, string context = "valor")
    {
        if (value is IntegerOrBool iob) return iob;
        throw new RuntimeErrorException(new RuntimeError(loc,
            $"Se esperaba un entero para {context}, pero se obtuvo {value?.GetType().Name ?? "null"}."));
    }

    private bool ConvertToBool(object value, CodeLocation loc, string context = "valor")
    {
        if (value is IntegerOrBool iob) return iob;
        throw new RuntimeErrorException(new RuntimeError(loc,
            $"Se esperaba un booleano para {context}, pero se obtuvo {value?.GetType().Name ?? "null"}."));
    }

    private string ConvertToString(object value, CodeLocation loc, string context = "valor")
    {
        if (value is string s) return s;
        throw new RuntimeErrorException(new RuntimeError(loc,
            $"Se esperaba un string para {context}, pero se obtuvo {value?.GetType().Name ?? "null"}."));
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
            $"No se puede convertir {val?.GetType().Name ?? "null"} a string para concatenación."));
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
        _currentWallEPosition = new Point(x, y);
        return null;
    }

    public object? VisitColorStmt(ColorStmt stmt)
    {
        var colorStr = ConvertToString(Evaluate(stmt.ColorExpr), stmt.ColorExpr.Location, "argumento de Color");
        if (WallEColor.TryParse(colorStr, out var newColor))
            _currentColor = newColor;
        else
            throw new RuntimeErrorException(new RuntimeError(stmt.ColorExpr.Location,
                $"String de color inválido '{colorStr}' en tiempo de ejecución."));
        return null;
    }

    public object? VisitSizeStmt(SizeStmt stmt)
    {
        _currentSize = ConvertToInt(Evaluate(stmt.SizeExpr), stmt.SizeExpr.Location, "argumento de Size");
        if (_currentSize <= 0)
            throw new RuntimeErrorException(new RuntimeError(stmt.SizeExpr.Location,
                "El tamaño Size debe ser un entero positivo."));
        return null;
    }

    public object? VisitDrawLineStmt(DrawLineStmt stmt)
    {
        var dirX = ConvertToInt(Evaluate(stmt.DirX), stmt.DirX.Location, "DirX de DrawLine");
        var dirY = ConvertToInt(Evaluate(stmt.DirY), stmt.DirY.Location, "DirY de DrawLine");
        var distance = ConvertToInt(Evaluate(stmt.Distance), stmt.Distance.Location, "Distance de DrawLine");

        if (Math.Abs(dirX) > 1 || Math.Abs(dirY) > 1 || (dirX == 0 && dirY == 0 && distance != 0))
            throw new RuntimeErrorException(new RuntimeError(stmt.Location,
                "Dirección inválida para DrawLine (componentes deben ser -1, 0, o 1, y no (0,0) si distancia no es 0)."));

        var startPoint = _currentWallEPosition;
        var endPoint = new Point(_currentWallEPosition.X + dirX * distance, _currentWallEPosition.Y + dirY * distance);

        using (var pen = new Pen(ToSystemColor(_currentColor), _currentSize))
        {
            _graphics.DrawLine(pen, startPoint, endPoint);
        }

        _currentWallEPosition = endPoint;
        return null;
    }

    public object? VisitDrawCircleStmt(DrawCircleStmt stmt)
    {
        var dirX = ConvertToInt(Evaluate(stmt.DirX), stmt.DirX.Location, "DirX de DrawCircle");
        var dirY = ConvertToInt(Evaluate(stmt.DirY), stmt.DirY.Location, "DirY de DrawCircle");
        var radius = ConvertToInt(Evaluate(stmt.Radius), stmt.Radius.Location, "Radius de DrawCircle");

        if (radius <= 0)
            throw new RuntimeErrorException(new RuntimeError(stmt.Radius.Location, "El radio debe ser positivo."));
        if (Math.Abs(dirX) > 1 || Math.Abs(dirY) > 1)
            throw new RuntimeErrorException(new RuntimeError(stmt.DirX.Location,
                "DirX/DirY para el centro de DrawCircle deben ser -1, 0, o 1."));

        var circleCenter = new Point(_currentWallEPosition.X + dirX, _currentWallEPosition.Y + dirY);
        var boundingBox = new Rectangle(circleCenter.X - radius, circleCenter.Y - radius, radius * 2, radius * 2);

        if (_isFilling)
        {
            using (var brush = new SolidBrush(ToSystemColor(_currentColor)))
            {
                _graphics.FillEllipse(brush, boundingBox);
            }

            _isFilling = false;
        }
        else
        {
            using (var pen = new Pen(ToSystemColor(_currentColor), _currentSize))
            {
                _graphics.DrawEllipse(pen, boundingBox);
            }
        }

        _currentWallEPosition = circleCenter;
        return null;
    }

    public object? VisitDrawRectangleStmt(DrawRectangleStmt stmt)
    {
        var dirX = ConvertToInt(Evaluate(stmt.DirX), stmt.DirX.Location, "DirX de DrawRectangle");
        var dirY = ConvertToInt(Evaluate(stmt.DirY), stmt.DirY.Location, "DirY de DrawRectangle");
        var distance = ConvertToInt(Evaluate(stmt.Distance), stmt.Distance.Location, "Distance de DrawRectangle");
        var width = ConvertToInt(Evaluate(stmt.Width), stmt.Width.Location, "Width de DrawRectangle");
        var height = ConvertToInt(Evaluate(stmt.Height), stmt.Height.Location, "Height de DrawRectangle");

        if (width <= 0 || height <= 0)
            throw new RuntimeErrorException(new RuntimeError(stmt.Location,
                "Width y Height para DrawRectangle deben ser positivos."));
        if (Math.Abs(dirX) > 1 || Math.Abs(dirY) > 1 || (dirX == 0 && dirY == 0 && distance != 0))
            throw new RuntimeErrorException(new RuntimeError(stmt.Location, "Dirección inválida para DrawRectangle."));

        var topLeft = new Point(_currentWallEPosition.X + dirX * distance, _currentWallEPosition.Y + dirY * distance);
        var rect = new Rectangle(topLeft.X, topLeft.Y, width, height);

        if (_isFilling)
        {
            using (var brush = new SolidBrush(ToSystemColor(_currentColor)))
            {
                _graphics.FillRectangle(brush, rect);
            }

            _isFilling = false;
        }
        else
        {
            using (var pen = new Pen(ToSystemColor(_currentColor), _currentSize))
            {
                _graphics.DrawRectangle(pen, rect);
            }
        }

        _currentWallEPosition = new Point(topLeft.X + width, topLeft.Y + height);
        return null;
    }

    public object? VisitFillStmt(FillStmt stmt)
    {
        var targetSystemColor = _canvas.GetPixel(_currentWallEPosition.X, _currentWallEPosition.Y);
        var targetWallEColor = new WallEColor(targetSystemColor.R, targetSystemColor.G, targetSystemColor.B,
            targetSystemColor.A);

        var fillColor = _currentColor;
        var fillSystemColor = ToSystemColor(fillColor);
        if (targetWallEColor == fillColor) return null;
        var pixelsToProcess = new Queue<Point>();
        pixelsToProcess.Enqueue(_currentWallEPosition);

        while (pixelsToProcess.Count > 0)
        {
            var currentPixel = pixelsToProcess.Dequeue();

            var x = currentPixel.X;
            var y = currentPixel.Y;

            var pixelColorAtCurrent = _canvas.GetPixel(x, y);
            if (pixelColorAtCurrent == targetSystemColor)
            {
                _canvas.SetPixel(x, y, fillSystemColor);
                if (y - 1 >= 0) pixelsToProcess.Enqueue(new Point(x, y - 1));
                if (y + 1 < _canvas.Height) pixelsToProcess.Enqueue(new Point(x, y + 1));
                if (x - 1 >= 0) pixelsToProcess.Enqueue(new Point(x - 1, y));
                if (x + 1 < _canvas.Width) pixelsToProcess.Enqueue(new Point(x + 1, y));
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
        var condition = ConvertToBool(Evaluate(stmt.Condition), stmt.Condition.Location, "condición de GoTo");
        if (!condition) return null;
        if (_labelIndexMap.TryGetValue(stmt.Label, out var targetIndex)) _statementPointer = targetIndex;
        return null;
    }

    //Expr
    public object VisitLiteralExpr(LiteralExpr expr)
    {
        return expr.Value.Value ??
               throw new RuntimeErrorException(new RuntimeError(expr.Location, "Literal value is null."));
    }

    public object VisitBangExpr(BangExpr expr)
    {
        var value = ConvertToBool(Evaluate(expr.Right), expr.Right.Location, "operando de !");
        return new IntegerOrBool(!value);
    }

    public object VisitMinusExpr(MinusExpr expr)
    {
        var value = ConvertToInt(Evaluate(expr.Right), expr.Right.Location, "operando de negación -");
        return new IntegerOrBool(-value);
    }

    public object VisitAddExpr(AddExpr expr)
    {
        var leftVal = Evaluate(expr.Left);
        var rightVal = Evaluate(expr.Right);

        if (leftVal is string || rightVal is string)
            return ConvertValToString(leftVal, expr.Left.Location) + ConvertValToString(rightVal, expr.Right.Location);
        return new IntegerOrBool(ConvertToInt(leftVal, expr.Left.Location, "operando izquierdo de +") +
                                 ConvertToInt(rightVal, expr.Right.Location, "operando derecho de +"));
    }

    public object VisitSubtractExpr(SubtractExpr expr)
    {
        var left = ConvertToInt(Evaluate(expr.Left), expr.Left.Location, "operando izquierdo de -");
        var right = ConvertToInt(Evaluate(expr.Right), expr.Right.Location, "operando derecho de -");
        return new IntegerOrBool(left - right);
    }

    public object VisitMultiplyExpr(MultiplyExpr expr)
    {
        var leftVal = Evaluate(expr.Left);
        var rightVal = Evaluate(expr.Right);

        if (leftVal is string sVal)
        {
            var count = ConvertToInt(rightVal, expr.Right.Location, "multiplicador para string (derecho)");
            if (count < 0)
                throw new RuntimeErrorException(new RuntimeError(expr.Right.Location,
                    "No se puede repetir un string un número negativo de veces."));
            return string.Concat(Enumerable.Repeat(sVal, count));
        }

        if (rightVal is string sRVal)
        {
            var count = ConvertToInt(leftVal, expr.Left.Location, "multiplicador para string (izquierdo)");
            if (count < 0)
                throw new RuntimeErrorException(new RuntimeError(expr.Left.Location,
                    "No se puede repetir un string un número negativo de veces."));
            return string.Concat(Enumerable.Repeat(sRVal, count));
        }

        return new IntegerOrBool(ConvertToInt(leftVal, expr.Left.Location, "operando izquierdo de *") *
                                 ConvertToInt(rightVal, expr.Right.Location, "operando derecho de *"));
    }

    public object VisitDivideExpr(DivideExpr expr)
    {
        var left = ConvertToInt(Evaluate(expr.Left), expr.Left.Location, "dividendo");
        var right = ConvertToInt(Evaluate(expr.Right), expr.Right.Location, "divisor");
        if (right == 0) throw new RuntimeErrorException(new RuntimeError(expr.Right.Location, "División por cero."));
        return new IntegerOrBool(left / right);
    }

    public object VisitPowerExpr(PowerExpr expr)
    {
        var numBase = ConvertToInt(Evaluate(expr.Left), expr.Left.Location, "base de la potencia");
        var exponent = ConvertToInt(Evaluate(expr.Right), expr.Right.Location, "exponente de la potencia");
        if (exponent < 0)
            throw new RuntimeErrorException(new RuntimeError(expr.Right.Location,
                "El exponente debe ser no-negativo para la potencia entera."));
        try
        {
            return new IntegerOrBool((int)Math.Pow(numBase, exponent));
        }
        catch (OverflowException)
        {
            throw new RuntimeErrorException(new RuntimeError(expr.Location,
                "Resultado de la potencia fuera de rango para un entero."));
        }
    }

    public object VisitModuloExpr(ModuloExpr expr)
    {
        var left = ConvertToInt(Evaluate(expr.Left), expr.Left.Location, "dividendo de %");
        var right = ConvertToInt(Evaluate(expr.Right), expr.Right.Location, "divisor de %");
        if (right == 0) throw new RuntimeErrorException(new RuntimeError(expr.Right.Location, "Módulo por cero."));
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
            $"Tipos incompatibles para comparación '==' en tiempo de ejecución: {left?.GetType()} y {right?.GetType()}"));
    }

    public object VisitBangEqualExpr(BangEqualExpr expr)
    {
        var isEqualResult =
            (IntegerOrBool)VisitEqualEqualExpr(new EqualEqualExpr(expr.Left, expr.Right, expr.Location));
        return new IntegerOrBool(!isEqualResult);
    }

    private IntegerOrBool PerformNumericComparison(BinaryExpr expr, Func<int, int, bool> comparison, string ope)
    {
        var left = ConvertToInt(Evaluate(expr.Left), expr.Left.Location, $"operando izquierdo de '{ope}'");
        var right = ConvertToInt(Evaluate(expr.Right), expr.Right.Location, $"operando derecho de '{ope}'");
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
        var left = ConvertToBool(Evaluate(expr.Left), expr.Left.Location, "operando izquierdo de 'and'");
        if (!left) return new IntegerOrBool(false);
        return new IntegerOrBool(ConvertToBool(Evaluate(expr.Right), expr.Right.Location, "operando derecho de 'and'"));
    }

    public object VisitOrExpr(OrExpr expr)
    {
        var left = ConvertToBool(Evaluate(expr.Left), expr.Left.Location, "operando izquierdo de 'or'");
        if (left) return new IntegerOrBool(true);
        return new IntegerOrBool(ConvertToBool(Evaluate(expr.Right), expr.Right.Location, "operando derecho de 'or'"));
    }

    public object VisitGroupExpr(GroupExpr expr)
    {
        return Evaluate(expr.Expr);
    }

    public object VisitVariableExpr(VariableExpr expr)
    {
        if (_environment.TryGetValue(expr.Name, out var value)) return value!;
        throw new RuntimeErrorException(new RuntimeError(expr.Location, $"Variable no definida '{expr.Name}'."));
    }

    public object VisitCallExpr(CallExpr expr)
    {
        var evaluatedArgs = new List<object>();
        foreach (var argExpr in expr.Arguments) evaluatedArgs.Add(Evaluate(argExpr));

        if (expr.CalledFunction == "GetActualX") return new IntegerOrBool(_currentWallEPosition.X);

        if (expr.CalledFunction == "GetActualY") return new IntegerOrBool(_currentWallEPosition.Y);

        if (expr.CalledFunction == "GetCanvasSize") return new IntegerOrBool(_canvas.Width);

        if (expr.CalledFunction == "IsBrushColor")
        {
            var brushColorStr =
                ConvertToString(evaluatedArgs[0], expr.Arguments[0].Location, "color para IsBrushColor");
            if (WallEColor.TryParse(brushColorStr, out var checkColor))
                return new IntegerOrBool(_currentColor == checkColor);
            throw new RuntimeErrorException(new RuntimeError(expr.Arguments[0].Location,
                $"String de color inválido '{brushColorStr}' para IsBrushColor."));
        }

        if (expr.CalledFunction == "IsBrushSize")
            return new IntegerOrBool(_currentSize ==
                                     ConvertToInt(evaluatedArgs[0], expr.Arguments[0].Location,
                                         "tamaño para IsBrushSize"));

        if (expr.CalledFunction == "IsCanvasColor")
        {
            var canvasColorStr =
                ConvertToString(evaluatedArgs[0], expr.Arguments[0].Location, "color para IsCanvasColor");
            var cx = ConvertToInt(evaluatedArgs[1], expr.Arguments[1].Location, "x para IsCanvasColor");
            var cy = ConvertToInt(evaluatedArgs[2], expr.Arguments[2].Location, "y para IsCanvasColor");

            if (!WallEColor.TryParse(canvasColorStr, out var targetColor))
                throw new RuntimeErrorException(new RuntimeError(expr.Arguments[0].Location,
                    $"String de color inválido '{canvasColorStr}' para IsCanvasColor."));

            if (cx < 0 || cx >= _canvas.Width || cy < 0 || cy >= _canvas.Height) return new IntegerOrBool(false);

            var pixelColor = _canvas.GetPixel(cx, cy);
            return new IntegerOrBool(
                new WallEColor(pixelColor.R, pixelColor.G, pixelColor.B, pixelColor.A) == targetColor);
        }

        if (expr.CalledFunction == "GetColorCount")
        {
            var searchColorStr =
                ConvertToString(evaluatedArgs[0], expr.Arguments[0].Location, "color para GetColorCount");
            var rX = ConvertToInt(evaluatedArgs[1], expr.Arguments[1].Location, "x para GetColorCount");
            var rY = ConvertToInt(evaluatedArgs[2], expr.Arguments[2].Location, "y para GetColorCount");
            var rW = ConvertToInt(evaluatedArgs[3], expr.Arguments[3].Location, "width para GetColorCount");
            var rH = ConvertToInt(evaluatedArgs[4], expr.Arguments[4].Location, "height para GetColorCount");

            if (!WallEColor.TryParse(searchColorStr, out var targetSColor))
                throw new RuntimeErrorException(new RuntimeError(expr.Arguments[0].Location,
                    $"String de color inválido '{searchColorStr}' para GetColorCount."));

            if (rW <= 0 || rH <= 0) return new IntegerOrBool(0);

            var count = 0;
            var startX = Math.Max(0, rX);
            var startY = Math.Max(0, rY);
            var endX = Math.Min(_canvas.Width, rX + rW);
            var endY = Math.Min(_canvas.Height, rY + rH);

            for (var ix = startX; ix < endX; ix++)
            for (var iy = startY; iy < endY; iy++)
            {
                var pxColor = _canvas.GetPixel(ix, iy);
                if (new WallEColor(pxColor.R, pxColor.G, pxColor.B, pxColor.A) == targetSColor) count++;
            }

            return new IntegerOrBool(count);
        }

        throw new RuntimeErrorException(new RuntimeError(expr.Location,
            $"Función no definida '{expr.CalledFunction}'."));
    }
}