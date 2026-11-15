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
        CountText.Text = count == 1 ? "1 item" : $"{count} items";
        PreviewText.Text = preview;
    }
}
