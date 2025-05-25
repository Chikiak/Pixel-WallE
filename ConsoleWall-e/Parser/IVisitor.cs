using ConsoleWall_e.Parser.AST;

namespace ConsoleWall_e.Parser;

public interface IVisitor<T>
{
    //Expressions
    T VisitLiteralExpr(LiteralExpr expr);
    T VisitUnaryExpr(UnaryExpr expr);
    T VisitBinaryExpr(BinaryExpr expr);
    T VisitGroupExpr(GroupExpr expr);
    T VisitVariableExpr(VariableExpr expr);
    T VisitCallExpr(CallExpr expr);

    // Statements
    //ToDo
    /*
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
    */
}