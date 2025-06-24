using System.Text;
using PixelWallE.Core.Common;
using PixelWallE.Core.Parsers.AST;
using PixelWallE.Core.Parsers.AST.Exprs;
using PixelWallE.Core.Parsers.AST.Stmts;

namespace PixelWallE.Core.DevUtils;

public class ASTPrinter : IVisitor<string>
{
    private const int IndentSize = 4;
    private int _indentLevel;


    public string VisitLiteralExpr(LiteralExpr expr)
    {
        return Indent($"Literal({expr.Value})");
    }

    public string VisitBangExpr(BangExpr expr)
    {
        return Indent("! (NOT)\n" + VisitWithIndent(expr.Right));
    }

    public string VisitMinusExpr(MinusExpr expr)
    {
        return Indent("- (Negate)\n" + VisitWithIndent(expr.Right));
    }

    public string VisitAddExpr(AddExpr expr)
    {
        return Indent("+ (Add)\n" +
                      VisitWithIndent(expr.Left) + "\n" +
                      VisitWithIndent(expr.Right));
    }

    public string VisitSubtractExpr(SubtractExpr expr)
    {
        return Indent("- (Sub)\n" +
                      VisitWithIndent(expr.Left) + "\n" +
                      VisitWithIndent(expr.Right));
    }

    public string VisitMultiplyExpr(MultiplyExpr expr)
    {
        return Indent("* (mult)\n" +
                      VisitWithIndent(expr.Left) + "\n" +
                      VisitWithIndent(expr.Right));
    }

    public string VisitDivideExpr(DivideExpr expr)
    {
        return Indent("/ (Div)\n" +
                      VisitWithIndent(expr.Left) + "\n" +
                      VisitWithIndent(expr.Right));
    }

    public string VisitPowerExpr(PowerExpr expr)
    {
        return Indent("** (Power)\n" +
                      VisitWithIndent(expr.Left) + "\n" +
                      VisitWithIndent(expr.Right));
    }

    public string VisitModuloExpr(ModuloExpr expr)
    {
        return Indent("% (Mod)\n" +
                      VisitWithIndent(expr.Left) + "\n" +
                      VisitWithIndent(expr.Right));
    }

    public string VisitEqualEqualExpr(EqualEqualExpr expr)
    {
        return Indent("== (EqualE)\n" +
                      VisitWithIndent(expr.Left) + "\n" +
                      VisitWithIndent(expr.Right));
    }

    public string VisitBangEqualExpr(BangEqualExpr expr)
    {
        return Indent("!= (BangE)\n" +
                      VisitWithIndent(expr.Left) + "\n" +
                      VisitWithIndent(expr.Right));
    }

    public string VisitGreaterExpr(GreaterExpr expr)
    {
        return Indent("> (Greater)\n" +
                      VisitWithIndent(expr.Left) + "\n" +
                      VisitWithIndent(expr.Right));
    }

    public string VisitGreaterEqualExpr(GreaterEqualExpr expr)
    {
        return Indent("> (GreaterE)\n" +
                      VisitWithIndent(expr.Left) + "\n" +
                      VisitWithIndent(expr.Right));
    }

    public string VisitLessExpr(LessExpr expr)
    {
        return Indent("< (Less)\n" +
                      VisitWithIndent(expr.Left) + "\n" +
                      VisitWithIndent(expr.Right));
    }

    public string VisitLessEqualExpr(LessEqualExpr expr)
    {
        return Indent("<= (LessE)\n" +
                      VisitWithIndent(expr.Left) + "\n" +
                      VisitWithIndent(expr.Right));
    }

    public string VisitAndExpr(AndExpr expr)
    {
        return Indent("and\n" +
                      VisitWithIndent(expr.Left) + "\n" +
                      VisitWithIndent(expr.Right));
    }

    public string VisitOrExpr(OrExpr expr)
    {
        return Indent("or\n" +
                      VisitWithIndent(expr.Left) + "\n" +
                      VisitWithIndent(expr.Right));
    }

