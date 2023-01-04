using System;
using System.Collections.Generic;
using MECF.Framework.Common.OperationCenter;
using MECF.Framework.UI.Client.ClientBase;
using OpenSEMI.ClientBase;
using OpenSEMI.ClientBase.Command;

using System.Globalization;

namespace MECF.Framework.UI.Client.CenterViews.Configs.SystemConfig
{
    public class SystemConfigViewModel : ModuleUiViewModelBase, ISupportMultipleSystem
    {
        #region Properties
        public bool IsPermission { get => this.Permission == 3; }

        private List<ConfigNode> _ConfigNodes = new List<ConfigNode>();
        public List<ConfigNode> ConfigNodes
        {
            get { return _ConfigNodes; }
            set { _ConfigNodes = value; NotifyOfPropertyChange("ConfigNodes"); }
        }

        private List<ConfigItem> _configItems = null;
        public List<ConfigItem> ConfigItems
        {
            get { return _configItems; }
            set
            {
                _configItems = value;
                NotifyOfPropertyChange("ConfigItems");
            }
        }

        string _CurrentNodeName = string.Empty;

        public BaseCommand<ConfigNode> TreeViewSelectedItemChangedCmd { private set; get; }


        private string _currentCriteria = String.Empty;
        public string CurrentCriteria
        {
            get { return _currentCriteria; }
            set
            {
                if (value == _currentCriteria)
                    return;

                _currentCriteria = value;
                NotifyOfPropertyChange("CurrentCriteria");
                ApplyFilter();
            }
        }

        private void ApplyFilter()
        {
            foreach (var node in ConfigNodes)
                node.ApplyCriteria(CurrentCriteria, new Stack<ConfigNode>());
        }
        #endregion

        #region Functions
        public SystemConfigViewModel()
        {
            this.DisplayName = "System Config";
            TreeViewSelectedItemChangedCmd = new BaseCommand<ConfigNode>(TreeViewSelectedItemChanged);
        }

        protected override void OnInitialize()
        {
            base.OnInitialize();

            ConfigNodes = SystemConfigProvider.Instance.GetConfigTree(SystemName).SubNodes;
        }

        private void TreeViewSelectedItemChanged(ConfigNode node)
        {
            _CurrentNodeName = string.IsNullOrEmpty(node.Path) ? node.Name : $"{node.Path}.{node.Name}";
            ConfigItems = node.Items;

            GetDataOfConfigItems();
        }

        private void GetDataOfConfigItems()
        {
            if (ConfigItems == null)
                return;

            for (int i = 0; i < ConfigItems.Count; i++)
            {
                string key = String.Format("{0}{1}{2}", _CurrentNodeName, ".", ConfigItems[i].Name);
                ConfigItems[i].CurrentValue = SystemConfigProvider.Instance.GetValueByName(SystemName, key);

                if (ConfigItems[i].Type == DataType.Bool)
                {
                    bool value;
                    if (bool.TryParse(ConfigItems[i].CurrentValue, out value))
                    {
                        ConfigItems[i].BoolValue = value;

                        ConfigItems[i].CurrentValue = value ? "Yes" : "No";
                    }
                }
                else
                {
                    try
                    {
                        if (ConfigItems[i].Type == DataType.Int)
                        {
                            ConfigItems[i].StringValue = Convert.ToDouble(ConfigItems[i].CurrentValue).ToString("N0");
                            //ConfigItems[i].StringValue = ConfigItems[i].CurrentValue;

                        }
                        else if (ConfigItems[i].Type == DataType.Double)
                        {
                            ConfigItems[i].StringValue = Convert.ToDouble(ConfigItems[i].CurrentValue).ToString("N2");
                        }
                        else
                        {
                            ConfigItems[i].StringValue = ConfigItems[i].CurrentValue;
                        }
                    }
                    catch
                    {
                        //这部分应该不会运行到
                        ConfigItems[i].StringValue = ConfigItems[i].CurrentValue;
                    }
                }
            }
        }

