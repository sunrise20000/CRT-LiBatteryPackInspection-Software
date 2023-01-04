#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using Aitex.Core.RT.Log;
using Aitex.Core.Utilities;
using MECF.Framework.Common.Equipment;

namespace MECF.Framework.Common.FAServices
{
	public class VIDGenerator
	{
		public Dictionary<string, int> _moduleIndex = new Dictionary<string, int>();

		public Dictionary<string, int> _moduleTypeIndex = new Dictionary<string, int>();

		public Dictionary<string, int> _moduleTypeUnitIndex = new Dictionary<string, int>();

		public Dictionary<string, int> _parameterIndex = new Dictionary<string, int>();

		public Dictionary<string, int> _index = new Dictionary<string, int>();

		public Dictionary<string, int> _max = new Dictionary<string, int>();

		private string _defaultPathFile;

		private string _type;

		public string Type { get; set; }

		public string SourceFileName { get; set; }

		public VIDGenerator(string type, string defaultPathFile)
		{
			_type = type;
			_defaultPathFile = defaultPathFile;
		}

		public void Initialize()
		{
			XmlDocument xmlDocument = new XmlDocument();
			try
			{
				xmlDocument.Load(_defaultPathFile);
				XmlNodeList xmlNodeList = xmlDocument.SelectNodes("DataItems/DataItem");
				foreach (object item in xmlNodeList)
				{
					if (!(item is XmlElement xmlElement))
					{
						continue;
					}
					string text = xmlElement.GetAttribute("name").Trim();
					string text2 = xmlElement.GetAttribute("index").Trim();
					if (!string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(text2) && (text2.Length == 9 || text2.Length == 8))
					{
						Tuple<string, string, string, string> tuple = ParseName(text);
						_moduleTypeIndex[tuple.Item1 + "." + tuple.Item2] = int.Parse(text2.Substring(text2.Length - 7, 2));
						_moduleTypeUnitIndex[tuple.Item1 + "." + tuple.Item2 + "." + tuple.Item3] = int.Parse(text2.Substring(text2.Length - 5, 2));
						_parameterIndex[tuple.Item1 + "." + tuple.Item2 + "." + tuple.Item3 + "." + tuple.Item4] = int.Parse(text2.Substring(text2.Length - 3, 3));
						if (!_max.ContainsKey(tuple.Item1 ?? ""))
						{
							_max[tuple.Item1 ?? ""] = 0;
						}
						_max[tuple.Item1 ?? ""] = Math.Max(_moduleTypeIndex[tuple.Item1 + "." + tuple.Item2], _max[tuple.Item1 ?? ""]);
						if (!_max.ContainsKey(tuple.Item1 + "." + tuple.Item2))
						{
							_max[tuple.Item1 + "." + tuple.Item2] = 0;
						}
						_max[tuple.Item1 + "." + tuple.Item2] = Math.Max(_moduleTypeUnitIndex[tuple.Item1 + "." + tuple.Item2 + "." + tuple.Item3], _max[tuple.Item1 + "." + tuple.Item2]);
						if (!_max.ContainsKey(tuple.Item1 + "." + tuple.Item2 + "." + tuple.Item3))
						{
							_max[tuple.Item1 + "." + tuple.Item2 + "." + tuple.Item3] = 0;
						}
						_max[tuple.Item1 + "." + tuple.Item2 + "." + tuple.Item3] = Math.Max(_parameterIndex[tuple.Item1 + "." + tuple.Item2 + "." + tuple.Item3 + "." + tuple.Item4], _max[tuple.Item1 + "." + tuple.Item2 + "." + tuple.Item3]);
						_index[text] = int.Parse(text2);
					}
				}
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
			}
			EnumLoop<ModuleName>.ForEach(delegate(ModuleName item)
			{
				_moduleIndex[item.ToString()] = (int)(item + 1);
			});
		}

		public Tuple<string, string, string, string> ParseName(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				return null;
			}
			string item = ModuleName.System.ToString();
			string text = "";
			string item2 = "";
			string item3 = "";
			string[] array = name.Split('.');
			if (array.Length >= 1)
			{
				item3 = array[array.Length - 1];
				if (array.Length >= 2)
				{
					if (_moduleIndex.ContainsKey(array[0]))
					{
						item = array[0];
						if (array.Length >= 3)
						{
							item2 = array[array.Length - 2];
							if (array.Length >= 4)
							{
								for (int i = 1; i < array.Length - 2; i++)
								{
									text += array[i];
									if (i != array.Length - 3)
									{
										text += ".";
									}
								}
							}
						}
					}
					else
					{
						item2 = array[array.Length - 2];
						if (array.Length >= 3)
						{
							for (int j = 0; j < array.Length - 2; j++)
							{
								text += array[j];
								if (j != array.Length - 3)
								{
									text += ".";
								}
							}
						}
					}
				}
			}
			return Tuple.Create(item, text, item2, item3);
		}

