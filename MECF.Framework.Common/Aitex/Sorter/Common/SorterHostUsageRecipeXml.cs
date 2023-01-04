using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml.Linq;
using Aitex.Core.RT.Log;

namespace Aitex.Sorter.Common
{
	[Serializable]
	[DataContract]
	public class SorterHostUsageRecipeXml : INotifyPropertyChanged
	{
		private string name;

		private bool _isReadLaserMarker1 = false;

		private bool _isVerifyLaserMarker1;

		private bool _isVerifyChecksumLaserMarker1;

		private bool _isReadLaserMarker2 = false;

		private bool _isVerifyLaserMarker2;

		private bool _isVerifyChecksumLaserMarker2;

		private bool _isVerifyLaserMarkLength;

		private bool _isVerifySlotPositionInLaserMarker;

		private bool _isVerifyWithHostDefine;

		private int _intVerifyLaserMarkLength;

		private int _intVerifySlotPositionInLaserMark;

		private bool _isAlign;

		private double _preAlignAngle = 0.0;

		private double _postAlignAngle = 0.0;

		private double _bladeRegulatorRatio = 0.0;

		private bool _isPostAlign;

		private bool _isTurnOver;

		private bool _isTurnOverBeforeAlign;

		private int _laserMarkerWaferReaderIndex1;

		private int _laserMarkerWaferReaderIndex2;

		private bool _isRobotFlippeBeforeAlign;

		private bool _isRobotFlippeAfterAlign;

		private ObservableCollection<KeyValuePair<string, string>> _ocrReaderJob1;

		private ObservableCollection<KeyValuePair<string, string>> _ocrReaderJob2;

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
		public bool IsReadLaserMarker1
		{
			get
			{
				return _isReadLaserMarker1;
			}
			set
			{
				_isReadLaserMarker1 = value;
				OnPropertyChanged("IsReadLaserMarker1");
			}
		}

		[DataMember]
		public bool IsVerifyLaserMarker1
		{
			get
			{
				return _isVerifyLaserMarker1;
			}
			set
			{
				_isVerifyLaserMarker1 = value;
				OnPropertyChanged("IsVerifyLaserMarker1");
			}
		}

		[DataMember]
		public bool IsVerifyChecksumLaserMarker1
		{
			get
			{
				return _isVerifyChecksumLaserMarker1;
			}
			set
			{
				_isVerifyChecksumLaserMarker1 = value;
				OnPropertyChanged("IsVerifyChecksumLaserMarker1");
			}
		}

		[DataMember]
		public bool IsReadLaserMarker2
		{
			get
			{
				return _isReadLaserMarker2;
			}
			set
			{
				_isReadLaserMarker2 = value;
				OnPropertyChanged("IsReadLaserMarker2");
			}
		}

		[DataMember]
		public bool IsVerifyLaserMarker2
		{
			get
			{
				return _isVerifyLaserMarker2;
			}
			set
			{
				_isVerifyLaserMarker2 = value;
				OnPropertyChanged("IsVerifyLaserMarker2");
			}
		}

		[DataMember]
		public bool IsVerifyChecksumLaserMarker2
		{
			get
			{
				return _isVerifyChecksumLaserMarker2;
			}
			set
			{
				_isVerifyChecksumLaserMarker2 = value;
				OnPropertyChanged("IsVerifyChecksumLaserMarker2");
			}
		}

		[DataMember]
		public bool IsVerifyLaserMarkLength
		{
			get
			{
				return _isVerifyLaserMarkLength;
			}
			set
			{
				_isVerifyLaserMarkLength = value;
				OnPropertyChanged("IsVerifyLaserMarkLength");
			}
		}

		[DataMember]
		public bool IsVerifySlotPositionInLaserMarker
		{
			get
			{
				return _isVerifySlotPositionInLaserMarker;
			}
			set
			{
				_isVerifySlotPositionInLaserMarker = value;
				OnPropertyChanged("IsVerifySlotPositionInLaserMark");
			}
		}

