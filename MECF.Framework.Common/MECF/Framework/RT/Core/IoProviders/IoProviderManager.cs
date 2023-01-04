using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.IOCore;

namespace MECF.Framework.RT.Core.IoProviders
{
	public class IoProviderManager : Singleton<IoProviderManager>
	{
		private List<IIoProvider> _providers = new List<IIoProvider>();

		private Dictionary<string, IIoProvider> _dicProviders = new Dictionary<string, IIoProvider>();

		public List<IIoProvider> Providers => _providers;

		public IIoProvider GetProvider(string name)
		{
			if (_dicProviders.ContainsKey(name))
			{
				return _dicProviders[name];
			}
			return null;
		}

		public void Initialize(string xmlConfigFile, Dictionary<string, Dictionary<int, string>> ioMappingPathFile)
		{
			XmlDocument xmlDocument = new XmlDocument();
			try
			{
				xmlDocument.Load(xmlConfigFile);
				XmlNodeList xmlNodeList = xmlDocument.SelectNodes("IoProviders/IoProvider");
				foreach (object item in xmlNodeList)
				{
					if (!(item is XmlElement xmlElement) || !(xmlElement.SelectSingleNode("Parameter") is XmlElement nodeParameter))
					{
						continue;
					}
					string text = xmlElement.GetAttribute("module").Trim();
					string text2 = xmlElement.GetAttribute("name").Trim();
					string name = xmlElement.GetAttribute("class").Trim();
					string assemblyString = xmlElement.GetAttribute("assembly").Trim();
					string text3 = xmlElement.GetAttribute("load_condition").Trim();
					bool boolValue = SC.GetConfigItem("System.IsSimulatorMode").BoolValue;
					if ((boolValue && text3 != "0" && text3 != "2") || (!boolValue && text3 != "1" && text3 != "2"))
					{
						continue;
					}
					string text4 = text + "." + text2;
					Type type = Assembly.Load(assemblyString).GetType(name);
					if (type == null)
					{
						throw new Exception(string.Format("ioProvider config file class and assembly not valid," + text4));
					}
					IIoProvider ioProvider;
					try
					{
						ioProvider = (IIoProvider)Activator.CreateInstance(type);
						_providers.Add(ioProvider);
						_dicProviders[text4] = ioProvider;
					}
					catch (Exception ex)
					{
						LOG.Write(ex);
						throw new Exception(string.Format("ioProvider can not be created," + text4));
					}
					List<IoBlockItem> list = new List<IoBlockItem>();
					XmlNodeList xmlNodeList2 = xmlElement.SelectNodes("Blocks/Block");
					foreach (object item2 in xmlNodeList2)
					{
						if (!(item2 is XmlElement xmlElement2))
						{
							continue;
						}
						IoBlockItem ioBlockItem = new IoBlockItem();
						string attribute = xmlElement2.GetAttribute("type");
						string attribute2 = xmlElement2.GetAttribute("offset");
						string attribute3 = xmlElement2.GetAttribute("size");
						string attribute4 = xmlElement2.GetAttribute("value_type");
						if (!int.TryParse(attribute2, out var result))
						{
							continue;
						}
						ioBlockItem.Offset = result;
						if (int.TryParse(attribute3, out result))
						{
							ioBlockItem.Size = result;
							switch (attribute.ToLower())
							{
							case "ai":
								ioBlockItem.Type = IoType.AI;
								break;
							case "ao":
								ioBlockItem.Type = IoType.AO;
								break;
							case "di":
								ioBlockItem.Type = IoType.DI;
								break;
							case "do":
								ioBlockItem.Type = IoType.DO;
								break;
							default:
								continue;
							}
							if (ioBlockItem.Type == IoType.AI || ioBlockItem.Type == IoType.AO)
							{
								ioBlockItem.AIOType = ((string.IsNullOrEmpty(attribute4) || attribute4.ToLower() != "float") ? typeof(short) : typeof(float));
							}
							list.Add(ioBlockItem);
						}
					}
					if (ioMappingPathFile.ContainsKey(text4))
					{
						ioProvider.Initialize(text, text2, list, Singleton<IoManager>.Instance, nodeParameter, ioMappingPathFile[text4]);
						ioProvider.Start();
						continue;
					}
					throw new Exception(string.Format("can not find io map config files," + text4));
				}
			}
			catch (Exception ex2)
			{
				throw new ApplicationException("IoProvider configuration not valid," + ex2.Message);
			}
		}

