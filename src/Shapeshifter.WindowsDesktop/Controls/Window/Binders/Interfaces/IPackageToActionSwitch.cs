namespace Shapeshifter.WindowsDesktop.Controls.Window.Binders.Interfaces
{
    using ViewModels.Interfaces;

    public interface IPackageToActionSwitch
    {
        void PrepareBinder(
            IClipboardListViewModel clipboardListViewModel);
    }
}