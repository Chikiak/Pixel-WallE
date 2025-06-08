using Core.Common;
using Core.Errors;
using Core.Parser.AST;
using Core.Parser.AST.Exprs;
using Core.Parser.AST.Stmts;

namespace Core.Parser;

public class CheckSemantic : IVisitor<Result<Type>>
{
    private readonly List<Error> _errors = new();
    private readonly Dictionary<string, Type> _symbolTable = new();
    private readonly HashSet<string> _definedLabels = new();
    private readonly List<(string LabelName, CodeLocation Location)> _gotoUsages = new();
    private readonly Dictionary<string, FunctionSignature> _definedFunctions = new();
    private bool _firstStatementProcessed = false;

    public CheckSemantic()
    {
        InitializeDefinedFunctions();
    }

    private void InitializeDefinedFunctions()
    {
        _definedFunctions.Add("GetActualX",
            new FunctionSignature("GetActualX", typeof(IntegerOrBool), new List<Type>()));
        _definedFunctions.Add("GetActualY",
            new FunctionSignature("GetActualY", typeof(IntegerOrBool), new List<Type>()));
        _definedFunctions.Add("GetCanvasSize",
            new FunctionSignature("GetCanvasSize", typeof(IntegerOrBool), new List<Type>()));
        _definedFunctions.Add("GetColorCount", new FunctionSignature("GetColorCount", typeof(IntegerOrBool),
            new List<Type>
            {
                typeof(string), typeof(IntegerOrBool), typeof(IntegerOrBool), typeof(IntegerOrBool),
                typeof(IntegerOrBool)
            }));
        _definedFunctions.Add("IsBrushColor", new FunctionSignature("IsBrushColor", typeof(IntegerOrBool),
            new List<Type> { typeof(string) }));
        _definedFunctions.Add("IsBrushSize", new FunctionSignature("IsBrushSize", typeof(IntegerOrBool),
            new List<Type> { typeof(IntegerOrBool) }));
        _definedFunctions.Add("IsCanvasColor", new FunctionSignature("IsCanvasColor", typeof(IntegerOrBool),
            new List<Type> { typeof(string), typeof(IntegerOrBool), typeof(IntegerOrBool) }));
    }

    public Result<object> Analize(ProgramStmt program)
    {
        _errors.Clear();
        _symbolTable.Clear();
        _definedLabels.Clear();
        _gotoUsages.Clear();
        _firstStatementProcessed = false;

        program.Accept(this);

        if (program.Statements.Count > 0)
        {
            var firstEffectiveStmt = program.Statements[0];
            if (firstEffectiveStmt is not SpawnStmt)
                _errors.Add(new SemanticError(firstEffectiveStmt.Location, "Program must start with a Spawn command."));
        }
        else if (program.Statements.Count == 0 && program.Location != null) // Programa vacío
        {
            _errors.Add(new SemanticError(program.Location,
                "Program cannot be empty and must start with a Spawn command."));
        }

        foreach (var usage in _gotoUsages)
            if (!_definedLabels.Contains(usage.LabelName))
                _errors.Add(new SemanticError(usage.Location,
                    $"Label '{usage.LabelName}' referenced in GoTo statement is not defined."));

        if (_errors.Any()) return Result<object>.Failure(_errors);
        return Result<object>.Success(new object());
    }

    public Result<Type> VisitLiteralExpr(LiteralExpr expr)
    {
        return Result<Type>.Success(expr.Value.Type);
    }

    public Result<Type> VisitBangExpr(BangExpr expr)
    {
        return CheckUnaryExp(expr, typeof(IntegerOrBool));
    }

    public Result<Type> VisitMinusExpr(MinusExpr expr)
    {
        return CheckUnaryExp(expr, typeof(IntegerOrBool));
    }

    private Result<Type> CheckUnaryExp(UnaryExpr expr, Type type)
    {
        var rightResult = expr.Right.Accept(this);
        if (!rightResult.IsSuccess) return Result<Type>.Failure(rightResult.Errors);
        if (rightResult.Value != type)
        {
            var error = new SemanticError(expr.Location,
                $"This unary operator can only be applied to {type} expressions.");
            _errors.Add(error);
            return Result<Type>.Failure(error);
        }

        return Result<Type>.Success(type);
    }

