using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.OperationCenter;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.RT.Core.IoProviders;

namespace MECF.Framework.Common.IOCore
{
	public class IoManager : Singleton<IoManager>, IIoBuffer
	{
		private Dictionary<string, DIAccessor> _diMap = new Dictionary<string, DIAccessor>();

		private Dictionary<string, DOAccessor> _doMap = new Dictionary<string, DOAccessor>();

		private Dictionary<string, AIAccessor> _aiMap = new Dictionary<string, AIAccessor>();

		private Dictionary<string, AOAccessor> _aoMap = new Dictionary<string, AOAccessor>();

		private Dictionary<string, Dictionary<int, bool[]>> _diBuffer = new Dictionary<string, Dictionary<int, bool[]>>();

		private Dictionary<string, Dictionary<int, bool[]>> _doBuffer = new Dictionary<string, Dictionary<int, bool[]>>();

		private Dictionary<string, Dictionary<int, short[]>> _aiBufferShort = new Dictionary<string, Dictionary<int, short[]>>();

		private Dictionary<string, Dictionary<int, short[]>> _aoBufferShort = new Dictionary<string, Dictionary<int, short[]>>();

		private Dictionary<string, Dictionary<int, float[]>> _aiBufferFloat = new Dictionary<string, Dictionary<int, float[]>>();

		private Dictionary<string, Dictionary<int, float[]>> _aoBufferFloat = new Dictionary<string, Dictionary<int, float[]>>();

		private Dictionary<string, Dictionary<int, Type>> _aiBufferType = new Dictionary<string, Dictionary<int, Type>>();

		private Dictionary<string, Dictionary<int, Type>> _aoBufferType = new Dictionary<string, Dictionary<int, Type>>();

		private Dictionary<string, List<DIAccessor>> _diList = new Dictionary<string, List<DIAccessor>>();

		private Dictionary<string, List<DOAccessor>> _doList = new Dictionary<string, List<DOAccessor>>();

		private Dictionary<string, List<AIAccessor>> _aiList = new Dictionary<string, List<AIAccessor>>();

		private Dictionary<string, List<AOAccessor>> _aoList = new Dictionary<string, List<AOAccessor>>();

		private Dictionary<string, List<NotifiableIoItem>> _ioItemList = new Dictionary<string, List<NotifiableIoItem>>();

		private PeriodicJob _monitorThread;

		public void Initialize(string interlockConfigFile)
		{
			string reason = string.Empty;
			if (!Singleton<InterlockManager>.Instance.Initialize(interlockConfigFile, _doMap, _diMap, out reason))
			{
				throw new Exception($"interlock define file found error: \r\n {reason}");
			}
			_monitorThread = new PeriodicJob(200, OnTimer, "IO Monitor Thread", isStartNow: true);
		}

		private bool OnTimer()
		{
			try
			{
				Singleton<InterlockManager>.Instance.Monitor();
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
			}
			return true;
		}

		internal List<Tuple<int, int, string>> GetIONameList(string group, IOType ioType)
		{
			return null;
		}

		public bool CanSetDo(string doName, bool onOff, out string reason)
		{
			return Singleton<InterlockManager>.Instance.CanSetDo(doName, onOff, out reason);
		}

		public List<DIAccessor> GetDIList(string source)
		{
			return _diList.ContainsKey(source) ? _diList[source] : null;
		}

		public List<DOAccessor> GetDOList(string source)
		{
			return _doList.ContainsKey(source) ? _doList[source] : null;
		}

		public List<AIAccessor> GetAIList(string source)
		{
			return _aiList.ContainsKey(source) ? _aiList[source] : null;
		}

		public List<AOAccessor> GetAOList(string source)
		{
			return _aoList.ContainsKey(source) ? _aoList[source] : null;
		}

		public IoManager()
		{
			OP.Subscribe("System.SetDoValue", InvokeSetDo);
			OP.Subscribe("System.SetAoValue", InvokeSetAo);
			OP.Subscribe("System.SetAoValueFloat", InvokeSetAoFloat);
			OP.Subscribe("System.SetDoValueWithPrivoder", InvokeSetDoWithPrivoder);
			OP.Subscribe("System.SetAoValueWithPrivoder", InvokeSetAoWithPrivoder);
			OP.Subscribe("System.SetAiBuffer", InvokeSetAiBuffer);
			OP.Subscribe("System.SetDiBuffer", InvokeSetDiBuffer);
		}

