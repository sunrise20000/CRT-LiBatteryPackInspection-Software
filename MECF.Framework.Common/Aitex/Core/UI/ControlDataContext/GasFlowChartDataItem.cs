using System.Collections.ObjectModel;
using System.ComponentModel;

namespace Aitex.Core.UI.ControlDataContext
{
	public class GasFlowChartDataItem : INotifyPropertyChanged
	{
		private string _mOTotal = "";

		private string _hydrideTotal = "";

		public ObservableCollection<GasFlowButtonDataItem> MOGasItems { get; set; }

		public ObservableCollection<GasFlowButtonDataItem> NH3GasItems { get; set; }

		public string MOTotal
		{
			get
			{
				return _mOTotal;
			}
			set
			{
				_mOTotal = value;
				InvokePropertyChanged("MOTotal");
			}
		}

		public string HydrideTotal
		{
			get
			{
				return _hydrideTotal;
			}
			set
			{
				_hydrideTotal = value;
				InvokePropertyChanged("HydrideTotal");
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public void InvokePropertyChanged(string propertyName)
		{
			if (this.PropertyChanged != null)
			{
				this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}
	}
}