    public Result<Type> VisitAddExpr(AddExpr expr)
    {
        var leftResult = expr.Left.Accept(this);
        var rightResult = expr.Right.Accept(this);

        var currentErrors = new List<Error>();
        if (!leftResult.IsSuccess) currentErrors.AddRange(leftResult.Errors);
        if (!rightResult.IsSuccess) currentErrors.AddRange(rightResult.Errors);

        if (currentErrors.Any()) return Result<Type>.Failure(currentErrors);

        var leftType = leftResult.Value;
        var rightType = rightResult.Value;

        // Caso 1: int + int = int
        if (leftType == typeof(IntegerOrBool) && rightType == typeof(IntegerOrBool))
            return Result<Type>.Success(typeof(IntegerOrBool));
        // Caso 2: string + string = string Caso 3: string + int = string
        if (leftType == typeof(string) && (rightType == typeof(string) || rightType == typeof(IntegerOrBool)))
            return Result<Type>.Success(typeof(string));
        // Caso 4: int + string = string
        if (leftType == typeof(IntegerOrBool) && rightType == typeof(string))
            return Result<Type>.Success(typeof(string));
        var error = new SemanticError(expr.Location,
            $"Operator '+' cannot be applied to operands of type '{leftType.Name}' and '{rightType.Name}' in this combination.");
        _errors.Add(error);
        currentErrors.Add(error);
        return Result<Type>.Failure(currentErrors);
    }

    public Result<Type> VisitMultiplyExpr(MultiplyExpr expr)
    {
        var leftResult = expr.Left.Accept(this);
        var rightResult = expr.Right.Accept(this);

        var currentErrors = new List<Error>();
        if (!leftResult.IsSuccess) currentErrors.AddRange(leftResult.Errors);
        if (!rightResult.IsSuccess) currentErrors.AddRange(rightResult.Errors);

        if (currentErrors.Any()) return Result<Type>.Failure(currentErrors);

        var leftType = leftResult.Value;
        var rightType = rightResult.Value;

        // Caso 1: int * int = int
        if (leftType == typeof(IntegerOrBool) && rightType == typeof(IntegerOrBool))
            return Result<Type>.Success(typeof(IntegerOrBool));
        // Caso 2: string * int = string
        if (leftType == typeof(string) && rightType == typeof(IntegerOrBool))
            return Result<Type>.Success(typeof(string));
        // Caso 3: int + string = string
        if (leftType == typeof(IntegerOrBool) && rightType == typeof(string))
            return Result<Type>.Success(typeof(string));
        var error = new SemanticError(expr.Location,
            $"Operator '*' cannot be applied to operands of type '{leftType.Name}' and '{rightType.Name}' in this combination.");
        _errors.Add(error);
        currentErrors.Add(error);
        return Result<Type>.Failure(currentErrors);
    }

    public Result<Type> VisitSubtractExpr(SubtractExpr expr)
    {
        return CheckArithmeticExpr(expr, "-");
    }

    public Result<Type> VisitDivideExpr(DivideExpr expr)
    {
        return CheckArithmeticExpr(expr, "/");
    }

    public Result<Type> VisitPowerExpr(PowerExpr expr)
    {
        return CheckArithmeticExpr(expr, "**");
    }

    public Result<Type> VisitModuloExpr(ModuloExpr expr)
    {
        return CheckArithmeticExpr(expr, "%");
    }

    private Result<Type> CheckArithmeticExpr(BinaryExpr expr, string operatorName)
    {
        var leftResult = expr.Left.Accept(this);
        var rightResult = expr.Right.Accept(this);

        var currentErrors = new List<Error>();
        if (!leftResult.IsSuccess) currentErrors.AddRange(leftResult.Errors);
        if (!rightResult.IsSuccess) currentErrors.AddRange(rightResult.Errors);

        if (currentErrors.Any()) return Result<Type>.Failure(currentErrors);

        if (leftResult.Value != typeof(IntegerOrBool) || rightResult.Value != typeof(IntegerOrBool))
        {
            var error = new SemanticError(expr.Location,
                $"Operator '{operatorName}' can only be applied to integer operands. Found '{leftResult.Value.Name}' and '{rightResult.Value.Name}'.");
            _errors.Add(error);
            currentErrors.Add(error); // Añadir también a currentErrors para el return
            return Result<Type>.Failure(currentErrors);
        }

        return Result<Type>.Success(typeof(IntegerOrBool));
    }

