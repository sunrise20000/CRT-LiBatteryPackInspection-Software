using System;
using System.Windows.Input;

namespace Aitex.Core.UI.MVVM
{
	public class SubscriptionCommand<T> : ICommand
	{
		private readonly Action<T> _execute;

		private readonly Predicate<T> _canExecute;

		public event EventHandler CanExecuteChanged;

		public SubscriptionCommand(Action<T> execute)
			: this(execute, (Predicate<T>)null)
		{
		}

		public SubscriptionCommand(Action<T> execute, Predicate<T> canExecute)
		{
			if (execute == null)
			{
				throw new ArgumentNullException("execute");
			}
			_execute = execute;
			_canExecute = canExecute;
		}

		public bool CanExecute(object parameter)
		{
			return _canExecute == null || _canExecute((T)parameter);
		}

		public void RaiseCanExecuteChanged()
		{
			if (this.CanExecuteChanged != null)
			{
				this.CanExecuteChanged(this, EventArgs.Empty);
			}
		}

		public void Execute(object parameter)
		{
			_execute((T)parameter);
		}
	}
}
