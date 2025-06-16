// File: src\PixelWallE.WPF\ViewModels\MainViewModel.cs
// ==================================================

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PixelWallE.Core.Drawing;
using PixelWallE.Core.Errors;
using PixelWallE.Core.Services;
using PixelWallE.WPF.Converters;
using PixelWallE.WPF.Services;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace PixelWallE.WPF.ViewModels{

public enum ExecutionMode { Instant, StepByStep, PixelByPixel }

public partial class MainViewModel : ObservableObject, IDisposable
{
    #region Fields
    private readonly ICompilerService _compilerService;
    private readonly IExecutionService _executionService;
    private readonly IFileService _fileService;
    private readonly IImageService _imageService;
    private SKBitmap? _currentBitmap;
    private string _savedSourceCode = string.Empty;
    private CancellationTokenSource? _cancellationTokenSource;
    #endregion

    #region Properties (Observables y de Estado)
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
    [NotifyCanExecuteChangedFor(nameof(StopExecutionCommand))]
    [NotifyCanExecuteChangedFor(nameof(ResetCanvasCommand))]
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

    [ObservableProperty]
    private ExecutionMode _selectedExecutionMode = ExecutionMode.Instant;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ExecutionSpeedText))]
    private int _executionDelay = 25; // Delay en ms

    public string ExecutionSpeedText => $"{_executionDelay} ms delay";
    #endregion

    #region Constructors
    public MainViewModel() : this(new CompilerService(), new ExecutionService(), new FileService(), new ImageService())
    {
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
    
        _cancellationTokenSource = new CancellationTokenSource();
    
        try
        {
            var compileResult = _compilerService.Compile(SourceCode);
            if (!compileResult.IsSuccess)
            {
                StatusMessage = "Compilation failed";
                AddErrors("Compilation Errors:", compileResult.Errors);
                IsExecuting = false;
                return;
            }
    
            StatusMessage = "Executing...";
            InitializeBitmapForRun();
    
            var progress = new Progress<DrawingUpdate>(update =>
            {
                // Este código se ejecuta en el hilo de UI
                HandleDrawingUpdate(update);
            });
    
            // Ejecutar en un hilo de fondo para no bloquear la UI
            await Task.Run(async () =>
            {
                await _executionService.ExecuteAsync(
                    compileResult.Value, 
                    _currentBitmap, 
                    CanvasWidth, 
                    CanvasHeight,
                    progress,
                    _cancellationTokenSource.Token);
            }, _cancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            // Este catch se activa si la cancelación ocurre antes de que la ejecución comience
            // o si no se maneja dentro del propio servicio.
            if (IsExecuting) // Evitar doble mensaje si ya se manejó en el progress handler
            {
                StatusMessage = "Execution cancelled";
                Messages.Add("✗ Execution cancelled by user");
                IsExecuting = false;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Unexpected error: {ex.Message}";
            Messages.Add($"✗ Unexpected error: {ex.Message}");
            IsExecuting = false;
        }
        finally
        {
            // IsExecuting será puesto a false por el handler de `DrawingUpdate.Complete` o `Error`
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        }
    }
    
    // NUEVO MÉTODO
    private async void HandleDrawingUpdate(DrawingUpdate update)
    {
        // El handler se llama en el hilo UI, no se necesita Dispatcher.
        if (update.Bitmap != null)
        {
            _currentBitmap = update.Bitmap; // Actualizar nuestra referencia
            UpdateRenderedImage(update.Bitmap);
        }
    
        // No continuar si la ejecución ya se detuvo
        if (!IsExecuting && !update.IsComplete && !update.IsError) return;

        bool shouldDelay = false;
        switch (update.Type)
        {
            case DrawingUpdateType.Pixel:
                if (SelectedExecutionMode == ExecutionMode.PixelByPixel) shouldDelay = true;
                StatusMessage = "Executing (Pixel by Pixel)...";
                break;
                
            case DrawingUpdateType.Step:
                if (SelectedExecutionMode == ExecutionMode.StepByStep) shouldDelay = true;
                StatusMessage = "Executing (Step by Step)...";
                break;
                
            case DrawingUpdateType.Complete:
                StatusMessage = update.Message ?? "Execution completed";
                if (update.Errors != null && update.Errors.Any())
                {
                    AddErrors("Execution finished with non-fatal errors:", update.Errors);
                }
                else
                {
                    Messages.Add($"✓ {StatusMessage}");
                }
                IsExecuting = false;
                break;
                
            case DrawingUpdateType.Error:
                StatusMessage = update.Message ?? "Error occurred";
                if (update.Errors != null && update.Errors.Any())
                {
                    AddErrors("Execution failed:", update.Errors);
                }
                IsExecuting = false;
                break;
        }

        if (shouldDelay && ExecutionDelay > 0)
        {
            try
            {
                await Task.Delay(ExecutionDelay, _cancellationTokenSource?.Token ?? CancellationToken.None);
            }
            catch (OperationCanceledException) { /* Ignorar, es esperado */ }
        }
    }
    
    [RelayCommand(CanExecute = nameof(CanStop))]
    private void StopExecution()
    {
        _cancellationTokenSource?.Cancel();
    }
    
    private bool CanStop() => IsExecuting;

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
    
    [RelayCommand(CanExecute = nameof(CanResetCanvas))]
    private void ResetCanvas()
    {
        ClearCurrentImage();
        StatusMessage = "Canvas reset";
        Messages.Add("🎨 Canvas reset");
    }
    
    private bool CanResetCanvas() => !IsExecuting;
    #endregion

    #region Private Methods
    
    private void InitializeBitmapForRun()
    {
        // Case 1: The canvas must be (re)created because it doesn't exist
        // or its dimensions have changed.
        if (_currentBitmap == null || 
            _currentBitmap.Width != CanvasWidth || 
            _currentBitmap.Height != CanvasHeight)
        {
            ClearCurrentImage(); // Dispose of the old one if it exists.
        
            if (!string.IsNullOrEmpty(BackgroundImagePath) && File.Exists(BackgroundImagePath))
            {
                // Decode the background image and scale it to fit the canvas.
                using var original = SKBitmap.Decode(BackgroundImagePath);
                var info = new SKImageInfo(CanvasWidth, CanvasHeight);
                _currentBitmap = new SKBitmap(info);
                original.ScalePixels(_currentBitmap, SKFilterQuality.High);
            }
            else
            {
                // Otherwise, create a new blank canvas.
                var info = new SKImageInfo(CanvasWidth, CanvasHeight);
                _currentBitmap = new SKBitmap(info);
                using (var canvas = new SKCanvas(_currentBitmap))
                {
                    canvas.Clear(SKColors.White);
                }
            }
            // Update the UI to show the new initial state.
            UpdateRenderedImage(_currentBitmap);
        }
        // Case 2: The bitmap already exists and has the correct dimensions.
        // We will reuse it, allowing drawing on top of the previous result.
        // The user must click "Reset Canvas" to start fresh.
    }
    
    private bool CanRun() => !string.IsNullOrWhiteSpace(SourceCode) && !IsExecuting &&
                             CanvasWidth > 0 && CanvasHeight > 0 &&
                             CanvasWidth <= 2048 && CanvasHeight <= 2048;

    private void UpdateWindowTitle()
    {
        var fileName = string.IsNullOrEmpty(CurrentFilePath) ? "Untitled" : Path.GetFileName(CurrentFilePath);
        WindowTitle = $"PixelWallE Studio - {fileName}{(IsDirty ? "*" : "")}";
        OnPropertyChanged(nameof(IsDirty));
    }
    
    partial void OnSourceCodeChanged(string value) => UpdateWindowTitle();

    private void UpdateRenderedImage(SKBitmap bitmap)
    {
        RenderedImage = bitmap.ToBitmapImage();
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
        foreach (var error in errors.DistinctBy(e => e.ToString()))
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
        _cancellationTokenSource?.Cancel();
        _cancellationTokenSource?.Dispose();
        ClearCurrentImage();
        GC.SuppressFinalize(this);
    }
    #endregion
}
}