using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PixelWallE.WPF.Services;
using PixelWallE.WPF.Converters;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using SkiaSharp;
using System.Windows;

namespace PixelWallE.WPF.ViewModels;

public partial class MainViewModel : ObservableObject, IDisposable
{
    private readonly ICompilerService _compilerService;
    private readonly IExecutionService _executionService;
    private SKBitmap? _currentBitmap;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunCommand))]
    private string _sourceCode = GetDefaultCode();

    [ObservableProperty]
    private BitmapImage? _renderedImage;

    [ObservableProperty]
    private bool _isExecuting;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunCommand))]
    private int _canvasWidth = 500;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RunCommand))]
    private int _canvasHeight = 500;

    public ObservableCollection<string> Messages { get; } = new();

    public MainViewModel() : this(new CompilerService(), new ExecutionService())
    {
    }

    public MainViewModel(ICompilerService compilerService, IExecutionService executionService)
    {
        _compilerService = compilerService ?? throw new ArgumentNullException(nameof(compilerService));
        _executionService = executionService ?? throw new ArgumentNullException(nameof(executionService));
    }

    private static string GetDefaultCode()
    {
        return """
               # PixelWallE Sample Code
               Spawn(250, 250)

               # Draw a blue circle
               Color("blue")
               Size(3)
               DrawCircle(0, 0, 100)

               # Fill with yellow
               Color("#FFFF00")
               Fill()
               """;
    }

    private bool CanRun() => !string.IsNullOrWhiteSpace(SourceCode) && 
                             !IsExecuting && 
                             CanvasWidth > 0 && 
                             CanvasHeight > 0 &&
                             CanvasWidth <= 2000 && 
                             CanvasHeight <= 2000;

    [RelayCommand(CanExecute = nameof(CanRun))]
    private async Task RunAsync()
    {
        IsExecuting = true;
        StatusMessage = "Compiling...";
        Messages.Clear();
        ClearCurrentImage();

        try
        {
            await Application.Current.Dispatcher.InvokeAsync(() => { }); // Ensure UI updates

            // Compilation Phase
            var compileResult = await _compilerService.CompileAsync(SourceCode);

            if (!compileResult.IsSuccess)
            {
                StatusMessage = "Compilation failed";
                AddErrors("Compilation Errors:", compileResult.Errors);
                return;
            }

            StatusMessage = "Executing...";
            await Application.Current.Dispatcher.InvokeAsync(() => { }); // Ensure UI updates

            // Execution Phase
            var executionResult = await _executionService.ExecuteAsync(
                compileResult.Value, CanvasWidth, CanvasHeight);

            if (!executionResult.IsSuccess)
            {
                StatusMessage = "Execution failed";
                AddErrors("Execution Errors:", executionResult.Errors);
                return;
            }

            // Success - Update UI
            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                UpdateRenderedImage(executionResult.Value);
                StatusMessage = "Execution completed successfully";
                Messages.Add("✓ Compilation and execution completed successfully");
            });
        }
        catch (Exception ex)
        {
            StatusMessage = "Unexpected error occurred";
            Messages.Add($"✗ Unexpected error: {ex.Message}");
            if (ex.InnerException != null)
            {
                Messages.Add($"  Inner exception: {ex.InnerException.Message}");
            }
        }
        finally
        {
            IsExecuting = false;
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

    [RelayCommand]
    private void SaveImage()
    {
        if (_currentBitmap == null || IsExecuting) return;

        try
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "PNG Image|*.png|JPEG Image|*.jpg|All Files|*.*",
                DefaultExt = "png",
                FileName = "pixelwalle_output"
            };

            if (dialog.ShowDialog() == true)
            {
                using var image = SKImage.FromBitmap(_currentBitmap);
                using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                using var stream = System.IO.File.OpenWrite(dialog.FileName);
                data.SaveTo(stream);
                    
                Messages.Add($"✓ Image saved to: {dialog.FileName}");
                StatusMessage = "Image saved successfully";
            }
        }
        catch (Exception ex)
        {
            Messages.Add($"✗ Failed to save image: {ex.Message}");
            StatusMessage = "Failed to save image";
        }
    }

    partial void OnCanvasWidthChanged(int value)
    {
        if (value <= 0) CanvasWidth = 1;
        if (value > 2000) CanvasWidth = 2000;
    }

    partial void OnCanvasHeightChanged(int value)
    {
        if (value <= 0) CanvasHeight = 1;
        if (value > 2000) CanvasHeight = 2000;
    }

    private void UpdateRenderedImage(SKBitmap bitmap)
    {
        ClearCurrentImage();
        _currentBitmap = bitmap;
        RenderedImage = bitmap.ToBitmapImage();
    }

    private void ClearCurrentImage()
    {
        RenderedImage = null;
        _currentBitmap?.Dispose();
        _currentBitmap = null;
    }

    private void AddErrors(string header, System.Collections.Generic.IEnumerable<PixelWallE.Core.Errors.Error> errors)
    {
        Messages.Add(header);
        foreach (var error in errors)
        {
            Messages.Add($"  ✗ {error}");
        }
    }

    public void Dispose()
    {
        ClearCurrentImage();
        GC.SuppressFinalize(this);
    }
}