using System.Collections.Generic;
using System.ComponentModel;

namespace Aitex.Core.UI.ControlDataContext
{
	public class GasFlowButtonDataItem : INotifyPropertyChanged
	{
		private Dictionary<string, string> _colorDic = new Dictionary<string, string>
		{
			{ "N2", "SkyBlue" },
			{ "H2", "Pink" },
			{ "N2|H2", "Red" },
			{ "NH3", "Cyan" },
			{ "MO", "Gold" },
			{ "Default", "Gainsboro" }
		};

		private string _displayName = "";

		private string _gasType = "Default";

		private string _carrierGasName = "";

		private string _pipeGasName = "Default";

		private bool _isFlow2NH3 = false;

		private bool _isFlow2NH3E = false;

		private bool _isFlow2NH3C = false;

		private bool _isFlow2Run = false;

		private bool _isFlow2Vent = false;

		private string _totalFlow = "-";

		public string DeviceName { get; set; }

		public string DisplayName
		{
			get
			{
				return _displayName;
			}
			set
			{
				_displayName = value;
				InvokePropertyChanged("DisplayName");
			}
		}

		public string GasType
		{
			get
			{
				return _gasType;
			}
			set
			{
				_gasType = value;
				GasBackgroundColor = MapColor(value);
				InvokePropertyChanged("GasType");
				InvokePropertyChanged("GasBackgroundColor");
			}
		}

		public string GasBackgroundColor { get; set; }

		public string CarrierGasName
		{
			get
			{
				return _carrierGasName;
			}
			set
			{
				_carrierGasName = value;
				CarrierGasBackgroundColor = MapColor(value);
				InvokePropertyChanged("CarrierGasName");
				InvokePropertyChanged("CarrierGasBackgroundColor");
			}
		}

		public string CarrierGasBackgroundColor { get; set; }

		public string PipeGasName
		{
			get
			{
				return _pipeGasName;
			}
			set
			{
				_pipeGasName = value;
				PipeGasColor = MapColor(value);
				InvokePropertyChanged("PipeGasName");
				InvokePropertyChanged("PipeGasColor");
			}
		}

		public string PipeGasColor { get; set; }

		public bool IsFlow2NH3
		{
			get
			{
				return _isFlow2NH3;
			}
			set
			{
				_isFlow2NH3 = value;
				InvokePropertyChanged("IsFlow2NH3");
			}
		}

		public bool IsFlow2NH3E
		{
			get
			{
				return _isFlow2NH3E;
			}
			set
			{
				_isFlow2NH3E = value;
				InvokePropertyChanged("IsFlow2NH3E");
			}
		}

		public bool IsFlow2NH3C
		{
			get
			{
				return _isFlow2NH3C;
			}
			set
			{
				_isFlow2NH3C = value;
				InvokePropertyChanged("IsFlow2NH3C");
			}
		}

		public bool IsFlow2Run
		{
			get
			{
				return _isFlow2Run;
			}
			set
			{
				_isFlow2Run = value;
				InvokePropertyChanged("IsFlow2Run");
			}
		}

		public bool IsFlow2Vent
		{
			get
			{
				return _isFlow2Vent;
			}
			set
			{
				_isFlow2Vent = value;
				InvokePropertyChanged("IsFlow2Vent");
			}
		}

		public string TotalFlow
		{
			get
			{
				return _totalFlow;
			}
			set
			{
				_totalFlow = value;
				InvokePropertyChanged("TotalFlow");
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

		public GasFlowButtonDataItem()
		{
			GasType = "default";
			CarrierGasName = "";
			PipeGasName = "default";
		}

		private string MapColor(string gasName)
		{
			return _colorDic.ContainsKey(gasName) ? _colorDic[gasName] : _colorDic["Default"];
		}
	}
}
