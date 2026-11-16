using Avalonia.Controls;
using Shapeshifter.Desktop.Services;

namespace Shapeshifter.Desktop.Views;

public partial class ToastWindow : Window
{
    public ToastWindow()
    {
        InitializeComponent();
        NativeMaterial.Prepare(this, Chrome);

        // As the picker's "chrome.opaque": without a compositor the toast has to fill its window.
        Opened += (_, _) => Chrome.Classes.Set("opaque", ActualTransparencyLevel == WindowTransparencyLevel.None);
    }

    public void SetContent(int count, string preview)
    {
        CountText.Text = count == 1 ? "Copied · 1 item in your history" : $"Copied · {count} items in your history";
        PreviewText.Text = preview;
    }
}
