using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Schema;
using Aitex.Common.Util;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.Util;
using Aitex.Core.Utilities;
using Aitex.Core.WCF;
using MECF.Framework.Common.DBCore;
using MECF.Framework.Common.Properties;
using MECF.Framework.Common.RecipeCenter;

namespace Aitex.Core.RT.RecipeCenter
{
	public class RecipeFileManager : Singleton<RecipeFileManager>
	{
		public const string SequenceFolder = "Sequence";

		public const string SourceModule = "Recipe";

		private bool _recipeIsValid;

		private bool _recipeSaveInDB = false;

		private List<string> _validationErrors = new List<string>();

		private List<string> _validationWarnings = new List<string>();

		private Dictionary<int, Dictionary<string, string>> _recipeItems;

		private IRecipeFileContext _rcpContext;

		private ISequenceFileContext _seqContext;

		public void Initialize(IRecipeFileContext context)
		{
			Initialize(context, null, enableService: true);
		}

		public void Initialize(IRecipeFileContext context, bool enableService, bool recipeSaveInDB)
		{
			_recipeSaveInDB = recipeSaveInDB;
			Initialize(context, null, enableService);
		}

		public void Initialize(IRecipeFileContext rcpContext, ISequenceFileContext seqContext, bool enableService)
		{
			IRecipeFileContext rcpContext2;
			if (rcpContext != null)
			{
				rcpContext2 = rcpContext;
			}
			else
			{
				IRecipeFileContext recipeFileContext = new DefaultRecipeFileContext();
				rcpContext2 = recipeFileContext;
			}
			_rcpContext = rcpContext2;
			ISequenceFileContext seqContext2;
			if (seqContext != null)
			{
				seqContext2 = seqContext;
			}
			else
			{
				ISequenceFileContext sequenceFileContext = new DefaultSequenceFileContext();
				seqContext2 = sequenceFileContext;
			}
			_seqContext = seqContext2;
			CultureSupported.UpdateCoreCultureResource("en-US");
			if (enableService)
			{
				Singleton<WcfServiceManager>.Instance.Initialize(new Type[1] { typeof(RecipeService) });
			}
			string path = string.Format("{0}{1}\\", PathManager.GetRecipeDir(), "Sequence");
			DirectoryInfo directoryInfo = new DirectoryInfo(path);
			if (!directoryInfo.Exists)
			{
				directoryInfo.Create();
			}
			MoveRecipe();
			MoveSequence();
		}

		private void MoveSequence()
		{
			DataTable allData = SequenceInfoRecorder.GetAllData();
			if (!_recipeSaveInDB)
			{
				if (allData == null || allData.Rows.Count <= 0)
				{
					return;
				}
				for (int i = 0; i < allData.Rows.Count; i++)
				{
					try
					{
						string @string = Encoding.UTF8.GetString((byte[])allData.Rows[i]["fileDetail"]);
						SaveSequence(allData.Rows[i]["seqName"].ToString().Replace(".seq", ""), @string, notifyUI: false);
					}
					catch
					{
					}
				}
				SequenceInfoRecorder.DeleteAllData();
			}
			else if (allData == null || allData.Rows.Count <= 0)
			{
				MoveSequeceToDB();
			}
		}

		private void MoveRecipe()
		{
			DataTable allData = RecipeInfoRecorer.GetAllData();
			if (!_recipeSaveInDB)
			{
				List<string> list = new List<string>();
				if (allData == null || allData.Rows.Count <= 0)
				{
					return;
				}
				for (int i = 0; i < allData.Rows.Count; i++)
				{
					string text = allData.Rows[i]["rootName"].ToString() + "\\" + allData.Rows[i]["recipeName"].ToString();
					string arg = text;
					if (text.IndexOf(".rcp") > 0)
					{
						arg = text.Substring(0, text.LastIndexOf("\\"));
					}
					string path = $"{PathManager.GetRecipeDir()}{arg}\\";
					DirectoryInfo directoryInfo = new DirectoryInfo(path);
					if (!directoryInfo.Exists)
					{
						directoryInfo.Create();
					}
					if (text.IndexOf(".rcp") > 0)
					{
						try
						{
							string @string = Encoding.UTF8.GetString((byte[])allData.Rows[i]["fileDetail"]);
							SaveRecipe(allData.Rows[i]["rootName"].ToString(), allData.Rows[i]["recipeName"].ToString().Replace(".rcp", ""), @string, clearBarcode: false, notifyUI: false);
						}
						catch
						{
						}
					}
				}
				RecipeInfoRecorer.DeleteAllData();
			}
			else if (allData == null || allData.Rows.Count <= 0)
			{
				MoveReipeToDB();
			}
		}

		private void MoveReipeToDB()
		{
			string recipeDir = PathManager.GetRecipeDir();
			DirectoryInfo directoryInfo = new DirectoryInfo(recipeDir + "\\Sic");
			if (directoryInfo.Exists)
			{
				DirectoryInfo[] directories = directoryInfo.GetDirectories();
				DirectoryInfo[] array = directories;
				foreach (DirectoryInfo directoryInfo2 in array)
				{
					WriteDBFromLocal(directoryInfo2, "Sic\\" + directoryInfo2.Name, directoryInfo2.FullName + "\\");
				}
			}
		}

