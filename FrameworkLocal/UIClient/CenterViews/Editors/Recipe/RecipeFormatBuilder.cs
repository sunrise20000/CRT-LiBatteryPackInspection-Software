using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using Aitex.Core.RT.Log;
using MECF.Framework.UI.Client.CenterViews.Configs.Roles;
using MECF.Framework.UI.Client.CenterViews.Configs.SystemConfig;
using MECF.Framework.UI.Client.ClientBase;
using MECF.Framework.UI.Client.RecipeEditorLib.RecipeModel;
using RecipeEditorLib.DGExtension.CustomColumn;
using RecipeEditorLib.RecipeModel.Params;
using Sicentury.Core;

namespace MECF.Framework.UI.Client.CenterViews.Editors.Recipe
{
    public class RecipeFormatBuilder
    {
        public ObservableCollection<RecipeInfo> RecipeInfos
        {
            get;
            set;
        }

        public ObservableCollection<StepInfo> StepInfos
        {
            get;
            set;
        }

        public ObservableCollection<ContentInfo> ContentInfos
        {
            get;
            set;
        }

        public List<EditorDataGridTemplateColumnBase> Columns
        {
            get;
            set;
        }

        public RecipeStep Configs
        {
            get;
            set;
        }


        public RecipeStep OesConfig
        {
            get;
            set;
        }


        public RecipeStep VatConfig
        {
            get;
            set;
        }
        public RecipeStep BrandConfig
        {
            get;
            set;
        }

        public RecipeStep FineTuningConfig
        {
            get;
            set;
        }

        public string RecipeChamberType
        {
            get;
            set;
        }

        public string RecipeVersion
        {
            get;
            set;
        }
        /// <summary>
        /// 去除小数点、箭头、括号（含括号里的内容）、横杠
        /// </summary>
        /// <param name="DisplayName"></param>
        /// <returns></returns>
        private string strAbc(string DisplayName)
        {
            var displayname = DisplayName;
            if (Regex.IsMatch(displayname.Substring(0, 1), @"^[+-]?\d*[.]?\d*$"))
            {
                displayname = displayname.Remove(0, displayname.IndexOf(".") + 1);//去除序号
            }
            displayname = displayname.Trim().Replace(" ","").Replace(".", "").Replace("->", "_").Replace("-", "");
            if (displayname.Contains("(") && displayname.Contains(")"))
            {
                displayname=displayname.Remove(displayname.IndexOf("("), displayname.IndexOf(")") - displayname.IndexOf("(") + 1);
            }
            return displayname;
        }

        public double GetDoubleValueByName(string module, string name)
        {
            var value = SystemConfigProvider.Instance.GetValueByName(module, name);
            double returnValue = 0;
            if (String.IsNullOrEmpty(value))
            {
                if (name.EndsWith(".Default"))
                {
                    returnValue = 0;
                }
                else if (name.EndsWith(".Min"))
                {
                    returnValue = 0;
                }
                else if (name.EndsWith(".Max"))
                {
                    returnValue = 10000;
                }
            }
            else
            {
                returnValue = double.Parse(value);
            }
            return returnValue;
        }

        public string GetStringValueByName(string module, string name)
        {
            return SystemConfigProvider.Instance.GetValueByName(module, name);
        }

        private RecipeProvider recipeProvider = new RecipeProvider();

        //获取RecipInfo的Name信息
        public ObservableCollection<RecipeInfo> GetRecipeColumnName(string path, string module = "PM1", bool defaultCellEnable = true)
        {
            var str = recipeProvider.GetRecipeFormatXml(path);
            var doc = new XmlDocument();

            try
            {
                doc.LoadXml(str);

                var nodeRoot = doc.SelectSingleNode("TableRecipeFormat");

                RecipeChamberType = nodeRoot.Attributes["RecipeChamberType"].Value;

                RecipeVersion = nodeRoot.Attributes["RecipeVersion"].Value;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return null;
            }

            RecipeInfos = new ObservableCollection<RecipeInfo>();

            var nodes = doc.SelectNodes("TableRecipeFormat/Catalog/Group");
            foreach (XmlNode node in nodes)
            {
                var childNodes = node.SelectNodes("Step");
                foreach (XmlNode step in childNodes)
                {
                    RecipeInfos.Add(new RecipeInfo()
                    {
                        ID = step.Attributes["ControlName"].Value,
                        Name = step.Attributes["DisplayName"].Value
                    });
                }
            }

            return RecipeInfos;
        }

