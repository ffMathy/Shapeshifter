using Avalonia.Controls;

namespace Shapeshifter.Desktop.Views;

public partial class ToastWindow : Window
{
    public ToastWindow()
    {
        InitializeComponent();
    }

    public void SetContent(int count, string preview)
    {
        CountText.Text = count == 1 ? "Copied · 1 item in your history" : $"Copied · {count} items in your history";
        PreviewText.Text = preview;
    }
}