		private void WriteDBFromLocal(DirectoryInfo dir, string rootName, string rootPath)
		{
			DirectoryInfo[] directories = dir.GetDirectories();
			DirectoryInfo[] array = directories;
			foreach (DirectoryInfo dir2 in array)
			{
				WriteDBFromLocal(dir2, rootName, rootPath);
			}
			FileInfo[] files = dir.GetFiles("*.rcp");
			FileInfo[] array2 = files;
			foreach (FileInfo fileInfo in array2)
			{
				string recipeName = fileInfo.FullName.Replace(rootPath, "").Replace(".rcp", "");
				string recipeContent = LoadRecipe(rootName, recipeName, needValidation: false, isWriteToAnother: true);
				SaveRecipe(rootName, recipeName, recipeContent, clearBarcode: false, notifyUI: false);
			}
		}

		private void MoveSequeceToDB()
		{
			string path = string.Format("{0}{1}\\", PathManager.GetRecipeDir(), "Sequence");
			DirectoryInfo directoryInfo = new DirectoryInfo(path);
			if (directoryInfo.Exists)
			{
				FileInfo[] files = directoryInfo.GetFiles("*.seq");
				FileInfo[] array = files;
				foreach (FileInfo fileInfo in array)
				{
					string sequenceName = fileInfo.Name.Replace(".seq", "");
					string sequence = GetSequence(sequenceName, needValidation: false, isLoadAnother: true);
					SaveSequence(sequenceName, sequence, notifyUI: false);
				}
			}
		}

		private void ValidationEventHandler(object sender, ValidationEventArgs e)
		{
			switch (e.Severity)
			{
			case XmlSeverityType.Error:
				_validationErrors.Add(e.Message);
				_recipeIsValid = false;
				break;
			case XmlSeverityType.Warning:
				_validationWarnings.Add(e.Message);
				break;
			}
		}

		public bool ValidateRecipe(string chamberId, string recipeName, string recipeContent, out List<string> reason)
		{
			try
			{
				XmlDocument xmlDocument = new XmlDocument();
				xmlDocument.LoadXml(recipeContent);
				MemoryStream input = new MemoryStream(Encoding.ASCII.GetBytes(GetRecipeSchema(chamberId)));
				XmlReader reader = XmlReader.Create(input);
				XmlSchema schema = XmlSchema.Read(reader, ValidationEventHandler);
				xmlDocument.Schemas.Add(schema);
				xmlDocument.LoadXml(recipeContent);
				ValidationEventHandler validationEventHandler = ValidationEventHandler;
				_recipeIsValid = true;
				_validationErrors = new List<string>();
				_validationWarnings = new List<string>();
				xmlDocument.Validate(validationEventHandler);
			}
			catch (Exception ex)
			{
				LOG.Write(ex.Message);
				_recipeIsValid = false;
			}
			if (!_recipeIsValid && _validationErrors.Count == 0)
			{
				_validationErrors.Add(Resources.RecipeFileManager_ValidateRecipe_XMLSchemaValidateFailed);
			}
			reason = _validationErrors;
			return _recipeIsValid;
		}

		public bool CheckRampRate(int stepNo, string rampEnable, string varName, string rampTime, double maxRampUpRate, double maxRampDownRate)
		{
			try
			{
				if (stepNo <= 0)
				{
					return false;
				}
				switch (varName)
				{
				default:
					if (!(varName == "DZone.Setpoint"))
					{
						break;
					}
					goto case "AZone.Setpoint";
				case "AZone.Setpoint":
				case "BZone.Setpoint":
				case "CZone.Setpoint":
				{
					string text = _recipeItems[stepNo]["Heater.Mode"];
					string text2 = _recipeItems[stepNo - 1]["Heater.Mode"];
					if (text != text2)
					{
						return false;
					}
					break;
				}
				}
				bool flag = bool.Parse(rampEnable);
				string[] array = rampTime.Split(new char[1] { ':' }, StringSplitOptions.RemoveEmptyEntries);
				double num = 0.0;
				double num2 = 0.0;
				double num3 = 0.0;
				if (array.Length == 3)
				{
					num = double.Parse(array[0]);
					num2 = double.Parse(array[1]);
					num3 = double.Parse(array[2]);
				}
				else if (array.Length == 2)
				{
					num2 = double.Parse(array[0]);
					num3 = double.Parse(array[1]);
				}
				else if (array.Length == 1)
				{
					num3 = double.Parse(array[0]);
				}
				double num4 = num * 3600.0 + num2 * 60.0 + num3;
				double num5 = double.Parse(_recipeItems[stepNo][varName]) - double.Parse(_recipeItems[stepNo - 1][varName]);
				if (!flag || num4 <= 0.0)
				{
					if (num5 != 0.0)
					{
						return true;
					}
					return false;
				}
				double num6 = num5 / num4;
				if ((num6 > 0.0 && num6 >= maxRampUpRate) || (num6 < 0.0 && num6 <= 0.0 - maxRampDownRate))
				{
					return true;
				}
				return false;
			}
			catch (Exception ex)
			{
				LOG.Write(ex.Message);
				return true;
			}
		}

