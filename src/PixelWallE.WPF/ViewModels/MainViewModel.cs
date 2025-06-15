using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PixelWallE.Core.Errors;
using PixelWallE.WPF.Converters;
using PixelWallE.WPF.Services;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace PixelWallE.WPF.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable
{
    #region Fields
    private readonly ICompilerService _compilerService;
    private readonly IExecutionService _executionService;
    private readonly IFileService _fileService;
    private readonly IImageService _imageService;
    private SKBitmap? _currentBitmap;
    private string _savedSourceCode = string.Empty; // Para detectar si el archivo está "sucio"
    #endregion

    #region Properties
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveFileCommand))]
    [NotifyCanExecuteChangedFor(nameof(SaveFileAsCommand))]
    private string _sourceCode = GetDefaultCode();

    [ObservableProperty]
    private BitmapImage? _renderedImage;

    [ObservableProperty]
    private BitmapImage? _backgroundPreview;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunCommand))]
    private bool _isExecuting;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunCommand))]
    private int _canvasWidth = 500;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunCommand))]
    private int _canvasHeight = 500;
    
    [ObservableProperty]
    private string _currentFilePath = string.Empty;

    [ObservableProperty]
    private string? _backgroundImagePath;

    [ObservableProperty]
    private string _windowTitle = "PixelWallE Studio - Untitled*";
    
    public ObservableCollection<string> Messages { get; } = new();

    public bool IsDirty => _sourceCode != _savedSourceCode;
    #endregion

    #region Constructors
    public MainViewModel() : this(new CompilerService(), new ExecutionService(), new FileService(), new ImageService())
    {
        _savedSourceCode = _sourceCode;
        UpdateWindowTitle();
    }

    public MainViewModel(ICompilerService compilerService, IExecutionService executionService, IFileService fileService, IImageService imageService)
    {
        _compilerService = compilerService;
        _executionService = executionService;
        _fileService = fileService;
        _imageService = imageService;
        _savedSourceCode = _sourceCode;
        UpdateWindowTitle();
    }
    #endregion

    #region Commands
    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task RunAsync()
    {
        IsExecuting = true;
        StatusMessage = "Compiling...";
        Messages.Clear();
        ClearCurrentImage();

        try
        {
            var compileResult = await _compilerService.CompileAsync(SourceCode);
            if (!compileResult.IsSuccess)
            {
                StatusMessage = "Compilation failed";
                AddErrors("Compilation Errors:", compileResult.Errors);
                return;
            }

            StatusMessage = "Executing...";
            
            var executionOutcome = await _executionService.ExecuteAsync(
                compileResult.Value, CanvasWidth, CanvasHeight, BackgroundImagePath);

            if (executionOutcome.Bitmap != null)
            {
                UpdateRenderedImage(executionOutcome.Bitmap);
            }

            if (!executionOutcome.IsSuccess)
            {
                StatusMessage = "Execution completed with errors";
                AddErrors("Execution Errors:", executionOutcome.Errors);
            }
            else
            {
                StatusMessage = "Execution completed successfully";
                Messages.Add("✓ Compilation and execution completed successfully");
            }
        }
        catch (Exception ex)
        {
            StatusMessage = "Unexpected error occurred";
            Messages.Add($"✗ Unexpected error: {ex.Message}");
        }
        finally
        {
            IsExecuting = false;
            OnPropertyChanged(nameof(IsDirty)); // Actualizar estado sucio
            UpdateWindowTitle();
        }
    }

    [RelayCommand]
    private void NewFile()
    {
        SourceCode = GetDefaultCode();
        _savedSourceCode = SourceCode;
        CurrentFilePath = string.Empty;
        UpdateWindowTitle();
        Messages.Clear();
        StatusMessage = "New file created.";
    }

    [RelayCommand]
    private async Task OpenFileAsync()
    {
        var result = await _fileService.OpenPwFileAsync();
        if (result.IsSuccess && result.FilePath != null && result.Content != null)
        {
            SourceCode = result.Content;
            _savedSourceCode = result.Content;
            CurrentFilePath = result.FilePath;
            UpdateWindowTitle();
            StatusMessage = "File opened successfully.";
            Messages.Clear();
            Messages.Add($"✓ Opened: {CurrentFilePath}");
        }
        else if(result.FilePath != null)
        {
            StatusMessage = "Failed to open file.";
            Messages.Add($"✗ Error opening file '{result.FilePath}': {result.Content}");
        }
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task SaveFileAsync()
    {
        if (string.IsNullOrEmpty(CurrentFilePath))
        {
            await SaveFileAsAsync();
        }
        else
        {
            StatusMessage = "Saving...";
            var success = await _fileService.SaveFileAsync(CurrentFilePath, SourceCode);
            if (success)
            {
                _savedSourceCode = SourceCode;
                UpdateWindowTitle();
                StatusMessage = "File saved successfully.";
                Messages.Add($"✓ File saved to: {CurrentFilePath}");
            }
            else
            {
                StatusMessage = "Failed to save file.";
                Messages.Add($"✗ Error saving file to: {CurrentFilePath}");
            }
        }
    }

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task SaveFileAsAsync()
    {
        StatusMessage = "Saving as...";
        var result = await _fileService.SaveFileAsAsync(SourceCode);
        if (result.IsSuccess && result.FilePath != null)
        {
            _savedSourceCode = SourceCode;
            CurrentFilePath = result.FilePath;
            UpdateWindowTitle();
            StatusMessage = "File saved successfully.";
            Messages.Add($"✓ File saved to: {CurrentFilePath}");
        }
        else
        {
            StatusMessage = "Save as cancelled or failed.";
            if(result.Content != null) Messages.Add($"✗ Save as failed: {result.Content}");
        }
    }
    
    [RelayCommand]
    private async Task LoadBackgroundImageAsync()
    {
        BackgroundImagePath = await _imageService.SelectImageAsync();
        if (string.IsNullOrEmpty(BackgroundImagePath)) return;

        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(BackgroundImagePath);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();
            BackgroundPreview = bitmap;
            StatusMessage = "Background image loaded.";
            Messages.Add($"🖼️ Background set: {Path.GetFileName(BackgroundImagePath)}");
        }
        catch (Exception ex)
        {
            StatusMessage = "Failed to load background image.";
            Messages.Add($"✗ Error loading background: {ex.Message}");
            BackgroundImagePath = null;
        }
    }

    [RelayCommand]
    private void ClearBackgroundImage()
    {
        BackgroundImagePath = null;
        BackgroundPreview = null;
        StatusMessage = "Background image cleared.";
        Messages.Add("🖼️ Background cleared.");
    }
    
    [RelayCommand]
    private void SaveImage()
    {
        if (_currentBitmap == null || IsExecuting) return;
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Filter = "PNG Image|*.png|JPEG Image|*.jpg|BMP Image|*.bmp",
            DefaultExt = "png",
            FileName = $"pixelwalle_{DateTime.Now:yyyyMMdd_HHmmss}.png"
        };

        if (dialog.ShowDialog() != true) return;

        try
        {
            SKEncodedImageFormat format = Path.GetExtension(dialog.FileName).ToLower() switch
            {
                ".jpg" or ".jpeg" => SKEncodedImageFormat.Jpeg,
                ".bmp" => SKEncodedImageFormat.Bmp,
                _ => SKEncodedImageFormat.Png,
            };

            using var image = SKImage.FromBitmap(_currentBitmap);
            using var data = image.Encode(format, 95);
            using var stream = File.OpenWrite(dialog.FileName);
            data.SaveTo(stream);

            Messages.Add($"✓ Image saved to: {dialog.FileName}");
            StatusMessage = "Image saved successfully";
        }
        catch (Exception ex)
        {
            Messages.Add($"✗ Failed to save image: {ex.Message}");
            StatusMessage = "Failed to save image";
        }
    }

    [RelayCommand]
    private void ClearOutput()
    {
        Messages.Clear();
        StatusMessage = "Ready";
    }

    [RelayCommand]
    private void ResetCode()
    {
        if (IsExecuting) return;
        SourceCode = GetDefaultCode();
        ClearOutput();
    }
    #endregion

    #region Private Methods
    private bool CanRun() => !string.IsNullOrWhiteSpace(SourceCode) && !IsExecuting &&
                             CanvasWidth > 0 && CanvasHeight > 0 &&
                             CanvasWidth <= 2048 && CanvasHeight <= 2048;

    private void UpdateWindowTitle()
    {
        var fileName = string.IsNullOrEmpty(CurrentFilePath) ? "Untitled" : Path.GetFileName(CurrentFilePath);
        WindowTitle = $"PixelWallE Studio - {fileName}{(IsDirty ? "*" : "")}";
        OnPropertyChanged(nameof(IsDirty)); // Notificar cambio de IsDirty
    }
    
    partial void OnSourceCodeChanged(string value)
    {
        UpdateWindowTitle();
    }

    private void UpdateRenderedImage(SKBitmap bitmap)
    {
        ClearCurrentImage();
        _currentBitmap = bitmap;
        RenderedImage = _currentBitmap.ToBitmapImage();
    }

    private void ClearCurrentImage()
    {
        RenderedImage = null;
        _currentBitmap?.Dispose();
        _currentBitmap = null;
    }

    private void AddErrors(string header, IEnumerable<Error> errors)
    {
        Messages.Add(header);
        foreach (var error in errors)
        {
            Messages.Add($"  ✗ {error}");
        }
    }

    private static string GetDefaultCode() => """
# PixelWallE Sample Code
# Welcome to PixelWallE Studio!

Spawn(250, 250)

# Draw a blue circle
Color("blue")
Size(3)
DrawCircle(0, 0, 100)

# Fill with yellow
Color("#FFFF00")
Fill()

# This part will cause an error to show partial rendering
Color("invalid-color")
""";
    #endregion
    
    #region IDisposable
    public void Dispose()
    {
        ClearCurrentImage();
        GC.SuppressFinalize(this);
    }
    #endregion
}