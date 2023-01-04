using System;
using System.Runtime.Serialization;

namespace Aitex.Sorter.Common
{
	[Serializable]
	[DataContract]
	public class WaferHistoryWafer : WaferHistoryItem
	{
		[DataMember]
		public string ProcessJob { get; set; }

		[DataMember]
		public string Sequence { get; set; }

		[DataMember]
		public string Status { get; set; }

		public DateTime? ProcessStartTime { get; set; }

		[DataMember]
		public DateTime? ProcessEndTime { get; set; }

		public string ProcessDuration
		{
			get
			{
				if (!ProcessStartTime.HasValue || !ProcessEndTime.HasValue)
				{
					return string.Empty;
				}
				return ProcessEndTime.Value.Subtract(ProcessStartTime.Value).ToString("hh\\:mm\\:ss");
			}
		}
	}
}
