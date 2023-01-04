using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml.Linq;
using Aitex.Core.Common;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.Equipment;

namespace Aitex.Sorter.Common
{
	[Serializable]
	[DataContract]
	public class SorterRecipeXml : INotifyPropertyChanged
	{
		private string name;

		private bool _isVerifyLaserMarker;

		private bool _isVerifyT7Code;

		private OrderByMode _orderBy;

		private int waferReaderIndex;

		private int waferReader2Index;

		private static string _template = "<?xml version='1.0'?><AitexSorterRecipe Type='' Source='' Destination='' PlaceMode='' IsReadLaserMarker='' IsReadT7Code='' OrderBy='' IsAlign='' AlignAngle='' IsVerifyLaserMarker='' IsVerifyT7Code='' IsTurnOver=''><TransferTable></TransferTable></AitexSorterRecipe>";

		private XDocument doc = new XDocument();

		private SorterRecipeType _type = SorterRecipeType.Transfer1To1;

		private ObservableCollection<ModuleName> _source = new ObservableCollection<ModuleName>();

		private ObservableCollection<ModuleName> _destination = new ObservableCollection<ModuleName>();

		private SorterRecipePlaceModeTransfer1To1 _placeModeTransfer1To1 = SorterRecipePlaceModeTransfer1To1.FromBottom;

		private SorterRecipePlaceModePack _placeModePack = SorterRecipePlaceModePack.FromBottomInsert;

		private SorterRecipePlaceModeOrder _placeModeOrder = SorterRecipePlaceModeOrder.Forward;

		private bool _isAlign = false;

		private double _anlignAngle = 0.0;

		private bool _isReadLaserMarker = false;

		private bool _isReadT7Code = false;

		private bool _isTurnOver = false;

		private SorterPickMode _pickMode = SorterPickMode.FromBottom;

		private bool _isPostAlign = false;

		private double _postAlignAngle = 0.0;

		private string _lasermark1jobs = "";

		private string _lasermark2jobs = "";

		private string _readidrecipe = "";

		[DataMember]
		public string Name
		{
			get
			{
				return name;
			}
			set
			{
				name = value;
				OnPropertyChanged("Name");
			}
		}

		[DataMember]
		public SorterRecipeType RecipeType
		{
			get
			{
				return _type;
			}
			set
			{
				_type = value;
				OnPropertyChanged("RecipeType");
			}
		}

		[DataMember]
		public ObservableCollection<ModuleName> Source
		{
			get
			{
				return _source;
			}
			set
			{
				_source = value;
				OnPropertyChanged("Source");
				OnPropertyChanged("SourceStation");
			}
		}

		public string StringListSource
		{
			get
			{
				if (RecipeType != SorterRecipeType.HostNToN && RecipeType != SorterRecipeType.TransferNToN)
				{
					return string.Join(",", SourceStation);
				}
				return string.Empty;
			}
		}

		public ObservableCollection<string> SourceStation
		{
			get
			{
				return new ObservableCollection<string>(_source.Select((ModuleName x) => x.ToString()));
			}
			set
			{
				_source = new ObservableCollection<ModuleName>(value.Select((string x) => (ModuleName)Enum.Parse(typeof(ModuleName), x)));
				OnPropertyChanged("Source");
				OnPropertyChanged("SourceStation");
			}
		}

		[DataMember]
		public ObservableCollection<ModuleName> Destination
		{
			get
			{
				return _destination;
			}
			set
			{
				_destination = value;
				OnPropertyChanged("Destination");
				OnPropertyChanged("DestinationStation");
			}
		}

		public string StringListDestination
		{
			get
			{
				if (RecipeType == SorterRecipeType.Transfer1To1 || RecipeType == SorterRecipeType.TransferNTo1)
				{
					return string.Join(",", DestinationStation);
				}
				return string.Empty;
			}
		}

		public ObservableCollection<string> DestinationStation
		{
			get
			{
				return new ObservableCollection<string>(_destination.Select((ModuleName x) => x.ToString()));
			}
			set
			{
				_destination = new ObservableCollection<ModuleName>(value.Select((string x) => (ModuleName)Enum.Parse(typeof(ModuleName), x)));
				OnPropertyChanged("Destination");
				OnPropertyChanged("DestinationStation");
			}
		}

