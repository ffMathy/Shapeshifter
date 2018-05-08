namespace Shapeshifter.WindowsDesktop.Controls.Window.ViewModels.Interfaces
{
    using Data.Actions.Interfaces;

    public interface IActionViewModel
    {
        IAction Action { get; }
		
        string Title { get; }
    }
}