		[DataMember]
		public bool IsVerifyWithHostDefine
		{
			get
			{
				return _isVerifyWithHostDefine;
			}
			set
			{
				_isVerifyWithHostDefine = value;
				OnPropertyChanged("IsVerifyWithHostDefine");
			}
		}

		[DataMember]
		public int IntVerifyLaserMarkerLength
		{
			get
			{
				return _intVerifyLaserMarkLength;
			}
			set
			{
				_intVerifyLaserMarkLength = value;
				OnPropertyChanged("IntVerifyLaserMarkLength");
			}
		}

		[DataMember]
		public int IntVerifySlotPositionInLaserMark
		{
			get
			{
				return _intVerifySlotPositionInLaserMark;
			}
			set
			{
				_intVerifySlotPositionInLaserMark = value;
				OnPropertyChanged("IntVerifySlotPositionInLaserMark");
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
		public double PreAlignAngle
		{
			get
			{
				return _preAlignAngle;
			}
			set
			{
				_preAlignAngle = value;
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
		public double BladeRegulatorRatio
		{
			get
			{
				return _bladeRegulatorRatio;
			}
			set
			{
				_bladeRegulatorRatio = value;
				OnPropertyChanged("BladeRegulatorRatio");
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
		public bool IsTurnOverBeforeAlign
		{
			get
			{
				return _isTurnOverBeforeAlign;
			}
			set
			{
				_isTurnOverBeforeAlign = value;
				OnPropertyChanged("IsTurnOverBeforeAlign");
			}
		}

		[DataMember]
		public int LaserMarkerWaferReaderIndex1
		{
			get
			{
				return _laserMarkerWaferReaderIndex1;
			}
			set
			{
				_laserMarkerWaferReaderIndex1 = value;
				OnPropertyChanged("LaserMarkerWaferReaderIndex1");
			}
		}

		[DataMember]
		public int LaserMarkerWaferReaderIndex2
		{
			get
			{
				return _laserMarkerWaferReaderIndex2;
			}
			set
			{
				_laserMarkerWaferReaderIndex2 = value;
				OnPropertyChanged("LaserMarkerWaferReaderIndex2");
			}
		}

		[DataMember]
		public bool IsRobotFlippeBeforeAlign
		{
			get
			{
				return _isRobotFlippeBeforeAlign;
			}
			set
			{
				_isRobotFlippeBeforeAlign = value;
				OnPropertyChanged("IsRobotFlippeBeforeAlign");
			}
		}

		[DataMember]
		public bool IsRobotFlippeAfterAlign
		{
			get
			{
				return _isRobotFlippeAfterAlign;
			}
			set
			{
				_isRobotFlippeAfterAlign = value;
				OnPropertyChanged("IsRobotFlippeAfterAlign");
			}
		}

		[DataMember]
		public ObservableCollection<KeyValuePair<string, string>> OcrReaderJob1
		{
			get
			{
				return _ocrReaderJob1;
			}
			set
			{
				_ocrReaderJob1 = value;
				OnPropertyChanged("OcrReaderJob1");
			}
		}

		[DataMember]
		public ObservableCollection<KeyValuePair<string, string>> OcrReaderJob2
		{
			get
			{
				return _ocrReaderJob2;
			}
			set
			{
				_ocrReaderJob2 = value;
				OnPropertyChanged("OcrReaderJob2");
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public void OnPropertyChanged(string propertyName)
		{
			if (this.PropertyChanged != null)
			{
				this.PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		public SorterHostUsageRecipeXml(string name = "")
		{
			Name = name;
			OcrReaderJob1 = new ObservableCollection<KeyValuePair<string, string>>();
			OcrReaderJob2 = new ObservableCollection<KeyValuePair<string, string>>();
		}

		public SorterHostUsageRecipeXml(string content, string name = "")
		{
			Name = name;
			SetContent(content);
		}

		public bool SetContent(string content)
		{
			try
			{
				XDocument doc = XDocument.Parse(content);
				ParseContent(doc);
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
			XDocument xDocument = new XDocument();
			XElement xElement = new XElement("Recipe");
			XElement content = new XElement("AitexSorterHostUsageRecipe", new XAttribute("IsAlign", IsAlign), new XAttribute("PreAlignAngle", PreAlignAngle), new XAttribute("IsReadLaserMarker1", IsReadLaserMarker1.ToString()), new XAttribute("IsVerifyLaserMarker1", IsVerifyLaserMarker1.ToString()), new XAttribute("IsVerifyChecksumLaserMarker1", IsVerifyChecksumLaserMarker1.ToString()), new XAttribute("LaserMarkerWaferReaderIndex1", LaserMarkerWaferReaderIndex1), new XAttribute("IsReadLaserMarker2", IsReadLaserMarker2.ToString()), new XAttribute("IsVerifyLaserMarker2", IsVerifyLaserMarker2.ToString()), new XAttribute("IsVerifyChecksumLaserMarker2", IsVerifyChecksumLaserMarker2.ToString()), new XAttribute("LaserMarkerWaferReaderIndex2", LaserMarkerWaferReaderIndex2), new XAttribute("IsPostAlign", IsPostAlign.ToString()), new XAttribute("PostAlignAngle", PostAlignAngle), new XAttribute("OcrReaderJob1", (OcrReaderJob1 != null) ? string.Join(";", OcrReaderJob1.Select((KeyValuePair<string, string> x) => x.Value)) : ""), new XAttribute("OcrReaderJob2", (OcrReaderJob2 != null) ? string.Join(";", OcrReaderJob2.Select((KeyValuePair<string, string> x) => x.Value)) : ""), new XAttribute("IsTurnOver", IsTurnOver.ToString()), new XAttribute("IsTurnOverBeforeAlign", IsTurnOverBeforeAlign.ToString()), new XAttribute("IsVerifyLaserMarkLength", IsVerifyLaserMarkLength.ToString()), new XAttribute("IsVerifySlotPositionInLaserMarker", IsVerifySlotPositionInLaserMarker.ToString()), new XAttribute("IntVerifyLaserMarkerLength", IntVerifyLaserMarkerLength.ToString()), new XAttribute("IntVerifySlotPositionInLaserMark", IntVerifySlotPositionInLaserMark.ToString()), new XAttribute("IsVerifyWithHostDefine", IsVerifyWithHostDefine.ToString()), new XAttribute("BladeRegulatorRatio", BladeRegulatorRatio), new XAttribute("IsRobotFlippeBeforeAlign", IsRobotFlippeBeforeAlign.ToString()), new XAttribute("IsRobotFlippeAfterAlign", IsRobotFlippeAfterAlign.ToString()));
			xElement.Add(content);
			xDocument.Add(xElement);
			return xDocument.ToString();
		}

		private void ParseContent(XDocument doc)
		{
			try
			{
				XElement xElement = doc.Root.Element("AitexSorterHostUsageRecipe");
				if (xElement == null)
				{
					LOG.Write($"recipe not valid");
					return;
				}
				string value = xElement.Attribute("PreAlignAngle").Value;
				double.TryParse(value, out var result);
				PreAlignAngle = result;
				value = xElement.Attribute("IsReadLaserMarker1").Value;
				bool.TryParse(value, out var result2);
				IsReadLaserMarker1 = result2;
				value = xElement.Attribute("IsVerifyLaserMarker1").Value;
				bool.TryParse(value, out var result3);
				IsVerifyLaserMarker1 = result3;
				value = xElement.Attribute("IsVerifyChecksumLaserMarker1").Value;
				bool.TryParse(value, out var result4);
				IsVerifyChecksumLaserMarker1 = result4;
				LaserMarkerWaferReaderIndex1 = GetValue<int>(xElement, "LaserMarkerWaferReaderIndex1");
				value = xElement.Attribute("IsReadLaserMarker2").Value;
				bool.TryParse(value, out var result5);
				IsReadLaserMarker2 = result5;
				value = xElement.Attribute("IsVerifyLaserMarker2").Value;
				bool.TryParse(value, out var result6);
				IsVerifyLaserMarker2 = result6;
				value = xElement.Attribute("IsVerifyChecksumLaserMarker2").Value;
				bool.TryParse(value, out var result7);
				IsVerifyChecksumLaserMarker2 = result7;
				LaserMarkerWaferReaderIndex2 = GetValue<int>(xElement, "LaserMarkerWaferReaderIndex2");
				value = xElement.Attribute("IsPostAlign").Value;
				bool.TryParse(value, out var result8);
				IsPostAlign = result8;
				if (xElement.Attribute("IsAlign") != null)
				{
					value = xElement.Attribute("IsAlign").Value;
					bool.TryParse(value, out var result9);
					IsAlign = result9;
				}
				value = xElement.Attribute("PostAlignAngle").Value;
				double.TryParse(value, out var result10);
				PostAlignAngle = result10;
				value = xElement.Attribute("OcrReaderJob1").Value;
				string[] source = value.Split(new char[1] { ';' }, StringSplitOptions.RemoveEmptyEntries);
				OcrReaderJob1 = new ObservableCollection<KeyValuePair<string, string>>(source.ToDictionary((string k) => Path.GetFileNameWithoutExtension(k), (string v) => v));
				value = xElement.Attribute("OcrReaderJob2").Value;
				source = value.Split(new char[1] { ';' }, StringSplitOptions.RemoveEmptyEntries);
				OcrReaderJob2 = new ObservableCollection<KeyValuePair<string, string>>(source.ToDictionary((string k) => Path.GetFileNameWithoutExtension(k), (string v) => v));
				value = xElement.Attribute("IsTurnOver").Value;
				bool.TryParse(value, out var result11);
				IsTurnOver = result11;
				if (xElement.Attribute("IsTurnOverBeforeAlign") != null)
				{
					value = xElement.Attribute("IsTurnOverBeforeAlign").Value;
					bool.TryParse(value, out var result12);
					IsTurnOverBeforeAlign = result12;
				}
				if (xElement.Attribute("IsVerifyLaserMarkLength") != null)
				{
					value = xElement.Attribute("IsVerifyLaserMarkLength").Value;
					bool.TryParse(value, out var result13);
					IsVerifyLaserMarkLength = result13;
				}
				if (xElement.Attribute("IsVerifySlotPositionInLaserMarker") != null)
				{
					value = xElement.Attribute("IsVerifySlotPositionInLaserMarker").Value;
					bool.TryParse(value, out var result14);
					IsVerifySlotPositionInLaserMarker = result14;
				}
				if (xElement.Attribute("IntVerifyLaserMarkerLength") != null)
				{
					value = xElement.Attribute("IntVerifyLaserMarkerLength").Value;
					int.TryParse(value, out var result15);
					IntVerifyLaserMarkerLength = result15;
				}
				if (xElement.Attribute("IntVerifySlotPositionInLaserMark") != null)
				{
					value = xElement.Attribute("IntVerifySlotPositionInLaserMark").Value;
					int.TryParse(value, out var result16);
					IntVerifySlotPositionInLaserMark = result16;
				}
				if (xElement.Attribute("IsVerifyWithHostDefine") != null)
				{
					value = xElement.Attribute("IsVerifyWithHostDefine").Value;
					bool.TryParse(value, out var result17);
					IsVerifyWithHostDefine = result17;
				}
				if (xElement.Attribute("BladeRegulatorRatio") != null)
				{
					value = xElement.Attribute("BladeRegulatorRatio").Value;
					double.TryParse(value, out var result18);
					BladeRegulatorRatio = result18;
				}
				if (xElement.Attribute("IsRobotFlippeBeforeAlign") != null)
				{
					value = xElement.Attribute("IsRobotFlippeBeforeAlign").Value;
					bool.TryParse(value, out var result19);
					IsRobotFlippeBeforeAlign = result19;
				}
				if (xElement.Attribute("IsRobotFlippeAfterAlign") != null)
				{
					value = xElement.Attribute("IsRobotFlippeAfterAlign").Value;
					bool.TryParse(value, out var result20);
					IsRobotFlippeAfterAlign = result20;
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
