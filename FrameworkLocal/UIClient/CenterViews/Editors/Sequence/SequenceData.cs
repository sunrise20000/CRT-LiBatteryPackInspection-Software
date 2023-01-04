using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Xml;
using Caliburn.Micro.Core;
using MECF.Framework.UI.Client.RecipeEditorLib.RecipeModel;
using MECF.Framework.UI.Client.RecipeEditorLib.RecipeModel.Params;
using RecipeEditorLib.DGExtension.CustomColumn;
using RecipeEditorLib.RecipeModel.Params;

namespace MECF.Framework.UI.Client.CenterViews.Editors.Sequence
{
    public class SequenceData : PropertyChangedBase
    {
        public SequenceData()
        {
            Steps = new RecipeStepCollection();
        }

        public RecipeStepCollection Steps { get; set; }

        public RecipeStep CreateStep(ObservableCollection<EditorDataGridTemplateColumnBase> columns, XmlNode step = null)
        {
            PositionParam posParam = null;
            var posValue = string.Empty;

            var Params = new RecipeStep(null);
            foreach (var col in columns)
            {
                Param param = null;
                var value = string.Empty;
                if (!(col is ExpanderColumn) && step != null && !(col is StepColumn))
                {
                    if (step.Attributes[col.ControlName] != null)
                        value = step.Attributes[col.ControlName].Value;
                }

                if (col is StepColumn)
                {
                    param = new StepParam()
                    {
                        Name = col.ControlName
                    };
                }
                else if (col is TextBoxColumn)
                {
                    param = new StringParam()
                    {
                        Name = col.ControlName,
                        Value = value,
                        IsEnabled = !col.IsReadOnly
                    };
                }
                else if (col is NumColumn)
                {
                    if (!string.IsNullOrEmpty(value))
                        param = new IntParam() { Name = col.ControlName, Value = int.Parse(value), IsEnabled = !col.IsReadOnly };
                    else
                        param = new IntParam() { Name = col.ControlName, Value = 0, IsEnabled = !col.IsReadOnly };
                }
                else if (col is ComboxColumn)
                {
                    param = new ComboxParam() { Name = col.ControlName,
                        Value = value, Options = ((ComboxColumn)col).Options, IsEnabled = !col.IsReadOnly };
                }
                else if (col is PositionColumn)
                {
                    posParam = new PositionParam() { Name = col.ControlName, Options = ((PositionColumn)col).Options, IsEnabled = !col.IsReadOnly };
                    param = posParam;
                    posValue = value;
                }
                else if (col is ExpanderColumn)
                {
                    param = new ExpanderParam() { };
                }
                else if (col is RecipeSelectColumn)
                {
                    var path = ((RecipeSelectColumn) col).RecipeProcessType;
                    if (!string.IsNullOrEmpty(path) && value.StartsWith(path))
                    {
                        value = value.Substring(path.Length + 1);
                    }

                    param = new PathFileParam()
                    {
                        Name = col.ControlName,
                        Value = value,
                        IsEnabled = !col.IsReadOnly,
                        PrefixPath = ((RecipeSelectColumn)col).RecipeProcessType
                    };
                }
                else if (col is MultipleSelectColumn)
                {
                    param = new MultipleSelectParam((MultipleSelectColumn)col) { Name = col.ControlName, IsEnabled = !col.IsReadOnly };
                    var opts = value.Split(',');
                    ((MultipleSelectParam)param).Options.Apply(opt => {
                        for(var index = 0; index < opts.Length; index++)
                        {
                            if (opt.ControlName == opts[index])
                            {
                                opt.IsChecked = true;
                                break;
                            }
                        }
                    });
                }

                param.IsSaved = true;
                param.Parent = Params;

                Params.Add(param);
            }

            posParam.Value = posValue;

            return Params;
        }

        public RecipeStep CloneStep(ObservableCollection<EditorDataGridTemplateColumnBase> columns, RecipeStep sourceStep)
        {
            var targetParams = this.CreateStep(columns);

            PositionParam posParam = null;
            var posValue = string.Empty;
            for (var index = 0; index < sourceStep.Count; index++)
            {
                if (sourceStep[index] is PositionParam)
                {
                    posParam = (PositionParam)targetParams[index];
                    posValue = ((PositionParam)sourceStep[index]).Value;
                }
                else if (sourceStep[index] is StringParam)
                {
                    ((StringParam)targetParams[index]).Value = ((StringParam)sourceStep[index]).Value;
                }
                else if (sourceStep[index] is PathFileParam)
                {
                    ((PathFileParam)targetParams[index]).Value = ((PathFileParam)sourceStep[index]).Value;
                    ((PathFileParam)targetParams[index]).PrefixPath = ((PathFileParam)sourceStep[index]).PrefixPath;
                }
                else if (sourceStep[index] is IntParam)
                {
                    ((IntParam)targetParams[index]).Value = ((IntParam)sourceStep[index]).Value;
                }
                else if (sourceStep[index] is ComboxParam)
                {
                    ((ComboxParam)targetParams[index]).Value = ((ComboxParam)sourceStep[index]).Value;
                }
                else if (sourceStep[index] is BoolParam)
                {
                    ((BoolParam)targetParams[index]).Value = ((BoolParam)sourceStep[index]).Value;
                }
                else if (sourceStep[index] is IntParam)
                {
                    ((IntParam)targetParams[index]).Value = ((IntParam)sourceStep[index]).Value;
                }
                else if (sourceStep[index] is DoubleParam)
                {
                    ((DoubleParam)targetParams[index]).Value = ((DoubleParam)sourceStep[index]).Value;
                }


                targetParams[index].Parent = targetParams;
            }
            posParam.Value = posValue;

            return targetParams;
        }

