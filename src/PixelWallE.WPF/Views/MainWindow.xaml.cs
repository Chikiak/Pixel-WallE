using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using PixelWallE.WPF.Services;
using PixelWallE.WPF.ViewModels;

namespace PixelWallE.WPF.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        LoadSyntaxHighlighting();
    }

    private void LoadSyntaxHighlighting()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "PixelWallE.WPF.PixelWallE.xshd";

            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    throw new InvalidOperationException($"Could not find the embedded resource: {resourceName}");
                }
                using (var reader = new XmlTextReader(stream))
                {
                    // Se aplica el resaltado de sintaxis al editor de código.
                    CodeEditor.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error loading syntax highlighting: {ex.Message}");
            // Opcional: Mostrar un mensaje al usuario.
        }
    }
        
    protected override void OnClosed(EventArgs e)
    {
        if (DataContext is IDisposable disposable)
        {
            disposable.Dispose();
        }
        base.OnClosed(e);
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void SwitchToDark_Click(object sender, RoutedEventArgs e)
    {
        ThemeService.ApplyTheme(Theme.Dark);
    }

    private void SwitchToLight_Click(object sender, RoutedEventArgs e)
    {
        ThemeService.ApplyTheme(Theme.Light);
    }
        
    // EVENTO AÑADIDO: Gestiona el clic en el botón de configuración para abrir el menú.
    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.ContextMenu != null)
        {
            button.ContextMenu.PlacementTarget = button;
            button.ContextMenu.IsOpen = true;
        }
    }
}