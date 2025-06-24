using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SkiaSharp;

namespace PixelWallE.WPF.Converters;

public static class SkiaBitmapConverter
{
    public static BitmapImage? ToBitmapImage(this SKBitmap skiaBitmap)
    {
        if (skiaBitmap == null) return null;

        try
        {
            using var image = SKImage.FromBitmap(skiaBitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = data.AsStream();

            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.StreamSource = stream;
            bitmapImage.EndInit();
            bitmapImage.Freeze(); // Para thread safety

            return bitmapImage;
        }
        catch
        {
            return null;
        }
    }

    public static WriteableBitmap? ToWriteableBitmap(this SKBitmap skiaBitmap)
    {
        if (skiaBitmap == null) return null;

        try
        {
            var writeableBitmap = new WriteableBitmap(
                skiaBitmap.Width,
                skiaBitmap.Height,
                96, 96,
                PixelFormats.Bgra32,
                null);

            writeableBitmap.Lock();

            var pixels = skiaBitmap.GetPixels();
            writeableBitmap.WritePixels(
                new Int32Rect(0, 0, skiaBitmap.Width, skiaBitmap.Height),
                pixels,
                skiaBitmap.RowBytes,
                0);

            writeableBitmap.Unlock();
            writeableBitmap.Freeze();

            return writeableBitmap;
        }
        catch
        {
            return null;
        }
    }
}