		public bool CheckRecipe(string chamberId, string recipeContent, out List<string> reasons)
		{
			reasons = new List<string>();
			try
			{
				XmlDocument xmlDocument = new XmlDocument();
				xmlDocument.LoadXml(GetRecipeFormatXml(chamberId));
				Dictionary<string, double> dictionary = new Dictionary<string, double>();
				Dictionary<string, double> dictionary2 = new Dictionary<string, double>();
				foreach (XmlElement item in xmlDocument.SelectNodes("/TableRecipeFormat/Catalog/Group/Step[@DeviceType='MFC']"))
				{
					dictionary.Add(item.Attributes["ControlName"].Value, Convert.ToDouble(item.Attributes["Max"].Value));
				}
				foreach (XmlElement item2 in xmlDocument.SelectNodes("/TableRecipeFormat/Catalog/Group/Step[@DeviceType='PC']"))
				{
					dictionary2.Add(item2.Attributes["ControlName"].Value, Convert.ToDouble(item2.Attributes["Max"].Value));
				}
				XmlDocument xmlDocument2 = new XmlDocument();
				xmlDocument2.LoadXml(recipeContent);
				_recipeItems = new Dictionary<int, Dictionary<string, string>>();
				XmlNodeList xmlNodeList = xmlDocument2.SelectNodes("/TableRecipeData/Step");
				for (int i = 0; i < xmlNodeList.Count; i++)
				{
					XmlElement xmlElement3 = xmlNodeList[i] as XmlElement;
					Dictionary<string, string> dictionary3 = new Dictionary<string, string>();
					_recipeItems.Add(i, dictionary3);
					foreach (XmlAttribute attribute in xmlElement3.Attributes)
					{
						dictionary3.Add(attribute.Name, attribute.Value);
					}
					foreach (XmlElement childNode in xmlElement3.ChildNodes)
					{
						foreach (XmlAttribute attribute2 in childNode.Attributes)
						{
							dictionary3.Add(attribute2.Name, attribute2.Value);
						}
						foreach (XmlElement childNode2 in childNode.ChildNodes)
						{
							foreach (XmlAttribute attribute3 in childNode2.Attributes)
							{
								dictionary3.Add(attribute3.Name, attribute3.Value);
							}
							foreach (XmlElement childNode3 in childNode2.ChildNodes)
							{
								foreach (XmlAttribute attribute4 in childNode3.Attributes)
								{
									dictionary3.Add(attribute4.Name, attribute4.Value);
								}
							}
						}
					}
				}
				for (int j = 0; j < _recipeItems.Count; j++)
				{
					string text = _recipeItems[j]["Loop"];
					bool flag = Regex.IsMatch(text, "^Loop\\x20\\d+$");
					bool flag2 = Regex.IsMatch(text, "^Loop End$");
					bool flag3 = string.IsNullOrWhiteSpace(text);
					if (!flag2 && !flag && !flag3)
					{
						string arg = $"Value '{text}' not valid";
						reasons.Add($"第{j + 1}步，{arg}.");
					}
					if (flag2)
					{
						string arg2 = "Loop Start 缺失";
						reasons.Add($"第{j + 1}步，{arg2}.");
					}
					else
					{
						if (!flag)
						{
							continue;
						}
						for (int k = j + 1; k < _recipeItems.Count; k++)
						{
							string text2 = _recipeItems[k]["Loop"];
							bool flag4 = Regex.IsMatch(text2, "^Loop\\x20\\d+$");
							bool flag5 = Regex.IsMatch(text2, "^Loop End$");
							flag3 = string.IsNullOrWhiteSpace(text2);
							if (!flag5 && !flag4 && !flag3)
							{
								string arg3 = $"Value '{text2}' not valid";
								reasons.Add($"第{k + 1}步，{arg3}.");
							}
							else if (flag4)
							{
								string arg4 = "前面循环没有结束，不能设置新的Loop Start标志";
								reasons.Add($"第{k + 1}步，{arg4}.");
							}
							else if (flag5)
							{
								j = k;
								break;
							}
							if (k == _recipeItems.Count - 1)
							{
								j = k;
								string arg5 = "Loop End 缺失";
								reasons.Add($"第{k + 1}步，{arg5}.");
							}
						}
					}
				}
				for (int l = 0; l < _recipeItems.Count; l++)
				{
					foreach (string key in dictionary.Keys)
					{
						if (_recipeItems[l].ContainsKey(key))
						{
							double num = Convert.ToDouble(_recipeItems[l][key]);
							if (num < 0.0 || num > dictionary[key])
							{
								reasons.Add($"第{l + 1}步，{key}设定{num}，超出 0~{dictionary[key]}sccm范围.");
							}
						}
					}
					foreach (string key2 in dictionary2.Keys)
					{
						if (_recipeItems[l].ContainsKey(key2))
						{
							double num2 = Convert.ToDouble(_recipeItems[l][key2]);
							if (num2 < 0.0 || num2 > dictionary2[key2])
							{
								reasons.Add($"第{l + 1}步，{key2}设定{num2}，超出 0~{dictionary2[key2]}mbar范围.");
							}
						}
					}
				}
				Dictionary<string, string> dictionary4 = new Dictionary<string, string>();
				foreach (XmlElement item3 in xmlDocument.SelectNodes("/TableRecipeFormat/Validation/Predefine/Item"))
				{
					dictionary4.Add(item3.Attributes["VarName"].Value, item3.Attributes["Value"].Value);
				}
			}
			catch (Exception ex)
			{
				reasons.Add(Resources.RecipeFileManager_CheckRecipe_RecipeValidationFailed + ex.Message);
				LOG.Write(ex);
				return false;
			}
			return reasons.Count == 0;
		}