		public void Initialize(string xmlConfigFile)
		{
			XmlDocument xmlDocument = new XmlDocument();
			try
			{
				xmlDocument.Load(xmlConfigFile);
				XmlNodeList xmlNodeList = xmlDocument.SelectNodes("IoProviders/IoProvider");
				foreach (object item in xmlNodeList)
				{
					if (!(item is XmlElement xmlElement) || !(xmlElement.SelectSingleNode("Parameter") is XmlElement nodeParameter))
					{
						continue;
					}
					string text = xmlElement.GetAttribute("module").Trim();
					string text2 = xmlElement.GetAttribute("name").Trim();
					string name = xmlElement.GetAttribute("class").Trim();
					string assemblyString = xmlElement.GetAttribute("assembly").Trim();
					string text3 = xmlElement.GetAttribute("load_condition").Trim();
					bool boolValue = SC.GetConfigItem("System.IsSimulatorMode").BoolValue;
					if ((boolValue && text3 != "0" && text3 != "2") || (!boolValue && text3 != "1" && text3 != "2"))
					{
						continue;
					}
					string text4 = text + "." + text2;
					Type type = Assembly.Load(assemblyString).GetType(name);
					if (type == null)
					{
						throw new Exception(string.Format("ioProvider config file class and assembly not valid," + text4));
					}
					IIoProvider ioProvider;
					try
					{
						ioProvider = (IIoProvider)Activator.CreateInstance(type);
						_providers.Add(ioProvider);
						_dicProviders[text4] = ioProvider;
					}
					catch (Exception ex)
					{
						LOG.Write(ex);
						throw new Exception(string.Format("ioProvider can not be created," + text4));
					}
					List<IoBlockItem> list = new List<IoBlockItem>();
					XmlNodeList xmlNodeList2 = xmlElement.SelectNodes("Blocks/Block");
					foreach (object item2 in xmlNodeList2)
					{
						if (!(item2 is XmlElement xmlElement2))
						{
							continue;
						}
						IoBlockItem ioBlockItem = new IoBlockItem();
						string attribute = xmlElement2.GetAttribute("type");
						string attribute2 = xmlElement2.GetAttribute("offset");
						string attribute3 = xmlElement2.GetAttribute("size");
						string attribute4 = xmlElement2.GetAttribute("value_type");
						if (!int.TryParse(attribute2, out var result))
						{
							continue;
						}
						ioBlockItem.Offset = result;
						if (int.TryParse(attribute3, out result))
						{
							ioBlockItem.Size = result;
							switch (attribute.ToLower())
							{
							case "ai":
								ioBlockItem.Type = IoType.AI;
								break;
							case "ao":
								ioBlockItem.Type = IoType.AO;
								break;
							case "di":
								ioBlockItem.Type = IoType.DI;
								break;
							case "do":
								ioBlockItem.Type = IoType.DO;
								break;
							default:
								continue;
							}
							if (ioBlockItem.Type == IoType.AI || ioBlockItem.Type == IoType.AO)
							{
								ioBlockItem.AIOType = ((string.IsNullOrEmpty(attribute4) || attribute4.ToLower() != "float") ? typeof(short) : typeof(float));
							}
							list.Add(ioBlockItem);
						}
					}
					string ioModule = xmlElement.GetAttribute("map_module").Trim();
					string text5 = xmlElement.GetAttribute("map_file").Trim();
					FileInfo fileInfo = new FileInfo(xmlConfigFile);
					string ioMappingPathFile = fileInfo.Directory.FullName + "\\" + text5;
					ioProvider.Initialize(text, text2, list, Singleton<IoManager>.Instance, nodeParameter, ioMappingPathFile, ioModule);
					ioProvider.Start();
				}
			}
			catch (Exception ex2)
			{
				throw new ApplicationException("IoProvider configuration not valid," + ex2.Message);
			}
		}

		public void Terminate()
		{
			try
			{
				foreach (IIoProvider provider in _providers)
				{
					provider.Stop();
				}
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
			}
		}

		public void Reset()
		{
			try
			{
				foreach (IIoProvider provider in _providers)
				{
					provider.Reset();
				}
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
			}
		}
	}
}
