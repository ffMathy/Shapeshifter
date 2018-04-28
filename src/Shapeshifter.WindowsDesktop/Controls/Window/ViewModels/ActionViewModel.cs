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
            string description)
        {
            Action = action;
            Title = action.Title;
            Description = description;
        }

        public IAction Action { get; }

        public string Description { get; }
        public string Title { get; }
    }
}