		private bool InvokeSetDo(string arg1, object[] args)
		{
			string text = (string)args[0];
			bool flag = (bool)args[1];
			if (!CanSetDo(text, flag, out var reason))
			{
				EV.PostWarningLog("System", $"Can not set DO {text} to {flag}, {reason}");
				return false;
			}
			DOAccessor iO = GetIO<DOAccessor>(text);
			if (iO == null)
			{
				EV.PostWarningLog("System", $"Can not set DO {text} to {flag}, not defined do");
				return false;
			}
			if (!iO.SetValue(flag, out reason))
			{
				EV.PostWarningLog("System", $"Can not set DO {text} to {flag}, {reason}");
				return false;
			}
			EV.PostInfoLog("System", $"Change DO {text} to {flag}");
			return true;
		}

		private bool InvokeSetAo(string arg1, object[] args)
		{
			string text = (string)args[0];
			short num = (short)args[1];
			AOAccessor iO = GetIO<AOAccessor>(text);
			if (iO == null)
			{
				EV.PostWarningLog("System", $"Can not set AO {text} to {num}, not defined do");
				return false;
			}
			iO.Value = num;
			EV.PostInfoLog("System", $"Change AO {text} to {num}");
			return true;
		}

		private bool InvokeSetAoFloat(string arg1, object[] args)
		{
			string text = (string)args[0];
			float num = (float)args[1];
			AOAccessor iO = GetIO<AOAccessor>(text);
			if (iO == null)
			{
				EV.PostWarningLog("System", $"Can not set AO {text} to {num}, not defined");
				return false;
			}
			iO.FloatValue = num;
			EV.PostInfoLog("System", $"Change AO {text} to {num}");
			return true;
		}

		private bool InvokeSetDoWithPrivoder(string arg1, object[] args)
		{
			string text = (string)args[0];
			int num = (int)args[1];
			string name = (string)args[2];
			bool flag = (bool)args[3];
			if (!CanSetDo(name, flag, out var reason))
			{
				EV.PostWarningLog("System", $"Can not set DO {text}.{name} to {flag}, {reason}");
				return false;
			}
			List<DOAccessor> dOList = GetDOList(text);
			if (dOList == null)
			{
				EV.PostWarningLog("System", $"Can not set DO {text}.{name} to {flag}, {reason}");
				return false;
			}
			DOAccessor dOAccessor = dOList.FirstOrDefault((DOAccessor x) => x.Name == name);
			if (dOAccessor == null)
			{
				EV.PostWarningLog("System", $"Can not set DO {text}.{name} to {flag}, {reason}");
				return false;
			}
			if (!dOAccessor.SetValue(flag, out reason))
			{
				EV.PostWarningLog("System", $"Can not set DO {text}.{name} to {flag}, {reason}");
				return false;
			}
			EV.PostInfoLog("System", $"Change DO {text}.{name} to {flag}");
			return true;
		}

		private bool InvokeSetAiBuffer(string arg1, object[] args)
		{
			string source = (string)args[0];
			int offset = (int)args[1];
			short[] buffer = (short[])args[2];
			SetAiBuffer(source, offset, buffer);
			return true;
		}

		private bool InvokeSetDiBuffer(string arg1, object[] args)
		{
			string source = (string)args[0];
			int offset = (int)args[1];
			bool[] buffer = (bool[])args[2];
			SetDiBuffer(source, offset, buffer);
			return true;
		}

		private bool InvokeSetAoWithPrivoder(string arg1, object[] args)
		{
			string text = (string)args[0];
			int num = (int)args[1];
			string name = (string)args[2];
			float num2 = (float)args[3];
			string text2 = "";
			List<AOAccessor> aOList = GetAOList(text);
			if (aOList == null)
			{
				EV.PostWarningLog("System", $"Can not set AO {text}.{name} to {num2}, {text2}");
				return false;
			}
			AOAccessor aOAccessor = aOList.FirstOrDefault((AOAccessor x) => x.Name == name);
			if (aOAccessor == null)
			{
				EV.PostWarningLog("System", $"Can not set AO {text}.{name} to {num2}, {text2}");
				return false;
			}
			aOAccessor.Value = (short)num2;
			EV.PostInfoLog("System", $"Change DO {text}.{name} to {num2}");
			return true;
		}

		public T GetIO<T>(string name) where T : class
		{
			if (typeof(T) == typeof(DIAccessor) && _diMap.ContainsKey(name))
			{
				return _diMap[name] as T;
			}
			if (typeof(T) == typeof(DOAccessor) && _doMap.ContainsKey(name))
			{
				return _doMap[name] as T;
			}
			if (typeof(T) == typeof(AIAccessor) && _aiMap.ContainsKey(name))
			{
				return _aiMap[name] as T;
			}
			if (typeof(T) == typeof(AOAccessor) && _aoMap.ContainsKey(name))
			{
				return _aoMap[name] as T;
			}
			return null;
		}

