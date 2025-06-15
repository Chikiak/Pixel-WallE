using System.Windows;

namespace PixelWallE.WPF.Views;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    protected override void OnClosed(System.EventArgs e)
    {
        // Cleanup resources
        if (DataContext is System.IDisposable disposable)
        {
            disposable.Dispose();
        }
        base.OnClosed(e);
    }
}