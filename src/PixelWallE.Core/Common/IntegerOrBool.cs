namespace PixelWallE.Core.Common;

public class IntegerOrBool
{
    public IntegerOrBool(int value)
    {
        Value = value;
    }

    public IntegerOrBool(bool value)
    {
        Value = value;
    }

    public object Value { get; }

    public static implicit operator int(IntegerOrBool value)
    {
        if (value.Value is int integer) return integer;
        if (value.Value is bool boolean) return boolean ? 1 : 0;
        return 0;
    }

    public static implicit operator bool(IntegerOrBool value)
    {
        if (value.Value is int integer) return integer != 0;
        if (value.Value is bool boolean) return boolean;
        return false;
    }

    public static implicit operator IntegerOrBool(int value)
    {
        return new IntegerOrBool(value);
    }

    public static implicit operator IntegerOrBool(bool value)
    {
        return new IntegerOrBool(value);
    }
}