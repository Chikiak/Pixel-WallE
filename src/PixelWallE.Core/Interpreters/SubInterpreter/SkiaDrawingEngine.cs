using PixelWallE.Core.Common;
using PixelWallE.Core.Interpreters.Interfaces;
using SkiaSharp;

namespace PixelWallE.Core.Interpreters.SubInterpreter;

public class SkiaDrawingEngine
{
    public Dictionary<(int x, int y), WallEColor> DrawLine(IWallEState wallEState, SkiaCanvas canvas, int dirX,
        int dirY, int distance)
    {
        ValidateDirection(dirX, dirY, distance);
        var result = new Dictionary<(int x, int y), WallEColor>();
        var startX = wallEState.Position.X;
        var startY = wallEState.Position.Y;
        for (var i = 0; i < distance; i++)
        {
            var x = startX + dirX * i;
            var y = startY + dirY * i;

            if (canvas.IsInBounds(x, y)) ApplyBrushSize(x, y, wallEState.BrushSize, wallEState.Color, canvas, result);
        }

        var finalX = startX + dirX * (distance > 0 ? distance - 1 : 0);
        var finalY = startY + dirY * (distance > 0 ? distance - 1 : 0);
        wallEState.SetPosition(finalX, finalY);

        return result;
    }

    public Dictionary<(int x, int y), WallEColor> DrawCircle(IWallEState wallEState, SkiaCanvas canvas, int dirX,
        int dirY, int radius)
    {
        if (radius <= 0) throw new ArgumentException("Radius must be positive", nameof(radius));
        ValidateDir(dirX, dirY);

        var result = new Dictionary<(int x, int y), WallEColor>();

        var centerX = wallEState.Position.X + dirX * radius;
        var centerY = wallEState.Position.Y + dirY * radius;

        if (wallEState.IsFilling)
            DrawFilledCircle(centerX, centerY, radius, wallEState.BrushSize, wallEState.Color, canvas, result);
        else
            DrawCircleOutline(centerX, centerY, radius, wallEState.BrushSize, wallEState.Color, canvas, result);

        wallEState.SetPosition(centerX, centerY);

        return result;
    }

    public Dictionary<(int x, int y), WallEColor> DrawRectangle(IWallEState wallEState, SkiaCanvas canvas, int dirX,
        int dirY, int distance, int width, int height)
    {
        ValidateDirection(dirX, dirY, distance);
        if (width <= 0) throw new ArgumentException("Width must be positive", nameof(width));
        if (height <= 0) throw new ArgumentException("Height must be positive", nameof(height));

        var result = new Dictionary<(int x, int y), WallEColor>();

        var startX = wallEState.Position.X - width / 2;
        var startY = wallEState.Position.Y - height / 2;

        if (wallEState.IsFilling)
            DrawFilledRectangle(startX, startY, width, height, wallEState.BrushSize, wallEState.Color, canvas, result);
        else
            DrawRectangleOutline(startX, startY, width, height, wallEState.BrushSize, wallEState.Color, canvas, result);

        var centerX = wallEState.Position.X + dirX * distance;
        var centerY = wallEState.Position.Y + dirY * distance;
        wallEState.SetPosition(centerX, centerY);

        return result;
    }

    public Dictionary<(int x, int y), WallEColor> Fill(IWallEState wallEState, SkiaCanvas canvas)
    {
        var startX = wallEState.Position.X;
        var startY = wallEState.Position.Y;
        var result = new Dictionary<(int x, int y), WallEColor>();

        if (!canvas.IsInBounds(startX, startY))
            return result;

        var targetColor = canvas.GetPixel(startX, startY);
            
        // Note: The original canvas color (targetColor) is used for the comparison,
        // but the blending for the final color happens in the Interpreter's ApplyDrawingChangesAsync
        // This is kept to maintain the refactored structure.
        var fillColor = wallEState.Color;

        if (targetColor == fillColor)
            return result;

        FloodFill(startX, startY, targetColor, fillColor, canvas, result);
        return result;
    }

    /// <summary>
    /// Correctly blends a source color (from the brush) onto a target color (from the canvas).
    /// </summary>
    /// <param name="targetColor">The existing color on the canvas.</param>
    /// <param name="sourceColor">The new color from the brush to apply.</param>
    /// <returns>The resulting blended color.</returns>
    public WallEColor BlendColors(WallEColor targetColor, WallEColor sourceColor)
    {
        // If the new color (brush) is fully opaque, it replaces the old color.
        if (sourceColor.Alpha == 255)
        {
            return sourceColor;
        }

        // If the new color (brush) is fully transparent, the old color (canvas) remains unchanged.
        if (sourceColor.Alpha == 0)
        {
            return targetColor;
        }

        // Standard alpha blending: C_out = C_src * A_src + C_bg * (1 - A_src)
        var sourceAlpha = sourceColor.Alpha / 255.0f;
        var targetInverseAlpha = 1.0f - sourceAlpha;

        var r = (byte)((sourceColor.Red * sourceAlpha) + (targetColor.Red * targetInverseAlpha));
        var g = (byte)((sourceColor.Green * sourceAlpha) + (targetColor.Green * targetInverseAlpha));
        var b = (byte)((sourceColor.Blue * sourceAlpha) + (targetColor.Blue * targetInverseAlpha));
            
        // The final alpha is a combination of both, but for an opaque canvas, the result is opaque.
        var a = (byte)(sourceColor.Alpha + targetColor.Alpha * targetInverseAlpha);


        return new WallEColor(r, g, b, a);
    }