		public string LoadRecipe(string chamberId, string recipeName, bool needValidation, bool isWriteToAnother = false)
		{
			string result = string.Empty;
			try
			{
				bool flag = _recipeSaveInDB;
				if (isWriteToAnother)
				{
					flag = !_recipeSaveInDB;
				}
				if (flag)
				{
					return RecipeInfoRecorer.GetRecipeInfoByFileName(chamberId, recipeName);
				}
				using StreamReader streamReader = new StreamReader(GenerateRecipeFilePath(chamberId, recipeName));
				result = streamReader.ReadToEnd();
				streamReader.Close();
			}
			catch (Exception ex)
			{
				LOG.Write(ex, "load recipe file failed, " + recipeName);
				result = string.Empty;
			}
			return result;
		}

		public IEnumerable<string> GetRecipes(string chamberId, bool includingUsedRecipe)
		{
			if (_recipeSaveInDB)
			{
				DataTable rootAllDirectoryAndFiles = RecipeInfoRecorer.GetRootAllDirectoryAndFiles(chamberId);
				List<string> list = new List<string>();
				if (rootAllDirectoryAndFiles != null && rootAllDirectoryAndFiles.Rows.Count > 0)
				{
					for (int i = 0; i < rootAllDirectoryAndFiles.Rows.Count; i++)
					{
						list.Add(rootAllDirectoryAndFiles.Rows[i][0].ToString().Replace(".rcp", ""));
					}
				}
				return list;
			}
			return _rcpContext.GetRecipes(chamberId, includingUsedRecipe);
		}

		public string GetXmlRecipeList(string chamberId, bool includingUsedRecipe)
		{
			XmlDocument xmlDocument = new XmlDocument();
			string recipeDirPath = getRecipeDirPath(chamberId);
			DirectoryInfo currentDir = new DirectoryInfo(recipeDirPath);
			xmlDocument.AppendChild(GenerateRecipeList(chamberId, currentDir, xmlDocument, includingUsedRecipe));
			return xmlDocument.OuterXml;
		}

		public void SaveRecipeHistory(string chamberId, string recipeName, string recipeContent, bool needSaveAs = true)
		{
			try
			{
				if (!string.IsNullOrEmpty(recipeName) && needSaveAs)
				{
					string recipeName2 = string.Format("HistoryRecipe\\{0}\\{1}", DateTime.Now.ToString("yyyyMM"), recipeName);
					SaveRecipe(chamberId, recipeName2, recipeContent, clearBarcode: true, notifyUI: false);
					LOG.Write($"{chamberId}通知TM保存工艺程序{recipeName}");
				}
			}
			catch (Exception ex)
			{
				LOG.Write(ex, $"保存{chamberId}工艺程序{recipeName}发生错误");
			}
		}

		private XmlElement GenerateRecipeList(string currentDir, List<string> lstDir, XmlDocument doc)
		{
			XmlElement xmlElement = doc.CreateElement("Folder");
			string value = currentDir;
			if (currentDir.IndexOf("\\") > 0)
			{
				value = currentDir.Substring(currentDir.LastIndexOf("\\") + 1);
			}
			xmlElement.SetAttribute("Name", value);
			List<string> nextForderList = GetNextForderList(lstDir, currentDir);
			if (nextForderList.Count > 0)
			{
				foreach (string item in nextForderList)
				{
					xmlElement.AppendChild(GenerateRecipeList(item, GetCurrentFileListAll(lstDir, item), doc));
				}
			}
			List<string> currentRcpList = GetCurrentRcpList(lstDir, currentDir);
			if (currentRcpList.Count > 0)
			{
				foreach (string item2 in currentRcpList)
				{
					XmlElement xmlElement2 = doc.CreateElement("File");
					string value2 = item2.Substring(0, item2.LastIndexOf("."));
					xmlElement2.SetAttribute("Name", value2);
					xmlElement.AppendChild(xmlElement2);
				}
			}
			return xmlElement;
		}

		private List<string> GetCurrentFileListAll(List<string> lstDir, string curForder)
		{
			List<string> list = new List<string>();
			for (int i = 0; i < lstDir.Count; i++)
			{
				if (string.IsNullOrEmpty(curForder))
				{
					return lstDir;
				}
				if ((lstDir[i].IndexOf(curForder + "\\") == 0 || lstDir[i] == curForder) && !list.Contains(lstDir[i]))
				{
					list.Add(lstDir[i]);
				}
			}
			return list;
		}

		private List<string> GetCurrentRcpList(List<string> lstDir, string curForder)
		{
			List<string> list = new List<string>();
			for (int i = 0; i < lstDir.Count; i++)
			{
				if (string.IsNullOrEmpty(curForder))
				{
					if (lstDir[i].IndexOf("\\") <= 0 && lstDir[i].IndexOf(".rcp") > 0 && !list.Contains(lstDir[i]))
					{
						list.Add(lstDir[i]);
					}
				}
				else if (lstDir[i].IndexOf(curForder + "\\") == 0)
				{
					string text = lstDir[i].Substring(curForder.Length + 1);
					if (text.IndexOf("\\") <= 0 && text.IndexOf(".rcp") > 0 && !list.Contains(lstDir[i]))
					{
						list.Add(lstDir[i]);
					}
				}
			}
			return list;
		}

