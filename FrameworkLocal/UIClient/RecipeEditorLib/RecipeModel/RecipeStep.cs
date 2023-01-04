using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using MECF.Framework.Common.DataCenter;
using MECF.Framework.UI.Client.CenterViews.Editors.Recipe;
using MECF.Framework.UI.Client.RecipeEditorLib.RecipeModel.Params;
using RecipeEditorLib.RecipeModel.Params;
using SciChart.Data.Model;

namespace MECF.Framework.UI.Client.RecipeEditorLib.RecipeModel
{
    public class RecipeStep : ObservableCollection<Param>
    {
        /// <summary>
        /// 步骤起始编号。
        /// </summary>
        public const int START_INDEX = 1;

        #region Variables

        private bool _isHideValue;
        private bool _isProcessed;


        /// <summary>
        /// 当配方步骤的IsSaved属性发生变化时，触发此事件。
        /// </summary>
        public event EventHandler<bool> SavedStateChanged;
        

        #endregion

        #region Constructors

        public RecipeStep(RecipeStep previous)
        {
            Previous = previous;
            IsSaved = true;
        }

        #endregion

        #region Properties
        
        /// <summary>
        /// 父级配方集合。
        /// </summary>
        public RecipeData Parent { get; internal set; }

        /// <summary>
        /// 返回配方步骤序号参数。
        /// </summary>
        public StepParam StepNoParam => this.FirstOrDefault(x => x is StepParam) as StepParam;

        public StringParam UidParam => this.FirstOrDefault(param => param.DisplayName == "Uid") as StringParam;

        /// <summary>
        /// 当前配方步骤的唯一识别码。
        /// </summary>
        public string StepUid
        {
            get => UidParam?.Value ?? null;
            set
            {
                var paramUid = UidParam;
                if (paramUid != null)
                {
                    paramUid.Value = value;
                }
            }
        }

        /// <summary>
        /// 返回步骤序号。
        /// </summary>
        public int? StepNo
        {
            get
            {
                var param = StepNoParam;
                return param?.Value ?? null;
            }
        }

        /// <summary>
        /// 返回步骤执行时长。
        /// </summary>
        public double? StepTime
        {
            get
            {
                var param = this.FirstOrDefault(x => x is DoubleParam && string.Equals(x.Name,
                    RecipColNo.Time.ToString(), StringComparison.CurrentCultureIgnoreCase)) as DoubleParam;
                return param?.Value;
            }
        }

        /// <summary>
        /// 返回当前配方步骤是否已保存。
        /// </summary>
        public bool IsSaved { get; private set;}

        /// <summary>
        /// 返回当前步骤是否已经跑完工艺。
        /// </summary>
        public bool IsProcessed
        {
            get => _isProcessed;
            set
            {
                if (_isProcessed != value)
                {
                    _isProcessed = value;
                    OnPropertyChanged(new PropertyChangedEventArgs(nameof(IsProcessed)));
                }
            }
        }

        /// <summary>
        /// 返回前序配方。
        /// </summary>
        public RecipeStep Previous { get; private set; }

        /// <summary>
        /// 返回后序配方。
        /// </summary>
        public RecipeStep Next { get; private set;}