		[DataMember]
		public bool IsReadLaserMarker
		{
			get
			{
				return _isReadLaserMarker;
			}
			set
			{
				_isReadLaserMarker = value;
				OnPropertyChanged("IsReadLaserMarker");
			}
		}

		[DataMember]
		public string LaserMark1Jobs
		{
			get
			{
				return _lasermark1jobs;
			}
			set
			{
				_lasermark1jobs = value;
				OnPropertyChanged("LaserMark1Jobs");
			}
		}

		[DataMember]
		public string ReadIDRecipe
		{
			get
			{
				return _readidrecipe;
			}
			set
			{
				_readidrecipe = value;
				OnPropertyChanged("ReadIDRecipe");
			}
		}

		[DataMember]
		public bool IsReadT7Code
		{
			get
			{
				return _isReadT7Code;
			}
			set
			{
				_isReadT7Code = value;
				OnPropertyChanged("IsReadT7Code");
			}
		}

		[DataMember]
		public string LaserMark2Jobs
		{
			get
			{
				return _lasermark2jobs;
			}
			set
			{
				_lasermark2jobs = value;
				OnPropertyChanged("LaserMark2Jobs");
			}
		}

		[DataMember]
		public bool IsTurnOver
		{
			get
			{
				return _isTurnOver;
			}
			set
			{
				_isTurnOver = value;
				OnPropertyChanged("IsTurnOver");
			}
		}

		[DataMember]
		public bool IsAlign
		{
			get
			{
				return _isAlign;
			}
			set
			{
				_isAlign = value;
				OnPropertyChanged("IsAlign");
			}
		}

		[DataMember]
		public bool IsPostAlign
		{
			get
			{
				return _isPostAlign;
			}
			set
			{
				_isPostAlign = value;
				OnPropertyChanged("IsPostAlign");
			}
		}

		[DataMember]
		public bool IsVerifyLaserMarker
		{
			get
			{
				return _isVerifyLaserMarker;
			}
			set
			{
				_isVerifyLaserMarker = value;
				OnPropertyChanged("IsVerifyLaserMarker");
			}
		}

		[DataMember]
		public bool IsVerifyT7Code
		{
			get
			{
				return _isVerifyT7Code;
			}
			set
			{
				_isVerifyT7Code = value;
				OnPropertyChanged("IsVerifyT7Code");
			}
		}

		[DataMember]
		public OrderByMode OrderBy
		{
			get
			{
				return _orderBy;
			}
			set
			{
				_orderBy = value;
				OnPropertyChanged("OrderBy");
			}
		}

		[DataMember]
		public double AlignAngle
		{
			get
			{
				return _anlignAngle;
			}
			set
			{
				_anlignAngle = value;
				OnPropertyChanged("AlignAngle");
			}
		}

		[DataMember]
		public double PostAlignAngle
		{
			get
			{
				return _postAlignAngle;
			}
			set
			{
				_postAlignAngle = value;
				OnPropertyChanged("PostAlignAngle");
			}
		}

		[DataMember]
		public SorterRecipePlaceModeTransfer1To1 PlaceModeTransfer1To1
		{
			get
			{
				return _placeModeTransfer1To1;
			}
			set
			{
				_placeModeTransfer1To1 = value;
				OnPropertyChanged("PlaceModeTransfer1To1");
				OnPropertyChanged("PlaceMode");
			}
		}

		[DataMember]
		public SorterPickMode PickMode
		{
			get
			{
				return _pickMode;
			}
			set
			{
				_pickMode = value;
				OnPropertyChanged("PickMode");
			}
		}

		[DataMember]
		public SorterRecipePlaceModeOrder PlaceModeOrder
		{
			get
			{
				return _placeModeOrder;
			}
			set
			{
				_placeModeOrder = value;
				OnPropertyChanged("PlaceModeOrder");
				OnPropertyChanged("PlaceMode");
			}
		}

		[DataMember]
		public SorterRecipePlaceModePack PlaceModePack
		{
			get
			{
				return _placeModePack;
			}
			set
			{
				_placeModePack = value;
				OnPropertyChanged("PlaceModePack");
				OnPropertyChanged("PlaceMode");
			}
		}

