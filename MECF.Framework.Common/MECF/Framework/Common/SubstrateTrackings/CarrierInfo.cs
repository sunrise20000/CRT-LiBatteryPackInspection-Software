using System;
using System.Runtime.Serialization;
using Aitex.Core.Common;
using Aitex.Core.Util;
using MECF.Framework.Common.CommonData;
using MECF.Framework.Common.Equipment;

namespace MECF.Framework.Common.SubstrateTrackings
{
	[Serializable]
	[DataContract]
	public class CarrierInfo : NotifiableItem
	{
		private CarrierStatus status;

		public object ProcessJob;

		public bool IsEmpty => Status == CarrierStatus.Empty;

		[DataMember]
		public CarrierStatus Status
		{
			get
			{
				return status;
			}
			set
			{
				status = value;
				InvokePropertyChanged("Status");
			}
		}

		[DataMember]
		public Guid InnerId { get; set; }

		[DataMember]
		public string Name { get; set; }

		[DataMember]
		public string CarrierId { get; set; }

		[DataMember]
		public string Rfid { get; set; }

		[DataMember]
		public string LotId { get; set; }

		[DataMember]
		public string ProductCategory { get; set; }

		[DataMember]
		public WaferInfo[] Wafers { get; set; }

		[DataMember]
		public SerializableDictionary<string, bool> ProcessStatus { get; set; }

		[DataMember]
		public SerializableDictionary<string, string> Attributes { get; set; }

		[DataMember]
		public bool IsStart { get; set; }

		[DataMember]
		public DateTime LoadTime { get; set; }

		[DataMember]
		public string JobSelectedRecipeName { get; set; }

		[DataMember]
		public int Priority { get; set; }

		[DataMember]
		public ModuleName InternalModuleName { get; set; }

		[DataMember]
		public WaferSize CarrierWaferSize { get; set; }

		[DataMember]
		public bool IsProcessCompleted { get; set; }

		[DataMember]
		public bool IsVertical { get; set; }

		[DataMember]
		public bool HasWaferIn { get; set; }

		[DataMember]
		public int NextSequenceStep { get; set; }

		public object Job { get; set; }

		public T GetProcessJob<T>()
		{
			return (T)ProcessJob;
		}

		public bool IsProcessed(string module)
		{
			return ProcessStatus.ContainsKey(module) && ProcessStatus[module];
		}

		public void Clear()
		{
			Status = CarrierStatus.Empty;
			InnerId = Guid.Empty;
			ProcessJob = null;
			Name = "";
			IsStart = false;
			CarrierId = "";
			Rfid = "";
			JobSelectedRecipeName = "";
			LotId = "";
			ProductCategory = "";
			ProcessStatus = new SerializableDictionary<string, bool>();
			Attributes = new SerializableDictionary<string, string>();
			Priority = 0;
			HasWaferIn = false;
			IsProcessCompleted = false;
			IsVertical = false;
			NextSequenceStep = -1;
			Job = null;
		}

		public CarrierInfo(int capacity)
		{
			Wafers = new WaferInfo[capacity];
			InnerId = Guid.Empty;
			Status = CarrierStatus.Empty;
			ProcessStatus = new SerializableDictionary<string, bool>();
			Attributes = new SerializableDictionary<string, string>();
		}

		public void CopyInfo(CarrierInfo source)
		{
			CarrierId = source.CarrierId;
			InnerId = source.InnerId;
			Attributes = source.Attributes;
			CarrierId = source.CarrierId;
			Rfid = source.Rfid;
			IsStart = source.IsStart;
			JobSelectedRecipeName = source.JobSelectedRecipeName;
			LoadTime = source.LoadTime;
			LotId = source.LotId;
			Name = source.Name;
			ProcessStatus = source.ProcessStatus;
			Status = source.status;
			ProductCategory = source.ProductCategory;
			Wafers = source.Wafers;
			ProcessJob = source.ProcessJob;
			Priority = source.Priority;
			CarrierWaferSize = source.CarrierWaferSize;
			InternalModuleName = source.InternalModuleName;
			HasWaferIn = source.HasWaferIn;
			IsProcessCompleted = source.IsProcessCompleted;
			IsVertical = source.IsVertical;
			NextSequenceStep = source.NextSequenceStep;
			Job = source.Job;
		}
	}
}
