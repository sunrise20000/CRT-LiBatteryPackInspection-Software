using System;
using System.Collections.Generic;
using System.Xml;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using MECF.Framework.Common.Equipment;

namespace Aitex.Core.RT.IOCore
{
	public class InterlockManager : Singleton<InterlockManager>
	{
		private List<InterlockAction> _actions = new List<InterlockAction>();

		private Dictionary<InterlockLimit, List<InterlockAction>> _dicLimit = new Dictionary<InterlockLimit, List<InterlockAction>>();

		public bool Initialize(string interlockFile, Dictionary<string, DOAccessor> doMap, Dictionary<string, DIAccessor> diMap, out string reason)
		{
			reason = string.Empty;
			try
			{
				XmlDocument xmlDocument = new XmlDocument();
				xmlDocument.Load(interlockFile);
				XmlNode xmlNode = xmlDocument.SelectSingleNode("Interlock");
				foreach (XmlNode childNode in xmlNode.ChildNodes)
				{
					if (childNode.NodeType == XmlNodeType.Comment || !(childNode is XmlElement xmlElement))
					{
						continue;
					}
					if (xmlElement.Name != "Action")
					{
						if (xmlElement.NodeType != XmlNodeType.Comment)
						{
							LOG.Write("interlock config file contains no comments content, " + xmlElement.InnerXml);
						}
						continue;
					}
					if (!xmlElement.HasAttribute("do") || !xmlElement.HasAttribute("value"))
					{
						reason += "action node has no [do] or [value] attribute \r\n";
						continue;
					}
					string attribute = xmlElement.GetAttribute("do");
					bool value = Convert.ToBoolean(xmlElement.GetAttribute("value"));
					string tip = string.Empty;
					Dictionary<string, string> dictionary = new Dictionary<string, string>();
					List<InterlockLimit> list = new List<InterlockLimit>();
					if (!doMap.ContainsKey(attribute))
					{
						reason = reason + "action node " + attribute + " no such DO defined \r\n";
						continue;
					}
					DOAccessor dOAccessor = doMap[attribute];
					if (dOAccessor == null)
					{
						reason = reason + "action node " + attribute + " no such DO defined \r\n";
						continue;
					}
					if (xmlElement.HasAttribute("tip"))
					{
						tip = xmlElement.GetAttribute("tip");
					}
					if (xmlElement.HasAttribute("tip.zh-CN"))
					{
						dictionary["zh-CN"] = xmlElement.GetAttribute("tip.zh-CN");
					}
					if (xmlElement.HasAttribute("tip.en-US"))
					{
						dictionary["en-US"] = xmlElement.GetAttribute("tip.en-US");
					}
					foreach (XmlElement childNode2 in xmlElement.ChildNodes)
					{
						if (childNode2.Name != "Limit")
						{
							if (childNode2.NodeType != XmlNodeType.Comment)
							{
								LOG.Write("interlock config file contains no comments content, " + childNode2.InnerXml);
							}
							continue;
						}
						if ((!childNode2.HasAttribute("di") && !childNode2.HasAttribute("do")) || !childNode2.HasAttribute("value"))
						{
							reason += "limit node lack of di/do or value attribute \r\n";
							continue;
						}
						string attribute2 = childNode2.GetAttribute("value");
						if (attribute2.Contains("*"))
						{
							continue;
						}
						bool value2 = Convert.ToBoolean(childNode2.GetAttribute("value"));
						string tip2 = string.Empty;
						Dictionary<string, string> dictionary2 = new Dictionary<string, string>();
						if (childNode2.HasAttribute("tip"))
						{
							tip2 = childNode2.GetAttribute("tip");
						}
						if (childNode2.HasAttribute("tip.zh-CN"))
						{
							dictionary2["zh-CN"] = childNode2.GetAttribute("tip.zh-CN");
						}
						if (childNode2.HasAttribute("tip.en-US"))
						{
							dictionary2["en-US"] = childNode2.GetAttribute("tip.en-US");
						}
						if (childNode2.HasAttribute("di"))
						{
							string attribute3 = childNode2.GetAttribute("di");
							if (!diMap.ContainsKey(attribute3))
							{
								reason = reason + "limit node " + attribute3 + " no such DI defined \r\n";
								continue;
							}
							DIAccessor dIAccessor = diMap[attribute3];
							if (dIAccessor == null)
							{
								reason = reason + "limit node " + attribute3 + " no such DI defined \r\n";
							}
							else
							{
								list.Add(new DiLimit(dIAccessor, value2, tip2, dictionary2));
							}
						}
						else
						{
							if (!childNode2.HasAttribute("do"))
							{
								continue;
							}
							string attribute4 = childNode2.GetAttribute("do");
							if (!doMap.ContainsKey(attribute4))
							{
								reason = reason + "limit node " + attribute4 + " no such DO defined \r\n";
								continue;
							}
							DOAccessor dOAccessor2 = doMap[attribute4];
							if (dOAccessor2 == null)
							{
								reason = reason + "limit node " + attribute4 + " no such DO defined \r\n";
							}
							else
							{
								list.Add(new DoLimit(dOAccessor2, value2, tip2, dictionary2));
							}
						}
					}
					InterlockAction item = new InterlockAction(dOAccessor, value, tip, dictionary, list);
					_actions.Add(item);
					foreach (InterlockLimit item2 in list)
					{
						bool flag = false;
						foreach (InterlockLimit key in _dicLimit.Keys)
						{
							if (item2.IsSame(key))
							{
								_dicLimit[key].Add(item);
								flag = true;
								break;
							}
						}
						if (!flag)
						{
							_dicLimit[item2] = new List<InterlockAction>();
							_dicLimit[item2].Add(item);
						}
					}
				}
			}
			catch (Exception ex)
			{
				reason += ex.Message;
			}
			if (!string.IsNullOrEmpty(reason))
			{
				return false;
			}
			return true;
		}

		public void Monitor()
		{
			foreach (KeyValuePair<InterlockLimit, List<InterlockAction>> item in _dicLimit)
			{
				if ((SC.ContainsItem("System.BypassInterlock") && SC.GetValue<bool>("System.BypassInterlock")) || !item.Key.IsTriggered())
				{
					continue;
				}
				string text = string.Empty;
				string module = "System";
				foreach (InterlockAction item2 in item.Value)
				{
					if (item2.Reverse(out var reason))
					{
						if (string.IsNullOrEmpty(text))
						{
							text = $"Due to the {item.Key.Tip}, {item.Key.Name}=[{!item.Key.LimitValue}]\n";
						}
						string[] array = item2.ActionName.Split('.');
						if (array != null && array.Length > 1 && ModuleHelper.IsPm(array[0]))
						{
							module = array[0];
						}
						text = text + reason + "\n";
					}
				}
				if (!string.IsNullOrEmpty(text))
				{
					EV.PostWarningLog(module, text.TrimEnd('\n'));
				}
			}
		}

		public bool CanSetDo(string doName, bool onOff, out string reason)
		{
			reason = string.Empty;
			if (SC.ContainsItem("System.BypassInterlock") && SC.GetValue<bool>("System.BypassInterlock"))
			{
				return true;
			}
			foreach (InterlockAction action in _actions)
			{
				if (action.IsSame(doName, onOff))
				{
					return action.CanDo(out reason);
				}
			}
			return true;
		}
	}
}
