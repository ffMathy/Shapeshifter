namespace Shapeshifter.WindowsDesktop.Controls.Window.ViewModels
{
    using Data.Actions.Interfaces;

    using Interfaces;

    public class ActionViewModel: IActionViewModel
    {
		public ActionViewModel() {
		}

        public ActionViewModel(
            IAction action,
            string title)
        {
            Action = action;
            Title = title;
        }

        public IAction Action { get; }

        public string Title { get; }
    }
}