		private List<string> GetNextForderList(List<string> lstDir, string curForder)
		{
			List<string> list = new List<string>();
			for (int i = 0; i < lstDir.Count; i++)
			{
				if (string.IsNullOrEmpty(curForder))
				{
					string text = lstDir[i];
					if (text.IndexOf("\\") > 0)
					{
						string item = text.Substring(0, text.IndexOf("\\"));
						if (!list.Contains(item))
						{
							list.Add(item);
						}
					}
					else if (text.IndexOf(".rcp") < 0 && !list.Contains(text))
					{
						list.Add(text);
					}
				}
				else
				{
					if (lstDir[i].IndexOf(curForder + "\\") != 0)
					{
						continue;
					}
					string text2 = lstDir[i].Substring(curForder.Length + 1);
					if (text2.IndexOf("\\") > 0)
					{
						string text3 = text2.Substring(0, text2.IndexOf("\\"));
						if (!list.Contains(curForder + "\\" + text3))
						{
							list.Add(curForder + "\\" + text3);
						}
					}
					else if (text2.IndexOf(".rcp") < 0 && !list.Contains(curForder + "\\" + text2))
					{
						list.Add(curForder + "\\" + text2);
					}
				}
			}
			return list;
		}

		private XmlElement GenerateRecipeList(string chamberId, DirectoryInfo currentDir, XmlDocument doc, bool includingUsedRecipe)
		{
			if (_recipeSaveInDB)
			{
				DataTable rootAllDirectoryAndFiles = RecipeInfoRecorer.GetRootAllDirectoryAndFiles(chamberId);
				List<string> list = new List<string>();
				if (rootAllDirectoryAndFiles != null && rootAllDirectoryAndFiles.Rows.Count > 0)
				{
					for (int i = 0; i < rootAllDirectoryAndFiles.Rows.Count; i++)
					{
						list.Add(rootAllDirectoryAndFiles.Rows[i][0].ToString());
					}
				}
				return GenerateRecipeList("", list, doc);
			}
			int length = getRecipeDirPath(chamberId).Length;
			XmlElement xmlElement = doc.CreateElement("Folder");
			xmlElement.SetAttribute("Name", currentDir.FullName.Substring(length));
			DirectoryInfo[] directories = currentDir.GetDirectories();
			DirectoryInfo[] array = directories;
			foreach (DirectoryInfo directoryInfo in array)
			{
				if (includingUsedRecipe || !(directoryInfo.Name == "HistoryRecipe"))
				{
					xmlElement.AppendChild(GenerateRecipeList(chamberId, directoryInfo, doc, includingUsedRecipe));
				}
			}
			FileInfo[] files = currentDir.GetFiles("*.rcp");
			FileInfo[] array2 = files;
			foreach (FileInfo fileInfo in array2)
			{
				XmlElement xmlElement2 = doc.CreateElement("File");
				string text = fileInfo.FullName.Substring(length).TrimStart('\\');
				text = text.Substring(0, text.LastIndexOf("."));
				xmlElement2.SetAttribute("Name", text);
				xmlElement.AppendChild(xmlElement2);
			}
			return xmlElement;
		}

		public bool DeleteRecipe(string chamberId, string recipeName)
		{
			try
			{
				if (_recipeSaveInDB)
				{
					RecipeInfoRecorer.DeleteRecipeInfoByFileName(chamberId, recipeName);
				}
				else
				{
					File.Delete(GenerateRecipeFilePath(chamberId, recipeName));
					InfoDialog(string.Format(Resources.RecipeFileManager_DeleteRecipe_RecipeFile0DeleteSucceeded, recipeName));
				}
			}
			catch (Exception ex)
			{
				LOG.Write(ex, "删除recipe file 出错");
				WarningDialog(string.Format(Resources.RecipeFileManager_DeleteRecipe_RecipeFile0DeleteFailed, recipeName));
				return false;
			}
			return true;
		}

		public bool RenameRecipe(string chamId, string oldName, string newName)
		{
			try
			{
				if (_recipeSaveInDB)
				{
					RecipeInfoRecorer.RenameRecipeFileName(chamId, oldName, newName);
				}
				else
				{
					if (File.Exists(GenerateRecipeFilePath(chamId, newName)))
					{
						WarningDialog(string.Format(Resources.RecipeFileManager_RenameRecipe_RecipeFile0FileExisted, oldName));
						return false;
					}
					File.Move(GenerateRecipeFilePath(chamId, oldName), GenerateRecipeFilePath(chamId, newName));
					InfoDialog(string.Format(Resources.RecipeFileManager_RenameRecipe_RecipeFile0Renamed, oldName, newName));
				}
			}
			catch (Exception ex)
			{
				LOG.Write(ex, "重命名recipe file 出错");
				WarningDialog(string.Format(Resources.RecipeFileManager_RenameRecipe_RecipeFile0RenameFailed, oldName, newName));
				return false;
			}
			return true;
		}

		private void InfoDialog(string message)
		{
			_rcpContext.PostInfoDialogMessage(message);
		}

		private void WarningDialog(string message)
		{
			_rcpContext.PostWarningDialogMessage(message);
		}

		private void EventDialog(string message, List<string> reason)
		{
			string text = message;
			foreach (string item in reason)
			{
				text = text + "\r\n" + item;
			}
			_rcpContext.PostDialogEvent(text);
		}

		private string GenerateRecipeFilePath(string chamId, string recipeName)
		{
			return getRecipeDirPath(chamId) + recipeName + ".rcp";
		}