    public Result<Type> VisitEqualEqualExpr(EqualEqualExpr expr)
    {
        return CheckComparisonExpr(expr, "==");
    }

    public Result<Type> VisitBangEqualExpr(BangEqualExpr expr)
    {
        return CheckComparisonExpr(expr, "!=");
    }

    public Result<Type> VisitGreaterExpr(GreaterExpr expr)
    {
        return CheckComparisonExpr(expr, ">");
    }

    public Result<Type> VisitGreaterEqualExpr(GreaterEqualExpr expr)
    {
        return CheckComparisonExpr(expr, ">=");
    }

    public Result<Type> VisitLessExpr(LessExpr expr)
    {
        return CheckComparisonExpr(expr, "<");
    }

    public Result<Type> VisitLessEqualExpr(LessEqualExpr expr)
    {
        return CheckComparisonExpr(expr, "<=");
    }

    private Result<Type> CheckComparisonExpr(BinaryExpr expr, string operatorName)
    {
        var leftResult = expr.Left.Accept(this);
        var rightResult = expr.Right.Accept(this);

        var currentErrors = new List<Error>();
        if (!leftResult.IsSuccess) currentErrors.AddRange(leftResult.Errors);
        if (!rightResult.IsSuccess) currentErrors.AddRange(rightResult.Errors);

        if (currentErrors.Any()) return Result<Type>.Failure(currentErrors);

        var typesCompatible = false;
        if ((expr is EqualEqualExpr || expr is BangEqualExpr) && leftResult.Value == rightResult.Value)
            typesCompatible = true;
        else if (leftResult.Value == typeof(IntegerOrBool) && rightResult.Value == typeof(IntegerOrBool))
            typesCompatible = true;

        if (!typesCompatible)
        {
            var error = new SemanticError(expr.Location,
                $"Cannot apply operator '{operatorName}' to operands of type '{leftResult.Value.Name}' and '{rightResult.Value.Name}'.");
            _errors.Add(error);
            currentErrors.Add(error);
            return Result<Type>.Failure(currentErrors);
        }

        return Result<Type>.Success(typeof(IntegerOrBool));
    }

    public Result<Type> VisitAndExpr(AndExpr expr)
    {
        return CheckLogicalExpr(expr, "and");
    }

    public Result<Type> VisitOrExpr(OrExpr expr)
    {
        return CheckLogicalExpr(expr, "or");
    }

    private Result<Type> CheckLogicalExpr(BinaryExpr expr, string operatorName)
    {
        var leftResult = expr.Left.Accept(this);
        var rightResult = expr.Right.Accept(this);

        var currentErrors = new List<Error>();
        if (!leftResult.IsSuccess) currentErrors.AddRange(leftResult.Errors);
        if (!rightResult.IsSuccess) currentErrors.AddRange(rightResult.Errors);

        if (currentErrors.Any()) return Result<Type>.Failure(currentErrors);

        if (leftResult.Value != typeof(IntegerOrBool) || rightResult.Value != typeof(IntegerOrBool))
        {
            var error = new SemanticError(expr.Location,
                $"Operator '{operatorName}' can only be applied to boolean operands");
            _errors.Add(error);
            currentErrors.Add(error);
            return Result<Type>.Failure(currentErrors);
        }

        return Result<Type>.Success(typeof(IntegerOrBool));
    }

    public Result<Type> VisitGroupExpr(GroupExpr expr)
    {
        return expr.Expr.Accept(this);
    }

    public Result<Type> VisitVariableExpr(VariableExpr expr)
    {
        if (_symbolTable.TryGetValue(expr.Name, out var type)) return Result<Type>.Success(type);
        var error = new SemanticError(expr.Location, $"Variable '{expr.Name}' is not defined.");
        _errors.Add(error);
        return Result<Type>.Failure(error);
    }

