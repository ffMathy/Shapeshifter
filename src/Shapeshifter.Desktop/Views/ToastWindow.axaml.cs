using Avalonia.Controls;
using Shapeshifter.Core.Localization;
using Shapeshifter.Desktop.Services;

namespace Shapeshifter.Desktop.Views;

public partial class ToastWindow : Window
{
    public ToastWindow()
    {
        InitializeComponent();
        if (Localizer.IsRightToLeft)
        {
            FlowDirection = Avalonia.Media.FlowDirection.RightToLeft;
        }

        NativeMaterial.Prepare(this, Chrome);

        // As the picker's "chrome.opaque": without a compositor the toast has to fill its window.
        Opened += (_, _) => Chrome.Classes.Set("opaque", ActualTransparencyLevel == WindowTransparencyLevel.None);

        // Moving the pointer onto the notification, or clicking it, dismisses it.
        Chrome.PointerEntered += (_, _) => Dismiss();
        Chrome.PointerPressed += (_, e) =>
        {
            e.Handled = true;
            Dismiss();
        };
    }

    /// <summary>Raised when the user dismissed the notification by pointing at it or clicking it.</summary>
    public event EventHandler? Dismissed;

    private void Dismiss()
    {
        if (!IsVisible)
        {
            return;
        }

        Hide();
        Dismissed?.Invoke(this, EventArgs.Empty);
    }

    public void SetContent(int count, string preview)
    {
        Chrome.Classes.Set("tip", false);
        CountText.Text = count == 1 ? Strings.ToastCopiedOne : Localizer.Format(Strings.ToastCopiedOther, count);
        PreviewText.Text = preview;
    }

    /// <summary>Shows a coaching tip: a title and a sentence or two, which the looks let wrap.</summary>
    public void SetTip(string title, string message)
    {
        Chrome.Classes.Set("tip", true);
        CountText.Text = title;
        PreviewText.Text = message;
    }
}