		public Dictionary<int, bool[]> GetDiBuffer(string source)
		{
			if (_diBuffer.ContainsKey(source))
			{
				return _diBuffer[source];
			}
			return null;
		}

		public Dictionary<int, bool[]> GetDoBuffer(string source)
		{
			if (_doBuffer.ContainsKey(source))
			{
				return _doBuffer[source];
			}
			return null;
		}

		public Dictionary<int, short[]> GetAiBuffer(string source)
		{
			if (_aiBufferShort.ContainsKey(source))
			{
				return _aiBufferShort[source];
			}
			return null;
		}

		public Dictionary<int, float[]> GetAoBufferFloat(string source)
		{
			if (_aoBufferFloat.ContainsKey(source))
			{
				return _aoBufferFloat[source];
			}
			return null;
		}

		public Dictionary<int, float[]> GetAiBufferFloat(string source)
		{
			if (_aiBufferFloat.ContainsKey(source))
			{
				return _aiBufferFloat[source];
			}
			return null;
		}

		public Dictionary<int, short[]> GetAoBuffer(string source)
		{
			if (_aoBufferShort.ContainsKey(source))
			{
				return _aoBufferShort[source];
			}
			return null;
		}

		public void SetDiBuffer(string source, int offset, bool[] buffer)
		{
			if (_diBuffer.ContainsKey(source) && _diBuffer[source].ContainsKey(offset))
			{
				for (int i = 0; i < buffer.Length && i < _diBuffer[source][offset].Length; i++)
				{
					_diBuffer[source][offset][i] = buffer[i];
				}
			}
		}

		public void SetAiBuffer(string source, int offset, short[] buffer, int skipSize = 0)
		{
			if (_aiBufferShort.ContainsKey(source) && _aiBufferShort[source].ContainsKey(offset))
			{
				for (int i = 0; i < buffer.Length && i < _aiBufferShort[source][offset].Length; i++)
				{
					_aiBufferShort[source][offset][i + skipSize] = buffer[i];
				}
			}
		}

		public void SetAiBufferFloat(string source, int offset, float[] buffer, int skipSize = 0)
		{
			if (_aiBufferFloat.ContainsKey(source) && _aiBufferFloat[source].ContainsKey(offset))
			{
				for (int i = 0; i < buffer.Length && i < _aiBufferFloat[source][offset].Length; i++)
				{
					_aiBufferFloat[source][offset][i + skipSize] = buffer[i];
				}
			}
		}

		public void SetDoBuffer(string source, int offset, bool[] buffer)
		{
			if (_doBuffer.ContainsKey(source) && _doBuffer[source].ContainsKey(offset))
			{
				for (int i = 0; i < buffer.Length && i < _doBuffer[source][offset].Length; i++)
				{
					_doBuffer[source][offset][i] = buffer[i];
				}
			}
		}

		public void SetAoBuffer(string source, int offset, short[] buffer)
		{
			if (_aoBufferShort.ContainsKey(source) && _aoBufferShort[source].ContainsKey(offset))
			{
				for (int i = 0; i < buffer.Length && i < _aoBufferShort[source][offset].Length; i++)
				{
					_aoBufferShort[source][offset][i] = buffer[i];
				}
			}
		}

		public void SetAoBufferFloat(string source, int offset, float[] buffer, int bufferStartIndex = 0)
		{
			if (_aoBufferFloat.ContainsKey(source) && _aoBufferFloat[source].ContainsKey(offset))
			{
				for (int i = 0; i < buffer.Length && i < _aoBufferFloat[source][offset].Length; i++)
				{
					_aoBufferFloat[source][offset][i] = buffer[i];
				}
			}
		}

		public void SetAoBuffer(string source, int offset, short[] buffer, int bufferStartIndex)
		{
			if (_aoBufferShort.ContainsKey(source) && _aoBufferShort[source].ContainsKey(offset) && _aoBufferShort[source][offset].Length > bufferStartIndex)
			{
				for (int i = 0; i < buffer.Length && bufferStartIndex + i < _aoBufferShort[source][offset].Length; i++)
				{
					_aoBufferShort[source][offset][bufferStartIndex + i] = buffer[i];
				}
			}
		}