		private string GenerateSequenceFilePath(string chamId, string recipeName)
		{
			return getRecipeDirPath(chamId) + recipeName + ".seq";
		}

		private string getRecipeDirPath(string chamId)
		{
			string text = $"{PathManager.GetRecipeDir()}{chamId}\\";
			DirectoryInfo directoryInfo = new DirectoryInfo(text);
			if (!directoryInfo.Exists)
			{
				directoryInfo.Create();
			}
			return text;
		}

		public bool DeleteFolder(string chamId, string folderName)
		{
			try
			{
				if (_recipeSaveInDB)
				{
					RecipeInfoRecorer.DeleteFolder(chamId, folderName);
				}
				else
				{
					Directory.Delete(getRecipeDirPath(chamId) + folderName, recursive: true);
					InfoDialog(string.Format(Resources.RecipeFileManager_DeleteFolder_RecipeFolder0DeleteSucceeded, folderName));
				}
			}
			catch (Exception ex)
			{
				LOG.Write(ex, "删除recipe folder 出错");
				WarningDialog($"recipe folder  {folderName} delete failed");
				return false;
			}
			return true;
		}

		public bool SaveAsRecipe(string chamId, string recipeName, string recipeContent)
		{
			string path = GenerateRecipeFilePath(chamId, recipeName);
			if (File.Exists(path))
			{
				WarningDialog(string.Format(Resources.RecipeFileManager_SaveAsRecipe_RecipeFile0savefailed, recipeName));
				return false;
			}
			return SaveRecipe(chamId, recipeName, recipeContent, clearBarcode: true, notifyUI: true);
		}

		public bool SaveRecipe(string chamId, string recipeName, string recipeContent, bool clearBarcode, bool notifyUI)
		{
			if (_recipeSaveInDB)
			{
				if (!RecipeInfoRecorer.WriteRecipeInfo(chamId, recipeName, recipeContent))
				{
					LOG.Write("保存recipe file 到数据库出错");
					return false;
				}
				return true;
			}
			bool result = true;
			try
			{
				string text = GenerateRecipeFilePath(chamId, recipeName);
				FileInfo fileInfo = new FileInfo(text);
				if (!fileInfo.Directory.Exists)
				{
					fileInfo.Directory.Create();
				}
				XmlDocument xmlDocument = new XmlDocument();
				xmlDocument.LoadXml(recipeContent);
				XmlTextWriter xmlTextWriter = new XmlTextWriter(text, Encoding.UTF8);
				xmlTextWriter.Formatting = Formatting.Indented;
				xmlDocument.Save(xmlTextWriter);
				xmlTextWriter.Close();
				if (notifyUI)
				{
					InfoDialog(string.Format(Resources.RecipeFileManager_SaveRecipe_RecipeFile0SaveCompleted, recipeName));
				}
				else
				{
					EV.PostMessage("System", EventEnum.GeneralInfo, string.Format(Resources.RecipeFileManager_SaveRecipe_RecipeFile0SaveCompleted, recipeName));
				}
			}
			catch (Exception ex)
			{
				LOG.Write(ex, "保存recipe file 出错");
				if (notifyUI)
				{
					WarningDialog(string.Format(Resources.RecipeFileManager_SaveRecipe_RecipeFile0SaveFailed, recipeName));
				}
				result = false;
			}
			return result;
		}

		public bool CreateFolder(string chamId, string folderName)
		{
			try
			{
				if (_recipeSaveInDB)
				{
					if (!RecipeInfoRecorer.CreateFolder(chamId, folderName))
					{
						LOG.Write("保存recipe file 到数据库出错");
						return false;
					}
				}
				else
				{
					Directory.CreateDirectory(getRecipeDirPath(chamId) + folderName);
					InfoDialog(string.Format(Resources.RecipeFileManager_CreateFolder_RecipeFolder0Created, folderName));
				}
			}
			catch (Exception ex)
			{
				LOG.Write(ex, "create recipe folder failed");
				WarningDialog(string.Format(Resources.RecipeFileManager_CreateFolder_RecipeFolder0CreateFailed, folderName));
				return false;
			}
			return true;
		}

		public bool RenameFolder(string chamId, string oldName, string newName)
		{
			try
			{
				string sourceDirName = getRecipeDirPath(chamId) + oldName;
				string destDirName = getRecipeDirPath(chamId) + newName;
				Directory.Move(sourceDirName, destDirName);
				InfoDialog(string.Format(Resources.RecipeFileManager_RenameFolder_RecipeFolder0renamed, oldName, newName));
			}
			catch (Exception ex)
			{
				LOG.Write(ex, "Rename recipe folder failed");
				WarningDialog(string.Format(Resources.RecipeFileManager_RenameFolder_RecipeFolder0RenameFailed, oldName, newName));
				return false;
			}
			return true;
		}

		private string GetRecipeBody(string chamberId, string nodePath)
		{
			if (_rcpContext == null)
			{
				return string.Empty;
			}
			string recipeDefiniton = _rcpContext.GetRecipeDefiniton(chamberId);
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.LoadXml(recipeDefiniton);
			XmlNode xmlNode = xmlDocument.SelectSingleNode(nodePath);
			return xmlNode.OuterXml;
		}

		public string GetRecipeFormatXml(string chamberId)
		{
			return GetRecipeBody(chamberId, "/Aitex/TableRecipeFormat");
		}

