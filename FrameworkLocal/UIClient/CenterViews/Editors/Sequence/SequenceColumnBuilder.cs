﻿using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using MECF.Framework.Common.RecipeCenter;
using RecipeEditorLib.DGExtension.CustomColumn;

namespace MECF.Framework.UI.Client.CenterViews.Editors.Sequence
{
    public class SequenceColumnBuilder
    {
        public ObservableCollection<EditorDataGridTemplateColumnBase> Build()
        {
            var temp = RecipeClient.Instance.Service.GetSequenceFormatXml();
            var doc = new XmlDocument();
            doc.LoadXml(temp);

            var columns = new ObservableCollection<EditorDataGridTemplateColumnBase>();
            
            EditorDataGridTemplateColumnBase col;

            //step number
            col = new StepColumn()
            {
                DisplayName = "Step",
                ControlName = "Step",
                StringCellTemplate = "TemplateStep",
                StringHeaderTemplate = "ParamTemplate"
            };
            columns.Add(col);

            var nodes = doc.SelectNodes("TableSequenceFormat/Catalog/Item");
            foreach (XmlNode node in nodes)
            {
                switch (node.Attributes["InputType"].Value)
                {
                    case "TextInput":
                        col = new TextBoxColumn()
                        {
                            ControlName = node.Attributes["Name"].Value,
                            DisplayName = node.Attributes["DisplayName"].Value,
                            StringCellTemplate = "TemplateText",
                            StringHeaderTemplate = "ParamTemplate"
                        };
                        columns.Add(col);
                        break;
                    case "ReadOnly":
                        col = new TextBoxColumn()
                        {
                            ControlName = node.Attributes["Name"].Value,
                            DisplayName = node.Attributes["DisplayName"].Value,
                            StringCellTemplate = "TemplateText",
                            StringHeaderTemplate = "ParamTemplate"
                        };
                        columns.Add(col);
                        break;
                    case "NumInput":
                        col = new NumColumn()
                        {
                            ControlName = node.Attributes["Name"].Value,
                            DisplayName = node.Attributes["DisplayName"].Value,
                            //Minimun = int.Parse(node.Attributes["Min"].Value),
                            //Maximun = int.Parse(node.Attributes["Max"].Value),
                            StringCellTemplate = "TemplateSignInteger",
                            StringHeaderTemplate = "ParamTemplate"
                        };
                        columns.Add(col);
                        break;
                    case "RecipeSelection":
                        col = new RecipeSelectColumn()
                        {
                            IsReadOnly = true,
                            ControlName = node.Attributes["Name"].Value,
                            DisplayName = node.Attributes["DisplayName"].Value,
                            RecipeProcessType = node.Attributes["Parameter"].Value,
                            StringCellTemplate = "TemplateRecipeSelection",
                            StringHeaderTemplate = "ParamTemplate",
                        };
                        columns.Add(col);
                        break;
                    case "EditableSelection":
                    case "ReadOnlySelection":
                        if (node.Attributes["Name"].Value == "Position")
                        {
                            col = new PositionColumn()
                            {
                                ControlName = node.Attributes["Name"].Value,
                                DisplayName = node.Attributes["DisplayName"].Value,
                                StringCellTemplate = "TemplateCombox",
                                StringHeaderTemplate = "ParamTemplate"
                            };
                            var items = node.SelectNodes("Selection");
                            foreach (XmlNode item in items)
                            {
                                var opt = new ComboxColumn.Option();
                                opt.ControlName = item.Attributes["Name"].Value;
                                opt.DisplayName = item.Attributes["DisplayName"].Value;
                                if (item.Attributes["Parameter"] != null)
                                    opt.RelatedParameters = item.Attributes["Parameter"].Value;
                                ((PositionColumn)col).Options.Add(opt);
                            }
                        }
                        else
                        {
                            col = new ComboxColumn()
                            {
                                ControlName = node.Attributes["Name"].Value,
                                DisplayName = node.Attributes["DisplayName"].Value,
                                StringCellTemplate = "TemplateCombox",
                                StringHeaderTemplate = "ParamTemplate"
                            };
                            var items = node.SelectNodes("Selection");
                            foreach (XmlNode item in items)
                            {
                                var opt = new ComboxColumn.Option();
                                opt.ControlName = item.Attributes["Name"].Value;
                                opt.DisplayName = item.Attributes["DisplayName"].Value;
                                if (item.Attributes["Parameter"] != null)
                                    opt.RelatedParameters = item.Attributes["Parameter"].Value;
                                ((ComboxColumn)col).Options.Add(opt);
                            }
                        }

                        columns.Add(col);
                        break;
                    case "MultiSelection":
                        col = new MultipleSelectColumn()
                        {
                            ControlName = node.Attributes["Name"].Value,
                            DisplayName = node.Attributes["DisplayName"].Value,
                            StringCellTemplate = "TemplateMultiSelection",
                            StringHeaderTemplate = "ParamTemplate"
                        };
                        var multiitems = node.SelectNodes("Selection");
                        foreach (XmlNode item in multiitems)
                        {
                            var opt = new MultipleSelectColumn.Option();
                            opt.IsChecked = false;
                            opt.ControlName = item.Attributes["Name"].Value;
                            opt.DisplayName = item.Attributes["DisplayName"].Value;
                            if (item.Attributes["Parameter"] != null)
                                opt.RelatedParameters = item.Attributes["Parameter"].Value;
                            ((MultipleSelectColumn)col).Options.Add(opt);
                        }
                        columns.Add(col);
                        break;
                }
            }
            return columns;
        }

        public static void ApplyTemplate(UserControl uc, ObservableCollection<EditorDataGridTemplateColumnBase> columns)
        {
            columns.ToList().ForEach(col =>
            {
                col.CellTemplate = (DataTemplate)uc.FindResource(col.StringCellTemplate);
                col.HeaderTemplate = (DataTemplate)uc.FindResource(col.StringHeaderTemplate);
            });
        }
    }
}