		public void SetBufferBlock(string provider, List<IoBlockItem> lstBlocks)
		{
			foreach (IoBlockItem lstBlock in lstBlocks)
			{
				switch (lstBlock.Type)
				{
				case IoType.AI:
					if (!_aiBufferShort.ContainsKey(provider))
					{
						_aiBufferShort[provider] = new Dictionary<int, short[]>();
						_aiBufferFloat[provider] = new Dictionary<int, float[]>();
						_aiBufferType[provider] = new Dictionary<int, Type>();
					}
					if (!_aiBufferShort[provider].ContainsKey(lstBlock.Offset))
					{
						_aiBufferShort[provider][lstBlock.Offset] = new short[lstBlock.Size];
						_aiBufferFloat[provider][lstBlock.Offset] = new float[lstBlock.Size];
						_aiBufferType[provider][lstBlock.Offset] = lstBlock.AIOType;
					}
					break;
				case IoType.AO:
					if (!_aoBufferShort.ContainsKey(provider))
					{
						_aoBufferShort[provider] = new Dictionary<int, short[]>();
						_aoBufferFloat[provider] = new Dictionary<int, float[]>();
						_aoBufferType[provider] = new Dictionary<int, Type>();
					}
					if (!_aoBufferShort[provider].ContainsKey(lstBlock.Offset))
					{
						_aoBufferShort[provider][lstBlock.Offset] = new short[lstBlock.Size];
						_aoBufferFloat[provider][lstBlock.Offset] = new float[lstBlock.Size];
						_aoBufferType[provider][lstBlock.Offset] = lstBlock.AIOType;
					}
					break;
				case IoType.DI:
					if (!_diBuffer.ContainsKey(provider))
					{
						_diBuffer[provider] = new Dictionary<int, bool[]>();
					}
					if (!_diBuffer[provider].ContainsKey(lstBlock.Offset))
					{
						_diBuffer[provider][lstBlock.Offset] = new bool[lstBlock.Size];
					}
					break;
				case IoType.DO:
					if (!_doBuffer.ContainsKey(provider))
					{
						_doBuffer[provider] = new Dictionary<int, bool[]>();
					}
					if (!_doBuffer[provider].ContainsKey(lstBlock.Offset))
					{
						_doBuffer[provider][lstBlock.Offset] = new bool[lstBlock.Size];
					}
					break;
				}
			}
		}

		private List<NotifiableIoItem> SubscribeDiData()
		{
			List<NotifiableIoItem> list = new List<NotifiableIoItem>();
			foreach (KeyValuePair<string, DIAccessor> item2 in _diMap)
			{
				NotifiableIoItem item = new NotifiableIoItem
				{
					Address = item2.Value.Addr,
					Name = item2.Value.Name,
					Description = item2.Value.Description,
					Index = item2.Value.Index,
					BoolValue = item2.Value.Value,
					Provider = item2.Value.Provider,
					BlockOffset = item2.Value.BlockOffset,
					BlockIndex = item2.Value.Index,
					Visible = item2.Value.Visible
				};
				list.Add(item);
			}
			return list;
		}

		private List<NotifiableIoItem> SubscribeDoData()
		{
			List<NotifiableIoItem> list = new List<NotifiableIoItem>();
			foreach (KeyValuePair<string, DOAccessor> item2 in _doMap)
			{
				NotifiableIoItem item = new NotifiableIoItem
				{
					Address = item2.Value.Addr,
					Name = item2.Value.Name,
					Description = item2.Value.Description,
					Index = item2.Value.Index,
					BoolValue = item2.Value.Value,
					Provider = item2.Value.Provider,
					BlockOffset = item2.Value.BlockOffset,
					BlockIndex = item2.Value.Index,
					Visible = item2.Value.Visible
				};
				list.Add(item);
			}
			return list;
		}

		private List<NotifiableIoItem> SubscribeAiData()
		{
			List<NotifiableIoItem> list = new List<NotifiableIoItem>();
			foreach (KeyValuePair<string, AIAccessor> item2 in _aiMap)
			{
				NotifiableIoItem item = new NotifiableIoItem
				{
					Address = item2.Value.Addr,
					Name = item2.Value.Name,
					Description = item2.Value.Description,
					Index = item2.Value.Index,
					ShortValue = item2.Value.Value,
					FloatValue = item2.Value.FloatValue,
					Provider = item2.Value.Provider,
					BlockOffset = item2.Value.BlockOffset,
					BlockIndex = item2.Value.Index,
					Visible = item2.Value.Visible
				};
				list.Add(item);
			}
			return list;
		}

		private List<NotifiableIoItem> SubscribeAoData()
		{
			List<NotifiableIoItem> list = new List<NotifiableIoItem>();
			foreach (KeyValuePair<string, AOAccessor> item2 in _aoMap)
			{
				NotifiableIoItem item = new NotifiableIoItem
				{
					Address = item2.Value.Addr,
					Name = item2.Value.Name,
					Description = item2.Value.Description,
					Index = item2.Value.Index,
					ShortValue = item2.Value.Value,
					FloatValue = item2.Value.FloatValue,
					Provider = item2.Value.Provider,
					BlockOffset = item2.Value.BlockOffset,
					BlockIndex = item2.Value.Index,
					Visible = item2.Value.Visible
				};
				list.Add(item);
			}
			return list;
		}

