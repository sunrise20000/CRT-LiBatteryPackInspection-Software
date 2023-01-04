using System.Windows.Input;

namespace Aitex.Core.UI.MVVM
{
	public interface IDelegateCommand : ICommand
	{
		void RaiseCanExecuteChanged();
	}
}