        /// <summary>
        /// 将千分位字符串转换成数字
        /// 说明：将诸如"–111,222,333的千分位"转换成-111222333数字
        /// 若转换失败则返回-1
        /// </summary>
        /// <param name="thousandthStr">需要转换的千分位</param>
        /// <returns>数字</returns>
        public int ParseThousandthStringInt(string thousandthStr)
        {

            if (!string.IsNullOrEmpty(thousandthStr))
            {

                try
                {
                    return int.Parse(thousandthStr, NumberStyles.AllowThousands | NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign);
                }
                catch (Exception ex)
                {
                    return -10000;
                    //Debug.WriteLine(string.Format("将千分位字符串{0}转换成数字异常，原因:{0}", thousandthStr, ex.Message));
                }
            }
            else
            {
                return -10000;
            }
        }
        public Double ParseThousandthStringDouble(string thousandthStr)
        {

            if (!string.IsNullOrEmpty(thousandthStr))
            {

                try
                {
                    return Double.Parse(thousandthStr, NumberStyles.AllowThousands | NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign);
                }
                catch (Exception ex)
                {
                    return -10000;
                    //Debug.WriteLine(string.Format("将千分位字符串{0}转换成数字异常，原因:{0}", thousandthStr, ex.Message));
                }
            }
            else
            {
                return -10000.0;
            }
        }