		public string GetRecipeTemplate(string chamberId)
		{
			if (_rcpContext != null)
			{
				return _rcpContext.GetRecipeTemplate(chamberId);
			}
			return GetRecipeBody(chamberId, "/Aitex/TableRecipeData");
		}

		public string GetRecipeSchema(string chamberId)
		{
			if (_rcpContext == null)
			{
				return string.Empty;
			}
			string recipeDefiniton = _rcpContext.GetRecipeDefiniton(chamberId);
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.LoadXml(recipeDefiniton);
			XmlNode xmlNode = xmlDocument.SelectSingleNode("/Aitex/TableRecipeSchema");
			return xmlNode.InnerXml;
		}

		public string GetRecipeByBarcode(string chamberId, string barcode)
		{
			try
			{
				string text = PathManager.GetRecipeDir() + chamberId + "\\";
				DirectoryInfo directoryInfo = new DirectoryInfo(text);
				FileInfo[] files = directoryInfo.GetFiles("*.rcp", SearchOption.AllDirectories);
				XmlDocument xmlDocument = new XmlDocument();
				FileInfo[] array = files;
				foreach (FileInfo fileInfo in array)
				{
					string text2 = fileInfo.FullName.Substring(text.Length);
					if (!text2.Contains("HistoryRecipe\\"))
					{
						xmlDocument.Load(fileInfo.FullName);
						if (xmlDocument.SelectSingleNode($"/TableRecipeData[@Barcode='{barcode}']") != null)
						{
							return text2.Substring(0, text2.LastIndexOf('.'));
						}
					}
				}
				return string.Empty;
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
				return string.Empty;
			}
		}

		private string GetSequenceConfig(string nodePath)
		{
			if (_seqContext == null)
			{
				return string.Empty;
			}
			string configXml = _seqContext.GetConfigXml();
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.LoadXml(configXml);
			XmlNode xmlNode = xmlDocument.SelectSingleNode(nodePath);
			return xmlNode.OuterXml;
		}

		public string GetSequence(string sequenceName, bool needValidation, bool isLoadAnother = false)
		{
			string text = string.Empty;
			try
			{
				bool flag = _recipeSaveInDB;
				if (isLoadAnother)
				{
					flag = !_recipeSaveInDB;
				}
				if (flag)
				{
					return SequenceInfoRecorder.GetSequenceInfoByFileName(sequenceName);
				}
				using (StreamReader streamReader = new StreamReader(GenerateSequenceFilePath("Sequence", sequenceName)))
				{
					text = streamReader.ReadToEnd();
					streamReader.Close();
				}
				if (needValidation && !_seqContext.Validation(text))
				{
					EV.PostWarningLog("Recipe", "Read " + sequenceName + " failed, validation failed");
					text = string.Empty;
				}
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
				EV.PostWarningLog("Recipe", "Read " + sequenceName + " failed, " + ex.Message);
				text = string.Empty;
			}
			return text;
		}

		public List<string> GetSequenceNameList()
		{
			List<string> list = new List<string>();
			try
			{
				if (_recipeSaveInDB)
				{
					DataTable allData = SequenceInfoRecorder.GetAllData();
					if (allData != null && allData.Rows.Count > 0)
					{
						for (int i = 0; i < allData.Rows.Count; i++)
						{
							string text = allData.Rows[i]["seqName"].ToString();
							int num = text.IndexOf(".seq");
							if (num > 0)
							{
								text = text.Substring(0, num);
								if (!list.Contains(text))
								{
									list.Add(text);
								}
							}
						}
					}
				}
				else
				{
					string text2 = PathManager.GetRecipeDir() + "Sequence\\";
					DirectoryInfo directoryInfo = new DirectoryInfo(text2);
					FileInfo[] files = directoryInfo.GetFiles("*.seq", SearchOption.AllDirectories);
					FileInfo[] array = files;
					foreach (FileInfo fileInfo in array)
					{
						string text3 = fileInfo.FullName.Substring(text2.Length);
						text3 = text3.Substring(0, text3.LastIndexOf('.'));
						list.Add(text3);
					}
				}
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
				EV.PostWarningLog("Recipe", "Get sequence list failed, " + ex.Message);
			}
			return list;
		}

		public bool DeleteSequence(string sequenceName)
		{
			try
			{
				if (_recipeSaveInDB)
				{
					SequenceInfoRecorder.DeleteByFileName(sequenceName);
				}
				else
				{
					File.Delete(GenerateSequenceFilePath("Sequence", sequenceName));
					EV.PostInfoLog("Recipe", "sequence " + sequenceName + " deleted");
				}
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
				EV.PostWarningLog("Recipe", "delete " + sequenceName + " failed, " + ex.Message);
				return false;
			}
			return true;
		}

