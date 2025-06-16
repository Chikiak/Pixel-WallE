// File: src/PixelWallE.WPF/Views/MainWindow.xaml.cs
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using System;
using System.Linq; // <-- AÑADIR ESTE USING
using System.Reflection;
using System.Windows;
using System.Xml;

namespace PixelWallE.WPF.Views
{
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

                // --- INICIO DEL CÓDIGO DE DEPURACIÓN ---
                // Imprime todos los recursos incrustados para encontrar el nombre correcto.
                string[] resourceNames = assembly.GetManifestResourceNames();
                System.Diagnostics.Debug.WriteLine("--- Embedded Resources Found ---");
                foreach (string name in resourceNames)
                {
                    System.Diagnostics.Debug.WriteLine(name);
                }
                System.Diagnostics.Debug.WriteLine("--------------------------------");
                // --- FIN DEL CÓDIGO DE DEPURACIÓN ---

                // Busca el nombre del recurso que termina con .xshd
                string? resourceName = resourceNames.FirstOrDefault(s => s.EndsWith("PixelWallE.xshd"));
                if (resourceName == null)
                {
                    throw new InvalidOperationException("Could not find the PixelWallE.xshd embedded resource.");
                }

                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        // Esto no debería pasar si resourceName no es null, pero es una buena práctica verificarlo.
                        throw new InvalidOperationException($"Could not load embedded resource stream for '{resourceName}'.");
                    }
                    
                    using (var reader = new XmlTextReader(stream))
                    {
                        CodeEditor.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading syntax highlighting: {ex.Message}");
            }
        }

        protected override void OnClosed(System.EventArgs e)
        {
            if (DataContext is System.IDisposable disposable)
            {
                disposable.Dispose();
            }
            base.OnClosed(e);
        }
        
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}