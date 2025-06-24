using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using PixelWallE.WPF.Services;
using System.Xml;
using System.IO;

namespace PixelWallE.WPF.Views;

public partial class MainWindow : Window
{
    private static readonly Dictionary<Theme, IHighlightingDefinition> _highlightingCache = new();
        
    public MainWindow()
    {
        InitializeComponent();
        // Load syntax highlighting for the default (Light) theme
        ApplySyntaxHighlighting(Theme.Light);
    }

    /// <summary>
    /// Creates and applies syntax highlighting definition programmatically based on the current theme.
    /// Uses caching to improve performance on theme switches.
    /// </summary>
    /// <param name="theme">The theme for which the highlighting will be applied.</param>
    private void ApplySyntaxHighlighting(Theme theme)
    {
        try
        {
            // Check cache first for better performance
            if (_highlightingCache.TryGetValue(theme, out var cachedDefinition))
            {
                CodeEditor.SyntaxHighlighting = cachedDefinition;
                Debug.WriteLine($"Applied cached {theme} syntax highlighting");
                return;
            }

            // Generate a unique name for this theme's highlighting definition
            string themeHighlightingName = $"PixelWallE-{theme}";

            // Load the theme dictionary to get colors
            var themeUri = new Uri($"/Themes/Theme.{theme}.xaml", UriKind.Relative);
            var themeDictionary = new ResourceDictionary { Source = themeUri };

            // Helper function to get color from theme with better error handling
            Color GetThemeColor(string brushKey, Color fallback)
            {
                try
                {
                    if (themeDictionary.Contains(brushKey) && themeDictionary[brushKey] is SolidColorBrush brush)
                        return brush.Color;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Warning: Could not load color for {brushKey}: {ex.Message}");
                }
                return fallback;
            }

            // Get theme-specific colors with fallbacks
            var colors = new Dictionary<string, Color>
            {
                ["Comment"] = GetThemeColor("SyntaxCommentBrush", theme == Theme.Dark ? Color.FromRgb(106, 153, 85) : Color.FromRgb(0, 128, 0)),
                ["String"] = GetThemeColor("SyntaxStringBrush", theme == Theme.Dark ? Color.FromRgb(206, 145, 120) : Color.FromRgb(163, 21, 21)),
                ["Number"] = GetThemeColor("SyntaxNumberBrush", theme == Theme.Dark ? Color.FromRgb(181, 206, 168) : Color.FromRgb(9, 134, 88)),
                ["Boolean"] = GetThemeColor("SyntaxBooleanBrush", theme == Theme.Dark ? Color.FromRgb(86, 156, 214) : Color.FromRgb(0, 0, 255)),
                ["Commands"] = GetThemeColor("SyntaxCommandsBrush", theme == Theme.Dark ? Color.FromRgb(197, 134, 192) : Color.FromRgb(175, 0, 219)),
                ["Functions"] = GetThemeColor("SyntaxFunctionsBrush", theme == Theme.Dark ? Color.FromRgb(220, 220, 170) : Color.FromRgb(121, 94, 38)),
                ["Variables"] = GetThemeColor("SyntaxVariablesBrush", theme == Theme.Dark ? Color.FromRgb(156, 220, 254) : Color.FromRgb(0, 16, 128)),
                ["Operators"] = GetThemeColor("SyntaxOperatorsBrush", theme == Theme.Dark ? Color.FromRgb(212, 212, 212) : Color.FromRgb(31, 41, 55)),
                ["Keywords"] = GetThemeColor("SyntaxKeywordsBrush", theme == Theme.Dark ? Color.FromRgb(86, 156, 214) : Color.FromRgb(0, 0, 255)),
                ["Labels"] = GetThemeColor("SyntaxLabelsBrush", theme == Theme.Dark ? Color.FromRgb(78, 201, 176) : Color.FromRgb(38, 127, 153))
            };

            // Create XSHD programmatically with theme colors
            string xshdContent = $@"<?xml version=""1.0""?>
<SyntaxDefinition name=""{themeHighlightingName}"" xmlns=""http://icsharpcode.net/sharpdevelop/syntaxdefinition/2008"">

    <!-- Theme-specific color definitions -->
    <Color name=""Comment"" foreground=""{colors["Comment"]}"" fontStyle=""italic""/>
    <Color name=""String"" foreground=""{colors["String"]}"" />
    <Color name=""Number"" foreground=""{colors["Number"]}"" />
    <Color name=""Boolean"" foreground=""{colors["Boolean"]}"" fontWeight=""bold""/>
    <Color name=""Commands"" foreground=""{colors["Commands"]}"" fontWeight=""bold""/>
    <Color name=""Functions"" foreground=""{colors["Functions"]}"" />
    <Color name=""Variables"" foreground=""{colors["Variables"]}"" />
    <Color name=""Operators"" foreground=""{colors["Operators"]}"" />
    <Color name=""Keywords"" foreground=""{colors["Keywords"]}"" fontWeight=""bold""/>
    <Color name=""Labels"" foreground=""{colors["Labels"]}"" fontStyle=""italic""/>

    <!-- Main rules with improved regex patterns -->
    <RuleSet>
        <!-- Comments: Start with # and go to end of line -->
        <Span color=""Comment"" begin=""#"" />

        <!-- Strings: Delimited by double quotes with escape sequence support -->
        <Span color=""String"">
            <Begin>&quot;</Begin>
            <End>&quot;</End>
            <RuleSet>
                <!-- Escape sequences in strings -->
                <Span begin=""\\"" end=""."" />
            </RuleSet>
        </Span>

        <!-- Numbers: Integer and decimal literals -->
        <Rule color=""Number"">
            \b\d+(\.\d+)?\b
        </Rule>

        <!-- Booleans -->
        <Keywords color=""Boolean"">
            <Word>true</Word>
            <Word>false</Word>
        </Keywords>

        <!-- Main Commands -->
        <Keywords color=""Commands"">
            <Word>Spawn</Word>
            <Word>Color</Word>
            <Word>Size</Word>
            <Word>DrawLine</Word>
            <Word>DrawCircle</Word>
            <Word>DrawRectangle</Word>
            <Word>Fill</Word>
            <Word>GoTo</Word>
        </Keywords>
        
        <!-- Native Functions -->
        <Keywords color=""Functions"">
            <Word>GetActualX</Word>
            <Word>GetActualY</Word>
            <Word>GetCanvasSize</Word>
            <Word>GetColorCount</Word>
            <Word>IsBrushColor</Word>
            <Word>IsBrushSize</Word>
            <Word>IsCanvasColor</Word>
        </Keywords>

        <!-- Logical keywords -->
        <Keywords color=""Keywords"">
            <Word>and</Word>
            <Word>or</Word>
            <Word>not</Word>
            <Word>if</Word>
            <Word>else</Word>
            <Word>while</Word>
            <Word>for</Word>
            <Word>return</Word>
            <Word>break</Word>
            <Word>continue</Word>
        </Keywords>

        <!-- Assignment and comparison operators -->
        <Rule color=""Operators"">
            (&lt;-|==|!=|&gt;=|&lt;=|&amp;&amp;|\|\||[+\-*/%&gt;&lt;]|\*\*)
        </Rule>
        
        <!-- Variables and identifiers (must be last to avoid conflicts) -->
        <Rule color=""Variables"">
            \b[a-zA-Z_][a-zA-Z0-9_\-]*\b
        </Rule>
    </RuleSet>
</SyntaxDefinition>";

            // Load the XSHD from string
            using (var stringReader = new StringReader(xshdContent))
            using (var reader = XmlReader.Create(stringReader))
            {
                var xshd = HighlightingLoader.LoadXshd(reader);
                var highlightingDefinition = HighlightingLoader.Load(xshd, HighlightingManager.Instance);
                    
                // Cache the definition for future use
                _highlightingCache[theme] = highlightingDefinition;
                    
                // Apply to the editor
                CodeEditor.SyntaxHighlighting = highlightingDefinition;
                    
                Debug.WriteLine($"Successfully created and applied {themeHighlightingName} syntax highlighting");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error applying syntax highlighting for theme {theme}: {ex.Message}");
            Debug.WriteLine(ex.StackTrace);
                
            // Try to set a basic highlighting as fallback
            ApplyFallbackHighlighting();
        }
    }

    /// <summary>
    /// Applies a fallback highlighting when the custom highlighting fails to load.
    /// </summary>
    private void ApplyFallbackHighlighting()
    {
        try
        {
            // Try built-in definitions in order of preference
            var fallbackOptions = new[] { "C#", "JavaScript", "XML", "HTML" };
                
            foreach (var option in fallbackOptions)
            {
                var definition = HighlightingManager.Instance.GetDefinition(option);
                if (definition != null)
                {
                    CodeEditor.SyntaxHighlighting = definition;
                    Debug.WriteLine($"Applied fallback {option} highlighting");
                    return;
                }
            }
                
            Debug.WriteLine("No fallback highlighting available");
            CodeEditor.SyntaxHighlighting = null;
        }
        catch (Exception fallbackEx)
        {
            Debug.WriteLine($"Fallback highlighting also failed: {fallbackEx.Message}");
            CodeEditor.SyntaxHighlighting = null;
        }
    }

    /// <summary>
    /// Clears the highlighting cache. Call this if you modify theme files.
    /// </summary>
    public static void ClearHighlightingCache()
    {
        _highlightingCache.Clear();
        Debug.WriteLine("Highlighting cache cleared");
    }

    protected override void OnClosed(EventArgs e)
    {
        // Clear cache when window closes to free memory
        ClearHighlightingCache();
            
        if (DataContext is IDisposable disposable) 
            disposable.Dispose();
            
        base.OnClosed(e);
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    private void SwitchToDark_Click(object sender, RoutedEventArgs e)
    {
        ThemeService.ApplyTheme(Theme.Dark);
        ApplySyntaxHighlighting(Theme.Dark);
    }

    private void SwitchToLight_Click(object sender, RoutedEventArgs e)
    {
        ThemeService.ApplyTheme(Theme.Light);
        ApplySyntaxHighlighting(Theme.Light);
    }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.ContextMenu != null)
        {
            button.ContextMenu.PlacementTarget = button;
            button.ContextMenu.IsOpen = true;
        }
    }
}