		public void SetIoMap(string provider, int blockOffset, List<DIAccessor> ioList)
		{
			SubscribeIoItemList(provider);
			bool flag = SC.GetConfigItem("System.IsIgnoreSaveDB")?.BoolValue ?? false;
			foreach (DIAccessor accessor in ioList)
			{
				accessor.Provider = provider;
				accessor.BlockOffset = blockOffset;
				_diMap[accessor.Name] = accessor;
				if (!_diList.ContainsKey(provider))
				{
					_diList[provider] = new List<DIAccessor>();
				}
				_diList[provider].Add(accessor);
				_ioItemList[provider + ".DIItemList"].Add(new NotifiableIoItem
				{
					Address = accessor.Addr,
					Name = accessor.Name,
					Description = accessor.Description,
					Index = accessor.Index,
					Provider = provider,
					BlockOffset = blockOffset,
					BlockIndex = accessor.BlockOffset,
					Visible = accessor.Visible
				});
				if (!flag)
				{
					DATA.Subscribe("IO." + accessor.Name, () => accessor.Value);
				}
			}
		}

		public void SetIoMap(string provider, int blockOffset, List<DOAccessor> ioList)
		{
			SubscribeIoItemList(provider);
			bool flag = SC.GetConfigItem("System.IsIgnoreSaveDB")?.BoolValue ?? false;
			foreach (DOAccessor accessor in ioList)
			{
				accessor.Provider = provider;
				accessor.BlockOffset = blockOffset;
				_doMap[accessor.Name] = accessor;
				if (!_doList.ContainsKey(provider))
				{
					_doList[provider] = new List<DOAccessor>();
				}
				_doList[provider].Add(accessor);
				_ioItemList[provider + ".DOItemList"].Add(new NotifiableIoItem
				{
					Address = accessor.Addr,
					Name = accessor.Name,
					Description = accessor.Description,
					Index = accessor.Index,
					Provider = provider,
					BlockOffset = blockOffset,
					BlockIndex = accessor.BlockOffset,
					Visible = accessor.Visible
				});
				if (!flag)
				{
					DATA.Subscribe("IO." + accessor.Name, () => accessor.Value);
				}
			}
		}

		public void SetIoMap(string provider, int blockOffset, List<AIAccessor> ioList)
		{
			SubscribeIoItemList(provider);
			bool flag = SC.GetConfigItem("System.IsIgnoreSaveDB")?.BoolValue ?? false;
			foreach (AIAccessor accessor in ioList)
			{
				accessor.Provider = provider;
				accessor.BlockOffset = blockOffset;
				_aiMap[accessor.Name] = accessor;
				if (!_aiList.ContainsKey(provider))
				{
					_aiList[provider] = new List<AIAccessor>();
				}
				_aiList[provider].Add(accessor);
				_ioItemList[provider + ".AIItemList"].Add(new NotifiableIoItem
				{
					Address = accessor.Addr,
					Name = accessor.Name,
					Description = accessor.Description,
					Index = accessor.Index,
					Provider = provider,
					BlockOffset = blockOffset,
					BlockIndex = accessor.BlockOffset,
					Visible = accessor.Visible
				});
				if (!flag)
				{
					DATA.Subscribe("IO." + accessor.Name, () => accessor.Value);
				}
			}
		}

		public void SetIoMap(string provider, int blockOffset, List<AOAccessor> ioList)
		{
			SubscribeIoItemList(provider);
			bool flag = SC.GetConfigItem("System.IsIgnoreSaveDB")?.BoolValue ?? false;
			foreach (AOAccessor accessor in ioList)
			{
				accessor.Provider = provider;
				accessor.BlockOffset = blockOffset;
				_aoMap[accessor.Name] = accessor;
				if (!_aoList.ContainsKey(provider))
				{
					_aoList[provider] = new List<AOAccessor>();
				}
				_aoList[provider].Add(accessor);
				_ioItemList[provider + ".AOItemList"].Add(new NotifiableIoItem
				{
					Address = accessor.Addr,
					Name = accessor.Name,
					Description = accessor.Description,
					Index = accessor.Index,
					Provider = provider,
					BlockOffset = blockOffset,
					BlockIndex = accessor.BlockOffset,
					Visible = accessor.Visible
				});
				if (!flag)
				{
					DATA.Subscribe("IO." + accessor.Name, () => accessor.Value);
				}
			}
		}