		public void GenerateId(List<VIDItem> dataList)
		{
			List<string> list = new List<string>();
			foreach (VIDItem data in dataList)
			{
				if (!_index.ContainsKey(data.Name))
				{
					list.Add(data.Name);
				}
			}
			AssignNewId(list);
		}

		private void AssignNewId(List<string> dataList)
		{
			if (dataList.Count == 0)
			{
				return;
			}
			foreach (string data in dataList)
			{
				Tuple<string, string, string, string> tuple = ParseName(data);
				string text = _moduleIndex[tuple.Item1].ToString();
				string key = tuple.Item1 + "." + tuple.Item2;
				if (!_moduleTypeIndex.ContainsKey(key))
				{
					if (!_max.ContainsKey(tuple.Item1))
					{
						_max[tuple.Item1] = 0;
					}
					if (_moduleTypeIndex.Count == 0)
					{
						_moduleTypeIndex[key] = 1;
					}
					else
					{
						_moduleTypeIndex[key] = _max[tuple.Item1] + 1;
					}
					_max[tuple.Item1] = _moduleTypeIndex[key];
				}
				text += $"{_moduleTypeIndex[key]:D2}";
				key = tuple.Item1 + "." + tuple.Item2 + "." + tuple.Item3;
				if (!_moduleTypeUnitIndex.ContainsKey(key))
				{
					if (!_max.ContainsKey(tuple.Item1 + "." + tuple.Item2))
					{
						_max[tuple.Item1 + "." + tuple.Item2] = 0;
					}
					if (_moduleTypeUnitIndex.Count == 0)
					{
						_moduleTypeUnitIndex[key] = 1;
					}
					else
					{
						_moduleTypeUnitIndex[key] = _max[tuple.Item1 + "." + tuple.Item2] + 1;
					}
					_max[tuple.Item1 + "." + tuple.Item2] = _moduleTypeUnitIndex[key];
				}
				text += $"{_moduleTypeUnitIndex[key]:D2}";
				key = tuple.Item1 + "." + tuple.Item2 + "." + tuple.Item3 + "." + tuple.Item4;
				if (!_parameterIndex.ContainsKey(key))
				{
					if (!_max.ContainsKey(tuple.Item1 + "." + tuple.Item2 + "." + tuple.Item3))
					{
						_max[tuple.Item1 + "." + tuple.Item2 + "." + tuple.Item3] = 0;
					}
					if (_parameterIndex.Count == 0)
					{
						_parameterIndex[key] = 1;
					}
					else
					{
						_parameterIndex[key] = _max[tuple.Item1 + "." + tuple.Item2 + "." + tuple.Item3] + 1;
					}
					_max[tuple.Item1 + "." + tuple.Item2 + "." + tuple.Item3] = _parameterIndex[key];
				}
				text += $"{_parameterIndex[key]:D3}";
				int num = int.Parse(text);
				if (_index.ContainsValue(num))
				{
					Trace.WriteLine($"duplicated, {num}, ignore {data}");
				}
				else
				{
					_index[data] = num;
				}
			}
			XmlDocument xmlDocument = new XmlDocument();
			try
			{
				xmlDocument.Load(_defaultPathFile);
				XmlNode xmlNode = xmlDocument.SelectSingleNode("DataItems");
				xmlNode.RemoveAll();
				Dictionary<int, string> dictionary = _index.OrderBy((KeyValuePair<string, int> o) => o.Value).ToDictionary((KeyValuePair<string, int> p) => p.Value, (KeyValuePair<string, int> o) => o.Key);
				foreach (KeyValuePair<int, string> item in dictionary)
				{
					XmlElement xmlElement = xmlDocument.CreateElement("DataItem");
					xmlElement.SetAttribute("name", item.Value);
					xmlElement.SetAttribute("index", item.Key.ToString());
					xmlNode.AppendChild(xmlElement);
				}
				xmlDocument.Save(_defaultPathFile);
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
			}
		}
	}
}
