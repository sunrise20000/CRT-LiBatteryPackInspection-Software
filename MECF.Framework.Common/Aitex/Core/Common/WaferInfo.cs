using System;
using System.Runtime.Serialization;
using MECF.Framework.Common.CommonData;
using MECF.Framework.Common.Jobs;

namespace Aitex.Core.Common
{
	[Serializable]
	[DataContract]
	public class WaferInfo : NotifiableItem
	{
		private string procesjobid;

		private string controljobid;

		private SubstrateTransportStatus substtransstatus;

		private EnumE90Status Subste90status;

		private SubstHistory[] substhists;

		private string waferID;

		private string _waferOrigin;

		private string hostlaserMark1;

		private string hostlaserMark2;

		private string laserMarker;

		private string t7Code;

		private string _laserMarkerScore;

		private string _t7CodeScore;

		private string _imageFileName;

		private string _imageFilePath;

		private WaferStatus status;

		private WaferTrayStatus trayState = WaferTrayStatus.Empty;

		private bool isChecked;

		private EnumWaferProcessStatus processState;

		private bool isSource;

		private bool isDestination;

		[DataMember]
		public string ProcessJobID
		{
			get
			{
				return procesjobid;
			}
			set
			{
				procesjobid = value;
				InvokePropertyChanged("ProcessJobID");
			}
		}

		[DataMember]
		public string ControlJobID
		{
			get
			{
				return controljobid;
			}
			set
			{
				controljobid = value;
				InvokePropertyChanged("ControlJobID");
			}
		}

		[DataMember]
		public SubstrateTransportStatus SubstTransStatus
		{
			get
			{
				return substtransstatus;
			}
			set
			{
				substtransstatus = value;
				InvokePropertyChanged("SubstTransStatus");
			}
		}

		[DataMember]
		public EnumE90Status SubstE90Status
		{
			get
			{
				return Subste90status;
			}
			set
			{
				Subste90status = value;
				InvokePropertyChanged("SubstE90Status");
			}
		}

		[DataMember]
		public SubstHistory[] SubstHists
		{
			get
			{
				return substhists;
			}
			set
			{
				substhists = value;
				InvokePropertyChanged("SubstHists");
			}
		}

		public bool IsEmpty => Status == WaferStatus.Empty;

		[DataMember]
		public string WaferID
		{
			get
			{
				return waferID;
			}
			set
			{
				waferID = value;
				InvokePropertyChanged("WaferID");
			}
		}

		[DataMember]
		public string WaferOrigin
		{
			get
			{
				return _waferOrigin;
			}
			set
			{
				_waferOrigin = value;
				InvokePropertyChanged("WaferOrigin");
			}
		}

		[DataMember]
		public string HostLaserMark1
		{
			get
			{
				return hostlaserMark1;
			}
			set
			{
				hostlaserMark1 = value;
				InvokePropertyChanged("HostLaserMark1");
			}
		}

		[DataMember]
		public string HostLaserMark2
		{
			get
			{
				return hostlaserMark2;
			}
			set
			{
				hostlaserMark2 = value;
				InvokePropertyChanged("HostLaserMark2");
			}
		}

		[DataMember]
		public string LaserMarker
		{
			get
			{
				return laserMarker;
			}
			set
			{
				laserMarker = value;
				InvokePropertyChanged("LaserMarker");
			}
		}

		[DataMember]
		public string T7Code
		{
			get
			{
				return t7Code;
			}
			set
			{
				t7Code = value;
				InvokePropertyChanged("T7Code");
			}
		}

		[DataMember]
		public string LaserMarkerScore
		{
			get
			{
				return _laserMarkerScore;
			}
			set
			{
				_laserMarkerScore = value;
				InvokePropertyChanged("LaserMarker1Score");
			}
		}

		[DataMember]
		public string T7CodeScore
		{
			get
			{
				return _t7CodeScore;
			}
			set
			{
				_t7CodeScore = value;
				InvokePropertyChanged("T7CodeScore");
			}
		}

