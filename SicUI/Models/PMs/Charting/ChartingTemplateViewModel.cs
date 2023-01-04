using Aitex.Common.Util;
using MECF.Framework.UI.Client.ClientBase;
using SciChart.Charting.Visuals.RenderableSeries;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace SicUI.Models.PMs.Charting
{
    public class ChartingTemplateViewModel : SicModuleUIViewModelBase, ISupportMultipleSystem
    {
        public class TemplateInfo
        { 
            public string TemplateName
            {
                get;set;
            }

            public string TemplateCreateTime
            {
                get;set;
            }
        }

        public ChartingTemplateViewModel()
        {
            TemplateInfos = new ObservableCollection<TemplateInfo>();
            LoadTemplateInfo();
        }

        public ObservableCollection<TemplateInfo> TemplateInfos { get; set; }
        
        public List<PMNodeInfo> CurrentDataInfo { get; set; }

        public TemplateInfo SelectTemplate { get; set; }

        private string _currentFileName;
        public string CurrentFileName
        {
            get => _currentFileName;
            set { _currentFileName = value; NotifyOfPropertyChange(nameof(CurrentFileName)); }
        }

        public void LoadTemplateInfo()
        {
            if (!Directory.Exists(PathManager.GetCfgDir() + @"ChartTemplate"))
            {
                Directory.CreateDirectory(PathManager.GetCfgDir() + @"ChartTemplate");
            }

            var path = PathManager.GetCfgDir() + $"ChartTemplate";
            string[] allFile = Directory.GetFiles(path, "*.template");

            if (TemplateInfos.Count > 0)
            {
                TemplateInfos.Clear();
            }
            for (int i = 0; i < allFile.Length; i++)
            {
                TemplateInfos.Add(new TemplateInfo()
                {
                    TemplateName = Path.GetFileNameWithoutExtension(allFile[i]),
                    TemplateCreateTime = File.GetCreationTime(allFile[i]).ToString("yyyy-MM-dd HH:mm:ss")
                }) ;
            }
        }

        public void SaveToTemplate()
        {            
            if (CurrentDataInfo != null && CurrentDataInfo.Count>0)
            {
                using (StreamWriter sw = new StreamWriter(PathManager.GetCfgDir() + @"ChartTemplate\" + CurrentFileName + ".template", false))
                {
                    foreach (PMNodeInfo node in CurrentDataInfo)
                    {
                        if (node.Selected)
                        {
                            sw.WriteLine(node.NodeStr);
                        }
                    }
                }
            }

            var temp = TemplateInfos.FirstOrDefault(x => x.TemplateName == CurrentFileName);
            if (temp == null)
            {
                TemplateInfos.Add(new TemplateInfo()
                {
                    TemplateName = CurrentFileName,
                    TemplateCreateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                });
            }
        }

        public void Load()
        {
            foreach(PMNodeInfo node in CurrentDataInfo)
            {
                node.Selected = false;
            }
            using (StreamReader sr = new StreamReader(PathManager.GetCfgDir() + @"ChartTemplate\" + SelectTemplate.TemplateName + ".template"))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    foreach (PMNodeInfo node in CurrentDataInfo)
                    {
                        if (node.NodeStr == line)
                        {
                            node.Selected = true;
                            break;
                        }
                    }
                }
            }
            this.TryClose(true);
        }

        public void Remove()
        {
            string curName = SelectTemplate.TemplateName;
            if (File.Exists(PathManager.GetCfgDir() + @"ChartTemplate\" + curName + ".template"))
            {
                File.Delete(PathManager.GetCfgDir() + @"ChartTemplate\" + curName + ".template");
                for (int i = 0; i < TemplateInfos.Count; i++)
                {
                    if (TemplateInfos[i].TemplateName == curName)
                    {
                        TemplateInfos.RemoveAt(i);
                    }
                }
            }

            
        }

      

    }
}
