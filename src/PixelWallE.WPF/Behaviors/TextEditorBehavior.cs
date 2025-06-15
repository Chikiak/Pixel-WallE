using ICSharpCode.AvalonEdit;
using System.Windows;

namespace PixelWallE.WPF.Behaviors;

public static class TextEditorBehavior
{
    public static readonly DependencyProperty BoundTextProperty =
        DependencyProperty.RegisterAttached(
            "BoundText",
            typeof(string),
            typeof(TextEditorBehavior),
            new FrameworkPropertyMetadata(
                string.Empty,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                OnBoundTextChanged));

    public static string GetBoundText(DependencyObject obj)
    {
        return (string)obj.GetValue(BoundTextProperty);
    }

    public static void SetBoundText(DependencyObject obj, string value)
    {
        obj.SetValue(BoundTextProperty, value);
    }

    private static void OnBoundTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextEditor textEditor)
        {
            textEditor.TextChanged -= TextEditor_TextChanged;
                
            var newText = e.NewValue as string ?? string.Empty;
            if (textEditor.Text != newText)
            {
                textEditor.Text = newText;
            }
                
            textEditor.TextChanged += TextEditor_TextChanged;
        }
    }

    private static void TextEditor_TextChanged(object? sender, System.EventArgs e)
    {
        if (sender is TextEditor textEditor)
        {
            SetBoundText(textEditor, textEditor.Text);
        }
    }
}