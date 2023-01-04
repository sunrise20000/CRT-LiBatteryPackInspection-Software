using Aitex.Common.Util;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.RecipeCenter;
using Aitex.Core.RT.SCCore;
using SicPM.RecipeExecutions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using static SicPM.PmDevices.DicMode;

namespace SicPM.RecipeRoutine
{
    public class RecipeParser
    {
        public static bool Parse(string recipeFile, string module, out RecipeHead recipeHead, out List<RecipeStep> recipeData, out string reason)
        {
            reason = string.Empty;
            recipeHead = new RecipeHead();
            recipeData = new List<RecipeStep>();

            string content = RecipeFileManager.Instance.LoadRecipe("", recipeFile, false);
            string chamberType = SC.GetConfigItem($"{module}.ChamberType")?.StringValue;

            if (string.IsNullOrEmpty(content))
            {
                reason = $"{recipeFile} is not a valid recipe file";
                return false;
            }

            try
            {
                //获取工艺程序文件中允许的命令字符串列表
                //目的：如果工艺程序文件中含有规定之外的命令，则被禁止执行
                HashSet<string> recipeAllowedCommands = new HashSet<string>();
                XmlDocument rcpFormatDoc = new XmlDocument();
                string recipeSchema = PathManager.GetCfgDir() + @"\Recipe\Sic\Routine\RecipeFormat.xml";
                rcpFormatDoc.Load(recipeSchema);
                XmlNodeList rcpItemNodeList = rcpFormatDoc.SelectNodes("/Aitex/TableRecipeFormat/Catalog/Group/Step");
                foreach (XmlElement item in rcpItemNodeList)
                    recipeAllowedCommands.Add(item.Attributes["ControlName"].Value);

                //获取工艺程序文件中所有步的内容
                XmlDocument rcpDataDoc = new XmlDocument();
                rcpDataDoc.LoadXml(content);

                XmlNode nodeModule = rcpDataDoc.SelectSingleNode($"/Aitex/TableRecipeData/Module[@Name='{module}']");
                if (nodeModule == null)
                {
                    reason = $"Recipe file does not contains step content for {module}";
                    return false;
                }

                XmlElement nodeConfig = nodeModule.SelectSingleNode($"Config") as XmlElement;
                recipeHead.ProcessPurgeCount = (nodeConfig != null && nodeConfig.HasAttribute("StepNo")) ? nodeConfig.Attributes["StepNo"].Value : "";

                XmlNodeList stepNodeList = nodeModule.SelectNodes($"Step");
                for (int i = 0; i < stepNodeList.Count; i++)
                {
                    var recipeStep = new RecipeStep();
                    recipeData.Add(recipeStep);

                    XmlElement stepNode = stepNodeList[i] as XmlElement;
                    Dictionary<string, string> dic = new Dictionary<string, string>();

                    //遍历Step节点
                    foreach (XmlAttribute att in stepNode.Attributes)
                    {
                        if (recipeAllowedCommands.Contains(att.Name))
                        {
                            dic.Add(att.Name, att.Value);
                        }
                    }
                    //遍历Step子节点中所有的attribute属性节点
                    foreach (XmlElement subStepNode in stepNode.ChildNodes)
                    {
                        foreach (XmlAttribute att in subStepNode.Attributes)
                        {
                            if (recipeAllowedCommands.Contains(att.Name))
                            {
                                dic.Add(att.Name, att.Value);
                            }
                        }
                        //遍历Step子节点的子节点中所有的attribute属性节点
                        foreach (XmlElement subsubStepNode in subStepNode.ChildNodes)
                        {
                            foreach (XmlAttribute att in subsubStepNode.Attributes)
                            {
                                if (recipeAllowedCommands.Contains(att.Name))
                                {
                                    dic.Add(att.Name, att.Value);
                                }
                            }
                        }
                    }

                    #region Modify dic

                    recipeStep.StepName = dic["Name"];
                    recipeStep.StepTime = Convert.ToDouble(dic["Time"]);
                    //转换成enum
                    recipeStep.EndBy = (EnumEndByCondition)Enum.Parse(typeof(EnumEndByCondition),dic["EndBy"]);

                    recipeStep.LoopCount = 0;
                    recipeStep.IsLoopStartStep = false;
                    recipeStep.IsLoopEndStep = false;

                    //recipeStep.IsLoopStartStep = System.Text.RegularExpressions.Regex.Match(loopStr, @"Loop x\d+\s*$").Success;
                    //recipeStep.IsLoopEndStep = System.Text.RegularExpressions.Regex.Match(loopStr, @"Loop x\d+\s* End$").Success;
                    //if (recipeStep.IsLoopStartStep)
                    //    recipeStep.LoopCount = Convert.ToInt32(loopStr.Replace("Loop x", string.Empty));
                    //else
                    //    recipeStep.LoopCount = 0;


                    int.TryParse(dic["LoopTimes"], out recipeStep.LoopCount);

                    if (dic.ContainsKey("LoopStart"))
                    {
                        bool.TryParse(dic["LoopStart"], out recipeStep.IsLoopStartStep);
                    }

                    if (dic.ContainsKey("LoopEnd"))
                    {
                        bool.TryParse(dic["LoopEnd"], out recipeStep.IsLoopEndStep);
                    }

                    //特殊处理
                    if (dic.ContainsKey("V33.GVTurnValve"))
                    {
                        if (dic["V33.GVTurnValve"] == "H2")
                        {
                            dic["V33.GVTurnValve"] = "True";
                        }
                        else if (dic["V33.GVTurnValve"] == "Ar")
                        {
                            dic["V33.GVTurnValve"] = "False";
                        }
                    }

                    if (dic.ContainsKey("V64.GVTurnValve"))
                    {
                        if (dic["V64.GVTurnValve"] == "H2")
                        {
                            dic["V64.GVTurnValve"] = "True";
                        }
                        else if (dic["V64.GVTurnValve"] == "Close")
                        {
                            dic["V64.GVTurnValve"] = "False";
                        }
                    }

                    if (dic.ContainsKey("V65.GVTurnValve"))
                    {
                        if (dic["V65.GVTurnValve"] == "Ar")
                        {
                            dic["V65.GVTurnValve"] = "True";
                        }
                        else if (dic["V65.GVTurnValve"] == "Close")
                        {
                            dic["V65.GVTurnValve"] = "False";
                        }
                    }

                    if (dic.ContainsKey("V68.GVTurnValve"))
                    {
                        if (dic["V68.GVTurnValve"] == "Ar")
                        {
                            dic["V68.GVTurnValve"] = "True";
                        }
                        else if (dic["V68.GVTurnValve"] == "Close")
                        {
                            dic["V68.GVTurnValve"] = "False";
                        }
                    }

                    dic = dic.ToDictionary(k => k.Key, v => v.Value == "Open" ? "True" : (v.Value == "Close" ? "False" : v.Value)); // 阀门
                    dic = dic.ToDictionary(k => k.Key, v => v.Value == "Enable" ? "True" : (v.Value == "Disable" ? "False" : v.Value)); // Enable

                    // 蝶阀
                    if (dic["TV.SetMode"] == "Position")
                    {
                        dic["TV.SetMode"] = PressureCtrlMode.TVPositionCtrl.ToString();
                    }
                    else if (dic["TV.SetMode"] == "Pressure")
                    {
                        dic["TV.SetMode"] = PressureCtrlMode.TVPressureCtrl.ToString();
                    }
                    else if (dic["TV.SetMode"] == "Close")
                    {
                        dic["TV.SetMode"] = PressureCtrlMode.TVClose.ToString();
                    }
                    else
                    {
                        dic["TV.SetMode"] = "Hold";
                    }

                    // PSU and SCR
                    HeaterControlMode PSUControlMode = (HeaterControlMode)Enum.Parse(typeof(HeaterControlMode), dic["TC1.SetHeaterMode"]);
                    HeaterControlMode SCRControlMode = (HeaterControlMode)Enum.Parse(typeof(HeaterControlMode), dic["TC2.SetHeaterMode2"]);
                    dic["TC1.SetHeaterMode"] = ((int)PSUControlMode).ToString();
                    dic["TC2.SetHeaterMode2"] = ((int)SCRControlMode).ToString();
                    
                    if (PSUControlMode == HeaterControlMode.Power)
                    {
                        dic.Remove("TC1.SetL1TargetSP");
                        dic.Remove("TC1.SetL2TargetSP");
                        dic.Remove("TC1.SetL3TargetSP");
                    }
                    if (SCRControlMode == HeaterControlMode.Power)
                    {
                        dic.Remove("TC2.SetL3TargetSP");
                    }

                    dic.Remove("StepNo");
                    dic.Remove("Name");
                    dic.Remove("EndBy");
                    dic.Remove("Time");
                    //移除Loop相关
                    dic.Remove("LoopStart");
                    dic.Remove("LoopEnd");
                    dic.Remove("LoopTimes");

                    foreach (string key in dic.Keys)
                    {
                        if(dic[key] != "Hold" && dic[key] !="Hold")
                        {
                            recipeStep.RecipeCommands.Add(key, dic[key]);
                        }
                    }
                        
                    #endregion
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);

                reason = $"Recipe file content not valid, {recipeFile}, {ex.Message}";
                return false;
            }
            return true;
        }
    }
}
