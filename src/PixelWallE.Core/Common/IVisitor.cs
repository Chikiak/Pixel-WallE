using PixelWallE.Core.Parser.AST;
using PixelWallE.Core.Parser.AST.Exprs;
using PixelWallE.Core.Parser.AST.Stmts;

namespace PixelWallE.Core.Common;

public interface IVisitor<T>
{
    //Expressions
    T VisitLiteralExpr(LiteralExpr expr);
    T VisitBangExpr(BangExpr expr);
    T VisitMinusExpr(MinusExpr expr);
    T VisitAddExpr(AddExpr addExpr);
    T VisitSubtractExpr(SubtractExpr expr);
    T VisitMultiplyExpr(MultiplyExpr expr);
    T VisitDivideExpr(DivideExpr expr);
    T VisitPowerExpr(PowerExpr expr);
    T VisitModuloExpr(ModuloExpr expr);
    T VisitEqualEqualExpr(EqualEqualExpr expr);
    T VisitBangEqualExpr(BangEqualExpr expr);
    T VisitGreaterExpr(GreaterExpr expr);
    T VisitGreaterEqualExpr(GreaterEqualExpr expr);
    T VisitLessExpr(LessExpr expr);
    T VisitLessEqualExpr(LessEqualExpr expr);
    T VisitAndExpr(AndExpr expr);
    T VisitOrExpr(OrExpr expr);
    T VisitGroupExpr(GroupExpr expr);
    T VisitVariableExpr(VariableExpr expr);
    T VisitCallExpr(CallExpr expr);

    // Statements
    T VisitProgramStmt(ProgramStmt stmt);
    T VisitExpressionStmt(ExpressionStmt stmt);
    T VisitSpawnStmt(SpawnStmt stmt);
    T VisitColorStmt(ColorStmt stmt);
    T VisitSizeStmt(SizeStmt stmt);
    T VisitDrawLineStmt(DrawLineStmt stmt);
    T VisitDrawCircleStmt(DrawCircleStmt stmt);
    T VisitDrawRectangleStmt(DrawRectangleStmt stmt);
    T VisitFillStmt(FillStmt stmt);
    T VisitAssignStmt(AssignStmt stmt);
    T VisitLabelStmt(LabelStmt stmt);
    T VisitGoToStmt(GoToStmt stmt);
}