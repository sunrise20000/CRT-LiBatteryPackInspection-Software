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
	public class SorterReadWaferIDRecipeXml : INotifyPropertyChanged
	{
		private string name;

		private bool _isReadLaserMarker1 = false;

		private bool _isVerifyLaserMarker1;

		private bool _isVerifyChecksumLaserMarker1;

		private bool _isReadLaserMarker2 = false;

		private bool _isVerifyLaserMarker2;

		private bool _isVerifyChecksumLaserMarker2;

		private double _preAlignAngle = 0.0;

		private double _postAlignAngle = 0.0;

		private bool _isPostAlign;

		private bool _isTurnOver;

		private int _laserMarkerWaferReaderIndex1;

		private int _laserMarkerWaferReaderIndex2;

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

		public SorterReadWaferIDRecipeXml(string name = "")
		{
			Name = name;
			OcrReaderJob1 = new ObservableCollection<KeyValuePair<string, string>>();
			OcrReaderJob2 = new ObservableCollection<KeyValuePair<string, string>>();
		}

		public SorterReadWaferIDRecipeXml(string content, string name = "")
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
			XElement content = new XElement("AitexSorterReadWaferIDRecipe", new XAttribute("PreAlignAngle", PreAlignAngle), new XAttribute("IsReadLaserMarker1", IsReadLaserMarker1.ToString()), new XAttribute("IsVerifyLaserMarker1", IsVerifyLaserMarker1.ToString()), new XAttribute("IsVerifyChecksumLaserMarker1", IsVerifyChecksumLaserMarker1.ToString()), new XAttribute("LaserMarkerWaferReaderIndex1", LaserMarkerWaferReaderIndex1), new XAttribute("IsReadLaserMarker2", IsReadLaserMarker2.ToString()), new XAttribute("IsVerifyLaserMarker2", IsVerifyLaserMarker2.ToString()), new XAttribute("IsVerifyChecksumLaserMarker2", IsVerifyChecksumLaserMarker2.ToString()), new XAttribute("LaserMarkerWaferReaderIndex2", LaserMarkerWaferReaderIndex2), new XAttribute("IsPostAlign", IsPostAlign.ToString()), new XAttribute("PostAlignAngle", PostAlignAngle), new XAttribute("OcrReaderJob1", (OcrReaderJob1 != null) ? string.Join(";", OcrReaderJob1.Select((KeyValuePair<string, string> x) => x.Value)) : ""), new XAttribute("OcrReaderJob2", (OcrReaderJob2 != null) ? string.Join(";", OcrReaderJob2.Select((KeyValuePair<string, string> x) => x.Value)) : ""), new XAttribute("IsTurnOver", IsTurnOver.ToString()));
			xElement.Add(content);
			xDocument.Add(xElement);
			return xDocument.ToString();
		}

		private void ParseContent(XDocument doc)
		{
			try
			{
				XElement xElement = doc.Root.Element("AitexSorterReadWaferIDRecipe");
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
				value = xElement.Attribute("PostAlignAngle").Value;
				double.TryParse(value, out var result9);
				PostAlignAngle = result9;
				value = xElement.Attribute("OcrReaderJob1").Value;
				string[] source = value.Split(new char[1] { ';' }, StringSplitOptions.RemoveEmptyEntries);
				OcrReaderJob1 = new ObservableCollection<KeyValuePair<string, string>>(source.ToDictionary((string k) => Path.GetFileNameWithoutExtension(k), (string v) => v));
				value = xElement.Attribute("OcrReaderJob2").Value;
				source = value.Split(new char[1] { ';' }, StringSplitOptions.RemoveEmptyEntries);
				OcrReaderJob2 = new ObservableCollection<KeyValuePair<string, string>>(source.ToDictionary((string k) => Path.GetFileNameWithoutExtension(k), (string v) => v));
				value = xElement.Attribute("IsTurnOver").Value;
				bool.TryParse(value, out var result10);
				IsTurnOver = result10;
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