        //获取StepInfo的No信息
        public ObservableCollection<StepInfo> GetRecipeStepNo()
        {
            StepInfos = new ObservableCollection<StepInfo>();

            for (var i = 0; i < 50; i++)
            {
                StepInfos.Add(new StepInfo()
                {
                    ID = (i + 1).ToString(),
                    Name = "Step" + (i + 1).ToString()
                });
            }

            return StepInfos;
        }

        //获取ContentInfo的Name信息
        public ObservableCollection<ContentInfo> GetContentName(string path, string module = "PM1", bool defaultCellEnable = true)
        {
            var str = recipeProvider.GetRecipeFormatXml(path);
            var doc = new XmlDocument();

            try
            {
                doc.LoadXml(str);

                var nodeRoot = doc.SelectSingleNode("TableRecipeFormat");

                RecipeChamberType = nodeRoot.Attributes["RecipeChamberType"].Value;

                RecipeVersion = nodeRoot.Attributes["RecipeVersion"].Value;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return null;
            }

            ContentInfos = new ObservableCollection<ContentInfo>();

            var nodesCatalog = doc.SelectNodes("TableRecipeFormat/Catalog");
            foreach (XmlNode catalog in nodesCatalog)
            {
                var catalogName = catalog.Attributes["DisplayName"].Value;
                var nodeGroups = catalog.SelectNodes("Group");
                foreach (XmlNode group in nodeGroups)
                {
                    var groupName = group.Attributes["DisplayName"].Value;
                    var nodeContents = group.SelectNodes("Content");
                    foreach (XmlNode content in nodeContents)
                    {
                        ContentInfos.Add(new ContentInfo()
                        {
                            Name = catalogName + "." + groupName + "." + content.Attributes["DisplayName"].Value
                        });
                    }
                }
            }

            return ContentInfos;
        }

