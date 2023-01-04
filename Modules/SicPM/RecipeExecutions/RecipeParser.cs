using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;
using Aitex.Common.Util;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.RecipeCenter;
using Aitex.Core.RT.SCCore;
using static SicPM.PmDevices.DicMode;

namespace SicPM.RecipeExecutions
{
    public class RecipeDataColumn
    {
        public string disPlay { get; set; }
        public double maxNum { get; set; }
        public double minMum { get; set; }
    }

    public class RecipeParser
    {
        public static bool Parse(string recipeFile, string module, out RecipeHead recipeHead, out List<RecipeStep> recipeData, out string reason)
        {
            reason = string.Empty;
            recipeHead = new RecipeHead();
            recipeData = new List<RecipeStep>();

            string content = RecipeFileManager.Instance.LoadRecipe("", recipeFile, false);

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
                string recipeSchema = PathManager.GetCfgDir() + @"\Recipe\Sic\Process\RecipeFormat.xml";
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

                    recipeStep.StepName = dic["Name"];
                    recipeStep.StepTime = Convert.ToDouble(dic["Time"]);
                    recipeStep.EndBy = EnumEndByCondition.ByTime;

                    if (dic.ContainsKey("Loop"))
                    {
                        string loopStr = dic["Loop"];
                        recipeStep.IsLoopStartStep = System.Text.RegularExpressions.Regex.Match(loopStr, @"Loop\x20\d+\s*$").Success;
                        recipeStep.IsLoopEndStep = System.Text.RegularExpressions.Regex.Match(loopStr, @"Loop End$").Success;
                        if (recipeStep.IsLoopStartStep)
                            recipeStep.LoopCount = Convert.ToInt32(loopStr.Replace("Loop", string.Empty));
                        else
                            recipeStep.LoopCount = 0;
                    }

                    //if (recipeData.Count >= 2)
                    //{
                    //    RecipeStep dummyStep = GetDummyStep(recipeData[recipeData.Count - 2].RecipeCommands, dic, module);

                    //    if (dummyStep.RecipeCommands.Count > 0)
                    //    {
                    //        dummyStep.IsDummyStep = true;
                    //        dummyStep.StepTime = 1; // 默认1秒
                    //        dummyStep.EndBy = EnumEndByCondition.ByTime;
                    //        recipeData.Insert(recipeData.Count - 1, dummyStep);
                    //    }
                    //}

                    #region Remove

                    dic.Remove("StepNo");
                    dic.Remove("Name");
                    dic.Remove("Time");
                    //Recipe Step去除Mfc30.Ramp
                    dic.Remove("Mfc30.Ramp");

                    HeaterControlMode PSUControlMode = (HeaterControlMode)Enum.Parse(typeof(HeaterControlMode), dic["TC1.SetHeaterMode"]);
                    HeaterControlMode SCRControlMode = (HeaterControlMode)Enum.Parse(typeof(HeaterControlMode), dic["TC2.SetHeaterMode2"]);

                    dic["TC1.SetHeaterMode"] = ((int)PSUControlMode).ToString();
                    dic["TC2.SetHeaterMode2"] = ((int)SCRControlMode).ToString();

                    //if (PSUControlMode == HeaterControlMode.Pyro)
                    //{
                    //    dic.Remove("TC1.RecipeSetL1Ratio");
                    //    dic.Remove("TC1.RecipeSetL2Ratio");
                    //    dic.Remove("TC1.RecipeSetL3Ratio");
                    //}
                    //else if (PSUControlMode == HeaterControlMode.TC)
                    //{
                    //    dic.Remove("TC1.RecipeSetL1Ratio");
                    //    dic.Remove("TC1.RecipeSetL2Ratio");
                    //    dic.Remove("TC1.RecipeSetL3Ratio");

                    //    dic.Remove("TC1.SetL1TargetSP");
                    //    dic.Remove("TC1.SetL3TargetSP");
                    //}

                    //Power模式不在移除TargetSP,也需要设置
                    //if (PSUControlMode == HeaterControlMode.Power)
                    //{
                    //    dic.Remove("TC1.SetL1TargetSP");
                    //    dic.Remove("TC1.SetL2TargetSP");
                    //    dic.Remove("TC1.SetL3TargetSP");
                    //}

                    //if (SCRControlMode == HeaterControlMode.Pyro)
                    //{
                    //    dic.Remove("TC2.RecipeSetL1Ratio");
                    //    dic.Remove("TC2.RecipeSetL2Ratio");
                    //    dic.Remove("TC2.RecipeSetL3Ratio");
                    //}
                    //else if (SCRControlMode == HeaterControlMode.TC)
                    //{
                    //    dic.Remove("TC2.RecipeSetL1Ratio");
                    //    dic.Remove("TC2.RecipeSetL2Ratio");
                    //    dic.Remove("TC2.RecipeSetL3Ratio");
                    //}
                    if (SCRControlMode == HeaterControlMode.Power)
                    {
                        dic.Remove("TC2.SetL3TargetSP");
                    }

                    //if (dic["GRPurgeHCI"] == "Disable")
                    //{
                    //    dic.Remove("Mfc30.Ramp");
                    //}

                    dic.Remove("SHTotalFlow");
                    dic.Remove("SHTotalFlowSplitRatio");
                    //dic.Remove("SiSourTotalFlow");
                    dic.Remove("SiSourSplitRatio");
                    //dic.Remove("CSourTotalFlow");
                    dic.Remove("CSourSplitRatio");
                    //dic.Remove("DopeTotalFlow");
                    dic.Remove("N2ActualFlow");
                    dic.Remove("DopeSplitRatio");

                    dic.Remove("SHSuppTotalFlow");
                    dic.Remove("SHInnerFlow");
                    dic.Remove("SHMidFlow");
                    dic.Remove("SHOutterFlow");
                    //dic.Remove("GRPurgeHCI");
                    //dic.Remove("TotalVentFlow");

                    #endregion

                    foreach (string key in dic.Keys)
                        recipeStep.RecipeCommands.Add(key, dic[key]);
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


        public static bool ParseXmlString(string xmlRecipeContent, string module, out RecipeHead recipeHead, out List<RecipeStep> recipeData, out string reason)
        {
            reason = string.Empty;
            recipeHead = new RecipeHead();
            recipeData = new List<RecipeStep>();

            var content = xmlRecipeContent;

            if (string.IsNullOrEmpty(content))
            {
                reason = $"Recipe content is empty";
                return false;
            }

            try
            {
                //获取工艺程序文件中允许的命令字符串列表
                //目的：如果工艺程序文件中含有规定之外的命令，则被禁止执行
                var recipeAllowedCommands = new HashSet<string>();
                var rcpFormatDoc = new XmlDocument();
                var recipeSchema = PathManager.GetCfgDir() + @"\Recipe\Sic\Process\RecipeFormat.xml";
                rcpFormatDoc.Load(recipeSchema);
                var rcpItemNodeList = rcpFormatDoc.SelectNodes("/Aitex/TableRecipeFormat/Catalog/Group/Step");
                foreach (XmlElement item in rcpItemNodeList)
                    recipeAllowedCommands.Add(item.Attributes["ControlName"].Value);

                //获取工艺程序文件中所有步的内容
                var rcpDataDoc = new XmlDocument();
                rcpDataDoc.LoadXml(content);

                var nodeModule = rcpDataDoc.SelectSingleNode($"/Aitex/TableRecipeData/Module[@Name='{module}']");
                if (nodeModule == null)
                {
                    reason = $"Recipe file does not contains step content for {module}";
                    return false;
                }

                var nodeConfig = nodeModule.SelectSingleNode($"Config") as XmlElement;
                recipeHead.ProcessPurgeCount = (nodeConfig != null && nodeConfig.HasAttribute("StepNo")) ? nodeConfig.Attributes["StepNo"].Value : "";

                var stepNodeList = nodeModule.SelectNodes($"Step");
                for (var i = 0; i < stepNodeList.Count; i++)
                {
                    var recipeStep = new RecipeStep();
                    recipeData.Add(recipeStep);

                    var stepNode = stepNodeList[i] as XmlElement;
                    var dic = new Dictionary<string, string>();

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

                    recipeStep.StepName = dic["Name"];
                    recipeStep.StepTime = Convert.ToDouble(dic["Time"]);
                    recipeStep.EndBy = EnumEndByCondition.ByTime;

                    if (dic.ContainsKey("Loop"))
                    {
                        var loopStr = dic["Loop"];
                        recipeStep.IsLoopStartStep = System.Text.RegularExpressions.Regex.Match(loopStr, @"Loop\x20\d+\s*$").Success;
                        recipeStep.IsLoopEndStep = System.Text.RegularExpressions.Regex.Match(loopStr, @"Loop End$").Success;
                        if (recipeStep.IsLoopStartStep)
                            recipeStep.LoopCount = Convert.ToInt32(loopStr.Replace("Loop", string.Empty));
                        else
                            recipeStep.LoopCount = 0;
                    }

                    //if (recipeData.Count >= 2)
                    //{
                    //    RecipeStep dummyStep = GetDummyStep(recipeData[recipeData.Count - 2].RecipeCommands, dic, module);

                    //    if (dummyStep.RecipeCommands.Count > 0)
                    //    {
                    //        dummyStep.IsDummyStep = true;
                    //        dummyStep.StepTime = 1; // 默认1秒
                    //        dummyStep.EndBy = EnumEndByCondition.ByTime;
                    //        recipeData.Insert(recipeData.Count - 1, dummyStep);
                    //    }
                    //}

                    #region Remove

                    dic.Remove("StepNo");
                    dic.Remove("Name");
                    dic.Remove("Time");
                    //Recipe Step去除Mfc30.Ramp
                    dic.Remove("Mfc30.Ramp");

                    var PSUControlMode = (HeaterControlMode)Enum.Parse(typeof(HeaterControlMode), dic["TC1.SetHeaterMode"]);
                    var SCRControlMode = (HeaterControlMode)Enum.Parse(typeof(HeaterControlMode), dic["TC2.SetHeaterMode2"]);

                    dic["TC1.SetHeaterMode"] = ((int)PSUControlMode).ToString();
                    dic["TC2.SetHeaterMode2"] = ((int)SCRControlMode).ToString();

                    //if (PSUControlMode == HeaterControlMode.Pyro)
                    //{
                    //    dic.Remove("TC1.RecipeSetL1Ratio");
                    //    dic.Remove("TC1.RecipeSetL2Ratio");
                    //    dic.Remove("TC1.RecipeSetL3Ratio");
                    //}
                    //else if (PSUControlMode == HeaterControlMode.TC)
                    //{
                    //    dic.Remove("TC1.RecipeSetL1Ratio");
                    //    dic.Remove("TC1.RecipeSetL2Ratio");
                    //    dic.Remove("TC1.RecipeSetL3Ratio");

                    //    dic.Remove("TC1.SetL1TargetSP");
                    //    dic.Remove("TC1.SetL3TargetSP");
                    //}

                    //Power模式不在移除TargetSP,也需要设置
                    //if (PSUControlMode == HeaterControlMode.Power)
                    //{
                    //    dic.Remove("TC1.SetL1TargetSP");
                    //    dic.Remove("TC1.SetL2TargetSP");
                    //    dic.Remove("TC1.SetL3TargetSP");
                    //}

                    //if (SCRControlMode == HeaterControlMode.Pyro)
                    //{
                    //    dic.Remove("TC2.RecipeSetL1Ratio");
                    //    dic.Remove("TC2.RecipeSetL2Ratio");
                    //    dic.Remove("TC2.RecipeSetL3Ratio");
                    //}
                    //else if (SCRControlMode == HeaterControlMode.TC)
                    //{
                    //    dic.Remove("TC2.RecipeSetL1Ratio");
                    //    dic.Remove("TC2.RecipeSetL2Ratio");
                    //    dic.Remove("TC2.RecipeSetL3Ratio");
                    //}
                    if (SCRControlMode == HeaterControlMode.Power)
                    {
                        dic.Remove("TC2.SetL3TargetSP");
                    }

                    //if (dic["GRPurgeHCI"] == "Disable")
                    //{
                    //    dic.Remove("Mfc30.Ramp");
                    //}

                    dic.Remove("SHTotalFlow");
                    dic.Remove("SHTotalFlowSplitRatio");
                    //dic.Remove("SiSourTotalFlow");
                    dic.Remove("SiSourSplitRatio");
                    //dic.Remove("CSourTotalFlow");
                    dic.Remove("CSourSplitRatio");
                    //dic.Remove("DopeTotalFlow");
                    dic.Remove("N2ActualFlow");
                    dic.Remove("DopeSplitRatio");

                    dic.Remove("SHSuppTotalFlow");
                    dic.Remove("SHInnerFlow");
                    dic.Remove("SHMidFlow");
                    dic.Remove("SHOutterFlow");
                    //dic.Remove("GRPurgeHCI");
                    //dic.Remove("TotalVentFlow");

                    #endregion

                    foreach (var key in dic.Keys)
                        recipeStep.RecipeCommands.Add(key, dic[key]);
                }
            }
            catch (Exception ex)
            {
                LOG.Write(ex);

                reason = $"Recipe content not valid, {ex.Message}";
                return false;
            }
            return true;
        }

        /// <summary>
        /// Check Run Vent Purge Switch, Add Dummy Step
        /// </summary>
        /// <param name="previousStep"></param>
        /// <param name="currentStep"></param>
        /// <returns></returns>
        private static RecipeStep GetDummyStep(Dictionary<string, string> previousStep, Dictionary<string, string> currentStep, string module)
        {
            RecipeStep step = new RecipeStep();
            List<string> lstFlowMode = new List<string>() { FlowMode.Run.ToString(), FlowMode.Vent.ToString() };
            bool isTotalVentFlowRunSwitch = false;

            #region Flow Mode

            string previousSiH4FlowMode = previousStep["SiH4.SetValve"];
            string previousTCSFlowMode = previousStep["TCS.SetValve"];
            string previousHClFlowMode = previousStep["HCl.SetValve"];
            string previousC2H4FlowMode = previousStep["C2H4.SetValve"];
            string previousN2FlowMode = previousStep["N2Dilution.SetValve"];
            string previousN2HighFlowMode = previousStep["N2HighDoping.SetValve"];
            string previousTMAFlowMode = previousStep["TMA.SetValve"];

            string currentSiH4FlowMode = currentStep["SiH4.SetValve"];
            string currentTCSFlowMode = currentStep["TCS.SetValve"];
            string currentHClFlowMode = currentStep["HCl.SetValve"];
            string currentC2H4FlowMode = currentStep["C2H4.SetValve"];
            string currentN2FlowMode = currentStep["N2Dilution.SetValve"];
            string currentN2HighFlowMode = currentStep["N2HighDoping.SetValve"];
            string currentTMAFlowMode = currentStep["TMA.SetValve"];

            #endregion

            #region Check Si Source

            double previousSiSourTotalFlow = Convert.ToDouble(previousStep["SiSourTotalFlow"]);
            double previousTCSBubbHighFlow = Convert.ToDouble(previousStep["Mfc10.Ramp"]);
            double previousTCSBubbLowFlow = Convert.ToDouble(previousStep["Mfc11.Ramp"]);
            double previousTCSPushFlow = Convert.ToDouble(previousStep["Mfc12.Ramp"]);
            double previousHClFlow = Convert.ToDouble(previousStep["Mfc13.Ramp"]);
            double previousSiH4Flow = Convert.ToDouble(previousStep["Mfc14.Ramp"]);

            double TCSFlow = 0;
            double TCS_Eff = SC.GetValue<double>($"PM.{module}.Efficiency.TCS-Eff");
            if (currentTCSFlowMode == FlowMode.Run.ToString() || currentTCSFlowMode == FlowMode.Vent.ToString())
                TCSFlow = (previousTCSBubbHighFlow + previousTCSBubbLowFlow) * TCS_Eff + previousTCSPushFlow;
            else if (currentTCSFlowMode == FlowMode.Purge.ToString())
                TCSFlow = previousTCSBubbHighFlow + previousTCSBubbLowFlow + previousTCSPushFlow;

            bool isSiSourRunVentSwitch = false;

            if ((previousSiH4FlowMode != currentSiH4FlowMode) && lstFlowMode.Contains(previousSiH4FlowMode) && lstFlowMode.Contains(currentSiH4FlowMode))
            {
                isSiSourRunVentSwitch = true;
                isTotalVentFlowRunSwitch = true;
                step.RecipeCommands.Add("SiH4.SetValve", currentSiH4FlowMode);
            }

            if ((previousTCSFlowMode != currentTCSFlowMode) && lstFlowMode.Contains(previousTCSFlowMode) && lstFlowMode.Contains(currentTCSFlowMode))
            {
                isSiSourRunVentSwitch = true;
                isTotalVentFlowRunSwitch = true;
                step.RecipeCommands.Add("TCS.SetValve", currentTCSFlowMode);
            }

            if ((previousHClFlowMode != currentHClFlowMode) && lstFlowMode.Contains(previousHClFlowMode) && lstFlowMode.Contains(currentHClFlowMode))
            {
                isSiSourRunVentSwitch = true;
                isTotalVentFlowRunSwitch = true;
                step.RecipeCommands.Add("HCl.SetValve", currentHClFlowMode);
            }

            //if (isSiSourRunVentSwitch)
            //{
            //    double startM9Flow = previousSiSourTotalFlow - (currentSiH4FlowMode == FlowMode.Run.ToString() ? previousSiH4Flow : 0)
            //        - (currentTCSFlowMode == FlowMode.Run.ToString() ? TCSFlow : 0) - (currentHClFlowMode == FlowMode.Run.ToString() ? previousHClFlow : 0);

            //    step.RecipeCommands.Add("Mfc9.Ramp", startM9Flow.ToString("F1"));
            //}

            #endregion

            #region Check C Source

            double previousCSourTotalFlow = Convert.ToDouble(previousStep["CSourTotalFlow"]);
            double previousC2H4Flow = Convert.ToDouble(previousStep["Mfc16.Ramp"]);

            bool isCSourRunVentSwitch = false;

            if ((previousC2H4FlowMode != currentC2H4FlowMode) && lstFlowMode.Contains(previousC2H4FlowMode) && lstFlowMode.Contains(currentC2H4FlowMode))
            {
                isCSourRunVentSwitch = true;
                isTotalVentFlowRunSwitch = true;
                step.RecipeCommands.Add("C2H4.SetValve", currentC2H4FlowMode);
            }

            //if (isCSourRunVentSwitch)
            //{
            //    double startM15Flow = previousCSourTotalFlow - (currentC2H4FlowMode == FlowMode.Run.ToString() ? previousC2H4Flow : 0);

            //    step.RecipeCommands.Add("Mfc15.Ramp", startM15Flow.ToString("F1"));
            //}

            #endregion

            #region Check Dope

            double previousDopeTotalFlow = Convert.ToDouble(previousStep["DopeTotalFlow"]);
            double previousN2HighFlow = Convert.ToDouble(previousStep["Mfc5.Ramp"]);
            double previousDilutedN2Flow = Convert.ToDouble(previousStep["Mfc6.Ramp"]);
            double previousTMABubbFlow = Convert.ToDouble(previousStep["Mfc7.Ramp"]);
            double previousTMAPushFlow = Convert.ToDouble(previousStep["Mfc8.Ramp"]);

            double TMAFlow = 0;
            double TMA_Eff = SC.GetValue<double>($"PM.{module}.Efficiency.TMA-Eff");
            if (currentTMAFlowMode == FlowMode.Run.ToString() || currentTMAFlowMode == FlowMode.Vent.ToString())
                TMAFlow = previousTMABubbFlow * TMA_Eff + previousTMAPushFlow;
            else if (currentTMAFlowMode == FlowMode.Purge.ToString())
                TMAFlow = previousTMABubbFlow + previousTMAPushFlow;

            bool isDopeRunVentSwitch = false;

            if ((previousN2FlowMode != currentN2FlowMode) && lstFlowMode.Contains(previousN2FlowMode) && lstFlowMode.Contains(currentN2FlowMode))
            {
                isDopeRunVentSwitch = true;
                isTotalVentFlowRunSwitch = true;
                step.RecipeCommands.Add("N2Dilution.SetValve", currentN2FlowMode);
            }

            if ((previousN2HighFlowMode != currentN2HighFlowMode) && lstFlowMode.Contains(previousN2HighFlowMode) && lstFlowMode.Contains(currentN2HighFlowMode))
            {
                isDopeRunVentSwitch = true;
                isTotalVentFlowRunSwitch = true;
                step.RecipeCommands.Add("N2HighDoping.SetValve", currentN2HighFlowMode);


                //#region M5 Control

                //step.RecipeCommands.Add("Mfc5.SetMode", "Normal");
                //step.RecipeCommands.Add("Mfc5.Ramp", (currentN2HighFlowMode == FlowMode.Run.ToString() ? currentN2HighFlow : 0).ToString("F1"));

                //#endregion
            }

            if (previousTMAFlowMode != currentTMAFlowMode && lstFlowMode.Contains(previousTMAFlowMode) && lstFlowMode.Contains(currentTMAFlowMode))
            {
                isDopeRunVentSwitch = true;
                isTotalVentFlowRunSwitch = true;
                step.RecipeCommands.Add("TMA.SetValve", currentTMAFlowMode);
            }

            //if (isDopeRunVentSwitch)
            //{
            //    double startM2Flow = previousDopeTotalFlow - (currentTMAFlowMode == FlowMode.Run.ToString() ? TMAFlow : 0)
            //        - (currentN2FlowMode == FlowMode.Run.ToString() ? previousDilutedN2Flow + (currentN2HighFlowMode == FlowMode.Run.ToString() ? previousN2HighFlow : 0) : 0);

            //    step.RecipeCommands.Add("Mfc2.Ramp", startM2Flow.ToString("F1"));
            //}

            #endregion

            #region Check Total Vent Flow

            double previousTotalVentFlow = Convert.ToDouble(previousStep["TotalVentFlow"]);
            double currentN2LowFlow = Convert.ToDouble(currentStep["Mfc4.Ramp"]);
            double currentDiluFlowForN2 = Convert.ToDouble(currentStep["Mfc3.Ramp"]);

            double currentSiSourTotalFlowForPurge = (currentSiH4FlowMode != FlowMode.Run.ToString() ? previousSiH4Flow : 0)
                + (currentTCSFlowMode != FlowMode.Run.ToString() ? TCSFlow : 0) + (currentHClFlowMode != FlowMode.Run.ToString() ? previousHClFlow : 0);

            double currentCSourTotalFlowForPurge = currentC2H4FlowMode != FlowMode.Run.ToString() ? previousC2H4Flow : 0;

            double N2Flow = (currentN2FlowMode != FlowMode.Run.ToString() ? previousDilutedN2Flow : 0) + (currentN2HighFlowMode != FlowMode.Run.ToString() ? previousN2HighFlow : 0);

            double currentDopeTotalFlowForPurge = N2Flow + (currentTMAFlowMode != FlowMode.Run.ToString() ? TMAFlow : 0);

            //if (isTotalVentFlowRunSwitch)
            //{
            //    double startM1Flow = previousTotalVentFlow - currentSiSourTotalFlowForPurge - currentCSourTotalFlowForPurge - currentDopeTotalFlowForPurge;

            //    step.RecipeCommands.Add("Mfc1.Ramp", startM1Flow.ToString("F1"));
            //}

            #endregion

            return step;
        }

        public static bool CheckDataAvalible(string recipeFile, string module, out string strErrorInfo)
        {
            strErrorInfo = "";
            return true;

            Dictionary<string, RecipeDataColumn> lstRecipeDataColumn = new Dictionary<string, RecipeDataColumn>();

            //获取模板的Double类型,并读取最大值最小值
            var str = RecipeFileManager.Instance.GetRecipeFormatXml("Sic//Process");//LoadRecipeFormat();
            XmlDocument doc = new XmlDocument();
            try
            {
                doc.LoadXml(str);
                XmlNode nodeRoot = doc.SelectSingleNode("TableRecipeFormat");
            }
            catch (Exception ex)
            {
                LOG.Write(ex);
            }

            XmlNodeList nodes = doc.SelectNodes("TableRecipeFormat/Catalog/Group");
            foreach (XmlNode node in nodes)
            {
                XmlNodeList childNodes = node.SelectNodes("Step");
                foreach (XmlNode step in childNodes)
                {
                    if (step.Attributes["InputType"].Value == "DoubleInput")
                    {
                        string recipeName = step.Attributes["ControlName"].Value;
                        if (recipeName == "Time")
                        {
                            continue;
                        }

                        //MFC和PC的最大值读取MFC的设定
                        string displayText = new Regex(@"\(M\d+\)").Match(step.Attributes["DisplayName"].Value).Value;
                        string maxConfig = $"PM.{module}.RecipeConfig.{strAbc(node.Attributes["DisplayName"].Value)}.{strAbc(step.Attributes["DisplayName"].Value)}.Max";
                        string minConfig = $"PM.{module}.RecipeConfig.{strAbc(node.Attributes["DisplayName"].Value)}.{strAbc(step.Attributes["DisplayName"].Value)}.Min";
                        if (displayText.Contains("M"))
                        {
                            maxConfig = $"PM.{module}.MFC.Mfc{displayText.Replace("(M", "").Replace(")", "")}.N2Scale";
                        }
                        else
                        {
                            displayText = new Regex(@"\(PC\d+\)").Match(step.Attributes["DisplayName"].Value).Value;
                            if (displayText.Contains("PC"))
                            {
                                maxConfig = $"PM.{module}.PC.PC{displayText.Replace("(PC", "").Replace(")", "")}.Scale";
                            }
                        }

                        double max = 0;// GetDoubleValueByName("PM", maxConfig);
                        double mix = 0;// GetDoubleValueByName("PM", minConfig);
                        if (displayText.Contains("M"))
                        {
                            max = GetDoubleValueByName("PM", maxConfig);
                            mix = max * GetDoubleValueByName("PM", $"PM.{module}.MFC.MinScale") / 100.0;
                        }
                        else
                        {
                            max = GetDoubleValueByName("PM", maxConfig);
                            mix = GetDoubleValueByName("PM", minConfig);
                        }


                        lstRecipeDataColumn.Add(recipeName, new RecipeDataColumn() { maxNum = max, minMum = mix, disPlay = step.Attributes["DisplayName"].Value });
                    }
                }
            }


            string content = RecipeFileManager.Instance.LoadRecipe("", recipeFile, false);
            if (string.IsNullOrEmpty(content))
            {
                return false;
            }
            //获取工艺程序文件中所有步的内容
            XmlDocument rcpDataDoc = new XmlDocument();
            rcpDataDoc.LoadXml(content);

            XmlNode nodeModule = rcpDataDoc.SelectSingleNode($"/Aitex/TableRecipeData/Module[@Name='{module}']");
            if (nodeModule == null)
            {
                return false;
            }

            XmlNodeList stepNodeList = nodeModule.SelectNodes($"Step");
            for (int i = 0; i < stepNodeList.Count; i++)
            {
                XmlElement stepNode = stepNodeList[i] as XmlElement;
                string stepNo = "";
                foreach (XmlAttribute att in stepNode.Attributes)
                {
                    //att.Name, att.Value
                    if (att.Name == "StepNo")
                    {
                        stepNo = att.Value;
                    }

                    if (lstRecipeDataColumn.ContainsKey(att.Name))
                    {
                        double cValue = 0;
                        double maxValue = lstRecipeDataColumn[att.Name].maxNum;
                        double minValue = lstRecipeDataColumn[att.Name].minMum;
                        string columnDisplay = lstRecipeDataColumn[att.Name].disPlay;
                        if (!double.TryParse(att.Value, out cValue))
                        {
                            strErrorInfo += "\r\n" + $"Step[{stepNo}]: Column [{columnDisplay}] value [{att.Value}] is incorrect numerical format";
                        }
                        else if (cValue > maxValue || cValue < minValue)
                        {
                            strErrorInfo += "\r\n" + $"Step {stepNo} {columnDisplay}: value {cValue} is out of range {minValue}-{maxValue}";
                        }
                    }
                }
            }

            if (!String.IsNullOrEmpty(strErrorInfo))
            {
                return false;
            }

            return true;

        }

        /// <summary>
        /// 去除小数点、箭头、括号（含括号里的内容）、横杠
        /// </summary>
        /// <param name="DisplayName"></param>
        /// <returns></returns>
        private static string strAbc(string DisplayName)
        {
            string displayname = DisplayName;
            if (Regex.IsMatch(displayname.Substring(0, 1), @"^[+-]?\d*[.]?\d*$"))
            {
                displayname = displayname.Remove(0, displayname.IndexOf(".") + 1);//去除序号
            }
            displayname = displayname.Trim().Replace(" ", "").Replace(".", "").Replace("->", "_").Replace("-", "");
            if (displayname.Contains("(") && displayname.Contains(")"))
            {
                displayname = displayname.Remove(displayname.IndexOf("("), displayname.IndexOf(")") - displayname.IndexOf("(") + 1);
            }
            return displayname;
        }

        private static double GetDoubleValueByName(string module, string name)
        {
            try
            {
                SCConfigItem item = SC.GetConfigItem(name);
                if (item != null)
                {
                    if (item.Type == "Integer")
                    {
                        return item.IntValue;
                    }
                    else if (item.Type == "Double")
                    {
                        return item.DoubleValue;
                    }
                }
                if (name.EndsWith(".Default"))
                {
                    return 0;
                }
                else if (name.EndsWith(".Min"))
                {
                    return 0;
                }
                else if (name.EndsWith(".Max"))
                {
                    return 10000;
                }
                return 0;

            }
            catch
            {
                return 0;
            }
        }
    }
}