		public bool SaveSequence(string sequenceName, string sequenceContent, bool notifyUI)
		{
			bool result = true;
			try
			{
				if (_recipeSaveInDB)
				{
					result = SequenceInfoRecorder.WriteSequenceInfo(sequenceName, sequenceContent);
				}
				else
				{
					string text = GenerateSequenceFilePath("Sequence", sequenceName);
					FileInfo fileInfo = new FileInfo(text);
					if (!fileInfo.Directory.Exists)
					{
						fileInfo.Directory.Create();
					}
					XmlDocument xmlDocument = new XmlDocument();
					xmlDocument.LoadXml(sequenceContent);
					XmlTextWriter xmlTextWriter = new XmlTextWriter(text, null);
					xmlTextWriter.Formatting = Formatting.Indented;
					xmlDocument.Save(xmlTextWriter);
					xmlTextWriter.Close();
				}
				if (notifyUI)
				{
					EV.PostPopDialogMessage(EventLevel.Information, "Save Complete", "Sequence " + sequenceName + " saved ");
				}
				else
				{
					EV.PostInfoLog("Recipe", "Sequence " + sequenceName + " saved ");
				}
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
				EV.PostWarningLog("Recipe", "save sequence " + sequenceName + " failed, " + ex.Message);
				if (notifyUI)
				{
					EV.PostPopDialogMessage(EventLevel.Alarm, "Save Error", "save sequence " + sequenceName + " failed, " + ex.Message);
				}
				result = false;
			}
			return result;
		}

		public bool SaveAsSequence(string sequenceName, string sequenceContent)
		{
			string path = GenerateSequenceFilePath("Sequence", sequenceName);
			if (File.Exists(path))
			{
				EV.PostWarningLog("Recipe", "save sequence " + sequenceName + " failed, already exist");
				return false;
			}
			return SaveSequence(sequenceName, sequenceContent, notifyUI: false);
		}

		public bool RenameSequence(string oldName, string newName)
		{
			try
			{
				if (_recipeSaveInDB)
				{
					SequenceInfoRecorder.RenameFileName(oldName, newName);
					return true;
				}
				if (File.Exists(GenerateSequenceFilePath("Sequence", newName)))
				{
					EV.PostWarningLog("Recipe", newName + " already exist, rename failed");
					return false;
				}
				File.Move(GenerateSequenceFilePath("Sequence", oldName), GenerateSequenceFilePath("Sequence", newName));
				EV.PostInfoLog("Recipe", "sequence " + oldName + " renamed to " + newName);
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
				EV.PostWarningLog("Recipe", "rename " + oldName + " failed, " + ex.Message);
				return false;
			}
			return true;
		}

		public string GetSequenceFormatXml()
		{
			return GetSequenceConfig("/Aitex/TableSequenceFormat");
		}

		internal bool DeleteSequenceFolder(string folderName)
		{
			try
			{
				Directory.Delete(PathManager.GetRecipeDir() + "Sequence\\" + folderName, recursive: true);
				EV.PostInfoLog("Recipe", "Folder " + folderName + "deleted");
			}
			catch (Exception ex)
			{
				LOG.Write(ex, "delete sequence folder exception");
				EV.PostWarningLog("Recipe", "can not delete folder " + folderName + ", " + ex.Message);
				return false;
			}
			return true;
		}

		internal bool CreateSequenceFolder(string folderName)
		{
			try
			{
				Directory.CreateDirectory(PathManager.GetRecipeDir() + "Sequence\\" + folderName);
				EV.PostInfoLog("Recipe", "Folder " + folderName + "created");
			}
			catch (Exception ex)
			{
				LOG.Write(ex, "sequence folder create exception");
				EV.PostWarningLog("Recipe", "can not create folder " + folderName + ", " + ex.Message);
				return false;
			}
			return true;
		}

		internal bool RenameSequenceFolder(string oldName, string newName)
		{
			try
			{
				string sourceDirName = PathManager.GetRecipeDir() + "Sequence\\" + oldName;
				string destDirName = PathManager.GetRecipeDir() + "Sequence\\" + newName;
				Directory.Move(sourceDirName, destDirName);
				EV.PostInfoLog("Recipe", "rename folder  from " + oldName + " to " + newName);
			}
			catch (Exception ex)
			{
				LOG.Write(ex, "rename sequence folder failed");
				EV.PostWarningLog("Recipe", "can not rename folder " + oldName + ", " + ex.Message);
				return false;
			}
			return true;
		}

		public string GetXmlSequenceList(string chamberId)
		{
			XmlDocument xmlDocument = new XmlDocument();
			DirectoryInfo currentDir = new DirectoryInfo(PathManager.GetRecipeDir() + "Sequence\\");
			xmlDocument.AppendChild(GenerateSequenceList(chamberId, currentDir, xmlDocument));
			return xmlDocument.OuterXml;
		}

		private XmlElement GenerateSequenceList(string chamberId, DirectoryInfo currentDir, XmlDocument doc)
		{
			int length = (PathManager.GetRecipeDir() + "Sequence\\").Length;
			XmlElement xmlElement = doc.CreateElement("Folder");
			xmlElement.SetAttribute("Name", currentDir.FullName.Substring(length));
			DirectoryInfo[] directories = currentDir.GetDirectories();
			DirectoryInfo[] array = directories;
			foreach (DirectoryInfo currentDir2 in array)
			{
				xmlElement.AppendChild(GenerateSequenceList(chamberId, currentDir2, doc));
			}
			FileInfo[] files = currentDir.GetFiles("*.seq");
			FileInfo[] array2 = files;
			foreach (FileInfo fileInfo in array2)
			{
				XmlElement xmlElement2 = doc.CreateElement("File");
				string text = fileInfo.FullName.Substring(length).TrimStart('\\');
				text = text.Substring(0, text.LastIndexOf("."));
				xmlElement2.SetAttribute("Name", text);
				xmlElement.AppendChild(xmlElement2);
			}
			return xmlElement;
		}
	}
}
