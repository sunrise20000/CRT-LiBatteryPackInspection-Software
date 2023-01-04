using System;
using System.Runtime.Serialization;
using Aitex.Core.UI.MVVM;

namespace Aitex.Core.UI.ControlDataContext
{
	[Serializable]
	[DataContract]
	public class RfItem : ViewModelBase
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

		public double PowerDisplay
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
		public string UIDisplay => $"{ForwardPower}:{ReflectPower}";

		public string BackgroundColor
		{
			get
			{
				if (IsError)
				{
					return "#FF0000";
				}
				if (!IsInterlockOn)
				{
					return "#FFFF00";
				}
				if (IsPowerOn)
				{
					return "#E69826";
				}
				return "#F1A2E4";
			}
		}

		public double ForwardPower { get; set; }

		public double ReflectPower { get; set; }

		public bool IsInterlockOn { get; set; }

		public bool IsPowerOn { get; set; }

		public bool IsError { get; set; }

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

		public int State { get; set; }

		public RfItem()
		{
			DisplayName = "Undefined";
		}

		public void Update(RfItem item)
		{
			DefaultValue = item.DefaultValue;
			Description = item.Description;
			DeviceId = item.DeviceId;
			DeviceName = item.DeviceName;
			DisplayName = item.DisplayName;
			ErroMessage = item.ErroMessage;
			Factor = item.Factor;
			FeedBack = item.FeedBack;
			IsWarning = item.IsWarning;
			PowerDisplay = item.PowerDisplay;
			Scale = item.Scale;
			SetPoint = item.SetPoint;
			Unit = item.Unit;
			Type = item.Type;
			InvokePropertyChanged();
		}
	}
}