        public void SetValues(List<ConfigItem> items)
        {
            //key ：System.IsSimulatorMode 
            //value: true or false 都是字符串

            //input check
            foreach (var item in items)
            {

                string value;
                if (item.Type == DataType.Bool)
                {
                    value = item.BoolValue.ToString().ToLower();
                }
                else
                {
                    if (item.TextSaved && item.Tag != "ReadOnlySelection")
                        continue;

                    if (item.Type == DataType.Int)
                    {
                        int iValue;
                        int _iValue = ParseThousandthStringInt(item.StringValue);
                        if (_iValue <= -100000)
                        {
                            DialogBox.ShowWarning("The value should be Type Int .");
                            continue;
                        }
                        string sValue = _iValue.ToString();
                        if (int.TryParse(sValue, out iValue))
                        {
                            if (!double.IsNaN(item.Max) && !double.IsNaN(item.Min))
                            {
                                if (iValue > item.Max || iValue < item.Min)
                                {
                                    DialogBox.ShowWarning(string.Format("The value should be between {0}  and {1}.", ((int)item.Min).ToString(), ((int)item.Max).ToString()));
                                    continue;
                                }
                            }
                        }
                        else
                        {
                            DialogBox.ShowWarning("Please input valid data.");
                            continue;
                        }
                        //value = item.StringValue;
                        value = sValue;
                    }
                    else if (item.Type == DataType.Double)
                    {
                        double fValue;
                        double _iValue = ParseThousandthStringDouble(item.StringValue);
                        if (_iValue <= -100000)
                        {
                            DialogBox.ShowWarning("The value should be Type Double .");
                            continue;
                        }
                        string sValue = _iValue.ToString();
                        if (double.TryParse(sValue, out fValue))
                        {
                            if (!double.IsNaN(item.Max) && !double.IsNaN(item.Min))
                            {
                                if (fValue > item.Max || fValue < item.Min)
                                {
                                    DialogBox.ShowWarning(string.Format("The value should be between {0}  and {1}.", item.Min.ToString(), item.Max.ToString()));
                                    continue;
                                }

                                string[] box = fValue.ToString().Split('.');
                                if (box.Length > 1 && box[1].Length > 3)
                                {
                                    DialogBox.ShowWarning(string.Format("The value should be more than three decimal places"));
                                    continue;
                                }
                            }
                        }
                        else
                        {
                            DialogBox.ShowWarning("Please input valid data.");
                            continue;
                        }
                        //value = item.StringValue;
                        value = sValue;
                    }
                    else
                        value = item.StringValue;
                }

                string key = String.Format("{0}{1}{2}", _CurrentNodeName, ".", item.Name);
                InvokeClient.Instance.Service.DoOperation($"{SystemName}.SetConfig", key, value);

                item.TextSaved = true;

                ExTrigger(key, value);
            }

            Reload();
            //扩展Set触发其他事件
        }
        public void SetValue(ConfigItem item)
        {
            //key ：System.IsSimulatorMode 
            //value: true or false 都是字符串

            //input check
            string value;
            if (item.Type == DataType.Bool)
            {
                value = item.BoolValue.ToString().ToLower();
            }
            else
            {
                if (item.TextSaved && item.Tag != "ReadOnlySelection")
                    return;

                if (item.Type == DataType.Int)
                {
                    int iValue;
                    int _iValue = ParseThousandthStringInt(item.StringValue);
                    if (_iValue <= -100000)
                    {
                        DialogBox.ShowWarning("The value should be Type Int .");
                        return;
                    }
                    string sValue = _iValue.ToString();
                    if (int.TryParse(sValue, out iValue))
                    {
                        if (!double.IsNaN(item.Max) && !double.IsNaN(item.Min))
                        {
                            if (iValue > item.Max || iValue < item.Min)
                            {
                                DialogBox.ShowWarning(string.Format("The value should be between {0}  and {1}.", ((int)item.Min).ToString(), ((int)item.Max).ToString()));
                                return;
                            }
                        }
                    }
                    else
                    {
                        DialogBox.ShowWarning("Please input valid data.");
                        return;
                    }
                    //value = item.StringValue;
                    value = sValue;
                }
                else if (item.Type == DataType.Double)
                {
                    double fValue;
                    double _iValue = ParseThousandthStringDouble(item.StringValue);
                    if (_iValue <= -100000)
                    {
                        DialogBox.ShowWarning("The value should be Type Double .");
                        return;
                    }
                    string sValue = _iValue.ToString();
                    if (double.TryParse(sValue, out fValue))
                    {
                        if (!double.IsNaN(item.Max) && !double.IsNaN(item.Min))
                        {
                            if (fValue > item.Max || fValue < item.Min)
                            {
                                DialogBox.ShowWarning(string.Format("The value should be between {0}  and {1}.", item.Min.ToString(), item.Max.ToString()));
                                return;
                            }

                            string[] box = fValue.ToString().Split('.');
                            if (box.Length > 1 && box[1].Length > 3)
                            {
                                DialogBox.ShowWarning(string.Format("The value should be more than three decimal places"));
                                return;
                            }
                        }
                    }
                    else
                    {
                        DialogBox.ShowWarning("Please input valid data.");
                        return;
                    }
                    //value = item.StringValue;
                    value = sValue;
                }
                else
                    value = item.StringValue;
            }

            string key = String.Format("{0}{1}{2}", _CurrentNodeName, ".", item.Name);
            InvokeClient.Instance.Service.DoOperation($"{SystemName}.SetConfig", key, value);

            item.TextSaved = true;

            Reload();
            //扩展Set触发其他事件
            ExTrigger(key, value);
        }
        #region 扩展Set触发其他事件
        private void ExTrigger(string key, string value)
        {
            if (key.Contains("_Offset"))
            {
                string[] strArray = key.Split('.');

                InvokeClient.Instance.Service.DoOperation($"{strArray[1]}.{strArray[3]}.SetPTOffset", value);
            }
            else if (key.Contains("_K"))
            {
                string[] strArray = key.Split('.');

                InvokeClient.Instance.Service.DoOperation($"{strArray[1]}.{strArray[3]}.SetPTK", value);
            }
            else if (key == "System.RecipePasswordReset")
            {
                RecipePasswordReset(key, value);
            }
        }
        
        private void RecipePasswordReset(string sKey, string sValue)
        {
            if (sValue.Trim() == "0")
            {
                InvokeClient.Instance.Service.DoOperation("PM1.Recipe.EditorChangePassword", sValue);
            }
        }
        
        
        
        #endregion

        public void Reload()
        {
            GetDataOfConfigItems();
        }

        public void SaveAll()
        {
            if (ConfigItems == null)
                return; 
            SetValues(ConfigItems);
            //ConfigItems.ForEach(item => SetValue(item));
        }

        public void CollapseAll()
        {
            SetExpand(ConfigNodes, false);
        }

        public void ExpandAll()
        {
            SetExpand(ConfigNodes, true);
        }

        public void ClearFilter()
        {
            CurrentCriteria = "";
        }

        public void SetExpand(List<ConfigNode> configs, bool expand)
        {
            if (configs == null)
                return;

            foreach (var configNode in configs)
            {
                configNode.IsExpanded = expand;

                if (configNode.SubNodes != null)
                    SetExpand(configNode.SubNodes, expand);
            }

        }

        #endregion
    }
}