		public void SetIoMap(string provider, int blockOffset, string xmlPathFile, string module = "")
		{
			SubscribeIoItemList(provider);
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.Load(xmlPathFile);
			XmlNodeList xmlNodeList = xmlDocument.SelectNodes("IO_DEFINE/Dig_In/DI_ITEM");
			bool flag = SC.GetConfigItem("System.IsIgnoreSaveDB")?.BoolValue ?? false;
			List<DIAccessor> list = new List<DIAccessor>();
			foreach (object item in xmlNodeList)
			{
				if (!(item is XmlElement xmlElement))
				{
					continue;
				}
				string attribute = xmlElement.GetAttribute("Index");
				string text = xmlElement.GetAttribute("BufferOffset");
				if (string.IsNullOrEmpty(text))
				{
					text = attribute;
				}
				string attribute2 = xmlElement.GetAttribute("Name");
				string attribute3 = xmlElement.GetAttribute("Addr");
				string attribute4 = xmlElement.GetAttribute("Description");
				bool visible = true;
				if (xmlElement.HasAttribute("Visible"))
				{
					visible = Convert.ToBoolean(xmlElement.GetAttribute("Visible"));
				}
				if (string.IsNullOrEmpty(attribute2) || string.IsNullOrEmpty(attribute) || string.IsNullOrEmpty(text))
				{
					continue;
				}
				attribute2 = attribute2.Trim();
				attribute = attribute.Trim();
				text = text.Trim();
				string text2 = (string.IsNullOrEmpty(module) ? attribute2 : (module + "." + attribute2));
				if (!int.TryParse(attribute, out var result) || !int.TryParse(text, out var result2))
				{
					continue;
				}
				if (!_diBuffer.ContainsKey(provider))
				{
					throw new Exception("Not defined DI buffer from IO provider, " + provider);
				}
				if (!_diBuffer[provider].ContainsKey(blockOffset))
				{
					throw new Exception("Not defined DI buffer from IO provider, " + provider);
				}
				DIAccessor diAccessor = new DIAccessor(text2, result2, _diBuffer[provider][blockOffset], _diBuffer[provider][blockOffset]);
				diAccessor.IoTableIndex = result;
				diAccessor.Addr = attribute3;
				diAccessor.Provider = provider;
				diAccessor.BlockOffset = blockOffset;
				diAccessor.Description = attribute4;
				diAccessor.Visible = visible;
				list.Add(diAccessor);
				_diMap[text2] = diAccessor;
				if (!_diList.ContainsKey(provider))
				{
					_diList[provider] = new List<DIAccessor>();
				}
				_diList[provider].Add(diAccessor);
				_ioItemList[provider + ".DIItemList"].Add(new NotifiableIoItem
				{
					Address = attribute3,
					Name = text2,
					Description = attribute4,
					Index = result,
					Provider = provider,
					BlockOffset = blockOffset,
					BlockIndex = result,
					Visible = visible
				});
				if (!flag)
				{
					DATA.Subscribe("IO." + text2, () => diAccessor.Value);
				}
			}
			XmlNodeList xmlNodeList2 = xmlDocument.SelectNodes("IO_DEFINE/Dig_Out/DO_ITEM");
			foreach (object item2 in xmlNodeList2)
			{
				if (!(item2 is XmlElement xmlElement2))
				{
					continue;
				}
				string attribute5 = xmlElement2.GetAttribute("Index");
				string text3 = xmlElement2.GetAttribute("BufferOffset");
				if (string.IsNullOrEmpty(text3))
				{
					text3 = attribute5;
				}
				string attribute6 = xmlElement2.GetAttribute("Name");
				string attribute7 = xmlElement2.GetAttribute("Addr");
				string attribute8 = xmlElement2.GetAttribute("Description");
				bool visible2 = true;
				if (xmlElement2.HasAttribute("Visible"))
				{
					visible2 = Convert.ToBoolean(xmlElement2.GetAttribute("Visible"));
				}
				if (string.IsNullOrEmpty(attribute6) || string.IsNullOrEmpty(attribute5) || string.IsNullOrEmpty(text3))
				{
					continue;
				}
				attribute6 = attribute6.Trim();
				attribute5 = attribute5.Trim();
				text3 = text3.Trim();
				string text4 = (string.IsNullOrEmpty(module) ? attribute6 : (module + "." + attribute6));
				if (!int.TryParse(attribute5, out var result3) || !int.TryParse(text3, out var result4))
				{
					continue;
				}
				if (!_doBuffer.ContainsKey(provider) || !_doBuffer[provider].ContainsKey(blockOffset))
				{
					throw new Exception("Not defined DO buffer from IO provider, " + provider);
				}
				DOAccessor doAccessor = new DOAccessor(text4, result4, _doBuffer[provider][blockOffset]);
				_doMap[text4] = doAccessor;
				doAccessor.IoTableIndex = result3;
				doAccessor.Addr = attribute7;
				doAccessor.Provider = provider;
				doAccessor.BlockOffset = blockOffset;
				doAccessor.Description = attribute8;
				doAccessor.Visible = visible2;
				if (!_doList.ContainsKey(provider))
				{
					_doList[provider] = new List<DOAccessor>();
				}
				_doList[provider].Add(doAccessor);
				_ioItemList[provider + ".DOItemList"].Add(new NotifiableIoItem
				{
					Address = attribute7,
					Name = text4,
					Description = attribute8,
					Index = result3,
					Provider = provider,
					BlockOffset = blockOffset,
					BlockIndex = result3,
					Visible = visible2
				});
				if (!flag)
				{
					DATA.Subscribe("IO." + text4, () => doAccessor.Value);
				}
			}
			XmlNodeList xmlNodeList3 = xmlDocument.SelectNodes("IO_DEFINE/Ana_Out/AO_ITEM");
			foreach (object item3 in xmlNodeList3)
			{
				if (!(item3 is XmlElement xmlElement3))
				{
					continue;
				}
				string attribute9 = xmlElement3.GetAttribute("Index");
				string text5 = xmlElement3.GetAttribute("BufferOffset");
				if (string.IsNullOrEmpty(text5))
				{
					text5 = attribute9;
				}
				string attribute10 = xmlElement3.GetAttribute("Name");
				string attribute11 = xmlElement3.GetAttribute("Addr");
				string attribute12 = xmlElement3.GetAttribute("Description");
				bool visible3 = true;
				if (xmlElement3.HasAttribute("Visible"))
				{
					visible3 = Convert.ToBoolean(xmlElement3.GetAttribute("Visible"));
				}
				if (string.IsNullOrEmpty(attribute10) || string.IsNullOrEmpty(attribute9) || string.IsNullOrEmpty(text5))
				{
					continue;
				}
				attribute10 = attribute10.Trim();
				attribute9 = attribute9.Trim();
				text5 = text5.Trim();
				string text6 = (string.IsNullOrEmpty(module) ? attribute10 : (module + "." + attribute10));
				if (!int.TryParse(attribute9, out var result5) || !int.TryParse(text5, out var result6))
				{
					continue;
				}
				if (!_aoBufferShort.ContainsKey(provider) || !_aoBufferShort[provider].ContainsKey(blockOffset))
				{
					throw new Exception("Not defined AO buffer from IO provider, " + provider);
				}
				AOAccessor aoAccessor = new AOAccessor(text6, result6, _aoBufferShort[provider][blockOffset], _aoBufferFloat[provider][blockOffset]);
				_aoMap[text6] = aoAccessor;
				aoAccessor.IoTableIndex = result5;
				aoAccessor.Addr = attribute11;
				aoAccessor.Provider = provider;
				aoAccessor.BlockOffset = blockOffset;
				aoAccessor.Description = attribute12;
				aoAccessor.Visible = visible3;
				if (!_aoList.ContainsKey(provider))
				{
					_aoList[provider] = new List<AOAccessor>();
				}
				_aoList[provider].Add(aoAccessor);
				_ioItemList[provider + ".AOItemList"].Add(new NotifiableIoItem
				{
					Address = attribute11,
					Name = text6,
					Description = attribute12,
					Index = result5,
					Provider = provider,
					BlockOffset = blockOffset,
					BlockIndex = result5,
					Visible = visible3
				});
				if (flag)
				{
					continue;
				}
				if (_aoBufferType.ContainsKey(provider) && _aoBufferType[provider].ContainsKey(blockOffset) && _aoBufferType[provider][blockOffset] == typeof(float))
				{
					DATA.Subscribe("IO." + text6, () => aoAccessor.FloatValue);
				}
				else
				{
					DATA.Subscribe("IO." + text6, () => aoAccessor.Value);
				}
			}
			XmlNodeList xmlNodeList4 = xmlDocument.SelectNodes("IO_DEFINE/Ana_In/AI_ITEM");
			foreach (object item4 in xmlNodeList4)
			{
				if (!(item4 is XmlElement xmlElement4))
				{
					continue;
				}
				string attribute13 = xmlElement4.GetAttribute("Index");
				string text7 = xmlElement4.GetAttribute("BufferOffset");
				if (string.IsNullOrEmpty(text7))
				{
					text7 = attribute13;
				}
				string attribute14 = xmlElement4.GetAttribute("Name");
				string attribute15 = xmlElement4.GetAttribute("Addr");
				string attribute16 = xmlElement4.GetAttribute("Description");
				bool visible4 = true;
				if (xmlElement4.HasAttribute("Visible"))
				{
					visible4 = Convert.ToBoolean(xmlElement4.GetAttribute("Visible"));
				}
				if (string.IsNullOrEmpty(attribute14) || string.IsNullOrEmpty(attribute13) || string.IsNullOrEmpty(text7))
				{
					continue;
				}
				attribute14 = attribute14.Trim();
				attribute13 = attribute13.Trim();
				text7 = text7.Trim();
				string text8 = (string.IsNullOrEmpty(module) ? attribute14 : (module + "." + attribute14));
				if (!int.TryParse(attribute13, out var result7) || !int.TryParse(text7, out var result8))
				{
					continue;
				}
				if (!_aiBufferShort.ContainsKey(provider) || !_aiBufferShort[provider].ContainsKey(blockOffset))
				{
					throw new Exception("Not defined AI buffer from IO provider, " + provider);
				}
				AIAccessor aiAccessor = new AIAccessor(text8, result8, _aiBufferShort[provider][blockOffset], _aiBufferFloat[provider][blockOffset]);
				_aiMap[text8] = aiAccessor;
				aiAccessor.IoTableIndex = result7;
				aiAccessor.Addr = attribute15;
				aiAccessor.Provider = provider;
				aiAccessor.BlockOffset = blockOffset;
				aiAccessor.Description = attribute16;
				aiAccessor.Visible = visible4;
				if (!_aiList.ContainsKey(provider))
				{
					_aiList[provider] = new List<AIAccessor>();
				}
				_aiList[provider].Add(aiAccessor);
				_ioItemList[provider + ".AIItemList"].Add(new NotifiableIoItem
				{
					Address = attribute15,
					Name = text8,
					Description = attribute16,
					Index = result7,
					Provider = provider,
					BlockOffset = blockOffset,
					BlockIndex = result7,
					Visible = visible4
				});
				if (flag)
				{
					continue;
				}
				if (_aiBufferType.ContainsKey(provider) && _aiBufferType[provider].ContainsKey(blockOffset) && _aiBufferType[provider][blockOffset] == typeof(float))
				{
					DATA.Subscribe("IO." + text8, () => aiAccessor.FloatValue);
				}
				else
				{
					DATA.Subscribe("IO." + text8, () => aiAccessor.Value);
				}
			}
		}