		[DataMember]
		public int WaferReaderIndex
		{
			get
			{
				return waferReaderIndex;
			}
			set
			{
				waferReaderIndex = value;
				OnPropertyChanged("WaferReaderIndex");
			}
		}

		[DataMember]
		public int WaferReader2Index
		{
			get
			{
				return waferReader2Index;
			}
			set
			{
				waferReader2Index = value;
				OnPropertyChanged("WaferReader2Index");
			}
		}

		[DataMember]
		public ObservableCollection<SorterRecipeTransferTableItem> TransferItems { get; set; }

		[DataMember]
		public ObservableCollection<SorterHostUsageRecipeTableItem> HostUsageItems { get; set; }

		[DataMember]
		public ObservableCollection<WaferInfo> TransferSourceLP1 { get; set; }

		[DataMember]
		public ObservableCollection<WaferInfo> TransferSourceLP2 { get; set; }

		[DataMember]
		public ObservableCollection<WaferInfo> TransferSourceLP3 { get; set; }

		[DataMember]
		public ObservableCollection<WaferInfo> TransferDestinationLP1 { get; set; }

		[DataMember]
		public ObservableCollection<WaferInfo> TransferDestinationLP2 { get; set; }

		[DataMember]
		public ObservableCollection<WaferInfo> TransferDestinationLP3 { get; set; }

		public string PlaceMode
		{
			get
			{
				string result = "";
				switch (RecipeType)
				{
				case SorterRecipeType.Transfer1To1:
				case SorterRecipeType.TransferNTo1:
					result = PlaceModeTransfer1To1.ToString();
					break;
				case SorterRecipeType.Pack:
					result = PlaceModePack.ToString();
					break;
				case SorterRecipeType.Order:
					result = PlaceModeOrder.ToString();
					break;
				}
				return result;
			}
		}

		public SlotTransferInfo[] TransferSlotInfoA => GetTransferSlotInfo(ModuleName.LP1);

		public SlotTransferInfo[] TransferSlotInfoB => GetTransferSlotInfo(ModuleName.LP2);

		public SlotTransferInfo[] TransferSlotInfoC => GetTransferSlotInfo(ModuleName.LP3);

		public event PropertyChangedEventHandler PropertyChanged;