        public void LoadSteps(ObservableCollection<EditorDataGridTemplateColumnBase> _columns, XmlNodeList _steps)
        {
            foreach (XmlNode _step in _steps)
            {
                this.Steps.Add(this.CreateStep(_columns, _step));
            }
        }

        public void Load(ObservableCollection<EditorDataGridTemplateColumnBase> _columns, XmlDocument doc)
        {
            var node = doc.SelectSingleNode("Aitex/TableSequenceData");
            if (node.Attributes["CreatedBy"] != null)
                this.Creator = node.Attributes["CreatedBy"].Value;
            if (node.Attributes["CreationTime"] != null)
                this.CreateTime = DateTime.Parse(node.Attributes["CreationTime"].Value);
            if (node.Attributes["LastRevisedBy"] != null)
                this.Revisor = node.Attributes["LastRevisedBy"].Value;
            if (node.Attributes["LastRevisionTime"] != null)
                this.ReviseTime = DateTime.Parse(node.Attributes["LastRevisionTime"].Value);
            if (node.Attributes["Description"] != null)
                this.Description = node.Attributes["Description"].Value;

            foreach (XmlNode _step in doc.SelectNodes("Aitex/TableSequenceData/Step"))
            {
                this.Steps.Add(this.CreateStep(_columns, _step));
            }
        }

        public string ToXml()
        {
            var builder = new StringBuilder();
            builder.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            builder.Append(string.Format("<Aitex><TableSequenceData CreatedBy=\"{0}\" CreationTime=\"{1}\" LastRevisedBy=\"{2}\" LastRevisionTime=\"{3}\" Description=\"{4}\" >", this.Creator, this.CreateTime.ToString("yyyy-MM-dd HH:mm:ss"), this.Revisor, this.ReviseTime.ToString("yyyy-MM-dd HH:mm:ss"), this.Description));
            foreach (var parameters in Steps)
            {
                builder.Append("<Step ");
                foreach (var parameter in parameters)
                {
                    if (parameter.Visible == System.Windows.Visibility.Visible)
                    {
                        if (parameter is IntParam)
                            builder.Append(parameter.Name + "=\"" + ((IntParam)parameter).Value + "\" ");
                        else if (parameter is DoubleParam)
                            builder.Append(parameter.Name + "=\"" + ((DoubleParam)parameter).Value + "\" ");
                        else if (parameter is StringParam)
                            builder.Append(parameter.Name + "=\"" + ((StringParam)parameter).Value + "\" ");
                        else if (parameter is PathFileParam)
                            builder.Append(parameter.Name + "=\"" + ((PathFileParam)parameter).Value + "\" ");
                        else if (parameter is ComboxParam)
                            builder.Append(parameter.Name + "=\"" + ((ComboxParam)parameter).Value + "\" ");
                        else if (parameter is PositionParam)
                            builder.Append(parameter.Name + "=\"" + ((PositionParam)parameter).Value + "\" ");
                        else if (parameter is BoolParam)
                            builder.Append(parameter.Name + "=\"" + ((BoolParam)parameter).Value + "\" ");
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
            builder.Append("</TableSequenceData></Aitex>");
            return builder.ToString();
        }

        private string name;
        public string Name
        {
            get { return this.name; }
            set
            {
                this.name = value;
                this.NotifyOfPropertyChange("Name");
            }
        }

        private string creator;
        public string Creator
        {
            get { return this.creator; }
            set
            {
                this.creator = value;
                this.NotifyOfPropertyChange("Creator");
            }
        }

        private DateTime createTime;
        public DateTime CreateTime
        {
            get { return this.createTime; }
            set
            {
                this.createTime = value;
                this.NotifyOfPropertyChange("CreateTime");
            }
        }

        private string description;
        public string Description
        {
            get { return this.description; }
            set
            {
                this.description = value;
                this.NotifyOfPropertyChange("Description");
            }
        }

        private string devisor;
        public string Revisor
        {
            get { return this.devisor; }
            set
            {
                this.devisor = value;
                this.NotifyOfPropertyChange("Revisor");
            }
        }

        private DateTime deviseTime;
        public DateTime ReviseTime
        {
            get { return this.deviseTime; }
            set
            {
                this.deviseTime = value;
                this.NotifyOfPropertyChange("ReviseTime");
            }
        }

    }
}