		public void SetIoMap(string provider, Dictionary<int, string> ioMappingPathFile)
		{
			foreach (KeyValuePair<int, string> item in ioMappingPathFile)
			{
				SetIoMap(provider, item.Key, item.Value);
			}
			DATA.Subscribe(provider, "DIList", SubscribeDiData);
			DATA.Subscribe(provider, "DOList", SubscribeDoData);
			DATA.Subscribe(provider, "AIList", SubscribeAiData);
			DATA.Subscribe(provider, "AOList", SubscribeAoData);
		}

		public void SetIoMapByModule(string provider, int offset, string ioMappingPathFile, string module)
		{
			SetIoMap(provider, offset, ioMappingPathFile, module);
			DATA.Subscribe(provider, "DIList", SubscribeDiData);
			DATA.Subscribe(provider, "DOList", SubscribeDoData);
			DATA.Subscribe(provider, "AIList", SubscribeAiData);
			DATA.Subscribe(provider, "AOList", SubscribeAoData);
		}

		private void SubscribeIoItemList(string provider)
		{
			string diKey = provider + ".DIItemList";
			if (!_ioItemList.ContainsKey(diKey))
			{
				_ioItemList[diKey] = new List<NotifiableIoItem>();
				DATA.Subscribe(diKey, () => _ioItemList[diKey]);
			}
			string doKey = provider + ".DOItemList";
			if (!_ioItemList.ContainsKey(doKey))
			{
				_ioItemList[doKey] = new List<NotifiableIoItem>();
				DATA.Subscribe(doKey, () => _ioItemList[doKey]);
			}
			string aiKey = provider + ".AIItemList";
			if (!_ioItemList.ContainsKey(aiKey))
			{
				_ioItemList[aiKey] = new List<NotifiableIoItem>();
				DATA.Subscribe(aiKey, () => _ioItemList[aiKey]);
			}
			string aoKey = provider + ".AOItemList";
			if (!_ioItemList.ContainsKey(aoKey))
			{
				_ioItemList[aoKey] = new List<NotifiableIoItem>();
				DATA.Subscribe(aoKey, () => _ioItemList[aoKey]);
			}
		}
	}
}