    #region Métodos Privados de Ayuda

    private void ValidateDirection(int dirX, int dirY, int distance)
    {
        if (Math.Abs(dirX) > 1 || Math.Abs(dirY) > 1)
            throw new ArgumentException("Direction values must be -1, 0, or 1");
    }

    private void ValidateDir(int dirX, int dirY)
    {
        if (Math.Abs(dirX) > 1 || Math.Abs(dirY) > 1)
            throw new ArgumentException("Offset values must be -1, 0, or 1");
    }

    private void ApplyBrushSize(int centerX, int centerY, int brushSize, WallEColor color, SkiaCanvas canvas,
        Dictionary<(int x, int y), WallEColor> result)
    {
        var halfSize = brushSize / 2;

        for (var dx = -halfSize; dx <= halfSize; dx++)
        for (var dy = -halfSize; dy <= halfSize; dy++)
        {
            var x = centerX + dx;
            var y = centerY + dy;

            if (canvas.IsInBounds(x, y))
            {
                // Do not overwrite a pixel that will be drawn in the same operation
                if (result.ContainsKey((x, y))) continue;
                result[(x, y)] = color;
            }
        }
    }

    private void DrawCircleOutline(int centerX, int centerY, int radius, int brushSize, WallEColor color,
        SkiaCanvas canvas, Dictionary<(int x, int y), WallEColor> result)
    {
        var x = 0;
        var y = radius;
        var d = 3 - 2 * radius;
        
        DrawCirclePoints(centerX, centerY, x, y, brushSize, color, canvas, result);

        while (y >= x)
        {
            x++;

            if (d > 0)
            {
                y--;
                d = d + 4 * (x - y) + 10;
            }
            else
            {
                d = d + 4 * x + 6;
            }

            DrawCirclePoints(centerX, centerY, x, y, brushSize, color, canvas, result);
        }
    }

    private void DrawCirclePoints(int centerX, int centerY, int x, int y, int brushSize, WallEColor color,
        SkiaCanvas canvas, Dictionary<(int x, int y), WallEColor> result)
    {
        ApplyBrushSize(centerX + x, centerY + y, brushSize, color, canvas, result);
        ApplyBrushSize(centerX - x, centerY + y, brushSize, color, canvas, result);
        ApplyBrushSize(centerX + x, centerY - y, brushSize, color, canvas, result);
        ApplyBrushSize(centerX - x, centerY - y, brushSize, color, canvas, result);
        ApplyBrushSize(centerX + y, centerY + x, brushSize, color, canvas, result);
        ApplyBrushSize(centerX - y, centerY + x, brushSize, color, canvas, result);
        ApplyBrushSize(centerX + y, centerY - x, brushSize, color, canvas, result);
        ApplyBrushSize(centerX - y, centerY - x, brushSize, color, canvas, result);
    }

    private void DrawFilledCircle(int centerX, int centerY, int radius, int brushSize, WallEColor color,
        SkiaCanvas canvas, Dictionary<(int x, int y), WallEColor> result)
    {
        for (var y = -radius; y <= radius; y++)
        for (var x = -radius; x <= radius; x++)
            if (x * x + y * y <= radius * radius)
                ApplyBrushSize(centerX + x, centerY + y, 1, color, canvas, result); // Use brush size 1 for fill
    }

    private void DrawRectangleOutline(int startX, int startY, int width, int height, int brushSize, WallEColor color,
        SkiaCanvas canvas, Dictionary<(int x, int y), WallEColor> result)
    {
        for (var x = 0; x < width; x++)
        {
            ApplyBrushSize(startX + x, startY, brushSize, color, canvas, result);
            ApplyBrushSize(startX + x, startY + height - 1, brushSize, color, canvas, result);
        }

        for (var y = 1; y < height - 1; y++)
        {
            ApplyBrushSize(startX, startY + y, brushSize, color, canvas, result);
            ApplyBrushSize(startX + width - 1, startY + y, brushSize, color, canvas, result);
        }
    }

    private void DrawFilledRectangle(int startX, int startY, int width, int height, int brushSize, WallEColor color,
        SkiaCanvas canvas, Dictionary<(int x, int y), WallEColor> result)
    {
        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
            ApplyBrushSize(startX + x, startY + y, 1, color, canvas, result); // Use brush size 1 for fill
    }

    private void FloodFill(int startX, int startY, WallEColor targetColor, WallEColor fillColor, SkiaCanvas canvas,
        Dictionary<(int x, int y), WallEColor> result)
    {
        var stack = new Stack<(int x, int y)>();
            
        // We don't need a separate visited set if we check the result dictionary
        if (result.ContainsKey((startX, startY))) return;

        stack.Push((startX, startY));

        while (stack.Count > 0)
        {
            var (x, y) = stack.Pop();

            if (!canvas.IsInBounds(x, y))
                continue;

            if (result.ContainsKey((x, y)))
                continue;

            if (canvas.GetPixel(x, y) != targetColor)
                continue;

            result[(x,y)] = fillColor;

            stack.Push((x + 1, y));
            stack.Push((x - 1, y));
            stack.Push((x, y + 1));
            stack.Push((x, y - 1));
        }
    }

    #endregion
}