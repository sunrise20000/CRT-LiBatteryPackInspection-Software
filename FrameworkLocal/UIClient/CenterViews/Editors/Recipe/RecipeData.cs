using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Xml;
using Aitex.Core.RT.Log;
using Caliburn.Micro.Core;
using MECF.Framework.UI.Client.CenterViews.Configs.Roles;
using MECF.Framework.UI.Client.ClientBase;
using MECF.Framework.UI.Client.RecipeEditorLib.RecipeModel;
using MECF.Framework.UI.Client.RecipeEditorLib.RecipeModel.Params;
using RecipeEditorLib.DGExtension.CustomColumn;
using RecipeEditorLib.RecipeModel.Params;
using Sicentury.Core;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace MECF.Framework.UI.Client.CenterViews.Editors.Recipe
{
    public class RecipeData : PropertyChangedBase
    {
        #region Variables

        private XmlDocument _doc;
        private bool _isSavedDesc;
        private string _name;
        private string _chamberType;
        private string _recipeVersion;
        private string _prefixPath;
        private string _creator;
        private DateTime _createTime;
        private string _description;
        private string _devisor;
        private DateTime _deviseTime;
        private int _recipeTotalTime;

        #endregion  

        #region Constructors

        public RecipeData()
        {
            Steps = new RecipeStepCollection(this);
            StepTolerances = new RecipeStepCollection(this);
            PopSettingSteps = new Dictionary<string, RecipeStepCollection>();
            PopEnable = new Dictionary<string, bool>();
            ConfigItems = new RecipeStep(null);
            IsSavedDesc = true;

            _doc = new XmlDocument();
            var node = _doc.CreateElement("Aitex");
            _doc.AppendChild(node);
            node.AppendChild(_doc.CreateElement("TableRecipeData"));
        }

        #endregion

        #region Properties

        public bool IsChanged
        {
            get
            {
                var changed = !IsSavedDesc;

                if (!changed)
                {
                    changed = ChkChanged(Steps) || ChkChanged(PopSettingSteps);
                }

                if (!changed)
                {
                    foreach (var config in ConfigItems)
                    {
                        if (!config.IsSaved)
                        {
                            changed = true;
                            break;
                        }
                    }
                }
                return changed;
            }
        }

        public bool IsSavedDesc
        {
            get => _isSavedDesc;
            set
            {
                _isSavedDesc = value;
                NotifyOfPropertyChange();
            }
        }

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                NotifyOfPropertyChange();
            }
        }

        public string FullName => $"{PrefixPath}\\{Name}";

        public string RecipeChamberType
        {
            get => _chamberType;
            set
            {
                _chamberType = value;
                NotifyOfPropertyChange();
            }
        }

        public string RecipeVersion
        {
            get => _recipeVersion;
            set
            {
                _recipeVersion = value;
                NotifyOfPropertyChange();
            }
        }

        public string PrefixPath
        {
            get => _prefixPath;
            set
            {
                _prefixPath = value;
                NotifyOfPropertyChange();
            }
        }

        public string Creator
        {
            get => _creator;
            set
            {
                _creator = value;
                NotifyOfPropertyChange();
            }
        }
        public DateTime CreateTime
        {
            get => _createTime;
            set
            {
                _createTime = value;
                NotifyOfPropertyChange();
            }
        }

        public string Description
        {
            get => _description;
            set
            {
                _description = value;
                NotifyOfPropertyChange();
            }
        }

        public string Revisor
        {
            get => _devisor;
            set
            {
                _devisor = value;
                NotifyOfPropertyChange();
            }
        }

        public DateTime ReviseTime
        {
            get => _deviseTime;
            set
            {
                _deviseTime = value;
                NotifyOfPropertyChange();
            }
        }

        public int RecipeTotalTime
        {
            get => _recipeTotalTime;
            set
            {
                _recipeTotalTime = value;
                NotifyOfPropertyChange();
            }
        }

        public RecipeStepCollection Steps { get; private set; }

        public Dictionary<string, RecipeStepCollection> PopSettingSteps { get; private set; }

        public RecipeStepCollection StepTolerances { get; private set; }

        public RecipeStep ConfigItems { get; private set; }


        /// <summary>
        /// 返回当前Recipe所属的反应腔。
        /// </summary>
        public string Module { get; private set; }

        public bool ToleranceEnable { get; set; }

        public Dictionary<string, bool> PopEnable { get; set; }

        public bool IsCompatibleWithCurrentFormat { get; set; }

        #endregion

        #region Methods


        public bool ChkChanged(RecipeStepCollection steps)
        {
            foreach (var parameters in steps)
            {
                if (parameters.FirstOrDefault(param => param.IsSaved == false) != null)
                {
                    return true;
                }
            }

            return false;
        }

        public bool ChkChanged(Dictionary<string, RecipeStepCollection> popSteps)
        {
            foreach (var parameters in popSteps.Values)
            {
                if (ChkChanged(parameters))
                {
                    return true;
                }
            }
            return false;
        }

        public void Clear()
        {
            Steps.Clear();
            PopSettingSteps.Clear();
            StepTolerances.Clear();
            ConfigItems.Clear();
            RecipeChamberType = "";
            RecipeVersion = "";
            IsSavedDesc = true;
            Module = "";
        }

        public void DataSaved()
        {
            Steps.Save();
            StepTolerances.Save();
            PopSettingSteps.Values.ToList().ForEach(x => x.Save());
            ConfigItems.Save();

            IsSavedDesc = true;
        }

        public async Task InitData(string prefixPath, string recipeName, string recipeContent,
            List<EditorDataGridTemplateColumnBase> columnDefine, 
            RecipeStep configDefine, string module, Dispatcher dispatcher = null, 
            bool isHideValueWhenLoading = false)
        {
            IsCompatibleWithCurrentFormat = false;

            Name = recipeName;
            PrefixPath = prefixPath;
            Module = module;
            try
            {
                _doc = new XmlDocument();
                _doc.LoadXml(recipeContent);

                if (!LoadHeader(_doc.SelectSingleNode("Aitex/TableRecipeData")))
                    return;

                var nodeSteps = _doc.SelectNodes($"Aitex/TableRecipeData/Module[@Name='{module}']/Step");
                if (nodeSteps == null)
                    nodeSteps = _doc.SelectNodes($"Aitex/TableRecipeData/Step");

                var processType = "";

                if(PrefixPath.Contains("Process"))
                {
                    processType = "Process";
                }

                await LoadSteps(columnDefine, nodeSteps, processType, dispatcher, isHideValueWhenLoading);
                ValidLoopData();

                var nodeConfig =
                    _doc.SelectSingleNode($"Aitex/TableRecipeData/Module[@Name='{module}']/Config");
                if (nodeSteps == null)
                    nodeConfig = _doc.SelectSingleNode($"Aitex/TableRecipeData/Config");

                LoadConfigs(configDefine, nodeConfig);

                IsCompatibleWithCurrentFormat = true;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
        }

        public async Task ChangeChamber(List<EditorDataGridTemplateColumnBase> columnDefine,
            RecipeStep configDefine, string module, Dispatcher dispatcher)
        {
            Module = module;

            try
            {
                var nodeSteps = _doc.SelectNodes($"Aitex/TableRecipeData/Module[@Name='{module}']/Step") ??
                                _doc.SelectNodes($"Aitex/TableRecipeData/Step");

                await LoadSteps(columnDefine, nodeSteps, dispatcher: dispatcher);
                ValidLoopData();

                var nodeConfig =
                    _doc.SelectSingleNode($"Aitex/TableRecipeData/Module[@Name='{module}']/Config");
                if (nodeSteps == null)
                    nodeConfig = _doc.SelectSingleNode($"Aitex/TableRecipeData/Config");

                LoadConfigs(configDefine, nodeConfig);
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }
        }

        public void SaveTo(string[] moduleList)
        {
            GetXmlString();

            var nodeModule = _doc.SelectSingleNode($"Aitex/TableRecipeData/Module[@Name='{Module}']");
            if (nodeModule == null)
            {
                LOG.Write("recipe not find modules," + Name);
                return;
            }

            var nodeData = nodeModule.ParentNode;

            foreach (var module in moduleList)
            {
                if (module == Module)
                {
                    continue;
                }

                var child = _doc.SelectSingleNode($"Aitex/TableRecipeData/Module[@Name='{module}']");
                if (child != null)
                    nodeData.RemoveChild(child);

                var node = nodeModule.Clone() as XmlElement;
                node.SetAttribute("Name", module);
                nodeData.AppendChild(node);
            }
        }

        /// <summary>
        /// 创建一个Recipe空步骤。
        /// </summary>
        /// <param name="columns">Recipe列集合</param>
        /// <param name="stepNode"></param>
        /// <param name="previousRecipeStep">前序Recipe步骤</param>
        /// /// <param name="stepPermission">权限</param>
        /// <param name="stepId"></param>
        /// <param name="isHideValue">创建Step后是否直接隐藏参数值。</param>
        /// <returns></returns>
        public RecipeStep CreateStep(List<EditorDataGridTemplateColumnBase> columns,
            XmlNode stepNode = null, RecipeStep previousRecipeStep = null,
            MenuPermissionEnum stepPermission = MenuPermissionEnum.MP_READ_WRITE,
            int? stepId = null)
        {
            var recipeStep = new RecipeStep(previousRecipeStep);

            foreach (var col in columns)
            {
                var value = string.Empty;
                if (!(col is ExpanderColumn) && stepNode != null && !(col is StepColumn) && !(col is PopSettingColumn))
                {
                    if (string.IsNullOrEmpty(col.ModuleName) && stepNode.Attributes[col.ControlName] != null)
                    {
                        value = stepNode.Attributes[col.ControlName].Value;
                    }
                    else
                    {
                        if (stepNode.Attributes[col.ControlName] != null &&
                            stepNode.SelectSingleNode(col.ModuleName) != null &&
                            stepNode.SelectSingleNode(col.ModuleName).Attributes[col.ControlName] != null)
                            value = stepNode.SelectSingleNode(col.ModuleName).Attributes[col.ControlName].Value;
                    }
                }

                Param cell = null;

                switch (col)
                {
                    case StepColumn _:
                        cell = stepId.HasValue ? new StepParam(stepId.Value) : new StepParam();
                        cell.Name = col.ControlName;
                        cell.EnableTolerance = col.EnableTolerance;
                        break;

                    case RatioColumn colRatio:
                        if (colRatio.MaxSets == 3)
                        {
                            cell = new Sets3RatioParam(col.Default)
                            {
                                Name = col.ControlName,
                                IsEnabled = col.IsEnable,
                                EnableTolerance = col.EnableTolerance,
                            };
                            
                            // 如果XML中保存的Value格式正确，则将设置为保存的Value。
                            if(Sets3RatioParam.ValidateRatioString(value))
                            {
                                ((Sets3RatioParam)cell).Value = value;
                            }
                        }

                        break;

                    case TextBoxColumn _:
                        cell = new StringParam(string.IsNullOrEmpty(value)
                            ? (string.IsNullOrEmpty(col.Default) ? "" : col.Default)
                            : value)
                        {
                            Name = col.ControlName,
                            IsEnabled = col.IsEnable,
                            EnableTolerance = col.EnableTolerance,
                        };
                        break;

                    case NumColumn colNum:
                        cell = new IntParam(
                            (int)GlobalDefs.TryParseToDouble(value, GlobalDefs.TryParseToDouble(colNum.Default, colNum.Minimun)),
                            (int)colNum.Minimun, (int)colNum.Maximun)
                        {
                            Name = colNum.ControlName,
                            IsEnabled = colNum.IsEnable,
                            EnableTolerance = colNum.EnableTolerance,
                        };
                        break;

                    case DoubleColumn colDbl:
                        cell = new DoubleParam(
                            GlobalDefs.TryParseToDouble(value, GlobalDefs.TryParseToDouble(colDbl.Default, colDbl.Minimun)),
                            colDbl.Minimun, colDbl.Maximun)
                        {
                            Name = colDbl.ControlName,
                            IsEnabled = colDbl.IsEnable,
                            Resolution = colDbl.Resolution,
                            EnableTolerance = colDbl.EnableTolerance,
                        };
                        break;

                    case FlowModeColumn colFlowMode:
                    {
                        //string displayValue;
                        //if (!string.IsNullOrEmpty(value) &&
                        //    colFlowMode.Options.FirstOrDefault(x => x.ControlName == value) != null)
                        //{
                        //    displayValue = colFlowMode.Options.First(x => x.ControlName == value).DisplayName;
                        //}
                        //else
                        //{
                        //    if (!string.IsNullOrEmpty(colFlowMode.Default) &&
                        //        colFlowMode.Options.Any(x => x.DisplayName == col.Default))
                        //    {
                        //        displayValue = colFlowMode.Default;
                        //    }
                        //    else
                        //    {
                        //        var selIndex = 0;
                        //        displayValue = displayValue = colFlowMode.Options[selIndex].DisplayName;
                        //    }
                        //}

                        cell = new FlowModeParam(string.IsNullOrEmpty(value)
                            ? (string.IsNullOrEmpty(colFlowMode.Default) ? colFlowMode.Options[0].DisplayName : colFlowMode.Default)
                            : value)
                        {
                            Name = colFlowMode.ControlName,
                            Options = colFlowMode.Options,
                            IsEditable = colFlowMode.IsEditable,
                            EnableTolerance = colFlowMode.EnableTolerance,
                        };
                        break;
                    }

                    case ComboxColumn colCbx:
                    {
                        //string displayValue;
                        //if (!string.IsNullOrEmpty(value) &&
                        //    colCbx.Options.FirstOrDefault(x => x.ControlName == value) != null)
                        //{
                        //    displayValue = colCbx.Options.First(x => x.ControlName == value).DisplayName;
                        //}
                        //else
                        //{
                        //    if (!string.IsNullOrEmpty(colCbx.Default) &&
                        //        colCbx.Options.Any(x => x.DisplayName == col.Default))
                        //    {
                        //        displayValue = colCbx.Default;
                        //    }
                        //    else
                        //    {
                        //        var selIndex = 0;
                        //        displayValue = displayValue = colCbx.Options[selIndex].DisplayName;
                        //    }
                        //}

                        cell = new ComboxParam(string.IsNullOrEmpty(value)
                            ? (string.IsNullOrEmpty(colCbx.Default) ? colCbx.Options[0].DisplayName : colCbx.Default)
                            : value)
                        {
                            Name = colCbx.ControlName,
                            Options = colCbx.Options,
                            IsEditable = colCbx.IsEditable,
                            EnableTolerance = colCbx.EnableTolerance,
                        };
                        break;
                    }

                    case LoopComboxColumn colLoopCbx:
                    {
                        var selIndex = 0;
                        cell = new LoopComboxParam(string.IsNullOrEmpty(value)
                            ? colLoopCbx.Options[selIndex].DisplayName
                            : value)
                        {
                            Name = colLoopCbx.ControlName,
                            Options = colLoopCbx.Options,
                            IsEditable = colLoopCbx.IsEditable,
                            IsLoopStep = false,
                            EnableTolerance = colLoopCbx.EnableTolerance,
                        };
                        break;
                    }

                    case ExpanderColumn _:
                        cell = new ExpanderParam();
                        break;

                    case PopSettingColumn _:
                        cell = new PopSettingParam()
                        {
                            Name = col.ControlName,
                            DisplayName = col.DisplayName,
                            UnitName = col.UnitName,
                            EnableTolerance = col.EnableTolerance,
                        };
                        break;
                }

                if (cell == null)
                    break;
                
                cell.Feedback = col.Feedback;

                if (col is LoopComboxColumn)
                {
                    cell.Feedback = LoopCellFeedback;
                }

                cell.DisplayName = col.DisplayName;
                recipeStep.Add(cell);
            }

            foreach (var cell in recipeStep)
            {
                if (stepPermission == MenuPermissionEnum.MP_NONE)
                    cell.Visible = Visibility.Hidden;
                else
                    cell.Visible = Visibility.Visible;

                cell.Feedback?.Invoke(cell);
            }

            recipeStep.IsHideValue = Steps.IsHideValue;

            return recipeStep;
        }

        public bool CreateStepTolerance(ObservableCollection<EditorDataGridTemplateColumnBase> columns,
            Dictionary<string, RecipeStep> popSettingColumns, XmlNode stepNode, out RecipeStep step, out RecipeStep warning, out RecipeStep alarm,
            out Dictionary<string, RecipeStep> popSettingStep)
        {
            step = new RecipeStep(null);
            warning = new RecipeStep(null);
            alarm = new RecipeStep(null);
            popSettingStep = new Dictionary<string, RecipeStep>();

            foreach (var col in columns)
            {
                var warningValue = string.Empty;
                var alarmValue = string.Empty;
                var stepValue = string.Empty;
                var popValues = new Dictionary<string, Dictionary<string, string>>();

                if (!(col is ExpanderColumn) && stepNode != null && !(col is StepColumn) && !(col is PopSettingColumn))
                {
                    var warningNode = stepNode.SelectSingleNode("Warning");
                    if (warningNode != null && warningNode.Attributes[col.ControlName] != null)
                    {
                        warningValue = warningNode.Attributes[col.ControlName].Value;
                    }
                    var alarmNode = stepNode.SelectSingleNode("Alarm");
                    if (alarmNode != null && alarmNode.Attributes[col.ControlName] != null)
                    {
                        alarmValue = alarmNode.Attributes[col.ControlName].Value;
                    }

                    if (string.IsNullOrEmpty(col.ModuleName) && stepNode.Attributes[col.ControlName] != null)
                    {
                        stepValue = stepNode.Attributes[col.ControlName].Value;
                    }
                    else
                    {
                        if (stepNode.Attributes[col.ControlName] != null && stepNode.SelectSingleNode(col.ModuleName) != null && stepNode.SelectSingleNode(col.ModuleName).Attributes[col.ControlName] != null)
                            stepValue = stepNode.SelectSingleNode(col.ModuleName).Attributes[col.ControlName].Value;
                    }
                }

                if (col is PopSettingColumn)
                {
                    foreach (var key in popSettingColumns.Keys)
                    {
                        var popNode = stepNode.SelectSingleNode(key);
                        if (popNode != null)
                        {
                            var values = new Dictionary<string, string>();
                            foreach (var item in popSettingColumns[key])
                            {
                                if (popNode.Attributes[item.Name] != null)
                                    values.Add(item.Name, popNode.Attributes[item.Name].Value);
                            };

                            popValues.Add(key, values);
                        }
                    }
                }


                Param stepCell = new DoubleParam(GlobalDefs.TryParseToDouble(stepValue))
                {
                    Name = col.ControlName,
                    DisplayName = col.DisplayName,
                    UnitName = col.UnitName,
                    IsEnabled = false,
                    StepCheckVisibility = Visibility.Hidden,
                };

                stepCell.Parent = step;
                step.Add(stepCell);

                if (col is PopSettingColumn)
                {
                    for (var i = 0; i < popSettingColumns[col.ControlName].Count; i++)
                    {
                        var name = popSettingColumns[col.ControlName][i].Name;
                        var value = popValues[col.ControlName].Where(x => x.Key == name).Count() > 0 ?
                            popValues[col.ControlName].First(x => x.Key == name).Value : "";
                        if (popSettingColumns[col.ControlName][i] is DoubleParam)
                        {
                            stepCell = new DoubleParam(GlobalDefs.TryParseToDouble(value))
                            {
                                Name = name,
                                DisplayName = popSettingColumns[col.ControlName][i].DisplayName,
                                IsEnabled = false,
                                StepCheckVisibility = Visibility.Hidden,
                            };
                        }
                        if (popSettingColumns[col.ControlName][i] is StringParam)
                        {
                            stepCell = new StringParam(value)
                            {
                                Name = name,
                                DisplayName = popSettingColumns[col.ControlName][i].DisplayName,
                                IsEnabled = false,
                                StepCheckVisibility = Visibility.Hidden,
                            };
                        }
                        if (popSettingColumns[col.ControlName][i] is ComboxParam)
                        {
                            stepCell = new ComboxParam(value)
                            {
                                Name = name,
                                DisplayName = popSettingColumns[col.ControlName][i].DisplayName,
                                Options = ((ComboxParam)popSettingColumns[col.ControlName][i]).Options,
                                IsEditable = !col.IsReadOnly,
                                EnableTolerance = col.EnableTolerance,
                            };
                        }

                        if (!popSettingStep.ContainsKey(col.ControlName))
                        {
                            popSettingStep.Add(col.ControlName, new RecipeStep(null));
                        }
                        stepCell.Parent = popSettingStep[col.ControlName];
                        popSettingStep[col.ControlName].Add(stepCell);
                    }
                }
                Param warningCell = new DoubleParam(col.EnableTolerance ? (GlobalDefs.TryParseToDouble(warningValue)) : double.NaN)
                {
                    Name = col.ControlName,
                    DisplayName = col.DisplayName,
                    UnitName = col.UnitName,
                    IsEnabled = col.EnableTolerance && stepValue != "0",
                    StepCheckVisibility = Visibility.Collapsed,
                };


                warningCell.Feedback = col.Feedback;
                warningCell.Parent = warning;
                warning.Add(warningCell);

                //Param alarmCell = new DoubleParam(col.EnableTolerance ? (string.IsNullOrEmpty(alarmValue) ? "0" : alarmValue) : "*")
                Param alarmCell = new DoubleParam(col.EnableTolerance ? (GlobalDefs.TryParseToDouble(alarmValue)) : double.NaN)
                {
                    Name = col.ControlName,
                    DisplayName = col.DisplayName,
                    UnitName = col.UnitName,
                    IsEnabled = col.EnableTolerance && stepValue != "0",
                    StepCheckVisibility = Visibility.Collapsed,
                };


                alarmCell.Feedback = col.Feedback;
                alarmCell.Parent = alarm;
                alarm.Add(alarmCell);
            }

            return true;
        }

        public void ValidLoopData()
        {
            if (Steps.Count == 0)
                return;

            for (var j = 0; j < Steps[0].Count; j++)
            {
                if (Steps[0][j] is LoopComboxParam)
                {
                    LoopCellFeedback(Steps[0][j]);
                }
            }
        }

        private void LoopCellFeedback(Param cell)
        {
            var loopCell = cell as LoopComboxParam;
            var rowIndex = -1;
            var colIndex = -1;

            for (var i = 0; i < Steps.Count; i++)
            {
                for (var j = 0; j < Steps[i].Count; j++)
                {
                    if (Steps[i][j] == loopCell)
                    {
                        rowIndex = i;
                        colIndex = j;
                    }
                }
            }

            if (rowIndex < 0 || colIndex < 0)
                return;

            for (var i = 0; i < Steps.Count; i++)
            {
                loopCell = Steps[i][colIndex] as LoopComboxParam;
                var loopStr = loopCell.Value;

                var isLoopStart = Regex.IsMatch(loopStr, @"^Loop\x20x\d+$");
                var isLoopEnd = Regex.IsMatch(loopStr, @"^Loop End$");
                var isNullOrEmpty = string.IsNullOrWhiteSpace(loopStr);

                if (!isLoopEnd && !isLoopStart && !isNullOrEmpty)
                {
                    loopCell.IsLoopStep = true;
                    loopCell.IsValidLoop = false;
                    continue;
                }

                if (isLoopEnd)
                {
                    loopCell.IsLoopStep = true;
                    loopCell.IsValidLoop = false;
                    continue;
                }

                if (isLoopStart)
                {
                    if (i + 1 == Steps.Count)
                    {
                        loopCell.IsLoopStep = true;
                        loopCell.IsValidLoop = true;
                    }

                    for (var j = i + 1; j < Steps.Count; j++)
                    {
                        var loopCell2 = Steps[j][colIndex] as LoopComboxParam;
                        var loopStr2 = loopCell2.Value;
                        var isLoopStart2 = Regex.IsMatch(loopStr2, @"^Loop\x20x\d+$");
                        var isLoopEnd2 = Regex.IsMatch(loopStr2, @"^Loop End$");
                        var isNullOrEmpty2 = string.IsNullOrWhiteSpace(loopStr2);

                        if (!isLoopEnd2 && !isLoopStart2 && !isNullOrEmpty2)
                        {
                            for (var k = i; k < j + 1; k++)
                            {
                                (Steps[k][colIndex] as LoopComboxParam).IsLoopStep = true;
                                (Steps[k][colIndex] as LoopComboxParam).IsValidLoop = false;
                            }
                            i = j;
                            break;
                        }
                        if (isLoopStart2)
                        {
                            loopCell.IsLoopStep = true;
                            loopCell.IsValidLoop = true;
                            i = j - 1;
                            break;
                        }

                        if (isLoopEnd2)
                        {
                            for (var k = i; k < j + 1; k++)
                            {
                                (Steps[k][colIndex] as LoopComboxParam).IsLoopStep = true;
                                (Steps[k][colIndex] as LoopComboxParam).IsValidLoop = true;
                            }
                            i = j;
                            break;
                        }

                        if (j == Steps.Count - 1)
                        {
                            loopCell.IsLoopStep = true;
                            loopCell.IsValidLoop = true;
                            i = j;
                            break;
                        }

                    }
                    continue;
                }

                loopCell.IsLoopStep = false;
                loopCell.IsValidLoop = false;
            }

        }

        private bool LoadHeader(XmlNode nodeHeader)
        {
            if (nodeHeader == null)
                return false;

            if (nodeHeader.Attributes["CreatedBy"] != null)
                Creator = nodeHeader.Attributes["CreatedBy"].Value;
            if (nodeHeader.Attributes["CreationTime"] != null)
                CreateTime = DateTime.Parse(nodeHeader.Attributes["CreationTime"].Value);
            if (nodeHeader.Attributes["LastRevisedBy"] != null)
                Revisor = nodeHeader.Attributes["LastRevisedBy"].Value;
            if (nodeHeader.Attributes["LastRevisionTime"] != null)
                ReviseTime = DateTime.Parse(nodeHeader.Attributes["LastRevisionTime"].Value);
            if (nodeHeader.Attributes["Description"] != null)
                Description = nodeHeader.Attributes["Description"].Value;
            var chamberType = string.Empty;
            if (nodeHeader.Attributes["RecipeChamberType"] != null)
                chamberType = nodeHeader.Attributes["RecipeChamberType"].Value;

            if (!string.IsNullOrEmpty(chamberType) && chamberType != RecipeChamberType)
            {
                LOG.Write($"{chamberType} is not accordance with {RecipeChamberType}");
                return false;
            }

            var version = string.Empty;
            if (nodeHeader.Attributes["RecipeVersion"] != null)
                version = nodeHeader.Attributes["RecipeVersion"].Value;
            if (!string.IsNullOrEmpty(version) && version != RecipeVersion)
            {
                LOG.Write($"{version} is not accordance with {RecipeVersion}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 加载Recipe。
        /// </summary>
        /// <param name="columns"></param>
        /// <param name="steps"></param>
        /// <param name="processType"></param>
        /// <param name="dispatcher"></param>
        /// <param name="isHideValueWhenLoading"></param>
        private async Task LoadSteps(List<EditorDataGridTemplateColumnBase> columns, XmlNodeList steps,
            string processType = "", Dispatcher dispatcher = null, bool isHideValueWhenLoading = true)
        {
            RecipeStep previousStep = null;
            
            Steps.Clear();
            PopSettingSteps.Clear();
            StepTolerances.Clear();
            
            var stepId = 1;
            RecipeTotalTime = 0;

            var roleManager = new RoleManager();
            var menuPermission = new MenuPermission();

            //如果是Process才判断是否要隐藏Step
            if (processType.Contains("Process"))
            {
                roleManager.Initialize();
                var roleItem = roleManager.GetRoleByName(BaseApp.Instance.UserContext.RoleName);
                menuPermission.ParsePermission(roleItem.Role.MenuPermission);
            }


            foreach (XmlNode nodeStep in steps)
            {
                var stepPermissionEnum = MenuPermissionEnum.MP_READ_WRITE;

                if (processType.Contains("Process"))
                {
                    //根据编号获取该Step的用户权限
                    if (menuPermission.MenuPermissionDictionary.ContainsKey("Step" + stepId))
                    {
                        if (menuPermission.MenuPermissionDictionary["Step" + stepId] ==
                            MenuPermissionEnum.MP_NONE)
                        {
                            //设置rows不可见
                            stepPermissionEnum = MenuPermissionEnum.MP_NONE;
                        }
                    }
                }

                var step = CreateStep(columns, nodeStep, previousStep, stepPermissionEnum, stepId);
                step.IsHideValue = isHideValueWhenLoading;
                step.Save();
                previousStep = step;

                RecipeTotalTime += (int)((DoubleParam)step[(int)RecipColNo.Time]).Value;

                var t = dispatcher?.BeginInvoke(DispatcherPriority.Background, (Action)(() => { Steps.Add(step); }));
                if (t != null)
                    await t;
                else
                {
                    // 确保在UI线程上执行
                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        Steps.Add(step);
                    });
                }
                    

                stepId++;
            }
        }

        private void LoadConfigs(RecipeStep configDefine, XmlNode configNode)
        {
            ConfigItems.Clear();

            foreach (var param in configDefine)
            {
                if (param is DoubleParam param1)
                {
                    var config = new DoubleParam()
                    {
                        Name = param.Name,
                        Value = param1.Value,
                        DisplayName = param.DisplayName,
                        Minimun = param1.Minimun,
                        Maximun = param1.Maximun,
                        Resolution = param1.Resolution
                    };

                    if (configNode?.Attributes?[param1.Name] != null)
                        config.Value = double.Parse(configNode.Attributes[param1.Name].Value);

                    ConfigItems.Add(config);
                }

                if (param is StringParam paramString)
                {
                    var config = new StringParam()
                    {
                        Name = param.Name,
                        Value = paramString.Value,
                        DisplayName = param.DisplayName,
                    };

                    if (configNode?.Attributes?[paramString.Name] != null)
                        config.Value = configNode.Attributes[paramString.Name].Value;

                    ConfigItems.Add(config);
                }

            }
        }

        private void SetAttribute(RecipeStep parameters, XmlElement popSettingStep)
        {
            if (parameters != null)

                foreach (var parameter1 in parameters)
                {
                    if (parameter1.Visible != Visibility.Visible)
                        continue;

                    if (parameter1 is IntParam)
                        popSettingStep.SetAttribute(parameter1.Name, ((IntParam)parameter1).Value.ToString());
                    else if (parameter1 is DoubleParam)
                        popSettingStep.SetAttribute(parameter1.Name, ((DoubleParam)parameter1).Value.ToString());
                    else if (parameter1 is StringParam)
                        popSettingStep.SetAttribute(parameter1.Name, ((StringParam)parameter1).Value.ToString());
                    else if (parameter1 is ComboxParam)
                        popSettingStep.SetAttribute(parameter1.Name, ((ComboxParam)parameter1).Value.ToString());
                    else if (parameter1 is LoopComboxParam)
                        popSettingStep.SetAttribute(parameter1.Name, ((LoopComboxParam)parameter1).Value.ToString());
                    else if (parameter1 is PositionParam)
                        popSettingStep.SetAttribute(parameter1.Name, ((PositionParam)parameter1).Value.ToString());
                    else if (parameter1 is BoolParam)
                        popSettingStep.SetAttribute(parameter1.Name, ((BoolParam)parameter1).Value.ToString());
                    else if (parameter1 is StepParam)
                        popSettingStep.SetAttribute(parameter1.Name, ((StepParam)parameter1).Value.ToString());
                    else if (parameter1 is MultipleSelectParam)
                    {
                        var selected1 = new List<string>();
                        ((MultipleSelectParam)parameter1).Options.Apply(
                            opt =>
                            {
                                if (opt.IsChecked)
                                    selected1.Add(opt.ControlName);
                            }
                            );
                        popSettingStep.SetAttribute(parameter1.Name, string.Join(",", selected1));
                    }
                }
        }

        public RecipeStep CloneStep(List<EditorDataGridTemplateColumnBase> columns, RecipeStep sourceParams)
        {
            var targetParams = CreateStep(columns);

            for (var index = 0; index < sourceParams.Count; index++)
            {
                // Uid不能克隆。
                if (sourceParams[index] is Param para && para.Name == "StepUid")
                    continue;

                if (sourceParams[index] is StringParam)
                {
                    ((StringParam)targetParams[index]).Value = ((StringParam)sourceParams[index]).Value;
                }
                else if (sourceParams[index] is Sets3RatioParam)
                {
                    ((Sets3RatioParam)targetParams[index]).Value = ((Sets3RatioParam)sourceParams[index]).Value;
                }
                else if (sourceParams[index] is IntParam)
                {
                    ((IntParam)targetParams[index]).Value = ((IntParam)sourceParams[index]).Value;
                }
                else if (sourceParams[index] is ComboxParam)
                {
                    ((ComboxParam)targetParams[index]).Value = ((ComboxParam)sourceParams[index]).Value;
                }
                else if (sourceParams[index] is LoopComboxParam)
                {
                    ((LoopComboxParam)targetParams[index]).Value = ((LoopComboxParam)sourceParams[index]).Value;
                }
                else if (sourceParams[index] is BoolParam)
                {
                    ((BoolParam)targetParams[index]).Value = ((BoolParam)sourceParams[index]).Value;
                }
                else if (sourceParams[index] is IntParam)
                {
                    ((IntParam)targetParams[index]).Value = ((IntParam)sourceParams[index]).Value;
                }
                else if (sourceParams[index] is DoubleParam)
                {
                    ((DoubleParam)targetParams[index]).Value = ((DoubleParam)sourceParams[index]).Value;
                }
            }

            return targetParams;
        }

        public string GetXmlString()
        {
            try
            {
                var nodeData = _doc.SelectSingleNode($"Aitex/TableRecipeData") as XmlElement;

                nodeData.SetAttribute("CreatedBy", Creator);
                nodeData.SetAttribute("CreationTime", CreateTime.ToString("yyyy-MM-dd HH:mm:ss"));
                nodeData.SetAttribute("LastRevisedBy", Revisor);
                nodeData.SetAttribute("LastRevisionTime", ReviseTime.ToString("yyyy-MM-dd HH:mm:ss"));
                nodeData.SetAttribute("Description", Description);
                nodeData.SetAttribute("RecipeChamberType", RecipeChamberType);
                nodeData.SetAttribute("RecipeVersion", RecipeVersion);

                var nodeModule = _doc.SelectSingleNode($"Aitex/TableRecipeData/Module[@Name='{Module}']");
                if (nodeModule == null)
                {
                    nodeModule = _doc.CreateElement("Module");
                    nodeData.AppendChild(nodeModule);
                }
                nodeModule.RemoveAll();
                (nodeModule as XmlElement).SetAttribute("Name", Module);
                var i = 0;
                foreach (var parameters in Steps)
                {
                    var nodeWarning = _doc.CreateElement("Warning");
                    var nodeAlarm = _doc.CreateElement("Alarm");
                    var nodePop = new Dictionary<string, XmlElement>();
                    foreach (var key in PopEnable.Keys)
                    {
                        nodePop.Add(key, _doc.CreateElement(key));
                    }

                    var nodeStep = _doc.CreateElement("Step");
                    foreach (var parameter in parameters)
                    {
                        if (ToleranceEnable)
                        {
                            if (parameter.EnableTolerance)
                            {
                                nodeWarning.SetAttribute(parameter.Name, "1");
                                nodeAlarm.SetAttribute(parameter.Name, "7");
                            }
                        }

                        if (parameter is IntParam intParam)
                        {
                            //去除Int型数据前面多余的"0"
                            if (int.TryParse(intParam.Value.ToString(), out var result))
                            {
                                if (result != 0)
                                {
                                    intParam.Value = int.Parse(intParam.Value.ToString().TrimStart('0'));
                                }
                            }

                            nodeStep.SetAttribute(intParam.Name, intParam.Value.ToString());
                        }
                        else if (parameter is DoubleParam doubleParam)
                        {
                            //去除Double型数据前面多余的"0"
                            //if (int.TryParse(((DoubleParam)parameter).Value, out var result))
                            //{
                            //    if (result != 0)
                            //    {
                            //        ((DoubleParam)parameter).Value = ((DoubleParam)parameter).Value.TrimStart('0');
                            //    }
                            //}

                            nodeStep.SetAttribute(doubleParam.Name, doubleParam.Value.ToString());
                        }
                        else if(parameter is Sets3RatioParam param)
                            nodeStep.SetAttribute(param.Name, param.Value.ToString());
                        else if (parameter is StringParam stringParam)
                            nodeStep.SetAttribute(stringParam.Name, stringParam.Value.ToString());
                        else if (parameter is ComboxParam comboxParam)
                            nodeStep.SetAttribute(comboxParam.Name, comboxParam.Options.First(x => x.DisplayName == ((ComboxParam)parameter).Value.ToString()).ControlName);
                        else if (parameter is LoopComboxParam loopComboxParam)
                            nodeStep.SetAttribute(loopComboxParam.Name, loopComboxParam.Value.ToString());
                        else if (parameter is PositionParam positionParam)
                            nodeStep.SetAttribute(positionParam.Name, positionParam.Value.ToString());
                        else if (parameter is BoolParam boolParam)
                            nodeStep.SetAttribute(boolParam.Name, boolParam.Value.ToString());
                        else if (parameter is StepParam stepParam)
                            nodeStep.SetAttribute(stepParam.Name, stepParam.Value.ToString());
                        else if (parameter is MultipleSelectParam selectParam)
                        {
                            var selected = new List<string>();
                            selectParam.Options.Apply(
                                 opt =>
                                 {
                                     if (opt.IsChecked)
                                         selected.Add(opt.ControlName);
                                 }
                                                        );
                            nodeStep.SetAttribute(selectParam.Name, string.Join(",", selected));
                        }
                        else if (parameter is PopSettingParam)
                        {
                            SetAttribute(PopSettingSteps[parameter.Name][i], nodePop[parameter.Name]);
                        }
                    }

                    if (ToleranceEnable)
                    {
                        nodeStep.AppendChild(nodeWarning);
                        nodeStep.AppendChild(nodeAlarm);
                    }

                    foreach (var key in PopEnable.Keys)
                    {
                        if (PopEnable[key])
                        {
                            nodeStep.AppendChild(nodePop[key]);
                        }
                    }

                    nodeModule.AppendChild(nodeStep);

                    i++;
                }

                var nodeConfig = _doc.CreateElement("Config");
                foreach (var parameter in ConfigItems)
                {
                    if (parameter.Visible == Visibility.Visible)
                    {
                        if (parameter is IntParam)
                            nodeConfig.SetAttribute(parameter.Name, ((IntParam)parameter).Value.ToString());
                        else if (parameter is DoubleParam)
                        {
                            /*var strValue = ((DoubleParam)parameter).Value;
                            var succed = double.TryParse(strValue, out var dValue);

                            if (!succed)
                            {
                                MessageBox.Show($"The set value of {parameter.DisplayName} is {strValue}, not a valid value");
                                return null;
                            }*/

                            var dValue = ((DoubleParam)parameter).Value;

                            var config = ConfigItems.Where(m => m.Name == parameter.Name).FirstOrDefault();

                            if (config is DoubleParam param1)
                            {
                                if (param1.Minimun == 0 && param1.Maximun == 0)
                                {
                                    //没有设定范围
                                }
                                else if (dValue > param1.Maximun || dValue < param1.Minimun)
                                {
                                    MessageBox.Show($"The set value of {parameter.DisplayName} is {dValue}, out of the range {param1.Minimun}~{param1.Maximun}");
                                    return null;
                                }
                            }

                            nodeConfig.SetAttribute(parameter.Name, ((DoubleParam)parameter).Value.ToString());
                        }
                        else if (parameter is StringParam)
                            nodeConfig.SetAttribute(parameter.Name, ((StringParam)parameter).Value.ToString());
                        else if (parameter is ComboxParam)
                            nodeConfig.SetAttribute(parameter.Name, ((ComboxParam)parameter).Value.ToString());
                        else if (parameter is LoopComboxParam)
                            nodeConfig.SetAttribute(parameter.Name, ((LoopComboxParam)parameter).Value.ToString());
                        else if (parameter is PositionParam)
                            nodeConfig.SetAttribute(parameter.Name, ((PositionParam)parameter).Value.ToString());
                        else if (parameter is BoolParam)
                            nodeConfig.SetAttribute(parameter.Name, ((BoolParam)parameter).Value.ToString());
                        else if (parameter is StepParam)
                            nodeConfig.SetAttribute(parameter.Name, ((StepParam)parameter).Value.ToString());
                        else if (parameter is MultipleSelectParam)
                        {
                            var selected = new List<string>();
                            ((MultipleSelectParam)parameter).Options.Apply(
                                 opt =>
                                 {
                                     if (opt.IsChecked)
                                         selected.Add(opt.ControlName);
                                 }
                                                        );
                            nodeConfig.SetAttribute(parameter.Name, string.Join(",", selected));
                        }
                    }
                }
                nodeModule.AppendChild(nodeConfig);

                return _doc.OuterXml;
            }
            catch (Exception ex)
            {
                LOG.Write(ex.Message);
                return "";
            }
        }

        public string ToXmlString()
        {
            var builder = new StringBuilder();
            builder.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            builder.Append(string.Format("<Aitex><TableRecipeData CreatedBy=\"{0}\" CreationTime=\"{1}\" LastRevisedBy=\"{2}\" LastRevisionTime=\"{3}\" Description=\"{4}\"  RecipeChamberType=\"{5}\" RecipeVersion=\"{6}\">", Creator, CreateTime.ToString("yyyy-MM-dd HH:mm:ss"), Revisor, ReviseTime.ToString("yyyy-MM-dd HH:mm:ss"), Description, RecipeChamberType, RecipeVersion));
            foreach (var parameters in Steps)
            {
                builder.Append("<Step ");
                foreach (var parameter in parameters)
                {
                    if (parameter.Visible == Visibility.Visible)
                    {
                        if (parameter is IntParam)
                            builder.Append(parameter.Name + "=\"" + ((IntParam)parameter).Value + "\" ");
                        else if (parameter is DoubleParam)
                            builder.Append(parameter.Name + "=\"" + ((DoubleParam)parameter).Value + "\" ");
                        else if (parameter is StringParam)
                            builder.Append(parameter.Name + "=\"" + ((StringParam)parameter).Value + "\" ");
                        else if (parameter is ComboxParam)
                            builder.Append(parameter.Name + "=\"" + ((ComboxParam)parameter).Value + "\" ");
                        else if (parameter is LoopComboxParam)
                            builder.Append(parameter.Name + "=\"" + ((LoopComboxParam)parameter).Value + "\" ");
                        else if (parameter is PositionParam)
                            builder.Append(parameter.Name + "=\"" + ((PositionParam)parameter).Value + "\" ");
                        else if (parameter is BoolParam)
                            builder.Append(parameter.Name + "=\"" + ((BoolParam)parameter).Value + "\" ");
                        else if (parameter is StepParam)
                            builder.Append(parameter.Name + "=\"" + ((StepParam)parameter).Value + "\" ");
                        else if (parameter is MultipleSelectParam)
                        {
                            var selected = new List<string>();
                            ((MultipleSelectParam)parameter).Options.Apply(
                                opt =>
                                {
                                    if (opt.IsChecked)
                                        selected.Add(opt.ControlName);
                                }
                                );
                            builder.Append(parameter.Name + "=\"" + string.Join(",", selected) + "\" ");
                        }
                    }
                }
                builder.Append("/>");
            }

            builder.Append("<Config ");
            foreach (var parameter in ConfigItems)
            {
                if (parameter.Visible == Visibility.Visible)
                {
                    if (parameter is IntParam)
                        builder.Append(parameter.Name + "=\"" + ((IntParam)parameter).Value + "\" ");
                    else if (parameter is DoubleParam)
                        builder.Append(parameter.Name + "=\"" + ((DoubleParam)parameter).Value + "\" ");
                    else if (parameter is StringParam)
                        builder.Append(parameter.Name + "=\"" + ((StringParam)parameter).Value + "\" ");
                    else if (parameter is ComboxParam)
                        builder.Append(parameter.Name + "=\"" + ((ComboxParam)parameter).Value + "\" ");
                    else if (parameter is LoopComboxParam)
                        builder.Append(parameter.Name + "=\"" + ((LoopComboxParam)parameter).Value + "\" ");
                    else if (parameter is PositionParam)
                        builder.Append(parameter.Name + "=\"" + ((PositionParam)parameter).Value + "\" ");
                    else if (parameter is BoolParam)
                        builder.Append(parameter.Name + "=\"" + ((BoolParam)parameter).Value + "\" ");
                    else if (parameter is StepParam)
                        builder.Append(parameter.Name + "=\"" + ((StepParam)parameter).Value + "\" ");
                    else if (parameter is MultipleSelectParam)
                    {
                        var selected = new List<string>();
                        ((MultipleSelectParam)parameter).Options.Apply(
                            opt =>
                            {
                                if (opt.IsChecked)
                                    selected.Add(opt.ControlName);
                            }
                            );
                        builder.Append(parameter.Name + "=\"" + string.Join(",", selected) + "\" ");
                    }
                }


            }
            builder.Append("/>");

            builder.Append("</TableRecipeData></Aitex>");
            return builder.ToString();
        }


        #region Import Export Methods

        private static byte[] GetMagicNumber()
        {
            return Encoding.ASCII.GetBytes("SIC");
        }

        /// <summary>
        /// 导入Recipe
        /// </summary>
        /// <param name="fileName"></param>
        /// <param name="xmlContent"></param>
        public static void ImportRecipe(string fileName, out string xmlContent)
        {
            xmlContent = "";

            using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                using (var br = new BinaryReader(fs))
                {
                    var magicNumDesired = GetMagicNumber();
                    var magicNum = br.ReadBytes(3);

                    // Magic Number incorrect
                    
                    if (!magicNum.SequenceEqual(magicNumDesired))
                        throw new Exception("format error, code -1.");

                    // read hash data
                    var lenHash = br.ReadInt32();
                    var hashInFile = br.ReadBytes(lenHash);

                    // read base64 recipe content
                    var lenContent = br.ReadInt32();
                    var buffContent = br.ReadBytes(lenContent);

                    // check hash
                    using (var sha = SHA256.Create())
                    {
                        var hash = sha.ComputeHash(buffContent);
                        if (!hash.SequenceEqual(hashInFile))
                            throw new Exception("format error, code -2.");
                    }

                    var strBase64 = Encoding.ASCII.GetString(buffContent);
                    var bufRecipe = Convert.FromBase64String(strBase64);
                    var xmlRecipe = Encoding.UTF8.GetString(bufRecipe);

                    xmlContent = xmlRecipe;

                }
            }
        }

        /// <summary>
        /// 导出Recipe
        /// </summary>
        /// <param name="fileName"></param>
        public void ExportRecipe(string fileName)
        {
            var xmlContent = GetXmlString();

            // 未加密，仅用Base64对Recipe字串编码，可直接用WinHex查看内容，方便后期调试。
            var ba = Encoding.UTF8.GetBytes(xmlContent);
            var base64 = Convert.ToBase64String(ba);
            ba = Encoding.ASCII.GetBytes(base64);
            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(ba);

                using (var fs = new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
                {

                    // discard the contents of the file by setting the length to 0
                    fs.SetLength(0);

                    using (var bw = new BinaryWriter(fs))
                    {
                        var magicNum = GetMagicNumber();
                        bw.Write(magicNum, 0, magicNum.Length);
                        bw.Write(hash.Length);
                        bw.Write(hash);
                        bw.Write(ba.Length);
                        bw.Write(ba);
                        bw.Flush();
                    }
                }
            }
        }

        #endregion

        #endregion
    }
}