    public string VisitGroupExpr(GroupExpr expr)
    {
        return Indent($"Group {expr.GroupType}\n" +
                      VisitWithIndent(expr.Expr));
    }

    public string VisitVariableExpr(VariableExpr expr)
    {
        return Indent("Variable " + expr.Name);
    }

    public string VisitCallExpr(CallExpr expr)
    {
        var sb = new StringBuilder();
        sb.AppendLine(Indent("Called " + expr.CalledFunction));
        _indentLevel++;
        foreach (var argument in expr.Arguments) sb.AppendLine(argument.Accept(this));
        _indentLevel--;
        return sb.ToString();
    }

    public string VisitProgramStmt(ProgramStmt stmt)
    {
        var sb = new StringBuilder();
        sb.AppendLine(Indent("Program:"));
        _indentLevel++;
        foreach (var statement in stmt.Statements) sb.AppendLine(statement.Accept(this));
        _indentLevel--;
        return sb.ToString();
    }

    public string VisitExpressionStmt(ExpressionStmt stmt)
    {
        return Indent("ExprStmt:\n") +
               VisitWithIndent(stmt.Expr);
    }

    public string VisitSpawnStmt(SpawnStmt stmt)
    {
        return Indent("Spawn:\n") +
               VisitWithIndent(stmt.X) + "\n" +
               VisitWithIndent(stmt.Y);
    }
    
    public string VisitRespawnStmt(RespawnStmt stmt)
    {
        return Indent("Respawn:\n") +
               VisitWithIndent(stmt.X) + "\n" +
               VisitWithIndent(stmt.Y);
    }
    
    public string VisitColorStmt(ColorStmt stmt)
    {
        return Indent("Color:\n") + VisitWithIndent(stmt.ColorExpr);
    }

    public string VisitSizeStmt(SizeStmt stmt)
    {
        return Indent("Size:\n") + VisitWithIndent(stmt.SizeExpr);
    }

    public string VisitDrawLineStmt(DrawLineStmt stmt)
    {
        return Indent("DrawLine:\n") +
               VisitWithIndent(stmt.DirX) + "\n" +
               VisitWithIndent(stmt.DirY) + "\n" +
               VisitWithIndent(stmt.Distance);
    }

    public string VisitDrawCircleStmt(DrawCircleStmt stmt)
    {
        return Indent("DrawCircle:\n") +
               VisitWithIndent(stmt.DirX) + "\n" +
               VisitWithIndent(stmt.DirY) + "\n" +
               VisitWithIndent(stmt.Radius);
    }

    public string VisitDrawRectangleStmt(DrawRectangleStmt stmt)
    {
        return Indent("DrawRectangle:\n") +
               VisitWithIndent(stmt.DirX) + "\n" +
               VisitWithIndent(stmt.DirY) + "\n" +
               VisitWithIndent(stmt.Distance) + "\n" +
               VisitWithIndent(stmt.Width) + "\n" +
               VisitWithIndent(stmt.Height);
    }

    public string VisitFillStmt(FillStmt stmt)
    {
        return Indent("Fill");
    }

    public string VisitAssignStmt(AssignStmt stmt)
    {
        return Indent($"Assign {stmt.Name}\n") +
               VisitWithIndent(stmt.Value);
    }

    public string VisitLabelStmt(LabelStmt stmt)
    {
        return Indent($"Label {stmt.Label}");
    }

    public string VisitGoToStmt(GoToStmt stmt)
    {
        return Indent($"GoTo {stmt.Label}\n") +
               VisitWithIndent(stmt.Condition);
    }

    public string VisitFillingStmt(FillingStmt stmt)
    {
        return Indent("Filling:\n") + VisitWithIndent(stmt.BoolExpr);
    }

    public string Print(ASTNode node)
    {
        return node.Accept(this);
    }

    private string Indent(string text)
    {
        return new string(' ', _indentLevel * IndentSize) + text;
    }

    private string VisitWithIndent(ASTNode node)
    {
        _indentLevel++;
        var result = node.Accept(this);
        _indentLevel--;
        return result;
    }
}