        /// <summary>
        /// 返回是否隐藏当前步骤的参数值。
        /// </summary>
        public bool IsHideValue
        {
            get => _isHideValue;
            set
            {
                _isHideValue = value;
                this.ToList().ForEach(x => x.IsHideValue = value);
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// 当配方中的参数的属性发生变化时，触发此事件。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ParamOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (!(sender is IParam param)) 
                return;

            if (e.PropertyName == nameof(Param.IsSaved))
            {
                if (param.IsSaved == false)
                {
                    IsSaved = false;
                }
                else
                {
                    IsSaved = this.ToList().FirstOrDefault(x => x.IsSaved == false) == null;
                }
                
                SavedStateChanged?.Invoke(this, IsSaved);
            }
        }

        
        /// <summary>
        /// 将指定的Param链接到前序Param。
        /// </summary>
        /// <param name="param">当前Step中的参数。</param>
        private void LinkWithPreviousParam(IParam param)
        {
            var preParam = Previous?.FirstOrDefault(x => x.Name == param.Name);
            param.Previous = preParam;
            if (preParam != null)
                preParam.Next = param;
        }
        
        /// <summary>
        /// 将指定的Param链接到后序Param。
        /// </summary>
        /// <param name="param">当前Step中的参数。</param>
        private void LinkWithNextParam(IParam param)
        {
            var nextParam = Next?.FirstOrDefault(x => x.Name == param.Name);
            param.Next = nextParam;
            if (nextParam != null)
                nextParam.Previous = param;
        }

        /// <summary>
        /// 校验步骤中的所有参数。
        /// </summary>
        public void Validate()
        {
            this.ToList().ForEach(x=>x.Validate());
        }

        /// <summary>
        /// 取消所有参数的Highlight状态。
        /// </summary>
        public void ResetHighlight()
        {
            this.ToList().ForEach(x => x.ResetHighlight());
        }

        /// <summary>
        /// 获取高亮显示的参数。
        /// </summary>
        /// <returns></returns>
        public List<Param> GetHighlightedParams()
        {
            return this.ToList().Where(p => p.IsHighlighted).ToList();
        }

        /// <summary>
        /// 保存当前配方步骤。
        /// </summary>
        public void Save()
        {
            this.ToList().ForEach(x=>x.Save());
        }

        /// <summary>
        /// 設置前序步驟。
        /// </summary>
        /// <param name="step"></param>
        public void SetPreviousStep(RecipeStep step)
        {
            Previous = step;
            
            // 调整Step中Param的链接关系
            this.ToList().ForEach(LinkWithPreviousParam);

            if (Previous == null)
                return;

            // 将前序步骤的后序步骤链接到我自己。
            Previous.Next = this;
        }

        public void SetNextStep(RecipeStep step)
        {
            Next = step;
            this.ToList().ForEach(LinkWithNextParam);
        }

        /// <summary>
        /// 检查StepNo是否为有效值。
        /// </summary>
        /// <param name="stepNo"></param>
        /// <param name="validatedStepNo"></param>
        /// <returns></returns>
        public static bool ValidateStepNo(int? stepNo, out int validatedStepNo)
        {
            validatedStepNo = int.MinValue;
            var isValid =  stepNo >= START_INDEX;
            if (isValid)
                validatedStepNo = stepNo.Value;

            return isValid;
        }

        #endregion

        #region Gas Flow Calculation

        private static double GetTcsFlow(RecipeStep currentStep, string selectedChamber)
        {
            var currentTcsBubbleLowFlow = ((DoubleParam)currentStep[(int)RecipColNo.TCSBubbLowFlow]).Value;
            var currentTcsBubbleHighFlow = ((DoubleParam)currentStep[(int)RecipColNo.TCSBubbHighFlow]).Value;
            var currentTcsPushFlow = ((DoubleParam)currentStep[(int)RecipColNo.TCSPushFlow]).Value;
            var currentTcsFlowMode = (currentStep[(int)RecipColNo.TCSFlowMode] as ComboxParam)?.Value;

            var tcsFlow = 0d;

            var tcsEff =
                (double)QueryDataClient.Instance.Service.GetConfig($"PM.{selectedChamber}.Efficiency.TCS-Eff");
            if (string.Equals(currentTcsFlowMode, FlowModeParam.FlowModeEnum.Run.ToString(),
                    StringComparison.OrdinalIgnoreCase)
                || string.Equals(currentTcsFlowMode, FlowModeParam.FlowModeEnum.Vent.ToString(),
                    StringComparison.OrdinalIgnoreCase))
            {
                tcsFlow = (currentTcsBubbleHighFlow + currentTcsBubbleLowFlow) * tcsEff + currentTcsPushFlow;
            }
            else if (string.Equals(currentTcsFlowMode, FlowModeParam.FlowModeEnum.Purge.ToString(),
                         StringComparison.OrdinalIgnoreCase))
            {
                tcsFlow = currentTcsBubbleHighFlow + currentTcsBubbleLowFlow + currentTcsPushFlow;
            }

            return tcsFlow;
        }

        private static double GetTmaFlow(RecipeStep currentStep, string selectedChamber)
        {
            var currentTmaBubbleFlow = ((DoubleParam)currentStep[(int)RecipColNo.TMABubbFlow]).Value;
            var currentTmaPushFlow = ((DoubleParam)currentStep[(int)RecipColNo.TMAPushFlow]).Value;
            var currentTmaFlowMode = ((ComboxParam)currentStep[(int)RecipColNo.TMAFlowMode]).Value;

            var tmaFlow = 0d;
            var tmaEff =
                (double)QueryDataClient.Instance.Service.GetConfig($"PM.{selectedChamber}.Efficiency.TMA-Eff");

            if (string.Equals(currentTmaFlowMode, FlowModeParam.FlowModeEnum.Run.ToString(),
                    StringComparison.OrdinalIgnoreCase)
                || string.Equals(currentTmaFlowMode, FlowModeParam.FlowModeEnum.Vent.ToString(),
                    StringComparison.OrdinalIgnoreCase))
                tmaFlow = currentTmaBubbleFlow * tmaEff + currentTmaPushFlow;
            else if (string.Equals(currentTmaFlowMode, FlowModeParam.FlowModeEnum.Purge.ToString(),
                         StringComparison.OrdinalIgnoreCase))
                tmaFlow = currentTmaBubbleFlow + currentTmaPushFlow;

            return tmaFlow;
        }

        private static void CalSiSourceFlow(RecipeStep currentStep)
        {
            var currentSiSourSplitRatio = new double[3];
            if (currentStep[(int)RecipColNo.SiSourceSplitRatio] is Sets3RatioParam siSourceRatio)
                currentSiSourSplitRatio = siSourceRatio.Ratio;

            var currentSiSourTotalFlow = ((DoubleParam)currentStep[(int)RecipColNo.SiSourceTotalFlow]).Value;

            //double currentSiSourPushFlow = currentSiSourTotalFlow - (currentSiH4FlowMode == FlowMode.Run.ToString() ? currentSiH4Flow : 0)
            //    - (currentTCSFlowMode == FlowMode.Run.ToString() ? TCSFlow : 0) - (currentHClFlowMode == FlowMode.Run.ToString() ? currentHClFlow : 0);

            //(currentStep[(int)RecipColNo.SiSourceOuterFlow] as DoubleParam).Value = currentSiSourPushFlow.ToString("F1");

            double currentSiSourInnerFlow = 0;
            double currentSiSourMidFlow = 0;
            double currentSiSourOutFlow = 0;

            if (currentSiSourSplitRatio.Length == 3)
            {
                var rationSum = currentSiSourSplitRatio[0] + currentSiSourSplitRatio[1] + currentSiSourSplitRatio[2];

                currentSiSourInnerFlow = currentSiSourTotalFlow / rationSum * currentSiSourSplitRatio[0];
                currentSiSourMidFlow = currentSiSourTotalFlow / rationSum * currentSiSourSplitRatio[1];
                currentSiSourOutFlow = currentSiSourTotalFlow / rationSum * currentSiSourSplitRatio[2];
            }

            ((DoubleParam)currentStep[(int)RecipColNo.SiSourceInnerFlow]).Value = Math.Round(currentSiSourInnerFlow, 1);
            ((DoubleParam)currentStep[(int)RecipColNo.SiSourceMiddleFlow]).Value = Math.Round(currentSiSourMidFlow, 1);
            ((DoubleParam)currentStep[(int)RecipColNo.SiSourceOuterFlow]).Value = Math.Round(currentSiSourOutFlow, 1);
        }

        private static void CalCSourceFlow(RecipeStep currentStep)
        {
            var currentC2H4Flow = ((DoubleParam)currentStep[(int)RecipColNo.C2H4Flow]).Value;
            var currentCSourSplitRatio = new double[3];
            if (currentStep[(int)RecipColNo.CSourceSplitRatio] is Sets3RatioParam cSourceRatio)
                currentCSourSplitRatio = cSourceRatio.Ratio;

            var currentCSourTotalFlow = ((DoubleParam)currentStep[(int)RecipColNo.CSourceTotalFlow]).Value;

            var currentC2H4FlowMode = (currentStep[(int)RecipColNo.C2H4FlowMode] as ComboxParam)?.Value;
            var currentCSourPushFlow = currentCSourTotalFlow -
                                       (string.Equals(currentC2H4FlowMode, FlowModeParam.FlowModeEnum.Run.ToString(),
                                           StringComparison.OrdinalIgnoreCase)
                                           ? currentC2H4Flow
                                           : 0d);


            ((DoubleParam)currentStep[(int)RecipColNo.CSourceOuterFlow]).Value = Math.Round(currentCSourPushFlow, 1);

            double currentCSourInnerFlow = 0;
            double currentCSourMidFlow = 0;
            double currentCSourOuterFlow = 0;

            if (currentCSourSplitRatio.Length == 3)
            {
                var ratioSum = currentCSourSplitRatio[0] + currentCSourSplitRatio[1] + currentCSourSplitRatio[2];

                currentCSourInnerFlow = currentCSourTotalFlow / ratioSum * currentCSourSplitRatio[0];
                currentCSourMidFlow = currentCSourTotalFlow / ratioSum * currentCSourSplitRatio[1];
                currentCSourOuterFlow = currentCSourTotalFlow / ratioSum * currentCSourSplitRatio[2];
            }

            ((DoubleParam)currentStep[(int)RecipColNo.CSourceInnerFlow]).Value = Math.Round(currentCSourInnerFlow, 1);
            ((DoubleParam)currentStep[(int)RecipColNo.CSourceMiddleFlow]).Value = Math.Round(currentCSourMidFlow, 1);
            ((DoubleParam)currentStep[(int)RecipColNo.CSourceOuterFlow]).Value = Math.Round(currentCSourOuterFlow, 1);
        }

        private static void CalDopeFlow(RecipeStep currentStep)
        {
            var currentDopeTotalFlow = ((DoubleParam)currentStep[(int)RecipColNo.DopeTotalFlow]).Value;
            var currentN2ActualFlow = ((DoubleParam)currentStep[(int)RecipColNo.N2ActualFlow]).Value;
            var currentN2LowFlow = ((DoubleParam)currentStep[(int)RecipColNo.N2LowFlow]).Value;
            var currentDiluFlowForN2 = ((DoubleParam)currentStep[(int)RecipColNo.DilutFlowForN2]).Value;


            var currentDilutedN2Flow =
                (currentN2LowFlow + currentDiluFlowForN2) * currentN2ActualFlow / currentN2LowFlow;
            ((DoubleParam)currentStep[(int)RecipColNo.DilutedN2Flow]).Value = Math.Round(currentDilutedN2Flow, 1);


            var currentDopeSplitRatio = new double[3];
            if (currentStep[(int)RecipColNo.DopeSplitRatio] is Sets3RatioParam dopRatio)
                currentDopeSplitRatio = dopRatio.Ratio;



            //double currentDopePushFlow = currentDopeTotalFlow
            //    - (currentN2FlowMode == FlowMode.Run.ToString() ? currentDilutedN2Flow : 0)
            //    - (currentN2HighFlowMode == FlowMode.Run.ToString() ? currentN2HighFlow : 0)
            //    - (currentTMAFlowMode == FlowMode.Run.ToString() ? TMAFlow : 0);

            //(currentStep[(int)RecipColNo.DopeOuterFlow] as DoubleParam).Value = currentDopePushFlow.ToString("F1");

            double currentDopeInnerFlow = 0;
            double currentDopeMidFlow = 0;
            double currentDopeOuterFlow = 0;

            if (currentDopeSplitRatio.Length == 3)
            {
                var ratioSum = currentDopeSplitRatio[0] + currentDopeSplitRatio[1] + currentDopeSplitRatio[2];

                currentDopeInnerFlow = currentDopeTotalFlow / ratioSum * currentDopeSplitRatio[0];
                currentDopeMidFlow = currentDopeTotalFlow / ratioSum * currentDopeSplitRatio[1];
                currentDopeOuterFlow = currentDopeTotalFlow / ratioSum * currentDopeSplitRatio[2];
            }

            ((DoubleParam)currentStep[(int)RecipColNo.DopeInnerFlow]).Value = Math.Round(currentDopeInnerFlow, 1);
            ((DoubleParam)currentStep[(int)RecipColNo.DopeMiddleFlow]).Value = Math.Round(currentDopeMidFlow, 1);
            ((DoubleParam)currentStep[(int)RecipColNo.DopeOuterFlow]).Value = Math.Round(currentDopeOuterFlow, 1);
        }

        private static void CalShSupplementFlow(RecipeStep currentStep)
        {
            var currentSiSourTotalFlow = ((DoubleParam)currentStep[(int)RecipColNo.SiSourceTotalFlow]).Value;
            var currentCSourTotalFlow = ((DoubleParam)currentStep[(int)RecipColNo.CSourceTotalFlow]).Value;
            var currentDopeTotalFlow = ((DoubleParam)currentStep[(int)RecipColNo.DopeTotalFlow]).Value;

            var currentShTotalFlow = ((DoubleParam)currentStep[(int)RecipColNo.SHTotalFlow]).Value;
            var currentSurroundingFlow = ((DoubleParam)currentStep[(int)RecipColNo.CarrayGasFlow]).Value;


            var currentSiSourInnerFlow = ((DoubleParam)currentStep[(int)RecipColNo.SiSourceInnerFlow]).Value;
            var currentSiSourMidFlow = ((DoubleParam)currentStep[(int)RecipColNo.SiSourceMiddleFlow]).Value;

            var currentCSourInnerFlow = ((DoubleParam)currentStep[(int)RecipColNo.CSourceInnerFlow]).Value;
            var currentCSourMidFlow = ((DoubleParam)currentStep[(int)RecipColNo.CSourceMiddleFlow]).Value;

            var currentDopeInnerFlow = ((DoubleParam)currentStep[(int)RecipColNo.DopeInnerFlow]).Value;
            var currentDopeMidFlow = ((DoubleParam)currentStep[(int)RecipColNo.DopeMiddleFlow]).Value;


            var currentShTotalFlowSplitRatio = new double[3];
            if (currentStep[(int)RecipColNo.SHTotalFlowSplitRatio] is Sets3RatioParam shTotalRatio)
                currentShTotalFlowSplitRatio = shTotalRatio.Ratio;


            var currentShSuppTotalFlow = currentShTotalFlow - currentSurroundingFlow - currentSiSourTotalFlow -
                                         currentCSourTotalFlow - currentDopeTotalFlow;
            var sumShTotalFlowSplitRatio = currentShTotalFlowSplitRatio[0] + currentShTotalFlowSplitRatio[1] +
                                           currentShTotalFlowSplitRatio[2];
            var currentShInnerFlow = (currentShTotalFlow - currentSurroundingFlow) / sumShTotalFlowSplitRatio *
                                     currentShTotalFlowSplitRatio[0];
            var currentShMidFlow = (currentShTotalFlow - currentSurroundingFlow) / sumShTotalFlowSplitRatio *
                                   currentShTotalFlowSplitRatio[1];
            var currentShOutterFlow = (currentShTotalFlow - currentSurroundingFlow) / sumShTotalFlowSplitRatio *
                                      currentShTotalFlowSplitRatio[2];
            var currentInnerSuppFlow = currentShInnerFlow - currentSiSourInnerFlow - currentCSourInnerFlow -
                                       currentDopeInnerFlow;
            var currentMidSuppFlow = currentShMidFlow - currentSiSourMidFlow - currentCSourMidFlow - currentDopeMidFlow;
            var currentOutterSuppFlow = currentShSuppTotalFlow - currentInnerSuppFlow - currentMidSuppFlow;

            ((DoubleParam)currentStep[(int)RecipColNo.SHPushTotalFlow]).Value = Math.Round(currentShSuppTotalFlow, 1);
            ((DoubleParam)currentStep[(int)RecipColNo.SHInnerFlow]).Value = Math.Round(currentShInnerFlow, 1);
            ((DoubleParam)currentStep[(int)RecipColNo.SHMiddleFlow]).Value = Math.Round(currentShMidFlow, 1);
            ((DoubleParam)currentStep[(int)RecipColNo.SHOuterFlow]).Value = Math.Round(currentShOutterFlow, 1);
            ((DoubleParam)currentStep[(int)RecipColNo.InnerPushFlow]).Value = Math.Round(currentInnerSuppFlow, 1);
            ((DoubleParam)currentStep[(int)RecipColNo.MiddlePushFlow]).Value = Math.Round(currentMidSuppFlow, 1);
            ((DoubleParam)currentStep[(int)RecipColNo.OuterPushFlow]).Value = Math.Round(currentOutterSuppFlow, 1);
        }

        private static void CalVentFlow(RecipeStep currentStep, string selectedChamber)
        {
            var currentSiH4Flow = ((DoubleParam)currentStep[(int)RecipColNo.SiH4Flow]).Value;
            var currentTotalVentFlow = ((DoubleParam)currentStep[(int)RecipColNo.TotalVentFlow]).Value;
            var currentHClFlow = ((DoubleParam)currentStep[(int)RecipColNo.HCLFlow]).Value;
            var currentN2HighFlow = ((DoubleParam)currentStep[(int)RecipColNo.N2HighFlow]).Value;
            var currentC2H4Flow = ((DoubleParam)currentStep[(int)RecipColNo.C2H4Flow]).Value;
            var currentDilutedN2Flow = ((DoubleParam)currentStep[(int)RecipColNo.DilutedN2Flow]).Value;

            var currentN2FlowMode = ((ComboxParam)currentStep[(int)RecipColNo.N2FlowMode]).Value;
            var currentN2HighFlowMode = ((ComboxParam)currentStep[(int)RecipColNo.N2HighFlowMode]).Value;
            var currentSiH4FlowMode = (currentStep[(int)RecipColNo.SiH4FlowMode] as ComboxParam)?.Value;
            var currentTcsFlowMode = (currentStep[(int)RecipColNo.TCSFlowMode] as ComboxParam)?.Value;
            var currentHClFlowMode = (currentStep[(int)RecipColNo.HCLFlowMode] as ComboxParam)?.Value;
            var currentC2H4FlowMode = (currentStep[(int)RecipColNo.C2H4FlowMode] as ComboxParam)?.Value;
            var currentTmaFlowMode = ((ComboxParam)currentStep[(int)RecipColNo.TMAFlowMode]).Value;

            var tcsFlow = GetTcsFlow(currentStep, selectedChamber);
            var tmaFlow = GetTmaFlow(currentStep, selectedChamber);

            var currentSiSourTotalFlowForPurge =
                (string.Equals(currentSiH4FlowMode, FlowModeParam.FlowModeEnum.Run.ToString(),
                    StringComparison.OrdinalIgnoreCase) == false
                    ? currentSiH4Flow
                    : 0d)
                + (string.Equals(currentTcsFlowMode, FlowModeParam.FlowModeEnum.Run.ToString(),
                    StringComparison.OrdinalIgnoreCase) == false
                    ? tcsFlow
                    : 0d) +
                (string.Equals(currentHClFlowMode, FlowModeParam.FlowModeEnum.Run.ToString(),
                    StringComparison.OrdinalIgnoreCase) == false
                    ? currentHClFlow
                    : 0d);

            var currentCSourTotalFlowForPurge =
                string.Equals(currentC2H4FlowMode, FlowModeParam.FlowModeEnum.Run.ToString(),
                    StringComparison.OrdinalIgnoreCase) == false
                    ? currentC2H4Flow
                    : 0d;


            var n2Flow = (string.Equals(currentN2FlowMode, FlowModeParam.FlowModeEnum.Run.ToString(),
                             StringComparison.OrdinalIgnoreCase) == false
                             ? currentDilutedN2Flow
                             : 0d) +
                         (string.Equals(currentN2HighFlowMode, FlowModeParam.FlowModeEnum.Run.ToString(),
                             StringComparison.OrdinalIgnoreCase) == false
                             ? currentN2HighFlow
                             : 0d);

            var currentDopeTotalFlowForPurge = n2Flow + (string.Equals(currentTmaFlowMode,
                FlowModeParam.FlowModeEnum.Run.ToString(), StringComparison.OrdinalIgnoreCase) == false
                ? tmaFlow
                : 0d);

            var currentVentPushFlow = currentTotalVentFlow - currentSiSourTotalFlowForPurge -
                                      currentCSourTotalFlowForPurge - currentDopeTotalFlowForPurge;

            ((DoubleParam)currentStep[(int)RecipColNo.VentPushFlow]).Value = Math.Round(currentVentPushFlow, 1);
        }

        public void CalRecipeParameterForRunVent()
        {
            /*if (!(currentStep.StepTime > 0)) 
                return;*/

            CalSiSourceFlow(this);
            CalCSourceFlow(this);
            CalDopeFlow(this);
            CalShSupplementFlow(this);
            CalVentFlow(this, Parent.Module);
        }

        [Obsolete("该函数已废除，请使用分步计算函数。")]
        private void CalRecipeParameterForRunVent(RecipeStep previousStep,
            IReadOnlyList<Param> currentStep, double stepTime)
        {
            /*#region SH Total flow

            var currentShTotalFlow = ((DoubleParam)currentStep[(int)RecipColNo.SHTotalFlow]).Value;
            var currentSurroundingFlow = ((DoubleParam)currentStep[(int)RecipColNo.CarrayGasFlow]).Value;

            var currentShTotalFlowSplitRatio = new double[3];
            if (currentStep[(int)RecipColNo.SHTotalFlowSplitRatio] is Sets3RatioParam shTotalRatio)
                currentShTotalFlowSplitRatio = shTotalRatio.Ratio;

            #endregion

            #region Si Source

            var currentSiH4Flow = ((DoubleParam)currentStep[(int)RecipColNo.SiH4Flow]).Value;
            var currentTcsBubbleLowFlow = ((DoubleParam)currentStep[(int)RecipColNo.TCSBubbLowFlow]).Value;
            var currentTcsBubbleHighFlow = ((DoubleParam)currentStep[(int)RecipColNo.TCSBubbHighFlow]).Value;
            var currentTcsPushFlow = ((DoubleParam)currentStep[(int)RecipColNo.TCSPushFlow]).Value;
            var currentHClFlow = ((DoubleParam)currentStep[(int)RecipColNo.HCLFlow]).Value;

            var currentSiSourSplitRatio = new double[3];
            if (currentStep[(int)RecipColNo.SiSourceSplitRatio] is Sets3RatioParam siSourceRatio)
                currentSiSourSplitRatio = siSourceRatio.Ratio;

            var currentSiSourTotalFlow = ((DoubleParam)currentStep[(int)RecipColNo.SiSourceTotalFlow]).Value;

            var currentSiH4FlowMode = (currentStep[(int)RecipColNo.SiH4FlowMode] as ComboxParam)?.Value;
            var currentTcsFlowMode = (currentStep[(int)RecipColNo.TCSFlowMode] as ComboxParam)?.Value;
            var currentHClFlowMode = (currentStep[(int)RecipColNo.HCLFlowMode] as ComboxParam)?.Value;

            double tcsFlow = 0d;
            var tcsEff =
                (double)QueryDataClient.Instance.Service.GetConfig($"PM.{Parent.ChamberBelongTo}.Efficiency.TCS-Eff");
            if (string.Equals(currentTcsFlowMode, FlowModeParam.FlowModeEnum.Run.ToString(),
                    StringComparison.OrdinalIgnoreCase)
                || string.Equals(currentTcsFlowMode, FlowModeParam.FlowModeEnum.Vent.ToString(),
                    StringComparison.OrdinalIgnoreCase))
            {
                tcsFlow = (currentTcsBubbleHighFlow + currentTcsBubbleLowFlow) * tcsEff + currentTcsPushFlow;
            }
            else if (string.Equals(currentTcsFlowMode, FlowModeParam.FlowModeEnum.Purge.ToString(),
                         StringComparison.OrdinalIgnoreCase))
            {
                tcsFlow = currentTcsBubbleHighFlow + currentTcsBubbleLowFlow + currentTcsPushFlow;
            }

            //double currentSiSourPushFlow = currentSiSourTotalFlow - (currentSiH4FlowMode == FlowMode.Run.ToString() ? currentSiH4Flow : 0)
            //    - (currentTCSFlowMode == FlowMode.Run.ToString() ? TCSFlow : 0) - (currentHClFlowMode == FlowMode.Run.ToString() ? currentHClFlow : 0);

            //(currentStep[(int)RecipColNo.SiSourceOuterFlow] as DoubleParam).Value = currentSiSourPushFlow.ToString("F1");

            double currentSiSourInnerFlow = 0;
            double currentSiSourMidFlow = 0;
            double currentSiSourOutFlow = 0;

            if (currentSiSourSplitRatio.Length == 3)
            {
                var rationSum = currentSiSourSplitRatio[0] + currentSiSourSplitRatio[1] + currentSiSourSplitRatio[2];

                currentSiSourInnerFlow = currentSiSourTotalFlow / rationSum * currentSiSourSplitRatio[0];
                currentSiSourMidFlow = currentSiSourTotalFlow / rationSum * currentSiSourSplitRatio[1];
                currentSiSourOutFlow = currentSiSourTotalFlow / rationSum * currentSiSourSplitRatio[2];
            }

            ((DoubleParam)currentStep[(int)RecipColNo.SiSourceInnerFlow]).Value = Math.Round(currentSiSourInnerFlow, 1);
            ((DoubleParam)currentStep[(int)RecipColNo.SiSourceMiddleFlow]).Value = Math.Round(currentSiSourMidFlow, 1);
            ((DoubleParam)currentStep[(int)RecipColNo.SiSourceOuterFlow]).Value = Math.Round(currentSiSourOutFlow, 1);

            #endregion

            #region C Source

            var currentC2H4Flow = ((DoubleParam)currentStep[(int)RecipColNo.C2H4Flow]).Value;
            var currentCSourSplitRatio = new double[3];
            if (currentStep[(int)RecipColNo.CSourceSplitRatio] is Sets3RatioParam cSourceRatio)
                currentCSourSplitRatio = cSourceRatio.Ratio;

            var currentCSourTotalFlow = ((DoubleParam)currentStep[(int)RecipColNo.CSourceTotalFlow]).Value;

            var currentC2H4FlowMode = (currentStep[(int)RecipColNo.C2H4FlowMode] as ComboxParam)?.Value;
            var currentCSourPushFlow = currentCSourTotalFlow -
                                       (string.Equals(currentC2H4FlowMode, FlowModeParam.FlowModeEnum.Run.ToString(),
                                           StringComparison.OrdinalIgnoreCase)
                                           ? currentC2H4Flow
                                           : 0d);


            ((DoubleParam)currentStep[(int)RecipColNo.CSourceOuterFlow]).Value = Math.Round(currentCSourPushFlow, 1);

            double currentCSourInnerFlow = 0;
            double currentCSourMidFlow = 0;
            double currentCSourOuterFlow = 0;

            if (currentCSourSplitRatio.Length == 3)
            {
                var ratioSum = currentCSourSplitRatio[0] + currentCSourSplitRatio[1] + currentCSourSplitRatio[2];

                currentCSourInnerFlow = currentCSourTotalFlow / ratioSum * currentCSourSplitRatio[0];
                currentCSourMidFlow = currentCSourTotalFlow / ratioSum * currentCSourSplitRatio[1];
                currentCSourOuterFlow = currentCSourTotalFlow / ratioSum * currentCSourSplitRatio[2];
            }

            ((DoubleParam)currentStep[(int)RecipColNo.CSourceInnerFlow]).Value = Math.Round(currentCSourInnerFlow, 1);
            ((DoubleParam)currentStep[(int)RecipColNo.CSourceMiddleFlow]).Value = Math.Round(currentCSourMidFlow, 1);
            ((DoubleParam)currentStep[(int)RecipColNo.CSourceOuterFlow]).Value = Math.Round(currentCSourOuterFlow, 1);

            #endregion

            #region Dope

            var currentDopeTotalFlow = ((DoubleParam)currentStep[(int)RecipColNo.DopeTotalFlow]).Value;
            var currentN2ActualFlow = ((DoubleParam)currentStep[(int)RecipColNo.N2ActualFlow]).Value;
            var currentN2LowFlow = ((DoubleParam)currentStep[(int)RecipColNo.N2LowFlow]).Value;
            var currentDiluFlowForN2 = ((DoubleParam)currentStep[(int)RecipColNo.DilutFlowForN2]).Value;

            var currentN2HighFlow = ((DoubleParam)currentStep[(int)RecipColNo.N2HighFlow]).Value;
            var currentTmaBubbFlow = ((DoubleParam)currentStep[(int)RecipColNo.TMABubbFlow]).Value;
            var currentTmaPushFlow = ((DoubleParam)currentStep[(int)RecipColNo.TMAPushFlow]).Value;

            var currentDilutedN2Flow =
                (currentN2LowFlow + currentDiluFlowForN2) * currentN2ActualFlow / currentN2LowFlow;
            ((DoubleParam)currentStep[(int)RecipColNo.DilutedN2Flow]).Value = Math.Round(currentDilutedN2Flow, 1);


            var currentDopeSplitRatio = new double[3];
            if (currentStep[(int)RecipColNo.DopeSplitRatio] is Sets3RatioParam dopRatio)
                currentDopeSplitRatio = dopRatio.Ratio;

            var currentN2FlowMode = ((ComboxParam)currentStep[(int)RecipColNo.N2FlowMode]).Value;
            var currentN2HighFlowMode = ((ComboxParam)currentStep[(int)RecipColNo.N2HighFlowMode]).Value;
            var currentTmaFlowMode = ((ComboxParam)currentStep[(int)RecipColNo.TMAFlowMode]).Value;

            double tmaFlow = 0;
            var tmaEff =
                (double)QueryDataClient.Instance.Service.GetConfig($"PM.{Parent.ChamberBelongTo}.Efficiency.TMA-Eff");

            if (string.Equals(currentTmaFlowMode, FlowModeParam.FlowModeEnum.Run.ToString(),
                    StringComparison.OrdinalIgnoreCase)
                || string.Equals(currentTmaFlowMode, FlowModeParam.FlowModeEnum.Vent.ToString(),
                    StringComparison.OrdinalIgnoreCase))
                tmaFlow = currentTmaBubbFlow * tmaEff + currentTmaPushFlow;
            else if (string.Equals(currentTmaFlowMode, FlowModeParam.FlowModeEnum.Purge.ToString(),
                         StringComparison.OrdinalIgnoreCase))
                tmaFlow = currentTmaBubbFlow + currentTmaPushFlow;

            //double currentDopePushFlow = currentDopeTotalFlow
            //    - (currentN2FlowMode == FlowMode.Run.ToString() ? currentDilutedN2Flow : 0)
            //    - (currentN2HighFlowMode == FlowMode.Run.ToString() ? currentN2HighFlow : 0)
            //    - (currentTMAFlowMode == FlowMode.Run.ToString() ? TMAFlow : 0);

            //(currentStep[(int)RecipColNo.DopeOuterFlow] as DoubleParam).Value = currentDopePushFlow.ToString("F1");

            double currentDopeInnerFlow = 0;
            double currentDopeMidFlow = 0;
            double currentDopeOuterFlow = 0;

            if (currentDopeSplitRatio.Length == 3)
            {
                var ratioSum = currentDopeSplitRatio[0] + currentDopeSplitRatio[1] + currentDopeSplitRatio[2];

                currentDopeInnerFlow = currentDopeTotalFlow / ratioSum * currentDopeSplitRatio[0];
                currentDopeMidFlow = currentDopeTotalFlow / ratioSum * currentDopeSplitRatio[1];
                currentDopeOuterFlow = currentDopeTotalFlow / ratioSum * currentDopeSplitRatio[2];
            }

            (currentStep[(int)RecipColNo.DopeInnerFlow] as DoubleParam).Value = Math.Round(currentDopeInnerFlow, 1);
            (currentStep[(int)RecipColNo.DopeMiddleFlow] as DoubleParam).Value = Math.Round(currentDopeMidFlow, 1);
            (currentStep[(int)RecipColNo.DopeOuterFlow] as DoubleParam).Value = Math.Round(currentDopeOuterFlow, 1);

            #endregion

            #region SH Supp. Flow

            var currentShSuppTotalFlow = currentShTotalFlow - currentSurroundingFlow - currentSiSourTotalFlow -
                                         currentCSourTotalFlow - currentDopeTotalFlow;
            var sumShTotalFlowSplitRatio = currentShTotalFlowSplitRatio[0] + currentShTotalFlowSplitRatio[1] +
                                           currentShTotalFlowSplitRatio[2];
            var currentShInnerFlow = (currentShTotalFlow - currentSurroundingFlow) / sumShTotalFlowSplitRatio *
                                     currentShTotalFlowSplitRatio[0];
            var currentShMidFlow = (currentShTotalFlow - currentSurroundingFlow) / sumShTotalFlowSplitRatio *
                                   currentShTotalFlowSplitRatio[1];
            var currentShOutterFlow = (currentShTotalFlow - currentSurroundingFlow) / sumShTotalFlowSplitRatio *
                                      currentShTotalFlowSplitRatio[2];
            var currentInnerSuppFlow = currentShInnerFlow - currentSiSourInnerFlow - currentCSourInnerFlow -
                                       currentDopeInnerFlow;
            var currentMidSuppFlow = currentShMidFlow - currentSiSourMidFlow - currentCSourMidFlow - currentDopeMidFlow;
            var currentOutterSuppFlow = currentShSuppTotalFlow - currentInnerSuppFlow - currentMidSuppFlow;

            (currentStep[(int)RecipColNo.SHPushTotalFlow] as DoubleParam).Value = Math.Round(currentShSuppTotalFlow, 1);
            (currentStep[(int)RecipColNo.SHInnerFlow] as DoubleParam).Value = Math.Round(currentShInnerFlow, 1);
            (currentStep[(int)RecipColNo.SHMiddleFlow] as DoubleParam).Value = Math.Round(currentShMidFlow, 1);
            (currentStep[(int)RecipColNo.SHOuterFlow] as DoubleParam).Value = Math.Round(currentShOutterFlow, 1);
            (currentStep[(int)RecipColNo.InnerPushFlow] as DoubleParam).Value = Math.Round(currentInnerSuppFlow, 1);
            (currentStep[(int)RecipColNo.MiddlePushFlow] as DoubleParam).Value = Math.Round(currentMidSuppFlow, 1);
            (currentStep[(int)RecipColNo.OuterPushFlow] as DoubleParam).Value = Math.Round(currentOutterSuppFlow, 1);

            #endregion

            #region Vent Flow

            var currentTotalVentFlow = (currentStep[(int)RecipColNo.TotalVentFlow] as DoubleParam).Value;

            var currentSiSourTotalFlowForPurge =
                (string.Equals(currentSiH4FlowMode, FlowModeParam.FlowModeEnum.Run.ToString(),
                    StringComparison.OrdinalIgnoreCase) == false
                    ? currentSiH4Flow
                    : 0d)
                + (string.Equals(currentTcsFlowMode, FlowModeParam.FlowModeEnum.Run.ToString(),
                    StringComparison.OrdinalIgnoreCase) == false
                    ? tcsFlow
                    : 0d) +
                (string.Equals(currentHClFlowMode, FlowModeParam.FlowModeEnum.Run.ToString(),
                    StringComparison.OrdinalIgnoreCase) == false
                    ? currentHClFlow
                    : 0d);

            var currentCSourTotalFlowForPurge =
                string.Equals(currentC2H4FlowMode, FlowModeParam.FlowModeEnum.Run.ToString(),
                    StringComparison.OrdinalIgnoreCase) == false
                    ? currentC2H4Flow
                    : 0d;


            var n2Flow = (string.Equals(currentN2FlowMode, FlowModeParam.FlowModeEnum.Run.ToString(),
                             StringComparison.OrdinalIgnoreCase) == false
                             ? currentDilutedN2Flow
                             : 0d) +
                         (string.Equals(currentN2HighFlowMode, FlowModeParam.FlowModeEnum.Run.ToString(),
                             StringComparison.OrdinalIgnoreCase) == false
                             ? currentN2HighFlow
                             : 0d);

            var currentDopeTotalFlowForPurge = n2Flow + (string.Equals(currentTmaFlowMode,
                FlowModeParam.FlowModeEnum.Run.ToString(), StringComparison.OrdinalIgnoreCase) == false
                ? tmaFlow
                : 0d);

            var currentVentPushFlow = currentTotalVentFlow - currentSiSourTotalFlowForPurge -
                                      currentCSourTotalFlowForPurge - currentDopeTotalFlowForPurge;

            (currentStep[(int)RecipColNo.VentPushFlow] as DoubleParam).Value = Math.Round(currentVentPushFlow, 1);

            #endregion*/
        }

        #endregion

        #region Override Methods

        protected override void InsertItem(int index, Param item)
        {
            if (item != null)
            {
                item.Parent = this;
                LinkWithPreviousParam(item);
                LinkWithNextParam(item);
                
                item.PropertyChanged += ParamOnPropertyChanged;
            }

            base.InsertItem(index, item);
        }

        protected override void SetItem(int index, Param item)
        {
            throw new NotSupportedException();
        }

        protected override void RemoveItem(int index)
        {
            if (Count > 0)
            {
                if (index >= 0 && index < Count)
                {
                    this[index].PropertyChanged -= ParamOnPropertyChanged;
                }
            }

            // 将前后两个Param链接起来
            var preParam = this[index].Previous;
            var nextParam = this[index].Next;
            if (preParam != null && nextParam != null)
            {
                preParam.Next = nextParam;
                nextParam.Previous = preParam;
            }
            else if (preParam == null && nextParam != null)
            {
                nextParam.Previous = null;
            }
            else if (preParam != null)
            {
                preParam.Next = null;
            }


            IsSaved = false;
            SavedStateChanged?.Invoke(this, true);

            base.RemoveItem(index);
        }

        protected override void ClearItems()
        {
            this.ToList().ForEach(x => x.PropertyChanged -= ParamOnPropertyChanged);

            IsSaved = Count <= 0;
            SavedStateChanged?.Invoke(this, true);

            base.ClearItems();
        }

        public override string ToString()
        {
            return Count > 0 ? (StepNoParam == null?"Recipe Step, Item={Count}" : StepNoParam.ToString()): "Empty Recipe Step";
        }

        #endregion
    }
}
