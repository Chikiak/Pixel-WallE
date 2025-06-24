namespace PixelWallE.Core.Tokens;

public enum TokenType
{
    //Single-character tokens
    LeftParen,
    RightParen, // ()
    LeftBracket,
    RightBracket, // []
    Comma, // ,
    Minus,
    Plus,
    Slash,
    Modulo, // - + / %

    //One or two character tokens
    Star,
    Power, // * & **
    Bang,
    BangEqual, // ! & !=
    Equal,
    EqualEqual, // = & ==
    Greater,
    GreaterEqual, // > & >=
    Less,
    LessEqual, // < & <=
    Assign, // <-

    //Literals
    Identifier,
    String,
    Number,

    //Comandos
    Spawn,
    Color,
    Size,
    DrawLine,
    DrawCircle,
    DrawRectangle,
    Fill,
    Filling,
    Respawn,

    //Palabras Clave
    And,
    Or,
    True,
    False,
    GoTo,
    Endl,

    EOF //End of File
}