        public List<EditorDataGridTemplateColumnBase> Build(string path, string module = "PM1", bool defaultCellEnable = true, string roleName = "管理员")
        {
            var str = recipeProvider.GetRecipeFormatXml(path);
            var doc = new XmlDocument();

            try
            {
                doc.LoadXml(str);

                var nodeRoot = doc.SelectSingleNode("TableRecipeFormat");

                RecipeChamberType = nodeRoot.Attributes["RecipeChamberType"].Value;

                RecipeVersion = nodeRoot.Attributes["RecipeVersion"].Value;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return null;
            }

            //初始化RoleManager
            var roleManager = new RoleManager();
            roleManager.Initialize();

            //得到当前登录的RoleItem
            var roleItem = roleManager.GetRoleByName(roleName);

            var menuPermission = new MenuPermission();
            menuPermission.ParsePermission(roleItem.Role.MenuPermission);

            var columns = new List<EditorDataGridTemplateColumnBase>();

            EditorDataGridTemplateColumnBase col = null;
            var nodes = doc.SelectNodes("TableRecipeFormat/Catalog/Group");
            foreach (XmlNode node in nodes)
            {
                var colExpander = new ExpanderColumn()
                {
                    DisplayName = node.Attributes["DisplayName"].Value,
                    StringCellTemplate = "TemplateExpander",
                    StringHeaderTemplate = "ParamExpander"
                };
                columns.Add(colExpander);

                var childNodes = node.SelectNodes("Step");
                foreach (XmlNode step in childNodes)
                {
                    //step number
                    if (step.Attributes["ControlName"].Value == "StepNo")
                    {
                        col = new StepColumn()
                        {
                            DisplayName = "Step",
                            UnitName = "",
                            ControlName = "StepNo",
                            StringCellTemplate = "TemplateStep",
                            StringHeaderTemplate = "ParamTemplate",
                            IsEnable = defaultCellEnable,
                            IsReadOnly = true
                        };
                    }
                    else if (step.Attributes["ControlName"].Value == "StepUid")
                    {
                        col = new TextBoxColumn()
                        {
                            ModuleName = step.Attributes["ModuleName"].Value,
                            ControlName = step.Attributes["ControlName"].Value,
                            DisplayName = step.Attributes["DisplayName"].Value,
                            UnitName = step.Attributes["UnitName"].Value,
                            StringCellTemplate = "TemplateCellDisplay",
                            StringHeaderTemplate = "ParamTemplate",
                            IsReadOnly = true,
                            IsEnable = defaultCellEnable,
                        };
                    }
                    else
                    {
                        switch (step.Attributes["InputType"].Value)
                        {
                            case "Set3RatioInput":
                                col = new RatioColumn()
                                {
                                    ModuleName = step.Attributes["ModuleName"].Value,
                                    ControlName = step.Attributes["ControlName"].Value,
                                    DisplayName = step.Attributes["DisplayName"].Value,
                                    UnitName = step.Attributes["UnitName"].Value,
                                    Default = GetStringValueByName("PM",
                                        $"PM.{module}.RecipeConfig.{strAbc(node.Attributes["DisplayName"].Value)}.{strAbc(step.Attributes["DisplayName"].Value)}.Default"),
                                    StringCellTemplate = "TemplateCellDisplay",
                                    StringCellEditingTemplate = "TemplateSets3Ratio",
                                    StringHeaderTemplate = "ParamTemplate",
                                    EnableConfig = step.Attributes["EnableConfig"] != null &&
                                                   Convert.ToBoolean(step.Attributes["EnableConfig"].Value),
                                    EnableTolerance = step.Attributes["EnableTolerance"] != null &&
                                                      Convert.ToBoolean(step.Attributes["EnableTolerance"].Value),
                                    IsEnable = defaultCellEnable,
                                    MaxSets = 3
                                };
                                break;

                            case "TextInput":
                                col = new TextBoxColumn()
                                {
                                    ModuleName = step.Attributes["ModuleName"].Value,
                                    ControlName = step.Attributes["ControlName"].Value,
                                    DisplayName = step.Attributes["DisplayName"].Value,
                                    UnitName = step.Attributes["UnitName"].Value,
                                    Default = GetStringValueByName("PM",
                                        $"PM.{module}.RecipeConfig.{strAbc(node.Attributes["DisplayName"].Value)}.{strAbc(step.Attributes["DisplayName"].Value)}.Default"),
                                    StringCellTemplate = "TemplateCellDisplay",
                                    StringCellEditingTemplate = "TemplateText",
                                    StringHeaderTemplate = "ParamTemplate",
                                    EnableConfig = step.Attributes["EnableConfig"] != null &&
                                                   Convert.ToBoolean(step.Attributes["EnableConfig"].Value),
                                    EnableTolerance = step.Attributes["EnableTolerance"] != null &&
                                                      Convert.ToBoolean(step.Attributes["EnableTolerance"].Value),
                                    IsEnable = defaultCellEnable,
                                };
                                break;

                            case "ReadOnly":
                                col = new TextBoxColumn()
                                {
                                    ModuleName = step.Attributes["ModuleName"].Value,
                                    ControlName = step.Attributes["ControlName"].Value,
                                    DisplayName = step.Attributes["DisplayName"].Value,
                                    UnitName = step.Attributes["UnitName"].Value,
                                    StringCellTemplate = "TemplateCellDisplay",
                                    StringCellEditingTemplate = "TemplateReadOnly",
                                    StringHeaderTemplate = "ParamTemplate",
                                    IsReadOnly = true,
                                    EnableConfig = step.Attributes["EnableConfig"] != null &&
                                                   Convert.ToBoolean(step.Attributes["EnableConfig"].Value),
                                    EnableTolerance = step.Attributes["EnableTolerance"] != null &&
                                                      Convert.ToBoolean(step.Attributes["EnableTolerance"].Value),
                                    IsEnable = defaultCellEnable,
                                };
                                break;

                            case "NumInput":
                                col = new NumColumn()
                                {
                                    ModuleName = step.Attributes["ModuleName"].Value,
                                    ControlName = step.Attributes["ControlName"].Value,
                                    DisplayName = step.Attributes["DisplayName"].Value,
                                    UnitName = step.Attributes["UnitName"].Value,
                                    InputMode = step.Attributes["InputMode"].Value,
                                    //Minimun = double.Parse(step.Attributes["Min"].Value),
                                    Minimun = GetDoubleValueByName("PM",
                                        $"PM.{module}.RecipeConfig.{strAbc(node.Attributes["DisplayName"].Value)}.{strAbc(step.Attributes["DisplayName"].Value)}.Min"),
                                    //Maximun = double.Parse(step.Attributes["Max"].Value),
                                    Maximun = GetDoubleValueByName("PM",
                                        $"PM.{module}.RecipeConfig.{strAbc(node.Attributes["DisplayName"].Value)}.{strAbc(step.Attributes["DisplayName"].Value)}.Max"),
                                    StringCellTemplate = "TemplateCellDisplay",
                                    StringCellEditingTemplate = "TemplateNumber",
                                    StringHeaderTemplate = "ParamTemplate",
                                    EnableConfig = step.Attributes["EnableConfig"] != null &&
                                                   Convert.ToBoolean(step.Attributes["EnableConfig"].Value),
                                    EnableTolerance = step.Attributes["EnableTolerance"] != null &&
                                                      Convert.ToBoolean(step.Attributes["EnableTolerance"].Value),
                                    IsEnable = defaultCellEnable,
                                };
                                break;

                            case "DoubleInput":
                                //MFC和PC的最大值读取MFC的设定
                                var displayText = new Regex(@"\(M\d+\)").Match(step.Attributes["DisplayName"].Value)
                                    .Value;
                                var maxConfig =
                                    $"PM.{module}.RecipeConfig.{strAbc(node.Attributes["DisplayName"].Value)}.{strAbc(step.Attributes["DisplayName"].Value)}.Max";
                                var minConfig =
                                    $"PM.{module}.RecipeConfig.{strAbc(node.Attributes["DisplayName"].Value)}.{strAbc(step.Attributes["DisplayName"].Value)}.Min";
                                if (displayText.Contains("M"))
                                {
                                    maxConfig =
                                        $"PM.{module}.MFC.Mfc{displayText.Replace("(M", "").Replace(")", "")}.N2Scale";
                                }
                                else
                                {
                                    displayText = new Regex(@"\(PC\d+\)").Match(step.Attributes["DisplayName"].Value)
                                        .Value;
                                    if (displayText.Contains("PC"))
                                    {
                                        maxConfig =
                                            $"PM.{module}.PC.PC{displayText.Replace("(PC", "").Replace(")", "")}.Scale";
                                    }
                                }

                                col = new DoubleColumn()
                                {
                                    ModuleName = step.Attributes["ModuleName"].Value,
                                    ControlName = step.Attributes["ControlName"].Value,
                                    DisplayName = step.Attributes["DisplayName"].Value,
                                    UnitName = step.Attributes["UnitName"].Value,
                                    InputMode = step.Attributes["InputMode"].Value,
                                    Maximun = GetDoubleValueByName("PM", maxConfig),
                                    Minimun = GetDoubleValueByName("PM", minConfig),
                                    Default = GetStringValueByName("PM",
                                        $"PM.{module}.RecipeConfig.{strAbc(node.Attributes["DisplayName"].Value)}.{strAbc(step.Attributes["DisplayName"].Value)}.Default"),
                                    StringCellTemplate = "TemplateCellDisplay",
                                    StringCellEditingTemplate = "TemplateNumber",
                                    StringHeaderTemplate = "ParamTemplate",
                                    EnableConfig = step.Attributes["EnableConfig"] != null &&
                                                   Convert.ToBoolean(step.Attributes["EnableConfig"].Value),
                                    EnableTolerance = step.Attributes["EnableTolerance"] != null &&
                                                      Convert.ToBoolean(step.Attributes["EnableTolerance"].Value),
                                    IsEnable = defaultCellEnable,
                                };


                                if (displayText.Contains("M"))
                                {
                                    ((DoubleColumn)col).Minimun = ((DoubleColumn)col).Maximun *
                                        GetDoubleValueByName("PM", $"PM.{module}.MFC.MinScale") / 100.0;
                                }
                                break;

                            case "DoubleTextInput":
                                //MFC和 PC 的最大值读取MFC的设定
                                displayText = new Regex(@"\(M\d+\)").Match(step.Attributes["DisplayName"].Value).Value;
                                maxConfig =
                                    $"PM.{module}.RecipeConfig.{strAbc(node.Attributes["DisplayName"].Value)}.{strAbc(step.Attributes["DisplayName"].Value)}.Max";
                                minConfig =
                                    $"PM.{module}.RecipeConfig.{strAbc(node.Attributes["DisplayName"].Value)}.{strAbc(step.Attributes["DisplayName"].Value)}.Min";
                                if (displayText.Contains("M"))
                                {
                                    maxConfig =
                                        $"PM.{module}.MFC.Mfc{displayText.Replace("(M", "").Replace(")", "")}.N2Scale";
                                }
                                else
                                {
                                    displayText = new Regex(@"\(PC\d+\)").Match(step.Attributes["DisplayName"].Value)
                                        .Value;
                                    if (displayText.Contains("PC"))
                                    {
                                        maxConfig =
                                            $"PM.{module}.PC.PC{displayText.Replace("(PC", "").Replace(")", "")}.Scale";
                                    }
                                }

                                col = new DoubleColumn()
                                {
                                    ModuleName = step.Attributes["ModuleName"].Value,
                                    ControlName = step.Attributes["ControlName"].Value,
                                    DisplayName = step.Attributes["DisplayName"].Value,
                                    UnitName = step.Attributes["UnitName"].Value,
                                    InputMode = step.Attributes["InputMode"].Value,
                                    Minimun = GetDoubleValueByName("PM", minConfig),
                                    Maximun = GetDoubleValueByName("PM", maxConfig),
                                    Default = "Hold",
                                    StringCellTemplate = "TemplateCellDisplay",
                                    StringCellEditingTemplate = "TemplateNumberText",
                                    StringHeaderTemplate = "ParamTemplate",
                                    EnableConfig = step.Attributes["EnableConfig"] != null &&
                                                   Convert.ToBoolean(step.Attributes["EnableConfig"].Value),
                                    EnableTolerance = step.Attributes["EnableTolerance"] != null &&
                                                      Convert.ToBoolean(step.Attributes["EnableTolerance"].Value),
                                    IsEnable = defaultCellEnable,
                                };
                                break;

                            case "EditableSelection":
                            case "ReadOnlySelection":
                                col = new ComboxColumn()
                                {
                                    //IsReadOnly = step.Attributes["InputType"].Value == "ReadOnlySelection",
                                    IsEditable = step.Attributes["InputType"].Value != "ReadOnlySelection",
                                    ModuleName = step.Attributes["ModuleName"].Value,
                                    //Default = step.Attributes["Default"] != null ? step.Attributes["Default"].Value : "",
                                    Default = GetStringValueByName("PM",
                                        $"PM.{module}.RecipeConfig.{strAbc(node.Attributes["DisplayName"].Value)}.{strAbc(step.Attributes["DisplayName"].Value)}.Default"),
                                    ControlName = step.Attributes["ControlName"].Value,
                                    DisplayName = step.Attributes["DisplayName"].Value,
                                    UnitName = step.Attributes["UnitName"].Value,
                                    StringCellTemplate = "TemplateCellDisplay",
                                    StringCellEditingTemplate = "TemplateComboxEx",
                                    StringHeaderTemplate = "ParamTemplate",
                                    EnableConfig = step.Attributes["EnableConfig"] != null &&
                                                   Convert.ToBoolean(step.Attributes["EnableConfig"].Value),
                                    EnableTolerance = step.Attributes["EnableTolerance"] != null &&
                                                      Convert.ToBoolean(step.Attributes["EnableTolerance"].Value),
                                    IsEnable = defaultCellEnable,


                                };
                                var items = step.SelectNodes("Item");
                                foreach (XmlNode item in items)
                                {
                                    var opt = new ComboxColumn.Option();
                                    opt.ControlName = item.Attributes["ControlName"].Value;
                                    opt.DisplayName = item.Attributes["DisplayName"].Value;
                                    ((ComboxColumn)col).Options.Add(opt);
                                }
                                break;

                            case "FlowModeSelection":
                                col = new FlowModeColumn()
                                {
                                    //IsReadOnly = step.Attributes["InputType"].Value == "ReadOnlySelection",
                                    IsEditable = false,
                                    ModuleName = step.Attributes["ModuleName"].Value,
                                    //Default = step.Attributes["Default"] != null ? step.Attributes["Default"].Value : "",
                                    Default = GetStringValueByName("PM",
                                        $"PM.{module}.RecipeConfig.{strAbc(node.Attributes["DisplayName"].Value)}.{strAbc(step.Attributes["DisplayName"].Value)}.Default"),
                                    ControlName = step.Attributes["ControlName"].Value,
                                    DisplayName = step.Attributes["DisplayName"].Value,
                                    UnitName = step.Attributes["UnitName"].Value,
                                    StringCellTemplate = "TemplateCellDisplay",
                                    StringCellEditingTemplate = "TemplateComboxEx",
                                    StringHeaderTemplate = "ParamTemplate",
                                    EnableConfig = step.Attributes["EnableConfig"] != null &&
                                                   Convert.ToBoolean(step.Attributes["EnableConfig"].Value),
                                    EnableTolerance = step.Attributes["EnableTolerance"] != null &&
                                                      Convert.ToBoolean(step.Attributes["EnableTolerance"].Value),
                                    IsEnable = defaultCellEnable,


                                };
                                var flowMode = step.SelectNodes("Item");
                                foreach (XmlNode item in flowMode)
                                {
                                    var opt = new ComboxColumn.Option
                                    {
                                        ControlName = item.Attributes["ControlName"].Value,
                                        DisplayName = item.Attributes["DisplayName"].Value
                                    };
                                    ((ComboxColumn)col).Options.Add(opt);
                                }
                                break;

                            case "LoopSelection":
                                col = new LoopComboxColumn()
                                {
                                    IsReadOnly = false,
                                    ModuleName = step.Attributes["ModuleName"].Value,
                                    ControlName = step.Attributes["ControlName"].Value,
                                    DisplayName = step.Attributes["DisplayName"].Value,
                                    StringCellTemplate = "TemplateCellDisplay",
                                    StringCellEditingTemplate = "TemplateComboxEx",
                                    StringHeaderTemplate = "ParamTemplate",
                                    EnableTolerance = step.Attributes["EnableTolerance"] != null &&
                                                      Convert.ToBoolean(step.Attributes["EnableTolerance"].Value),
                                    IsEnable = defaultCellEnable,
                                };
                                var options = step.SelectNodes("Item");
                                foreach (XmlNode item in options)
                                {
                                    var opt = new LoopComboxColumn.Option();
                                    opt.ControlName = item.Attributes["ControlName"].Value;
                                    opt.DisplayName = item.Attributes["DisplayName"].Value;
                                    ((LoopComboxColumn)col).Options.Add(opt);
                                }
                                
                                break;

                            case "PopSetting":
                                col = new PopSettingColumn()
                                {
                                    ModuleName = step.Attributes["ModuleName"].Value,
                                    ControlName = step.Attributes["ControlName"].Value,
                                    DisplayName = step.Attributes["DisplayName"].Value,
                                    UnitName = step.Attributes["UnitName"].Value,
                                    StringCellTemplate = "TemplateCellDisplay",
                                    StringCellEditingTemplate = "TemplatePopSetting",
                                    StringHeaderTemplate = "ParamTemplate",
                                    EnableConfig = step.Attributes["EnableConfig"] != null &&
                                                   Convert.ToBoolean(step.Attributes["EnableConfig"].Value),
                                    EnableTolerance = step.Attributes["EnableTolerance"] != null &&
                                                      Convert.ToBoolean(step.Attributes["EnableTolerance"].Value),
                                    IsEnable = defaultCellEnable,
                                };
                                break;
                        }
                    }

                    // 单元格是否可编辑？
                    var isCellEditable = step.Attributes["IsColumnEditable"]?.Value;
                    if (string.IsNullOrEmpty(isCellEditable) == false && isCellEditable.ToLower() == "false")
                        col.StringCellEditingTemplate = "";
                   
                    SetPermission(col, menuPermission);  // 设置本列权限
                    colExpander.ChildColumns.Add(col); // 将本列追加到Expander中
                    columns.Add(col);
                }
            }

            Columns = columns;

            var configs = new RecipeStep(null);
            nodes = doc.SelectNodes("TableRecipeFormat/ProcessConfig/Configs");
            foreach (XmlNode node in nodes)
            {
                var childNodes = node.SelectNodes("Config");
                foreach (XmlNode configNode in childNodes)
                {
                    switch (configNode.Attributes["InputType"].Value)
                    {

                        case "DoubleInput":
                            var config = new DoubleParam(GlobalDefs.TryParseToDouble(configNode.Attributes["Default"].Value))
                            {
                                Name = configNode.Attributes["ControlName"].Value,
                                DisplayName = configNode.Attributes["DisplayName"].Value,
                                EnableConfig = configNode.Attributes["EnableConfig"] != null && Convert.ToBoolean(configNode.Attributes["EnableConfig"].Value),
                                EnableTolerance = configNode.Attributes["EnableTolerance"] != null && Convert.ToBoolean(configNode.Attributes["EnableTolerance"].Value),
                            };
                            if (double.TryParse(configNode.Attributes["Max"].Value, out var max))
                            {
                                (config as DoubleParam).Maximun = max;
                            }
                            if (double.TryParse(configNode.Attributes["Min"].Value, out var min))
                            {
                                (config as DoubleParam).Minimun = min;
                            }
                            configs.Add(config);
                            break;

                    }
                }
            }
            Configs = configs;

            BrandConfig = GetConfig(doc.SelectNodes("TableRecipeFormat/BrandConfig/Configs"));
            OesConfig = GetConfig(doc.SelectNodes("TableRecipeFormat/OesConfig/Configs"));
            VatConfig = GetConfig(doc.SelectNodes("TableRecipeFormat/VatConfig/Configs"));
            FineTuningConfig = GetConfig(doc.SelectNodes("TableRecipeFormat/FineTuningConfig/Configs"));

            return Columns;
        }
        
