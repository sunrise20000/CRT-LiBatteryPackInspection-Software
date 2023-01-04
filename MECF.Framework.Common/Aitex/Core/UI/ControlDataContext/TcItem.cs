using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace Aitex.Core.UI.ControlDataContext
{
	[Serializable]
	[DataContract]
	public class TcItem : INotifyPropertyChanged
	{
		public double _feedback;

		private double _factor = 1.0;

		[DataMember]
		public string DeviceName { get; set; }

		[DataMember]
		public string DisplayName { get; set; }

		[DataMember]
		public string DeviceId { get; set; }

		[DataMember]
		public string Unit { get; set; }

		[DataMember]
		public string Description { get; set; }

		[DataMember]
		public double Scale { get; set; }

		[DataMember]
		public double SetPoint { get; set; }

		public double FeedBack
		{
			get
			{
				return _feedback;
			}
			set
			{
				_feedback = value;
				InvokePropertyChanged("FeedBack");
			}
		}

		[DataMember]
		public double DefaultValue { get; set; }

		[DataMember]
		public bool IsWarning { get; set; }

		[DataMember]
		public string ErroMessage { get; set; }

		[DataMember]
		public string Type { get; set; }

		[DataMember]
		public double Factor
		{
			get
			{
				return _factor;
			}
			set
			{
				_factor = value;
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

		public TcItem()
		{
			DisplayName = "未定义设备";
		}
	}
}
