using System.Threading.Tasks;
using Microsoft.Win32;
using System;
using System.IO;

namespace PixelWallE.WPF.Services;

public record FileOperationResult(bool IsSuccess, string? FilePath, string? Content);

public interface IFileService
{
    Task<FileOperationResult> OpenPwFileAsync();
    Task<bool> SaveFileAsync(string filePath, string content);
    Task<FileOperationResult> SaveFileAsAsync(string content);
}
public class FileService : IFileService
{
    private const string FileFilter = "PixelWallE Files (*.pw)|*.pw|All files (*.*)|*.*";

    public async Task<FileOperationResult> OpenPwFileAsync()
    {
        var dialog = new OpenFileDialog
        {
            Filter = FileFilter,
            DefaultExt = ".pw"
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var content = await File.ReadAllTextAsync(dialog.FileName);
                return new FileOperationResult(true, dialog.FileName, content);
            }
            catch (Exception ex)
            {
                return new FileOperationResult(false, dialog.FileName, ex.Message);
            }
        }
        return new FileOperationResult(false, null, null);
    }

    public async Task<bool> SaveFileAsync(string filePath, string content)
    {
        try
        {
            await File.WriteAllTextAsync(filePath, content);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<FileOperationResult> SaveFileAsAsync(string content)
    {
        var dialog = new SaveFileDialog
        {
            Filter = FileFilter,
            DefaultExt = ".pw"
        };

        if (dialog.ShowDialog() == true)
        {
            var success = await SaveFileAsync(dialog.FileName, content);
            return new FileOperationResult(success, dialog.FileName, content);
        }
        return new FileOperationResult(false, null, null);
    }
}