        private RecipeStep GetConfig(XmlNodeList nodes)
        {
            var configs = new RecipeStep(null);
            foreach (XmlNode node in nodes)
            {
                var childNodes = node.SelectNodes("Config");
                foreach (XmlNode configNode in childNodes)
                {
                    switch (configNode.Attributes["InputType"].Value)
                    {
                        case "TextInput":
                            var text = new StringParam(configNode.Attributes["Default"].Value)
                            {
                                Name = configNode.Attributes["ControlName"].Value,
                                DisplayName = configNode.Attributes["DisplayName"].Value,
                            };
                            configs.Add(text);
                            break;

                        case "Set3RatioInput":
                            var param = new Sets3RatioParam()
                            {
                                Name = configNode.Attributes["ControlName"].Value,
                                DisplayName = configNode.Attributes["DisplayName"].Value
                            };
                            configs.Add(param);
                            break;

                        case "DoubleInput":
                            var config = new DoubleParam(GlobalDefs.TryParseToDouble(configNode.Attributes["Default"].Value))
                            {
                                Name = configNode.Attributes["ControlName"].Value,
                                DisplayName = configNode.Attributes["DisplayName"].Value,
                            };
                            if (double.TryParse(configNode.Attributes["Max"].Value, out var max))
                            {
                                (config as DoubleParam).Maximun = max;
                            }
                            if (double.TryParse(configNode.Attributes["Min"].Value, out var min))
                            {
                                (config as DoubleParam).Minimun = min;
                            }
                            configs.Add(config);
                            break;

                        case "ReadOnlySelection":

                            var col = new ComboxParam()
                            {
                                Name = configNode.Attributes["ControlName"].Value,
                                DisplayName = configNode.Attributes["DisplayName"].Value,
                                Value = configNode.Attributes["Default"] != null ? configNode.Attributes["Default"].Value : "",
                                Options = new ObservableCollection<ComboxColumn.Option>(),
                                IsEditable = configNode.Attributes["InputType"].Value != "ReadOnlySelection",
                                EnableTolerance = configNode.Attributes["EnableTolerance"] != null && Convert.ToBoolean(configNode.Attributes["EnableTolerance"].Value),

                            };
                            var items = configNode.SelectNodes("Item");
                            foreach (XmlNode item in items)
                            {
                                var opt = new ComboxColumn.Option
                                {
                                    ControlName = item.Attributes["ControlName"].Value,
                                    DisplayName = item.Attributes["DisplayName"].Value
                                };
                                col.Options.Add(opt);
                            }
                            col.Value = !string.IsNullOrEmpty(col.Value) ? col.Value : (col.Options.Count > 0 ? col.Options[0].ControlName : "");
                            configs.Add(col);
                            break;

                    }
                }
            }

            return configs;
        }

