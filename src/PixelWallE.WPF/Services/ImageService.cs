using Microsoft.Win32;

namespace PixelWallE.WPF.Services;

public interface IImageService
{
    Task<string?> SelectImageAsync();
}

public class ImageService : IImageService
{
    public Task<string?> SelectImageAsync()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Image Files|.png;.jpg;.jpeg;.bmp|All Files|.",
            Title = "Select a Background Image"
        };
        if (dialog.ShowDialog() == true) return Task.FromResult<string?>(dialog.FileName);

        return Task.FromResult<string?>(null);
    }
}