using ConsoleWall_e.Core.Common;

namespace ConsoleWall_e.Core.Parser.AST.Exprs;

public abstract class BinaryExpr(Expr left, Expr right, CodeLocation location) : Expr(location)
{
    public Expr Left { get; private set; } = left;
    public Expr Right { get; private set; } = right;
}

#region Arithmetic

public class AddExpr(Expr left, Expr right, CodeLocation location) : BinaryExpr(left, right, location)
{
    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitAddExpr(this);
    }
}

public class SubtractExpr(Expr left, Expr right, CodeLocation location) : BinaryExpr(left, right, location)
{
    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitSubtractExpr(this);
    }
}

public class MultiplyExpr(Expr left, Expr right, CodeLocation location) : BinaryExpr(left, right, location)
{
    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitMultiplyExpr(this);
    }
}

public class DivideExpr(Expr left, Expr right, CodeLocation location) : BinaryExpr(left, right, location)
{
    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitDivideExpr(this);
    }
}

public class PowerExpr(Expr left, Expr right, CodeLocation location) : BinaryExpr(left, right, location)
{
    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitPowerExpr(this);
    }
}

public class ModuloExpr(Expr left, Expr right, CodeLocation location) : BinaryExpr(left, right, location)
{
    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitModuloExpr(this);
    }
}

#endregion

#region Comparison

public class EqualEqualExpr(Expr left, Expr right, CodeLocation location) : BinaryExpr(left, right, location)
{
    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitEqualEqualExpr(this);
    }
}

public class BangEqualExpr(Expr left, Expr right, CodeLocation location) : BinaryExpr(left, right, location)
{
    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitBangEqualExpr(this);
    }
}

public class GreaterExpr(Expr left, Expr right, CodeLocation location) : BinaryExpr(left, right, location)
{
    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitGreaterExpr(this);
    }
}

public class GreaterEqualExpr(Expr left, Expr right, CodeLocation location) : BinaryExpr(left, right, location)
{
    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitGreaterEqualExpr(this);
    }
}

public class LessExpr(Expr left, Expr right, CodeLocation location) : BinaryExpr(left, right, location)
{
    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitLessExpr(this);
    }
}

public class LessEqualExpr(Expr left, Expr right, CodeLocation location) : BinaryExpr(left, right, location)
{
    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitLessEqualExpr(this);
    }
}

#endregion

#region Logical

public class AndExpr(Expr left, Expr right, CodeLocation location) : BinaryExpr(left, right, location)
{
    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitAndExpr(this);
    }
}

public class OrExpr(Expr left, Expr right, CodeLocation location) : BinaryExpr(left, right, location)
{
    public override T Accept<T>(IVisitor<T> visitor)
    {
        return visitor.VisitOrExpr(this);
    }
}

#endregion