        public static void ApplyTemplate(UserControl uc, List<EditorDataGridTemplateColumnBase> columns)
        {
            columns.ToList().ForEach(col =>
            {
                if (string.IsNullOrEmpty(col.StringCellTemplate) == false)
                    col.CellTemplate = (DataTemplate)uc.FindResource(col.StringCellTemplate);

                if (string.IsNullOrEmpty(col.StringCellEditingTemplate) == false)
                    col.CellEditingTemplate = (DataTemplate)uc.FindResource(col.StringCellEditingTemplate);

                if (string.IsNullOrEmpty(col.StringHeaderTemplate) == false)
                    col.HeaderTemplate = (DataTemplate)uc.FindResource(col.StringHeaderTemplate);
            });
        }
        
        /// <summary>
        /// 设置列权限。
        /// </summary>
        /// <param name="column"></param>
        /// <param name="permission"></param>
        /// <returns></returns>
        public static void SetPermission(EditorDataGridTemplateColumnBase column, MenuPermission permission)
        {
            if (column == null)
                throw new ArgumentNullException(nameof(column));

            if (permission == null)
                throw new ArgumentNullException(nameof(permission));

            var mpKey = column.DisplayName.Replace(" ", "");
            column.Permission = permission.MenuPermissionDictionary.TryGetValue(mpKey, out var perm)
                ? perm
                : MenuPermissionEnum.MP_NONE;
        }
    }
}