		[DataMember]
		public string ImageFileName
		{
			get
			{
				return _imageFileName;
			}
			set
			{
				_imageFileName = value;
				InvokePropertyChanged("ImageFileName");
			}
		}

		[DataMember]
		public string ImageFilePath
		{
			get
			{
				return _imageFilePath;
			}
			set
			{
				_imageFilePath = value;
				InvokePropertyChanged("ImageFilePath");
			}
		}

		[DataMember]
		public string LotId { get; set; }

		[DataMember]
		public string TransFlag { get; set; }

		[DataMember]
		public WaferStatus Status
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
		public WaferTrayStatus TrayState
		{
			get
			{
				return trayState;
			}
			set
			{
				trayState = value;
				InvokePropertyChanged("TrayState");
			}
		}

		[DataMember]
		public string CurrentCarrierID { get; set; }

		[DataMember]
		public int Station { get; set; }

		[DataMember]
		public int Slot { get; set; }

		[DataMember]
		public int TrayUsedForWhichPM { get; set; }

		[DataMember]
		public int TrayOriginStation { get; set; }

		[DataMember]
		public int TrayOriginSlot { get; set; }

		[DataMember]
		public int OriginStation { get; set; }

		[DataMember]
		public int OriginSlot { get; set; }

		[DataMember]
		public string OriginCarrierID { get; set; }

		[DataMember]
		public int DestinationStation { get; set; }

		[DataMember]
		public string DestinationCarrierID { get; set; }

		[DataMember]
		public int DestinationSlot { get; set; }

		[DataMember]
		public int NextStation { get; set; }

		[DataMember]
		public int NextStationSlot { get; set; }

		[DataMember]
		public int Notch { get; set; }

		[DataMember]
		public WaferSize Size { get; set; }

		public bool IsChecked
		{
			get
			{
				return isChecked;
			}
			set
			{
				isChecked = value;
				InvokePropertyChanged("IsChecked");
			}
		}

		[DataMember]
		public string PPID { get; set; }

		[DataMember]
		public EnumWaferProcessStatus ProcessState
		{
			get
			{
				return processState;
			}
			set
			{
				processState = value;
				InvokePropertyChanged("ProcessStatus");
			}
		}

		[DataMember]
		public bool IsSource
		{
			get
			{
				return isSource;
			}
			set
			{
				isSource = value;
				InvokePropertyChanged("IsSource");
			}
		}

		[DataMember]
		public bool IsDestination
		{
			get
			{
				return isDestination;
			}
			set
			{
				isDestination = value;
				InvokePropertyChanged("IsDestination");
			}
		}

		[DataMember]
		public Guid InnerId { get; set; }

		[DataMember]
		public ProcessJobInfo ProcessJob { get; set; }

		[DataMember]
		public int NextSequenceStep { get; set; }

		[DataMember]
		public int TrayProcessCount { get; set; }

		[DataMember]
		public bool HasWarning { get; set; }

		public WaferInfo()
		{
			InnerId = Guid.Empty;
		}

		public WaferInfo(string waferID, WaferStatus status = WaferStatus.Empty)
			: this()
		{
			WaferID = waferID;
			Status = status;
			TrayState = WaferTrayStatus.Empty;
		}

