using System;

namespace Shapeshifter.WindowsDesktop.Controls.Commands
{
	using System.Diagnostics;
	using System.Windows.Input;

	public class RelayCommand : ICommand
	{
		readonly Action<object> _execute;
		readonly Predicate<object> _canExecute;

		public RelayCommand(Action<object> execute)
			: this(execute, null)
		{
		}
		public RelayCommand(Action<object> execute, Predicate<object> canExecute)
		{
			_execute = execute ?? throw new ArgumentNullException(nameof(execute));
			_canExecute = canExecute;
		}
		
		[DebuggerStepThrough]
		public bool CanExecute(object parameters)
		{
			return _canExecute?.Invoke(parameters) ?? true;
		}

		public event EventHandler CanExecuteChanged
		{
			add => CommandManager.RequerySuggested += value;
			remove => CommandManager.RequerySuggested -= value;
		}

		public void Execute(object parameters)
		{
			_execute(parameters);
		}
	}
}
