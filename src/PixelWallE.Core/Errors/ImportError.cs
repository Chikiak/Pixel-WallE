namespace PixelWallE.Core.Errors;

public class ImportError(string message) : Error(ErrorType.Import, message);