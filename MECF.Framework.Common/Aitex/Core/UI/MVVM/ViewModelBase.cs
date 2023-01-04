using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Windows.Input;
using Aitex.Core.Utilities;

namespace Aitex.Core.UI.MVVM
{
	public class ViewModelBase : INotifyPropertyChanged, IViewModelControl
	{
		public event PropertyChangedEventHandler PropertyChanged;

		public void InvokePropertyChanged(string propertyName)
		{
			if (this.PropertyChanged != null)
			{
				this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		public void InvokeAllPropertyChanged()
		{
			PropertyInfo[] properties = GetType().GetProperties();
			PropertyInfo[] array = properties;
			foreach (PropertyInfo propertyInfo in array)
			{
				InvokePropertyChanged(propertyInfo.Name);
				if (propertyInfo.PropertyType == typeof(ICommand) && propertyInfo.GetValue(this, null) is DelegateCommand<string> delegateCommand)
				{
					delegateCommand.RaiseCanExecuteChanged();
				}
			}
			FieldInfo[] fields = GetType().GetFields();
			FieldInfo[] array2 = fields;
			foreach (FieldInfo fieldInfo in array2)
			{
				InvokePropertyChanged(fieldInfo.Name);
				if (fieldInfo.FieldType == typeof(ICommand) && fieldInfo.GetValue(this) is DelegateCommand<string> delegateCommand2)
				{
					delegateCommand2.RaiseCanExecuteChanged();
				}
			}
		}

		public void InvokePropertyChanged()
		{
			PropertyInfo[] properties = GetType().GetProperties();
			PropertyInfo[] array = properties;
			foreach (PropertyInfo propertyInfo in array)
			{
				if (!propertyInfo.GetCustomAttributes(inherit: false).Any((object attribute) => attribute is IgnorePropertyChangeAttribute))
				{
					InvokePropertyChanged(propertyInfo.Name);
				}
				if (propertyInfo.PropertyType == typeof(ICommand) && propertyInfo.GetValue(this, null) is IDelegateCommand delegateCommand)
				{
					delegateCommand.RaiseCanExecuteChanged();
				}
			}
			FieldInfo[] fields = GetType().GetFields();
			FieldInfo[] array2 = fields;
			foreach (FieldInfo fieldInfo in array2)
			{
				InvokePropertyChanged(fieldInfo.Name);
				if (fieldInfo.FieldType == typeof(ICommand) && fieldInfo.GetValue(this) is DelegateCommand<string> delegateCommand2)
				{
					delegateCommand2.RaiseCanExecuteChanged();
				}
			}
		}
	}
}