    public Result<Type> VisitCallExpr(CallExpr expr)
    {
        if (!_definedFunctions.TryGetValue(expr.CalledFunction, out var funcSignature))
        {
            var error = new SemanticError(expr.Location, $"Function '{expr.CalledFunction}' is not defined.");
            _errors.Add(error);
            return Result<Type>.Failure(error);
        }

        if (expr.Arguments.Count != funcSignature.ParameterTypes.Count)
        {
            var error = new SemanticError(expr.Location,
                $"Function '{funcSignature.Name}' expects {funcSignature.ParameterTypes.Count} arguments, but got {expr.Arguments.Count}.");
            _errors.Add(error);
            return Result<Type>.Failure(error);
        }

        var argumentErrors = new List<Error>();
        for (var i = 0; i < expr.Arguments.Count; i++)
        {
            var argExpr = expr.Arguments[i];
            var argResult = argExpr.Accept(this);

            if (!argResult.IsSuccess)
            {
                argumentErrors.AddRange(argResult.Errors);
                continue;
            }

            var expectedParamSysType = funcSignature.ParameterTypes[i];
            var actualArgSysType = argResult.Value;

            var typeMatch = expectedParamSysType == actualArgSysType;

            if (!typeMatch)
            {
                var typeError = new SemanticError(argExpr.Location,
                    $"Argument {i + 1} for function '{funcSignature.Name}' expects type '{expectedParamSysType.Name}', but got '{actualArgSysType.Name}'.");
                _errors.Add(typeError);
                argumentErrors.Add(typeError);
            }
        }

        if (argumentErrors.Any()) return Result<Type>.Failure(argumentErrors);
        return Result<Type>.Success(funcSignature.ReturnType);
    }

    public Result<Type> VisitProgramStmt(ProgramStmt stmt)
    {
        foreach (var statement in stmt.Statements) statement.Accept(this);
        return Result<Type>.Success(typeof(void));
    }

    public Result<Type> VisitExpressionStmt(ExpressionStmt stmt)
    {
        stmt.Expr.Accept(this);
        return Result<Type>.Success(typeof(void));
    }

    private void CheckCommandArg(Expr argExpr, Type expectedType, string commandName, string argName,
        CodeLocation errorLocation)
    {
        var argResult = argExpr.Accept(this);
        if (!argResult.IsSuccess) return;
        if (argResult.Value == expectedType) return;
        _errors.Add(new SemanticError(errorLocation,
                $"Argument '{argName}' for command '{commandName}' expects type '{expectedType.Name}', but got '{argResult.Value.Name}'."));
    }

    public Result<Type> VisitSpawnStmt(SpawnStmt stmt)
    {
        if (!_firstStatementProcessed)
            _firstStatementProcessed = true;
        else
            _errors.Add(new SemanticError(stmt.Location, "Spawn command can only be used once."));

        CheckCommandArg(stmt.X, typeof(IntegerOrBool), "Spawn", "X", stmt.X.Location);
        CheckCommandArg(stmt.Y, typeof(IntegerOrBool), "Spawn", "Y", stmt.Y.Location);
        return Result<Type>.Success(typeof(void));
    }

    public Result<Type> VisitColorStmt(ColorStmt stmt)
    {
        CheckCommandArg(stmt.ColorExpr, typeof(string), "Color", "color", stmt.ColorExpr.Location);
        if (stmt.ColorExpr is LiteralExpr literalColor && literalColor.Value.Value is string colorString)
            if (!WallEColor.TryParse(colorString, out _))
                _errors.Add(new SemanticError(stmt.ColorExpr.Location, $"Invalid color string: '{colorString}'."));

        return Result<Type>.Success(typeof(void));
    }

    public Result<Type> VisitSizeStmt(SizeStmt stmt)
    {
        CheckCommandArg(stmt.SizeExpr, typeof(IntegerOrBool), "Size", "size", stmt.SizeExpr.Location);
        return Result<Type>.Success(typeof(void));
    }