		public void OnPropertyChanged(string propertyName)
		{
			if (this.PropertyChanged != null)
			{
				this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		private SlotTransferInfo[] GetTransferSlotInfo(ModuleName station)
		{
			SlotTransferInfo[] array = new SlotTransferInfo[25];
			for (int i = 0; i < 25; i++)
			{
				SlotTransferInfo slotInfo = new SlotTransferInfo();
				slotInfo.Station = station;
				slotInfo.Slot = i;
				SorterRecipeTransferTableItem sorterRecipeTransferTableItem = TransferItems.FirstOrDefault((SorterRecipeTransferTableItem x) => x.SourceStation == slotInfo.Station && x.SourceSlot == slotInfo.Slot);
				if (sorterRecipeTransferTableItem != null)
				{
					slotInfo.DestinationStation = sorterRecipeTransferTableItem.DestinationStation;
					slotInfo.DestinationSlot = sorterRecipeTransferTableItem.DestinationSlot;
				}
				SorterRecipeTransferTableItem sorterRecipeTransferTableItem2 = TransferItems.FirstOrDefault((SorterRecipeTransferTableItem x) => x.DestinationStation == slotInfo.Station && x.DestinationSlot == slotInfo.Slot);
				if (sorterRecipeTransferTableItem2 != null)
				{
					slotInfo.SourceStation = sorterRecipeTransferTableItem2.SourceStation;
					slotInfo.SourceSlot = sorterRecipeTransferTableItem2.SourceSlot;
				}
				array[i] = slotInfo;
			}
			return array;
		}

		public SorterRecipeXml(string name = "")
			: this(_template, name)
		{
		}

		public SorterRecipeXml(string content, string name = "")
		{
			TransferItems = new ObservableCollection<SorterRecipeTransferTableItem>();
			HostUsageItems = new ObservableCollection<SorterHostUsageRecipeTableItem>();
			Name = name;
			SetContent(content);
		}

		public SorterRecipeXml(string content, bool IsRemoteRecipe, string name = "")
		{
			TransferItems = new ObservableCollection<SorterRecipeTransferTableItem>();
			HostUsageItems = new ObservableCollection<SorterHostUsageRecipeTableItem>();
			Name = name;
			try
			{
				doc = XDocument.Parse(content);
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
			}
		}

		public bool SetContent(string content)
		{
			try
			{
				doc = XDocument.Parse(content);
				ParseContent();
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
				return false;
			}
			return true;
		}

		public string GetContent()
		{
			return doc.ToString();
		}

		public void SaveContent()
		{
			List<XElement> list = new List<XElement>();
			foreach (SorterRecipeTransferTableItem transferItem in TransferItems)
			{
				list.Add(new XElement("TransferItem", new XAttribute("SourceStation", transferItem.SourceStation.ToString()), new XAttribute("SourceSlot", transferItem.SourceSlot.ToString()), new XAttribute("DestinationStation", transferItem.DestinationStation.ToString()), new XAttribute("DestinationSlot", transferItem.DestinationSlot.ToString()), new XAttribute("IsReadLaserMarker", transferItem.IsReadLaserMarker.ToString()), new XAttribute("IsReadT7Code", transferItem.IsReadT7Code.ToString()), new XAttribute("IsAlign", transferItem.IsAlign.ToString()), new XAttribute("AlignAngle", transferItem.AlignAngle.ToString()), new XAttribute("IsVerifyLaserMarker", transferItem.IsReadLaserMarker.ToString()), new XAttribute("IsVerifyT7Code", transferItem.IsReadT7Code.ToString()), new XAttribute("OrderBy", transferItem.OrderBy.ToString()), new XAttribute("IsTurnOver", transferItem.IsTurnOver.ToString())));
			}
			doc = new XDocument(new XElement("AitexSorterRecipe", new XAttribute("Type", RecipeType.ToString()), new XAttribute("Source", StringListSource), new XAttribute("Destination", StringListDestination), new XAttribute("PlaceMode", PlaceMode), new XAttribute("PickMode", PickMode), new XAttribute("IsReadLaserMarker", IsReadLaserMarker.ToString()), new XAttribute("IsReadT7Code", IsReadT7Code.ToString()), new XAttribute("IsVerifyLaserMarker", IsReadLaserMarker.ToString()), new XAttribute("IsVerifyT7Code", IsReadT7Code.ToString()), new XAttribute("OrderBy", OrderBy.ToString()), new XAttribute("IsAlign", IsAlign.ToString()), new XAttribute("AlignAngle", AlignAngle.ToString()), new XAttribute("IsTurnOver", IsTurnOver.ToString()), new XAttribute("IsPostAlign", IsPostAlign.ToString()), new XAttribute("PostAlignAngle", PostAlignAngle.ToString()), new XAttribute("WaferReaderIndex", WaferReaderIndex.ToString()), new XAttribute("WaferReader2Index", WaferReader2Index.ToString()), new XAttribute("LaserMark1Jobs", LaserMark1Jobs.ToString()), new XAttribute("LaserMark2Jobs", LaserMark2Jobs.ToString()), new XAttribute("ReadIDRecipe", ReadIDRecipe.ToString()), new XElement("TransferTable", list)));
		}

		public void SaveHostUsageContent()
		{
		}

		private ObservableCollection<ModuleName> ParseModule(string value)
		{
			ObservableCollection<ModuleName> observableCollection = new ObservableCollection<ModuleName>();
			if (!string.IsNullOrEmpty(value))
			{
				string[] array = value.Split(',');
				string[] array2 = array;
				foreach (string value2 in array2)
				{
					if (Enum.TryParse<ModuleName>(value2, out var result))
					{
						observableCollection.Add(result);
					}
				}
			}
			return observableCollection;
		}

		private void ParseContent()
		{
			try
			{
				XElement root = doc.Root;
				if (root == null)
				{
					LOG.Write($"recipe not valid");
					return;
				}
				string value = root.Attribute("Type").Value;
				Enum.TryParse<SorterRecipeType>(value, out var result);
				RecipeType = result;
				value = root.Attribute("Source").Value;
				Source = ParseModule(value);
				value = root.Attribute("Destination").Value;
				Destination = ParseModule(value);
				value = root.Attribute("PlaceMode").Value;
				Enum.TryParse<SorterRecipePlaceModePack>(value, out var result2);
				PlaceModePack = result2;
				Enum.TryParse<SorterRecipePlaceModeOrder>(value, out var result3);
				PlaceModeOrder = result3;
				Enum.TryParse<SorterRecipePlaceModeTransfer1To1>(value, out var result4);
				PlaceModeTransfer1To1 = result4;
				Enum.TryParse<SorterPickMode>(value, out var result5);
				PickMode = result5;
				value = root.Attribute("IsReadLaserMarker").Value;
				bool.TryParse(value, out var result6);
				IsReadLaserMarker = result6;
				value = root.Attribute("IsReadT7Code").Value;
				bool.TryParse(value, out var result7);
				IsReadT7Code = result7;
				value = root.Attribute("IsVerifyLaserMarker").Value;
				bool.TryParse(value, out var result8);
				IsVerifyLaserMarker = result8;
				value = root.Attribute("IsVerifyT7Code").Value;
				bool.TryParse(value, out var result9);
				IsVerifyT7Code = result9;
				value = root.Attribute("OrderBy").Value;
				Enum.TryParse<OrderByMode>(value, out var result10);
				OrderBy = result10;
				value = root.Attribute("IsAlign").Value;
				bool.TryParse(value, out var result11);
				IsAlign = result11;
				value = root.Attribute("AlignAngle").Value;
				double.TryParse(value, out var result12);
				AlignAngle = result12;
				value = root.Attribute("IsTurnOver").Value;
				bool.TryParse(value, out var result13);
				IsTurnOver = result13;
				if (root.Attribute("IsPostAlign") != null)
				{
					value = root.Attribute("IsPostAlign").Value;
					bool.TryParse(value, out var result14);
					IsPostAlign = result14;
				}
				if (root.Attribute("PostAlignAngle") != null)
				{
					value = root.Attribute("PostAlignAngle").Value;
					double.TryParse(value, out var result15);
					PostAlignAngle = result15;
				}
				if (root.Attribute("PickMode") != null)
				{
					value = root.Attribute("PickMode").Value;
					Enum.TryParse<SorterPickMode>(value, out var result16);
					PickMode = result16;
				}
				if (root.Attribute("LaserMark1Jobs") != null)
				{
					value = (LaserMark1Jobs = root.Attribute("LaserMark1Jobs").Value);
				}
				if (root.Attribute("ReadIDRecipe") != null)
				{
					value = (ReadIDRecipe = root.Attribute("ReadIDRecipe").Value);
				}
				if (root.Attribute("LaserMark2Jobs") != null)
				{
					value = (LaserMark2Jobs = root.Attribute("LaserMark2Jobs").Value);
				}
				WaferReaderIndex = GetValue<int>(root, "WaferReaderIndex");
				if (root.Attribute("WaferReader2Index") != null)
				{
					WaferReader2Index = GetValue<int>(root, "WaferReader2Index");
				}
				IEnumerable<XElement> enumerable = root.Element("TransferTable").Elements("TransferItem");
				if (!enumerable.Any())
				{
					return;
				}
				foreach (XElement item in enumerable)
				{
					Enum.TryParse<ModuleName>(item.Attribute("SourceStation").Value, out var result17);
					int.TryParse(item.Attribute("SourceSlot").Value, out var result18);
					Enum.TryParse<ModuleName>(item.Attribute("DestinationStation").Value, out var result19);
					int.TryParse(item.Attribute("DestinationSlot").Value, out var result20);
					bool.TryParse(item.Attribute("IsReadLaserMarker").Value, out var result21);
					bool.TryParse(item.Attribute("IsReadT7Code").Value, out var result22);
					bool.TryParse(item.Attribute("IsVerifyLaserMarker").Value, out var result23);
					bool.TryParse(item.Attribute("IsVerifyT7Code").Value, out var result24);
					bool.TryParse(item.Attribute("IsAlign").Value, out var result25);
					bool.TryParse(item.Attribute("IsTurnOver").Value, out var result26);
					Enum.TryParse<OrderByMode>(item.Attribute("OrderBy").Value, out var result27);
					double.TryParse(item.Attribute("AlignAngle").Value, out var result28);
					TransferItems.Add(new SorterRecipeTransferTableItem
					{
						SourceStation = result17,
						SourceSlot = result18,
						DestinationStation = result19,
						DestinationSlot = result20,
						IsReadLaserMarker = result21,
						IsReadT7Code = result22,
						IsVerifyLaserMarker = result23,
						IsVerifyT7Code = result24,
						IsAlign = result25,
						OrderBy = result27,
						AlignAngle = result28,
						IsTurnOver = result26
					});
				}
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
			}
		}

		private void ParseHostUsageContent()
		{
			try
			{
				XElement root = doc.Root;
				if (root == null)
				{
					LOG.Write($"recipe not valid");
					return;
				}
				string value = root.Attribute("Type").Value;
				Enum.TryParse<SorterRecipeType>(value, out var result);
				RecipeType = result;
				value = root.Attribute("Source").Value;
				Source = ParseModule(value);
				value = root.Attribute("Destination").Value;
				Destination = ParseModule(value);
				value = root.Attribute("PlaceMode").Value;
				Enum.TryParse<SorterRecipePlaceModePack>(value, out var result2);
				PlaceModePack = result2;
				Enum.TryParse<SorterRecipePlaceModeOrder>(value, out var result3);
				PlaceModeOrder = result3;
				Enum.TryParse<SorterRecipePlaceModeTransfer1To1>(value, out var result4);
				PlaceModeTransfer1To1 = result4;
				Enum.TryParse<SorterPickMode>(value, out var result5);
				PickMode = result5;
				value = root.Attribute("IsReadLaserMarker").Value;
				bool.TryParse(value, out var result6);
				IsReadLaserMarker = result6;
				value = root.Attribute("IsReadT7Code").Value;
				bool.TryParse(value, out var result7);
				IsReadT7Code = result7;
				value = root.Attribute("IsVerifyLaserMarker").Value;
				bool.TryParse(value, out var result8);
				IsVerifyLaserMarker = result8;
				value = root.Attribute("IsVerifyT7Code").Value;
				bool.TryParse(value, out var result9);
				IsVerifyT7Code = result9;
				value = root.Attribute("OrderBy").Value;
				Enum.TryParse<OrderByMode>(value, out var result10);
				OrderBy = result10;
				value = root.Attribute("IsAlign").Value;
				bool.TryParse(value, out var result11);
				IsAlign = result11;
				value = root.Attribute("AlignAngle").Value;
				double.TryParse(value, out var result12);
				AlignAngle = result12;
				WaferReaderIndex = GetValue<int>(root, "WaferReaderIndex");
				IEnumerable<XElement> enumerable = root.Element("TransferTable").Elements("TransferItem");
				if (!enumerable.Any())
				{
					return;
				}
				foreach (XElement item in enumerable)
				{
					Enum.TryParse<ModuleName>(item.Attribute("SourceStation").Value, out var result13);
					int.TryParse(item.Attribute("SourceSlot").Value, out var result14);
					Enum.TryParse<ModuleName>(item.Attribute("DestinationStation").Value, out var result15);
					int.TryParse(item.Attribute("DestinationSlot").Value, out var result16);
					bool.TryParse(item.Attribute("IsReadLaserMarker").Value, out var result17);
					bool.TryParse(item.Attribute("IsReadT7Code").Value, out var result18);
					bool.TryParse(item.Attribute("IsVerifyLaserMarker").Value, out var result19);
					bool.TryParse(item.Attribute("IsVerifyT7Code").Value, out var result20);
					bool.TryParse(item.Attribute("IsAlign").Value, out var result21);
					Enum.TryParse<OrderByMode>(item.Attribute("OrderBy").Value, out var result22);
					double.TryParse(item.Attribute("AlignAngle").Value, out var result23);
					TransferItems.Add(new SorterRecipeTransferTableItem
					{
						SourceStation = result13,
						SourceSlot = result14,
						DestinationStation = result15,
						DestinationSlot = result16,
						IsReadLaserMarker = result17,
						IsReadT7Code = result18,
						IsVerifyLaserMarker = result19,
						IsVerifyT7Code = result20,
						IsAlign = result21,
						OrderBy = result22,
						AlignAngle = result23
					});
				}
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
			}
		}

		private T GetValue<T>(XElement element, string Name)
		{
			XAttribute xAttribute = element.Attribute(Name);
			if (xAttribute != null)
			{
				return (T)Convert.ChangeType(xAttribute.Value, typeof(T));
			}
			return default(T);
		}
	}
}
