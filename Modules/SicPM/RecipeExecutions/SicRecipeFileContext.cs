using Aitex.Common.Util;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.RecipeCenter;
using Aitex.Core.RT.SCCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace SicPM.RecipeExecutions
{
    public class SicRecipeFileContext : IRecipeFileContext
    {
        public string GetRecipeDefiniton(string chamberId)
        {
            try
            {
                string recipeSchema = PathManager.GetCfgDir() + $@"\Recipe\{chamberId}\RecipeFormat.xml";

                XmlDocument xmlDom = new XmlDocument();
                xmlDom.Load(recipeSchema);

                //string[] gasNames = new string[6]
                //{
                //    SC.GetConfigItem("System.Source1.SourceName").StringValue,
                //    SC.GetConfigItem("System.Source2.SourceName").StringValue,
                //    SC.GetConfigItem("System.Source3.SourceName").StringValue,
                //    SC.GetConfigItem("System.Source4.SourceName").StringValue,
                //    SC.GetConfigItem("System.Gas5.GasName").StringValue,
                //    SC.GetConfigItem("System.Gas6.GasName").StringValue,
                //};

                //XmlElement xe = xmlDom.DocumentElement;
                //XmlElement selectXe = null;

                //for (int i = 1; i <= 2; i++)
                //{
                //    for (int j = 1; j <= 6; j++)
                //    {
                //        var path = GetSingleNodePath(i, j);
                //        selectXe = (XmlElement)xe.SelectSingleNode(path);
                //        selectXe.SetAttribute("DisplayName", gasNames[j - 1]);
                //    }
                //}

                //xmlDom.Save(recipeSchema);
                //xmlDom.Load(recipeSchema);
                return xmlDom.OuterXml;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return "";
            }
        }

        public string GetSingleNodePath(int sourceIndex, int gasIndex)
        {
            return $"/Aitex/TableRecipeFormat/Catalog[@DisplayName=\"Source{sourceIndex}\"]/Group/Step/Item[@ControlName=\"Gas{gasIndex}\"]";
        }

        public IEnumerable<string> GetRecipes(string path, bool includingUsedRecipe)
        {
            try
            {
                var recipes = new List<string>();
                string recipePath = PathManager.GetRecipeDir() + path + "\\";

                if (!Directory.Exists(recipePath))
                    return recipes;

                var di = new DirectoryInfo(recipePath);
                var fis = di.GetFiles("*.rcp", SearchOption.AllDirectories);


                foreach (var fi in fis)
                {
                    string str = fi.FullName.Substring(di.FullName.Length);
                    str = str.Substring(0, str.LastIndexOf('.'));
                    if (includingUsedRecipe || (!includingUsedRecipe && !str.Contains("HistoryRecipe\\")))
                    {
                        recipes.Add(str);
                    }
                }
                return recipes;
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
                return new List<string>();
            }
        }

        public void PostInfoEvent(string message)
        {
            EV.PostMessage("System", EventEnum.GeneralInfo, message);
        }

        public void PostWarningEvent(string message)
        {
            EV.PostMessage("System", EventEnum.DefaultWarning, message);
        }

        public void PostAlarmEvent(string message)
        {
            EV.PostMessage("System", EventEnum.DefaultAlarm, message);
        }

        public void PostDialogEvent(string message)
        {
            EV.PostNotificationMessage(message);
        }

        public void PostInfoDialogMessage(string message)
        {
            EV.PostMessage("System", EventEnum.GeneralInfo, message);

            EV.PostPopDialogMessage(EventLevel.Information, "System Information", message);
        }

        public void PostWarningDialogMessage(string message)
        {
            EV.PostMessage("System", EventEnum.GeneralInfo, message);

            EV.PostPopDialogMessage(EventLevel.Warning, "System Warning", message);
        }

        public void PostAlarmDialogMessage(string message)
        {
            EV.PostMessage("System", EventEnum.GeneralInfo, message);

            EV.PostPopDialogMessage(EventLevel.Alarm, "System Alarm", message);
        }

        public string GetRecipeTemplate(string chamberId)
        {
            string schema = GetRecipeDefiniton(chamberId);

            XmlDocument dom = new XmlDocument();

            dom.LoadXml(schema);

            XmlNode nodeTemplate = dom.SelectSingleNode("/Aitex/TableRecipeData");

            return nodeTemplate.OuterXml;
        }
    }
}