    private void CheckDirRange(Expr argExpr, string name)
    {
        if (argExpr is LiteralExpr lit && lit.Value.Value is IntegerOrBool value && (value < -1 || value > 1))
            _errors.Add(new SemanticError(lit.Location, $"Argument '{name}' for DrawLine must be -1, 0, or 1."));
    }

    public Result<Type> VisitDrawLineStmt(DrawLineStmt stmt)
    {
        CheckCommandArg(stmt.DirX, typeof(IntegerOrBool), "DrawLine", "DirX", stmt.DirX.Location);
        CheckCommandArg(stmt.DirY, typeof(IntegerOrBool), "DrawLine", "DirY", stmt.DirY.Location);
        CheckCommandArg(stmt.Distance, typeof(IntegerOrBool), "DrawLine", "Distance", stmt.Distance.Location);

        CheckDirRange(stmt.DirX, "DirX");
        CheckDirRange(stmt.DirY, "DirY");
        return Result<Type>.Success(typeof(void));
    }

    public Result<Type> VisitDrawCircleStmt(DrawCircleStmt stmt)
    {
        CheckCommandArg(stmt.DirX, typeof(IntegerOrBool), "DrawCircle", "DirX", stmt.DirX.Location);
        CheckCommandArg(stmt.DirY, typeof(IntegerOrBool), "DrawCircle", "DirY", stmt.DirY.Location);
        CheckCommandArg(stmt.Radius, typeof(IntegerOrBool), "DrawCircle", "Radius", stmt.Radius.Location);

        CheckDirRange(stmt.DirX, "DirX");
        CheckDirRange(stmt.DirY, "DirY");
        return Result<Type>.Success(typeof(void));
    }

    public Result<Type> VisitDrawRectangleStmt(DrawRectangleStmt stmt)
    {
        CheckCommandArg(stmt.DirX, typeof(IntegerOrBool), "DrawRectangle", "DirX", stmt.DirX.Location);
        CheckCommandArg(stmt.DirY, typeof(IntegerOrBool), "DrawRectangle", "DirY", stmt.DirY.Location);
        CheckCommandArg(stmt.Distance, typeof(IntegerOrBool), "DrawRectangle", "Distance", stmt.Distance.Location);
        CheckCommandArg(stmt.Width, typeof(IntegerOrBool), "DrawRectangle", "Width", stmt.Width.Location);
        CheckCommandArg(stmt.Height, typeof(IntegerOrBool), "DrawRectangle", "Height", stmt.Height.Location);

        CheckDirRange(stmt.DirX, "DirX");
        CheckDirRange(stmt.DirY, "DirY");
        return Result<Type>.Success(typeof(void));
    }

    public Result<Type> VisitFillStmt(FillStmt stmt)
    {
        return Result<Type>.Success(typeof(void));
    }

    public Result<Type> VisitAssignStmt(AssignStmt stmt)
    {
        var valueResult = stmt.Value.Accept(this);
        if (!valueResult.IsSuccess) return Result<Type>.Failure(valueResult.Errors);

        if (valueResult.Value == typeof(void))
        {
            var error = new SemanticError(stmt.Value.Location, "Cannot assign an expression of void type.");
            _errors.Add(error);
            return Result<Type>.Failure(error);
        }

        _symbolTable[stmt.Name] = valueResult.Value; // Almacena el tipo (ej. IntegerOrBool, string)
        return Result<Type>.Success(typeof(void));
    }

    public Result<Type> VisitLabelStmt(LabelStmt stmt)
    {
        if (!_definedLabels.Add(stmt.Label))
            _errors.Add(new SemanticError(stmt.Location, $"Label '{stmt.Label}' is already defined."));
        return Result<Type>.Success(typeof(void));
    }

    public Result<Type> VisitGoToStmt(GoToStmt stmt)
    {
        _gotoUsages.Add((stmt.Label, stmt.Location));
        var conditionResult = stmt.Condition.Accept(this);
        if (conditionResult.IsSuccess)
            if (conditionResult.Value != typeof(IntegerOrBool))
                _errors.Add(new SemanticError(stmt.Condition.Location,
                    "Condition for GoTo statement must be a boolean expression."));

        return Result<Type>.Success(typeof(void));
    }
}