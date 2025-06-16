namespace PixelWallE.Core.Drawing;

public enum DrawingUpdateType
{
    Pixel,    // Un pixel individual o un lote pequeño de píxeles
    Step,     // Un comando de dibujo completo (DrawLine, DrawCircle, etc.) ha terminado
    Complete, // La ejecución ha terminado (con o sin éxito)
    Error     // Un error fatal ha detenido la ejecución
}