		public void Update(WaferInfo source)
		{
			InnerId = source.InnerId;
			WaferID = source.waferID;
			WaferOrigin = source.WaferOrigin;
			LaserMarker = source.LaserMarker;
			LaserMarkerScore = source.LaserMarkerScore;
			T7Code = source.T7Code;
			T7CodeScore = source.T7CodeScore;
			Status = source.Status;
			ProcessState = source.ProcessState;
			IsSource = source.IsSource;
			IsDestination = source.IsDestination;
			Station = source.Station;
			Slot = source.Slot;
			OriginStation = source.OriginStation;
			OriginSlot = source.OriginSlot;
			if (source.OriginCarrierID != null)
			{
				OriginCarrierID = source.OriginCarrierID;
			}
			if (source.DestinationCarrierID != null)
			{
				DestinationCarrierID = source.DestinationCarrierID;
			}
			DestinationStation = source.DestinationStation;
			DestinationSlot = source.DestinationSlot;
			NextStation = source.NextStation;
			NextStationSlot = source.NextStationSlot;
			Notch = source.Notch;
			Size = source.Size;
			TransFlag = source.TransFlag;
			LotId = source.LotId;
			PPID = source.PPID;
			ProcessJobID = source.ProcessJobID;
			ProcessJob = source.ProcessJob;
			NextSequenceStep = source.NextSequenceStep;
			SubstHists = source.SubstHists;
			HasWarning = source.HasWarning;
			SubstE90Status = source.SubstE90Status;
			PPID = source.PPID;
			HostLaserMark1 = source.HostLaserMark1;
			HostLaserMark2 = source.HostLaserMark2;
			if (TrayState == WaferTrayStatus.Empty && source.TrayState != 0)
			{
				TrayState = source.TrayState;
				TrayUsedForWhichPM = source.TrayUsedForWhichPM;
				TrayOriginStation = source.TrayOriginStation;
				TrayOriginSlot = source.TrayOriginSlot;
				TrayProcessCount = source.TrayProcessCount;
			}
		}

		public WaferInfo Clone()
		{
			return new WaferInfo
			{
				InnerId = InnerId,
				WaferID = waferID,
				WaferOrigin = WaferOrigin,
				LaserMarker = LaserMarker,
				LaserMarkerScore = LaserMarkerScore,
				T7Code = T7Code,
				T7CodeScore = T7CodeScore,
				Status = Status,
				ProcessState = ProcessState,
				IsSource = IsSource,
				IsDestination = IsDestination,
				Station = Station,
				Slot = Slot,
				OriginStation = OriginStation,
				OriginSlot = OriginSlot,
				OriginCarrierID = OriginCarrierID,
				DestinationCarrierID = DestinationCarrierID,
				DestinationStation = DestinationStation,
				DestinationSlot = DestinationSlot,
				NextStation = NextStation,
				NextStationSlot = NextStationSlot,
				Notch = Notch,
				Size = Size,
				TransFlag = TransFlag,
				LotId = LotId,
				ProcessJob = ProcessJob,
				NextSequenceStep = NextSequenceStep,
				SubstHists = SubstHists,
				HasWarning = HasWarning,
				SubstE90Status = SubstE90Status,
				TrayUsedForWhichPM = TrayUsedForWhichPM,
				TrayState = TrayState,
				TrayOriginStation = TrayOriginSlot,
				TrayOriginSlot = TrayOriginSlot,
				TrayProcessCount = TrayProcessCount,
				PPID = PPID
			};
		}

		public void SetEmpty()
		{
			InnerId = Guid.Empty;
			WaferID = string.Empty;
			WaferOrigin = string.Empty;
			LaserMarker = string.Empty;
			LaserMarkerScore = string.Empty;
			T7Code = string.Empty;
			T7CodeScore = string.Empty;
			Status = WaferStatus.Empty;
			ProcessState = EnumWaferProcessStatus.Idle;
			IsSource = false;
			IsDestination = false;
			Station = 0;
			Slot = 0;
			OriginStation = 0;
			OriginSlot = 0;
			DestinationStation = 0;
			DestinationSlot = 0;
			Notch = 0;
			TransFlag = string.Empty;
			LotId = string.Empty;
			ProcessJob = null;
			NextSequenceStep = 0;
			HasWarning = false;
			PPID = "";
			TrayState = WaferTrayStatus.Empty;
			TrayUsedForWhichPM = 0;
			TrayOriginStation = 0;
			TrayOriginSlot = 0;
			TrayProcessCount = 0;
		}
	}
}
