using System.Collections.Generic;
using System.Collections.ObjectModel;
using Aitex.Core.Common.DeviceData;
using Aitex.Core.Util;
using Caliburn.Micro;
using MECF.Framework.Common.OperationCenter;
using System;
using System.Linq;
using MECF.Framework.Common.DataCenter;
using MECF.Framework.UI.Client.CenterViews.Editors.Sequence;
using MECF.Framework.UI.Client.CenterViews.Editors.Recipe;
using MECF.Framework.UI.Client.ClientBase;
using System.Windows;
using RecipeEditorLib.DGExtension.CustomColumn;
using RecipeEditorLib.RecipeModel.Params;
using System.Windows.Controls;
using Caliburn.Micro.Core;
using System.Windows.Media;
using SicUI.Client;
using System.Windows.Threading;
using MECF.Framework.UI.Client.CenterViews.Editors;
using SicUI.Controls;
using OpenSEMI.ClientBase;
using System.Data;
using System.Diagnostics;
using System.Threading.Tasks;
using MECF.Framework.UI.Client.RecipeEditorLib.RecipeModel.Params;
using Sicentury.Core;
using System.Text;
using System.Threading;
using Action = System.Action;

namespace SicUI.Models.PMs
{
    public class PMProcessViewModel : SicModuleUIViewModelBase, ISupportMultipleSystem
    {
        private bool _isHideRecipeValue = false;
        private bool _allowSaveRecipe = false;
        private bool _allowRipRecipe = false;

        private double _tmaFlowRatio = 1;
        private double _tcsFlowRatio = 1;
        private double _hclFlowRatio = 1;
        private double _C2H4FlowRatio = 1;
        private double _sih4FlowRatio = 1;
        private double _pn2FlowRatio = 1;

        private string _currectProcessingRecipe;
        private IProgress<int> _progressRecipeStepChanged;
        private IProgress<string> _progressLoadRecipe;
        private readonly object _lockerLoadingRecipe = new object();

        #region properties

        public bool IsPermission { get => this.Permission == 3; }

        public bool AllowSaveRecipe
        {
            get => _allowSaveRecipe;
            set
            {
                _allowSaveRecipe = value;
                NotifyOfPropertyChange();
            }
        }

        public bool AllowRipRecipe
        {
            get => _allowRipRecipe;
            set
            {
                _allowRipRecipe = value;
                NotifyOfPropertyChange();
            }
        }


        [Subscription("IsBusy")]
        public bool IsBusy { get; set; }

        [Subscription("Status")]
        public string Status { get; set; }

        public bool IsPMProcess => Status == "Process" || Status == "PostProcess" || Status == "Paused" || Status == "PMMacroPause" || Status == "PMMacro" || Status == "PostPMMacro";
       
        public bool IsPreProcess => Status == "PreProcess" || Status == "PrePMMacro";

        [Subscription("IsOnline")]
        public bool IsOnline { get; set; }

        [Subscription("Recipe.DeviceData")]
        public AITDeviceData Recipe { get; set; }

        public float[] RecipeData1 => RecipeData;
        
        public float[] RecipeData = new float[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 9, 10, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 11, 1, 1, 1, 1, 11, 1, 1, 1, 1, 1, 1, 1, 1, 1, 11, 1, 11, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 };

        [Subscription("SelectedRecipeName")]
        public string CurrentProcessingRecipeName
        {
            get => _currectProcessingRecipe;
            set
            {
                if (_currectProcessingRecipe != value)
                {
                    _currectProcessingRecipe = value;
                    DisplayingRecipeName = value;
                }
            }
        }

        [Subscription("RecipeStepName")]
        public string RecipeStepName { get; set; }

        private int _recipeStepNumber;

        [Subscription("RecipeStepNumber")]
        public int RecipeStepNumber
        {
            get => _recipeStepNumber;
            set
            {
                if (_recipeStepNumber != value)
                {
                    // Recipe Step Number 变了
                    _progressRecipeStepChanged.Report(value);
                    _recipeStepNumber = value;
                    NotifyOfPropertyChange();
                }
            }
        }

        [Subscription("ArH2Switch")]
        public string ArH2Switch { get; set; }

        [Subscription("N2FlowMode")]
        public string N2FlowMode { get; set; }

        [Subscription("RecipeStepElapseTime")]
        public int RecipeStepElapseTime { get; set; }

        [Subscription("RecipeStepTime")]
        public int RecipeStepTime { get; set; }

        [Subscription("RecipeStepElapseTime2")]
        public int RecipeStepElapseTime2 { get; set; }

        [Subscription("RecipeTotalElapseTime")]
        public int RecipeTotalElapseTime { get; set; }

        [Subscription("RecipeTotalTime")]
        public int RecipeTotalTime { get; set; }

        [Subscription("RecipeTotalElapseTime2")]
        public int RecipeTotalElapseTime2 { get; set; }

        public string StepNumber
        {
            get
            {
                if (IsPMProcess)
                {
                    return $"{RecipeStepNumber}";
                }
                else if (IsPreProcess)
                {
                    return "0";
                }
                return "--";

            }
            set
            {

            }
        }

        public string StepName
        {
            get
            {
                if (IsPMProcess)
                {
                    return $"{RecipeStepName}";
                }
                else if (IsPreProcess)
                {
                    return "0";
                }
                return "--";
            }
        }

        public string StepTime
        {
            get
            {
                if (IsPMProcess)
                {
                    return $"{RecipeStepElapseTime}/{RecipeStepTime}";
                }
                else if (IsPreProcess)
                {
                    return "0";
                }
                return "--";

            }
        }

        public string RecipeTime
        {
            get
            {
                if (IsPMProcess)
                {
                    return $"{RecipeTotalElapseTime}/{RecipeTotalTime}";
                }
                else if (IsPreProcess)
                {
                    return "0";
                }
                return "--";
            }
        }

        private string _displayingRecipeName;
        public string DisplayingRecipeName
        {
            get => _displayingRecipeName;
            private set
            {
                if (_displayingRecipeName != value)
                {
                    _displayingRecipeName = value;
                    _progressLoadRecipe.Report(value);
                    NotifyOfPropertyChange();
                }
            }
        }

        private bool _isRecipeLoading;

        public bool IsRecipeLoading
        {
            get => _isRecipeLoading;
            set
            {
                if (_isRecipeLoading != value)
                {
                    _isRecipeLoading = value;
                    NotifyOfPropertyChange();
                }
            }

        }

        //qbh 20220523
        private int x = 0;
        private DispatcherTimer timer;
        private ProcessMonitorView _winProcessMonitor = null;

        public event EventHandler TimerEvent;

        //
        public RecipeData CurrentRecipe { get; set; } = new RecipeData();
        public List<EditorDataGridTemplateColumnBase> Columns { get; set; } = new List<EditorDataGridTemplateColumnBase>();

        private RecipeFormatBuilder _columnBuilder;

        public bool IsSelectButtonEnable => !string.IsNullOrEmpty(Status) && !Status.Equals("Process")
             && !Status.Equals("PreProcess") && !Status.Equals("PostProcess") && !Status.Equals("Paused") && !IsOnline && !IsRecipeLoading;
        public bool IsStartButtonEnable => !string.IsNullOrEmpty(DisplayingRecipeName) && !string.IsNullOrEmpty(Status)
            && !Status.Equals("Process") && !Status.Equals("PreProcess") && !Status.Equals("PostProcess") &&
            !Status.Equals("PrePMMacro") && !Status.Equals("PMMacro") && !Status.Equals("PostPMMacro") &&
            !Status.Equals("Paused") && !IsOnline;// !IsProcessRunning;
        public bool IsStopButtonEnable => !string.IsNullOrEmpty(Status) && (Status.Equals("Process") || Status.Equals("PMMacro")) && !IsOnline;
        public bool IsAbortButtonEnable => !string.IsNullOrEmpty(Status) && (Status.Equals("Process") || Status.Equals("PMMacro")) && !IsOnline; //|| Status.Equals("PreProcess") || Status.Equals("PostProcess") || Status.Equals("Paused"));//IsProcessRunning;
        public bool IsPauseButtonEnable => !string.IsNullOrEmpty(Status) && (Status.Equals("Process") || Status.Equals("PMMacro")) && !IsOnline;
        public bool IsSkipButtonEnable => !string.IsNullOrEmpty(Status) && Status.Equals("Process") && !IsOnline;
        public bool IsContinueButtonEnable => !string.IsNullOrEmpty(Status) && (Status.Equals("Paused") || Status.Equals("PMMacroPause")) && !IsOnline;

        public WaferInfo MLLWafer
        {
            get
            {
                if (ModuleManager.ModuleInfos["LLH"].WaferManager.Wafers.Count > 0)
                    return ModuleManager.ModuleInfos["LLH"].WaferManager.Wafers[0];
                return null;
            }
        }

        public bool MLLHasWafer
        {
            get
            {
                return MLLWafer.WaferStatus > 0;
            }
        }

        public WaferInfo PMWafer
        {
            get
            {
                if (ModuleManager.ModuleInfos[SystemName].WaferManager.Wafers.Count > 0)
                    return ModuleManager.ModuleInfos[SystemName].WaferManager.Wafers[0];
                return null;
            }
        }

        public bool PMHasWafer => PMWafer.WaferStatus > 0;

        public Dictionary<int, string> DicGas { get; set; }

        public int MLLCurrentWafer { get; set; }
        public int MLLTotalWafer { get; set; }

        // public bool IsEnableGasMap => !IsBusy && IsMaintainMode;

        //public string SystemName { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public object View { get; set; }

        public string CurrentProcessType { get; set; }

        private bool needLoadRecipe = false;

        #endregion

        /// <summary>
        /// 获取角色权限配置.
        /// </summary>
        /// <returns></returns>
        private void LoadRolePermissions()
        {
            var roleID = BaseApp.Instance.UserContext.RoleID;
            _isHideRecipeValue = RoleAccountProvider.Instance.GetMenuPermission(roleID, "Recipe.Behaviour.ShowValueInProcessView") == (int)MenuPermissionEnum.MP_NONE;
            AllowSaveRecipe = RoleAccountProvider.Instance.GetMenuPermission(roleID, "Recipe.Behaviour.AllowSaveInProcessView") != (int)MenuPermissionEnum.MP_NONE;
            AllowRipRecipe = RoleAccountProvider.Instance.GetMenuPermission(roleID, "Recipe.Behaviour.AllowRipInProcessView") != (int)MenuPermissionEnum.MP_NONE;
   
        }
        
        protected override void OnInitialize()
        {
            base.OnInitialize();

            //权限
            string roleID = BaseApp.Instance.UserContext.RoleID;
            stepsVisibility = RoleAccountProvider.Instance.GetMenuPermission(roleID, "PM1.Process.Steps") == 3;
            _progressLoadRecipe = new Progress<string>(ProgLoadRecipeOnUiThread);
            _progressRecipeStepChanged = new Progress<int>(RecipeStepInProcessChanged);

            _columnBuilder = new RecipeFormatBuilder();

            UpdateRecipeFormat();
        }

        protected override void OnViewLoaded(object view)
        {
            View = view;
            
            RecipeFormatBuilder.ApplyTemplate((UserControl)view, this.Columns);
            var u = (PMProcessView)view;
            u.dgCustom.Columns.Clear();

            this.Columns.Apply((c) =>
            {
                c.Header = c;
                u.dgCustom.Columns.Add(c);

            });

            u.dgCustom.FrozenColumnCount = 5;

            base.OnViewLoaded(view);
        }
        
        protected override void OnActivate()
        {
            try
            {
                LoadRolePermissions();

                if (String.IsNullOrEmpty(DisplayingRecipeName))
                {
                    needLoadRecipe = false;
                    var recipeName =
                        (string)QueryDataClient.Instance.Service.GetConfig($"PM.{SystemName}.LastRecipeName");
                    DisplayingRecipeName = recipeName;

                }
                
                //流量需要乘以此系数
                _tmaFlowRatio = (double)QueryDataClient.Instance.Service.GetConfig($"PM.{SystemName}.TMAFlowRatio");
                _tcsFlowRatio = (double)QueryDataClient.Instance.Service.GetConfig($"PM.{SystemName}.TCSFlowRatio");
                _hclFlowRatio = (double)QueryDataClient.Instance.Service.GetConfig($"PM.{SystemName}.HCLFlowRatio");
                _C2H4FlowRatio = (double)QueryDataClient.Instance.Service.GetConfig($"PM.{SystemName}.C2H4FlowRatio");
                _sih4FlowRatio = (double)QueryDataClient.Instance.Service.GetConfig($"PM.{SystemName}.SiH4FlowRatio");
                _pn2FlowRatio = (double)QueryDataClient.Instance.Service.GetConfig($"PM.{SystemName}.PN2FlowRatio");
                base.OnActivate();
            }
            catch (Exception ex)
            {

            }
        }

        protected override void OnDeactivate(bool close)
        {
            ActiveUpdateData = true;
            base.OnDeactivate(close);
        }

        #region Recipe相关控制函数

        /// <summary>
        /// 根据权限设置Recipe参数的可见性。
        /// </summary>
        private void SetRecipeValueVisibilityByCurrentRole()
        {
            // 是否在编辑器中隐藏Recipe参数值
            CurrentRecipe.Steps.IsHideValue = _isHideRecipeValue; // 当权限为None时隐藏，其它时显示。

            // 白名单的Param显示出来
            ApplyRecipeCellAccessPermissionWhitelist();
        }

        /// <summary>
        /// 从数据库读取单元格访问白名单。
        /// </summary>
        /// <returns></returns>
        private DataTable ReadRecipeCellAccessPermissionWhitelist()
        {
            if (CurrentRecipe == null)
                return null;

            var cmd = $"select * from recipe_cell_access_permission_whitelist where \"recipeName\" = '{CurrentRecipe.FullName}'";
            var dt = QueryDataClient.Instance.Service.QueryData(cmd);
            return dt;
        }

        /// <summary>
        /// 如果Recipe参数设置为隐藏，将白名单中的单元格显示出来。
        /// </summary>
        private void ApplyRecipeCellAccessPermissionWhitelist()
        {
            var dt = ReadRecipeCellAccessPermissionWhitelist();

            if (dt != null && dt.Rows.Count > 0)
            {
                foreach (DataRow dataRow in dt.Rows)
                {
                    var stepUid = dataRow["stepUid"].ToString();
                    var controlName = dataRow["columnName"].ToString();

                    var step = CurrentRecipe.Steps.FirstOrDefault(s => s.StepUid == stepUid);
                    var param = step?.FirstOrDefault(p => p.Name == controlName);
                    if (param != null)
                        param.IsHideValue = false;
                }
            }
        }


        /// <summary>
        /// 正在工艺中的Recipe Step发生改变。
        /// </summary>
        /// <param name="currentStepNum"></param>
        private void RecipeStepInProcessChanged(int currentStepNum)
        {
            UpdateRecipeStepProcessedStatus(currentStepNum);
        }

        /// <summary>
        /// 更新Recipe步骤。
        /// </summary>
        /// <param name="currentStepNum"></param>
        private void UpdateRecipeStepProcessedStatus(int currentStepNum)
        {
            if (CurrentRecipe == null)
                return;

            if (CurrentRecipe.Steps.Count <= 0)
                return;

            // Recipe不是当前正在运行的Recipe。
            if (CurrentRecipe.FullName != DisplayingRecipeName)
                return;

            foreach (var step in CurrentRecipe.Steps)
            {
                if (step.StepNo.HasValue)
                {
                    if (step.StepNo < currentStepNum)
                        step.IsProcessed = true;
                    else if(step.StepNo == currentStepNum)
                    {
                        if (IsPMProcess)
                            step.IsProcessed = false;
                        else
                            step.IsProcessed = true;
                    }
                    else
                    {
                        step.IsProcessed = false;
                    }
                }

            }


            // 自动滚到当前做工艺的Step
            var dg = ((PMProcessView)View)?.dgCustom;
            if (currentStepNum > 0)
            {
                var stepScrollTo = currentStepNum - 1;
                if (stepScrollTo < 0)
                    stepScrollTo = 0;

                dg?.ScrollIntoView(CurrentRecipe.Steps[stepScrollTo]);
            }
        }

        /// <summary>
        /// 在UI线程加载Recipe
        /// </summary>
        /// <param name="recipeName"></param>
        private void ProgLoadRecipeOnUiThread(string recipeName)
        {
            lock (_lockerLoadingRecipe)
            {
                if (IsRecipeLoading)
                    return;

                IsRecipeLoading = true;
                NotifyOfPropertyChange(nameof(IsSelectButtonEnable));
            }

            if (recipeName.StartsWith("Sic\\Process"))
            {
                CurrentProcessType = "Process";
            }
            else if (recipeName.StartsWith("Sic\\Routine"))
            {
                CurrentProcessType = "Routine";
            }


            //UpdateRecipeFormat();
            LoadRecipe(recipeName).ContinueWith(t =>
            {
                if (!t.IsCanceled && !t.IsFaulted && t.IsCompleted)
                {
                    Application.Current?.Dispatcher.Invoke(() =>
                    {
                        UpdateRecipeStepProcessedStatus(RecipeStepNumber);
                    });
                }
            }).ContinueWith(t =>
            {
                Application.Current?.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() =>
                {
                    lock (_lockerLoadingRecipe)
                    {
                        IsRecipeLoading = false;
                    }
                }));
            });
         
        }


        #endregion

        private bool stepsVisibility;

        public bool StepsVisibility
        {
            get { return stepsVisibility; }
        }

        public void UpdateRecipeFormat()
        {
            this.Columns = this._columnBuilder.Build($"Sic\\Process", SystemName, false, ClientApp.Instance.UserContext.RoleName);

            this.CurrentRecipe = new RecipeData();
            CurrentRecipe.RecipeChamberType = _columnBuilder.RecipeChamberType;
            CurrentRecipe.RecipeVersion = _columnBuilder.RecipeVersion;
        }

        protected override void Poll()
        {
            try
            {
                base.Poll();

                /*var lastRecipe = (string)QueryDataClient.Instance.Service.GetConfig($"PM.{SystemName}.LastRecipeName");
                
                if (!string.IsNullOrEmpty(lastRecipe))
                {
                    // 如果当前Recipe已经是正在运行的Recipe，则不要重复加载
                    if (CurrentRecipe.FullName == lastRecipe)
                        return;

                    if(!IsRecipeLoading)
                        _progressLoadRecipe.Report(lastRecipe);
                }*/
            }
            catch (Exception ex)
            {
                Debugger.Break();
            }
        }

        protected override void InvokeAfterUpdateProperty(Dictionary<string, object> data)
        {
            try
            {
                if (needLoadRecipe)
                {
                    needLoadRecipe = false;
                    /*UpdateRecipeFormat();
                    OnViewLoaded(View);
                    LoadRecipe(DisplayingRecipeName);*/
                }

                base.InvokeAfterUpdateProperty(data);
            }
            catch (Exception ex)
            {

            }
        }

        // <summary>
        /// 展开DataGrid列组。
        /// </summary>
        /// <param name="col"></param>
        public void ExpandColumnsGroup(ExpanderColumn col)
        {
            foreach (var subCol in col.ChildColumns)
            {
                subCol.UpdateVisibility();
            }
        }

        /// <summary>
        /// 折叠DataGrid列组。
        /// </summary>
        /// <param name="col"></param>
        public void CollapseColumnsGroup(ExpanderColumn col)
        {
            foreach (var subCol in col.ChildColumns)
            {
                subCol.Visibility = Visibility.Collapsed;
            }
        }

        public void ParamsExpanded(ExpanderColumn col)
        {
            int index = this.Columns.IndexOf(col);
            for (var i = index + 1; i < this.Columns.Count; i++)
            {
                if (this.Columns[i] is ExpanderColumn)
                    break;
                this.Columns[i].Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// 校验整个Recipe。
        /// </summary>
        private void ValidateEntireRecipe()
        {
            CurrentRecipe.RecipeTotalTime = 0;

            for (var stepId = 0; stepId < CurrentRecipe.Steps.Count; stepId++)
            {
                var previousIndex = stepId == 0 ? 0 : stepId - 1;
                var currentIndex = stepId;
                var nextIndex = stepId == CurrentRecipe.Steps.Count - 1 ? stepId : stepId + 1;

                var previousStep = CurrentRecipe.Steps[previousIndex];
                var currentStep = CurrentRecipe.Steps[currentIndex];
                var nextStep = CurrentRecipe.Steps[nextIndex];

                //判断每步与上一步值是否相同，不同则以颜色区分
                RenderStepsWithSameValue(previousStep, currentStep);
                
                //如果是Recipe则总时间直接加
                CurrentRecipe.RecipeTotalTime +=
                    (int)((DoubleParam)currentStep[(int)RecipColNo.Time]).Value;


                currentStep.CalRecipeParameterForRunVent(); // 自动计算比例值
                currentStep.Validate(); // 校验单元格
            }
        }

        private static void RenderStepsWithSameValue(IReadOnlyList<Param> previousStep, IReadOnlyList<Param> currentStep)
        {
            for (var i = 0; i < currentStep.Count; i++)
            {
                if (currentStep[i] is DoubleParam currDblParam && previousStep[i] is DoubleParam prevDblParam)
                {
                    currDblParam.IsEqualsToPrevious =
                        GlobalDefs.IsDoubleValueEquals(currDblParam.Value, prevDblParam.Value);
                }

                else if (currentStep[i] is ComboxParam currCbxParam && previousStep[i] is ComboxParam prevCbxParam)
                {
                    if (currCbxParam.Value != prevCbxParam.Value)
                    {
                        currCbxParam.IsEqualsToPrevious = false;
                    }
                    else
                    {
                        currCbxParam.IsEqualsToPrevious = true;
                    }
                }

            }
        }


        /// <summary>
        /// 检查是否所有的参数均有效，并创建错误清单。
        /// </summary>
        /// <returns></returns>
        private bool CheckIfAllCellsValid(out string errorMessage)
        {
            errorMessage = string.Empty;

            /*var lstError = new List<string>();*/
            var validationResultList = new List<RecipeStepValidationInfo>();

            foreach (var step in CurrentRecipe.Steps)
            {
                var invalidParams = step.Where(x => x is IParam p && p.IsValidated == false).ToList();

                if (invalidParams.Any() == false)
                    continue;

                validationResultList.AddRange(invalidParams.Cast<IParam>()
                    .Select(param => new RecipeStepValidationInfo(
                        step,
                        param,
                        param.ValidationError)));
            }

            // 没错误直接返回
            if (validationResultList.Count <= 0)
                return true;
            
            var errCnt = 0;
            var strInfo = new StringBuilder();
            foreach (var t in validationResultList)
            {
                strInfo.AppendLine(t.ToString());
                errCnt++;

                if (errCnt > 10)
                    break;
            }

            if (errCnt < validationResultList.Count)
                strInfo.AppendLine("\r\n<please check for more errors...>");

            errorMessage = strInfo.ToString();
            return false;
        }

        /// <summary>
        /// 将发生变更的配方推送到当前工艺。
        /// </summary>
        public void PushRecipeToCurrentProcess()
        {
            // 如果配方没变，不需要推送
            if (!CurrentRecipe.IsChanged)
            {
                DialogBox.ShowWarning("The recipe has not changed.");
                return;
            }

            ValidateEntireRecipe();
            if (CheckIfAllCellsValid(out var errors) == false)
            {
                var ret = DialogBox.ShowDialog(DialogButton.Yes | DialogButton.No, DialogType.WARNING,
                    $"{errors}\r\n Are you sure to push changes to current process?");
                if (ret == DialogButton.No)
                    return;
                /*var mbr = MessageBox.Show($"{errors}\r\n Are you sure to push changes to current process", "Warning",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (mbr == MessageBoxResult.No)
                    return;*/
            }

            var xmlRecipe = CurrentRecipe.GetXmlString();
            InvokeClient.Instance.Service.DoOperation($"PM1.ReloadRecipe", xmlRecipe);
            //InvokeClient.Instance.Service.DoOperation($"PM2.ReloadRecipe");
        }


        public void SaveToBaselineRecipe()
        {
            if (string.IsNullOrEmpty(CurrentRecipe.Name))
            {
                DialogBox.ShowError("Recipe name can't be empty");
                return;
            }

            // 如果配方没变，不需要推送
            if (!CurrentRecipe.IsChanged)
            {
                DialogBox.ShowInfo("The recipe has not changed.");
                return;
            }

            ValidateEntireRecipe();

            if (CheckIfAllCellsValid(out var errors) == false)
            {
                var ret = DialogBox.ShowDialog(DialogButton.Yes | DialogButton.No, DialogType.WARNING,
                    $"{errors}\r\n Are you sure to save to baseline recipe?");
                if (ret == DialogButton.No)
                    return;
            }

            var result = false;


            CurrentRecipe.Revisor = BaseApp.Instance.UserContext.LoginName;
            CurrentRecipe.ReviseTime = DateTime.Now;

            var recipeProvider = new RecipeProvider();
            result = recipeProvider.WriteRecipeFile(CurrentRecipe.PrefixPath, CurrentRecipe.Name, CurrentRecipe.GetXmlString());

            if (result)
            {
                CurrentRecipe.DataSaved();
            }
            else
            {
                MessageBox.Show("Save failed!");
            }
        }


        public void ParamsCollapsed(ExpanderColumn col)
        {
            int index = this.Columns.IndexOf(col);
            for (var i = index + 1; i < this.Columns.Count; i++)
            {
                if (this.Columns[i] is ExpanderColumn)
                    break;
                this.Columns[i].Visibility = Visibility.Collapsed;
            }
        }
        

        public async void SelectRecipe()
        {
            var dialog = new RecipeSelectDialogViewModel
            {
                DisplayName = "Select Recipe"
            };
            
            var wm = new WindowManager();
            var bret = wm.ShowDialog(dialog);
            if (bret == true)
            {
                var recipeName = dialog.DialogResult;

                if (recipeName.StartsWith("Sic\\Process"))
                {
                    CurrentProcessType = "Process";
                }
                else if (recipeName.StartsWith("Sic\\Routine"))
                {
                    CurrentProcessType = "Routine";
                }

                DisplayingRecipeName = recipeName;

                InvokeClient.Instance.Service.DoOperation($"{SystemName}.SelectRecipe", recipeName);
                InvokeClient.Instance.Service.DoOperation("System.SetConfig", $"PM.{SystemName}.LastRecipeName", recipeName); // 记录最后一次工艺文件名称
            }

        }

        public bool CheckColumnDataAvalible()
        {
            //return true;

            //if (CurrentProcessType == ProcessTypeFileList[1].ProcessType)
            //{
            //    return true;
            //}

            List<string> lstNotCorrectInfo = new List<string>();
            List<string> lstOutOfRange = new List<string>();

            List<int> lstSplitColums = new List<int>()
            {
                (int)RecipColNo.SHTotalFlowSplitRatio,
                (int)RecipColNo.SiSourceSplitRatio,
                (int)RecipColNo.CSourceSplitRatio,
                (int)RecipColNo.DopeSplitRatio
            };


            for (var a = 0; a < this.CurrentRecipe.Steps.Count; a++)
            {
                var invalidParam =
                    CurrentRecipe.Steps[a]
                        .Where(param => param is IParam p && p.IsValidated == false).ToList();

                lstNotCorrectInfo.AddRange(invalidParam.Cast<IParam>()
                    .Select(iparam => $"(Step {a + 1}) {iparam.DisplayName}: {iparam.ValidationError}"));
                
                /*for (int i = 0; i < this.Columns.Count; i++)
                {
                    if (this.Columns[i] is DoubleColumn)
                    {
                        DoubleColumn column = (DoubleColumn)this.Columns[i];
                        double maxValue = column.Maximun;
                        double minValue = column.Minimun;

                        if (maxValue != minValue)
                        {
                            double cValue = 0;
                            if (!double.TryParse((this.CurrentRecipe.Steps[a][i] as DoubleParam).Value, out cValue))
                            {
                                if ((this.CurrentRecipe.Steps[a][i] as DoubleParam).Value != "Hold")
                                {
                                    lstNotCorrectInfo.Add($"Step {a + 1} {column.DisplayName}: value {(this.CurrentRecipe.Steps[a][i] as DoubleParam).Value} is incorrect numerical format ");
                                    (this.CurrentRecipe.Steps[a][i] as DoubleParam).Foreground = "Red";
                                }
                            }
                            else if (cValue > maxValue || cValue < minValue)
                            {
                                lstOutOfRange.Add($"Step {a + 1} {column.DisplayName}: value {cValue} is out of range {minValue}-{maxValue}");
                                (this.CurrentRecipe.Steps[a][i] as DoubleParam).Foreground = "Red";
                            }
                        }
                    }
                    else if (lstSplitColums.Contains(i))
                    {
                        double a1 = 0;
                        double a2 = 0;
                        double a3 = 0;
                        string[] arrSplitRatio = (this.CurrentRecipe.Steps[a][i] as StringParam).Value.Split(':');
                        if (arrSplitRatio.Length != 3
                            || !double.TryParse(arrSplitRatio[0], out a1) || !double.TryParse(arrSplitRatio[1], out a2) || !double.TryParse(arrSplitRatio[2], out a3)
                            || a1 <= 0 || a2 <= 0 || a3 <= 0)
                        {
                            lstNotCorrectInfo.Add($"(Step {i + 1}) {this.Columns[i].DisplayName}: value is not avalible!");
                        }
                    }
                }*/
            }

            if (lstNotCorrectInfo.Count > 0 || lstOutOfRange.Count > 0)
            {
                string strInfo = "";
                for (int i = 0; i < lstNotCorrectInfo.Count; i++)
                {
                    strInfo += lstNotCorrectInfo[i] + "\r\n";
                }
                for (int i = 0; i < lstOutOfRange.Count; i++)
                {
                    strInfo += lstOutOfRange[i] + "\r\n";
                }
                MessageBox.Show(strInfo, "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            return true;
        }

        public void DeleteProcessData()
        {

        }

        /// <summary>
        /// 重新加载配方，当前修改的参数被抛弃。
        /// </summary>
        public void RestoreToBaselineRecipe()
        {
            // 如果改变，提示
            if (CurrentRecipe.IsChanged)
            {
                if (DialogBox.Confirm(
                        "The recipe has changed, are you sure to discard changes and reload baseline recipe?") == false)
                    return;
            }

            if (CurrentRecipe != null 
                && !string.IsNullOrEmpty(CurrentRecipe.FullName))
                _progressLoadRecipe.Report(CurrentRecipe.FullName);
        }

        /// <summary>
        /// 根据配置获取Cascade方式呈现Recipe的Dispatcher，或返回null。
        /// </summary>
        /// <returns></returns>
        private static Dispatcher GetLoadingDispatcher()
        {
            // 判断Step呈现方式
            var isCascadeLoading = (bool)QueryDataClient.Instance.Service.GetConfig("System.RecipeCascadeLoading");
            var dispatcher = isCascadeLoading ? Application.Current?.Dispatcher: null;
            if (dispatcher == null || !dispatcher.CheckAccess())
                return null;

            return dispatcher;
        }

        private async Task LoadRecipe(string selectedRecipePath)
        {
            CurrentRecipe.Clear();

            var array = selectedRecipePath.Split(new char[] { '\\' });

            var recipeName = array.Last();
            var prefixPath = selectedRecipePath.Replace(recipeName, "").TrimEnd('\\');

            var recipeProvider = new RecipeProvider();
            var recipeContent = recipeProvider.ReadRecipeFile(prefixPath, recipeName);

            if (string.IsNullOrEmpty(recipeContent))
            {
                MessageBox.Show($"{prefixPath}\\{recipeName} is empty, please confirm the file is valid.");
                return;
            }

            CurrentRecipe.RecipeChamberType = _columnBuilder.RecipeChamberType;
            CurrentRecipe.RecipeVersion = _columnBuilder.RecipeVersion;

            await CurrentRecipe.InitData(prefixPath, recipeName, recipeContent, Columns,
                _columnBuilder.Configs, SystemName, GetLoadingDispatcher(), _isHideRecipeValue);

            // 显示Cell访问白名单中的参数
            SetRecipeValueVisibilityByCurrentRole();
        }

        public void Start()
        {
            ChooseDialogBoxViewModel dialog = new ChooseDialogBoxViewModel();
            dialog.DisplayName = "Tips";
            dialog.InfoStr = "Please Check All Gas Ready Before Start Process!";

            WindowManager wm = new WindowManager();
            bool? bret = wm.ShowDialog(dialog);
            if (!bret.HasValue || !bret.Value)
            {
                return;
            }

            var array = DisplayingRecipeName.Split(new char[] { '\\' });
            if (array[1] == "Process")
            {
                InvokeClient.Instance.Service.DoOperation($"{SystemName}.RunRecipe", DisplayingRecipeName, false, true);
            }
            else if (array[1] == "Routine")
            {
                InvokeClient.Instance.Service.DoOperation($"{SystemName}.RunPMMacro", DisplayingRecipeName);
            }
        }

        public void Skip()
        {
            var ret = ShowYesNoDialog("Are you sure to skip the current recipe?", "Warn");
            if (ret.HasValue && ret == true)
            {
                InvokeClient.Instance.Service.DoOperation($"{SystemName}.RecipeSkipStep");
            }
        }

        public void Stop()
        {
            var ret = ShowYesNoDialog("Are you sure to stop the current recipe?", "Warn");
            if (ret.HasValue && ret == true)
            {
                InvokeClient.Instance.Service.DoOperation($"{SystemName}.Abort");
            }
            
        }

        public void Pause()
        {
            var ret = ShowYesNoDialog("Are you sure to pause the current recipe?", "Warn");
            if (ret.HasValue && ret == true)
            {
                InvokeClient.Instance.Service.DoOperation($"{SystemName}.Pause");
            }
        }

        public void Continue()
        {
            var ret = ShowYesNoDialog("Are you sure to continue to run the current recipe?", "Warn");
            if (ret.HasValue && ret == true)
            {
                InvokeClient.Instance.Service.DoOperation($"{SystemName}.Continue", "Step continue");
            }
        }

        public void Abort()
        {
            var ret = ShowYesNoDialog("Are you sure to abort the current recipe?", "Warn");
            if (ret.HasValue && ret == true)
            {
                InvokeClient.Instance.Service.DoOperation($"{SystemName}.Abort");
            }
        }

        /// <summary>
        /// To fix issue 6: 对运行中的Job操作时弹出提示对话框。
        /// </summary>
        /// <param name="message"></param>
        /// <param name="title"></param>
        /// <returns></returns>
        private static bool? ShowYesNoDialog(string message, string title)
        {
           var ret =  DialogBox.ShowDialog(DialogButton.Yes | DialogButton.No, DialogType.CONFIRM, message, title);
           if (ret == DialogButton.Yes)
               return true;

           return null;
        }

        #region 腔体绑定数据
        [Subscription("TC1.L1InputTempSetPoint")]
        public float L1InputTemp { get; set; }

        [Subscription("TC1.L2InputTempSetPoint")]
        public float L2InputTemp { get; set; }

        [Subscription("TC1.L3InputTempSetPoint")]
        public float L3InputTemp { get; set; }

        [Subscription("TC2.L3InputTempSetPoint")]
        public float SCRL3InputTemp { get; set; }


        [Subscription("TC1.L1TargetSPSetPoint")]
        public float L1TargetSP { get; set; }

        [Subscription("TC1.L2TargetSPSetPoint")]
        public float L2TargetSP { get; set; }

        [Subscription("TC1.L3TargetSPSetPoint")]
        public float L3TargetSP { get; set; }

        [Subscription("TC2.L3TargetSPSetPoint")]
        public float SCRL3TargetSP { get; set; }

        [Subscription("TC1.TempCtrlTCIN")]
        public float PM1Temprature { get; set; }

        //底部温度
        [Subscription("TC1.TempCtrlTCIN")]
        public float TC1Temp2 { get; set; }

        [Subscription("TC1.HeaterModeSetPoint")]
        public float TC1HeaterMode { get; set; }
        public string TC1Mode
        {
            get
            {
                switch (TC1HeaterMode)
                {
                    case 0: return "Power";
                    case 1: return "Pyro";
                }
                return "Power";

            }
        }

        [Subscription("TC2.HeaterModeSetPoint")]
        public float TC2HeaterMode { get; set; }
        public string TC2Mode
        {
            get
            {
                switch (TC2HeaterMode)
                {
                    case 0: return "Power";
                    case 1: return "TC";
                    case 2: return "Pyro";
                }
                return "Power";

            }
        }


        [Subscription("SCR1.PowerFeedBack")]
        public float SCR1Power { get; set; }

        [Subscription("SCR2.PowerFeedBack")]
        public float SCR2Power { get; set; }

        [Subscription("SCR3.PowerFeedBack")]
        public float SCR3Power { get; set; }

        [Subscription("PSU1.OutputPowerFeedBack")]
        public float PSU1Power { get; set; }

        [Subscription("PSU2.OutputPowerFeedBack")]
        public float PSU2Power { get; set; }

        [Subscription("PSU3.OutputPowerFeedBack")]
        public float PSU3Power { get; set; }


        [Subscription("PMServo.ActualSpeedFeedback")]
        public float ActualSpeedFeedback { get; set; }

        [Subscription("PT1.FeedBack")]
        public float PT1Pressure { get; set; }
        #endregion 腔体绑定数据

        #region MFC
        [Subscription("Mfc1.DeviceData")]
        public AITMfcData Mfc1Data { get; set; }

        [Subscription("Mfc2.DeviceData")]
        public AITMfcData Mfc2Data { get; set; }

        [Subscription("Mfc3.DeviceData")]
        public AITMfcData Mfc3Data { get; set; }

        [Subscription("Mfc4.DeviceData")]
        public AITMfcData Mfc4Data { get; set; }

        [Subscription("Mfc5.DeviceData")]
        public AITMfcData Mfc5Data { get; set; }

        [Subscription("Mfc6.DeviceData")]
        public AITMfcData Mfc6Data { get; set; }

        [Subscription("Mfc7.DeviceData")]
        public AITMfcData Mfc7Data { get; set; }

        [Subscription("Mfc8.DeviceData")]
        public AITMfcData Mfc8Data { get; set; }

        [Subscription("Mfc9.DeviceData")]
        public AITMfcData Mfc9Data { get; set; }

        [Subscription("Mfc10.DeviceData")]
        public AITMfcData Mfc10Data { get; set; }

        [Subscription("Mfc11.DeviceData")]
        public AITMfcData Mfc11Data { get; set; }

        [Subscription("Mfc12.DeviceData")]
        public AITMfcData Mfc12Data { get; set; }

        [Subscription("Mfc13.DeviceData")]
        public AITMfcData Mfc13Data { get; set; }

        [Subscription("Mfc14.DeviceData")]
        public AITMfcData Mfc14Data { get; set; }

        [Subscription("Mfc15.DeviceData")]
        public AITMfcData Mfc15Data { get; set; }

        [Subscription("Mfc16.DeviceData")]
        public AITMfcData Mfc16Data { get; set; }



        [Subscription("Mfc19.DeviceData")]
        public AITMfcData Mfc19Data { get; set; }

        [Subscription("Mfc20.DeviceData")]
        public AITMfcData Mfc20Data { get; set; }



        [Subscription("Mfc22.DeviceData")]
        public AITMfcData Mfc22Data { get; set; }

        [Subscription("Mfc23.DeviceData")]
        public AITMfcData Mfc23Data { get; set; }



        [Subscription("Mfc25.DeviceData")]
        public AITMfcData Mfc25Data { get; set; }

        [Subscription("Mfc26.DeviceData")]
        public AITMfcData Mfc26Data { get; set; }

        [Subscription("Mfc27.DeviceData")]
        public AITMfcData Mfc27Data { get; set; }

        [Subscription("Mfc28.DeviceData")]
        public AITMfcData Mfc28Data { get; set; }

        [Subscription("Mfc29.DeviceData")]
        public AITMfcData Mfc29Data { get; set; }


        [Subscription("Mfc30.DeviceData")]
        public AITMfcData Mfc30Data { get; set; }

        [Subscription("Mfc31.DeviceData")]
        public AITMfcData Mfc31Data { get; set; }



        [Subscription("Mfc32.DeviceData")]
        public AITMfcData Mfc32Data { get; set; }

        [Subscription("Mfc33.DeviceData")]
        public AITMfcData Mfc33Data { get; set; }



        [Subscription("Mfc35.DeviceData")]
        public AITMfcData Mfc35Data { get; set; }

        [Subscription("Mfc36.DeviceData")]
        public AITMfcData Mfc36Data { get; set; }

        [Subscription("Mfc37.DeviceData")]
        public AITMfcData Mfc37Data { get; set; }

        [Subscription("Mfc38.DeviceData")]
        public AITMfcData Mfc38Data { get; set; }

        [Subscription("Mfc40.DeviceData")]
        public AITMfcData Mfc40Data { get; set; }
        #endregion

        #region Valve

        [Subscription("V27.DeviceData")]
        public AITValveData V27 { get; set; }

        [Subscription("V25.DeviceData")]
        public AITValveData V25 { get; set; }

        [Subscription("V31.DeviceData")]
        public AITValveData V31 { get; set; }

        [Subscription("V32.DeviceData")]
        public AITValveData V32 { get; set; }

        [Subscription("V33.DeviceData")]
        public AITValveData V33 { get; set; }

        [Subscription("V33s.DeviceData")]
        public AITValveData V33s { get; set; }

        [Subscription("V35.DeviceData")]
        public AITValveData V35 { get; set; }

        [Subscription("V36.DeviceData")]
        public AITValveData V36 { get; set; }

        [Subscription("V37.DeviceData")]
        public AITValveData V37 { get; set; }

        [Subscription("V37s.DeviceData")]
        public AITValveData V37s { get; set; }


        [Subscription("V39.DeviceData")]
        public AITValveData V39 { get; set; }

        [Subscription("V39s.DeviceData")]
        public AITValveData V39s { get; set; }

        [Subscription("V40.DeviceData")]
        public AITValveData V40 { get; set; }

        [Subscription("V40s.DeviceData")]
        public AITValveData V40s { get; set; }


        [Subscription("V41.DeviceData")]
        public AITValveData V41 { get; set; }

        [Subscription("V41s.DeviceData")]
        public AITValveData V41s { get; set; }


        [Subscription("V42.DeviceData")]
        public AITValveData V42 { get; set; }

        [Subscription("V43.DeviceData")]
        public AITValveData V43 { get; set; }

        [Subscription("V43s.DeviceData")]
        public AITValveData V43s { get; set; }


        [Subscription("V45.DeviceData")]
        public AITValveData V45 { get; set; }


        [Subscription("V46.DeviceData")]
        public AITValveData V46 { get; set; }

        [Subscription("V46s.DeviceData")]
        public AITValveData V46s { get; set; }

        [Subscription("V48.DeviceData")]
        public AITValveData V48 { get; set; }

        [Subscription("V48s.DeviceData")]
        public AITValveData V48s { get; set; }


        [Subscription("V49.DeviceData")]
        public AITValveData V49 { get; set; }

        [Subscription("V50.DeviceData")]
        public AITValveData V50 { get; set; }

        [Subscription("V50s.DeviceData")]
        public AITValveData V50s { get; set; }

        [Subscription("V51.DeviceData")]
        public AITValveData V51 { get; set; }

        [Subscription("V51s.DeviceData")]
        public AITValveData V51s { get; set; }


        [Subscription("V52.DeviceData")]
        public AITValveData V52 { get; set; }

        [Subscription("V52s.DeviceData")]
        public AITValveData V52s { get; set; }

        [Subscription("V53.DeviceData")]
        public AITValveData V53 { get; set; }

        [Subscription("V53s.DeviceData")]
        public AITValveData V53s { get; set; }

        [Subscription("V54.DeviceData")]
        public AITValveData V54 { get; set; }

        [Subscription("V54s.DeviceData")]
        public AITValveData V54s { get; set; }

        [Subscription("V55.DeviceData")]
        public AITValveData V55 { get; set; }

        [Subscription("V56.DeviceData")]
        public AITValveData V56 { get; set; }

        [Subscription("V58.DeviceData")]
        public AITValveData V58 { get; set; }

        [Subscription("V58s.DeviceData")]
        public AITValveData V58s { get; set; }



        [Subscription("V59.DeviceData")]
        public AITValveData V59 { get; set; }

        [Subscription("V60.DeviceData")]
        public AITValveData V60 { get; set; }

        [Subscription("V61.DeviceData")]
        public AITValveData V61 { get; set; }

        [Subscription("V62.DeviceData")]
        public AITValveData V62 { get; set; }

        [Subscription("V63.DeviceData")]
        public AITValveData V63 { get; set; }

        [Subscription("V64.DeviceData")]
        public AITValveData V64 { get; set; }

        [Subscription("V65.DeviceData")]
        public AITValveData V65 { get; set; }


        [Subscription("V68.DeviceData")]
        public AITValveData V68 { get; set; }

        [Subscription("V69.DeviceData")]
        public AITValveData V69 { get; set; }

        [Subscription("V70.DeviceData")]
        public AITValveData V70 { get; set; }

        [Subscription("V72.DeviceData")]
        public AITValveData V72 { get; set; }

        [Subscription("V73.DeviceData")]
        public AITValveData V73 { get; set; }

        [Subscription("V74.DeviceData")]
        public AITValveData V74 { get; set; }

        [Subscription("V75.DeviceData")]
        public AITValveData V75 { get; set; }

        [Subscription("V76.DeviceData")]
        public AITValveData V76 { get; set; }

        [Subscription("V87.DeviceData")]
        public AITValveData V87 { get; set; }

        [Subscription("V88.DeviceData")]
        public AITValveData V88 { get; set; }

        [Subscription("V89.DeviceData")]
        public AITValveData V89 { get; set; }

        [Subscription("V90.DeviceData")]
        public AITValveData V90 { get; set; }

        [Subscription("V91.DeviceData")]
        public AITValveData V91 { get; set; }

        [Subscription("V92.DeviceData")]
        public AITValveData V92 { get; set; }

        [Subscription("V93.DeviceData")]
        public AITValveData V93 { get; set; }

        [Subscription("V94.DeviceData")]
        public AITValveData V94 { get; set; }

        [Subscription("V95.DeviceData")]
        public AITValveData V95 { get; set; }

        [Subscription("V96.DeviceData")]
        public AITValveData V96 { get; set; }

        [Subscription("V97.DeviceData")]
        public AITValveData V97 { get; set; }

        [Subscription("EPV1.DeviceData")]
        public AITValveData EPV1 { get; set; }

        [Subscription("EPV2.DeviceData")]
        public AITValveData EPV2 { get; set; }

        [Subscription("V99.DeviceData")]
        public AITValveData V99 { get; set; }

        [Subscription("V99s.DeviceData")]
        public AITValveData V99s { get; set; }


        #endregion

        #region Pressure
        [Subscription("Pressure1.DeviceData")]
        public AITPressureMeterData PT1Data { get; set; }

        [Subscription("Pressure2.DeviceData")]
        public AITPressureMeterData PT2Data { get; set; }
        [Subscription("Pressure3.DeviceData")]
        public AITPressureMeterData PT3Data { get; set; }


        [Subscription("Pressure4.DeviceData")]
        public AITPressureMeterData PT4Data { get; set; }
        [Subscription("Pressure5.DeviceData")]
        public AITPressureMeterData PT5Data { get; set; }
        [Subscription("Pressure6.DeviceData")]
        public AITPressureMeterData PT6Data { get; set; }

        [Subscription("Pressure7.DeviceData")]
        public AITPressureMeterData PT7Data { get; set; }

        [Subscription("PT1.DeviceData")]
        public AITPressureMeterData ChamPress { get; set; }

        [Subscription("PT2.DeviceData")]
        public AITPressureMeterData ForelinePress { get; set; }


        #endregion

        #region 特气显示颜色

        public SolidColorBrush PN2Color
        {
            get
            {
                if (IsPN2RunMode)
                {
                    return new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 0, 0));
                }
                else
                {
                    return new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 0, 0));
                }
            }
        }
        public SolidColorBrush C2H4Color
        {
            get
            {
                if (IsC2H4RunMode)
                {
                    return new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 0, 0));
                }
                else
                {
                    return new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 0, 0));
                }
            }
        }
        public SolidColorBrush SIH4Color
        {
            get
            {
                if (IsSiH4RunMode)
                {
                    return new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 0, 0));
                }
                else
                {
                    return new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 0, 0));
                }
            }
        }
        public SolidColorBrush HCLColor
        {
            get
            {
                if (IsHCLRunMode)
                {
                    return new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 0, 0));
                }
                else
                {
                    return new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 0, 0));
                }
            }
        }

        public SolidColorBrush TMAColor
        {
            get
            {
                if (IsTMARunMode)
                {
                    return new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 0, 0));
                }
                else
                {
                    return new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 0, 0));
                }
            }
        }

        public SolidColorBrush TCSColor
        {
            get
            {
                if (IsTCSRunMode)
                {
                    return new SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 0, 0));
                }
                else
                {
                    return new SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 0, 0));
                }
            }
        }

        public SolidColorBrush PN2FlowColor
        {
            get
            {
                if (IsPN2RunMode)
                {
                    return new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 0, 255, 255));
                }
                else
                {
                    return new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 190, 190, 190));
                }
            }
        }
        public SolidColorBrush C2H4FlowColor
        {
            get
            {
                if (IsC2H4RunMode)
                {
                    return new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 0, 255, 255));
                }
                else
                {
                    return new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 190, 190, 190));
                }
            }
        }
        public SolidColorBrush SIH4FlowColor
        {
            get
            {
                if (IsSiH4RunMode)
                {
                    return new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 0, 255, 255));
                }
                else
                {
                    return new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 190, 190, 190));
                }
            }
        }
        public SolidColorBrush HCLFlowColor
        {
            get
            {
                if (IsHCLRunMode)
                {
                    return new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 0, 255, 255));
                }
                else
                {
                    return new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 190, 190, 190));
                }
            }
        }

        public SolidColorBrush TMAFlowColor
        {
            get
            {
                if (IsTMARunMode)
                {
                    return new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 0, 255, 255));
                }
                else
                {
                    return new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 190, 190, 190));
                }
            }
        }

        public SolidColorBrush TCSFlowColor
        {
            get
            {
                if (IsTCSRunMode)
                {
                    return new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 0, 255, 255));
                }
                else
                {
                    return new SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 190, 190, 190));
                }
            }
        }
        #endregion



        public bool IsPN2RunMode => N2FlowMode == "Run";
        public bool IsHCLRunMode => V54.IsOpen;
        public bool IsSiH4RunMode => V55.IsOpen && !V56.IsOpen;
        public bool IsC2H4RunMode => V59.IsOpen && !V60.IsOpen;
        public bool IsTCSRunMode => V53.IsOpen;
        public bool IsTMARunMode => V41.IsOpen && !V42.IsOpen;


        public double ArFlow => CalArFlowSum();
        public double H2Flow => CalH2FlowSum();
        public double PN2Flow => GetPN2MFCFlow(IsPN2RunMode) * _pn2FlowRatio;
        public double HCLFlow => GetHclMFCFlow(IsHCLRunMode) * _hclFlowRatio;
        public double SiH4Flow => GetSiH4MFCFlow(IsSiH4RunMode) * _sih4FlowRatio;
        public double C2H4Flow => GetC2H4MFCFlow(IsC2H4RunMode) * _C2H4FlowRatio;
        public double TCSFlow => GetTCSMFCFlow(IsTCSRunMode) * _tcsFlowRatio;
        public double TMAFlow => GetTMAMFCFlow(IsTMARunMode) * _tmaFlowRatio;

        public bool ISPN2Flow => PN2Flow > 0 && IsPN2RunMode;
        public bool ISHCLFlow => HCLFlow > 0 && IsHCLRunMode;
        public bool ISSiH4Flow => SiH4Flow > 0 && IsSiH4RunMode;
        public bool ISC2H4Flow => C2H4Flow > 0 && IsC2H4RunMode;
        public bool ISArFlow => ArFlow > 0;
        public bool ISH2Flow => H2Flow > 0;
        public bool ISTCSFlow => TCSFlow > 0 && IsTCSRunMode;
        public bool ISTMAFlow => TMAFlow > 0 && IsTMARunMode;


        #region 计算Ar 和 H2 总流量
        
        private double CalArFlowSum()
        {
            // 参与计算H2流量的MFC
            double divFlowCount = 0;
            var mfcs = new List<AITMfcData>();
            if (!V32.IsOpen)
            {
                return 0;
            }
            
            if (V64.IsOpen != V65.IsOpen)
            {
                if (V65.IsOpen)
                {
                    if (V97.IsOpen) mfcs.Add(Mfc40Data);
                    if (V91.IsOpen) mfcs.Add(Mfc33Data);
                    if (V87.IsOpen) mfcs.Add(Mfc27Data);
                }
            }
            
            if (V33s.IsOpen && V35.IsOpen)
            {
                if (V97.IsOpen) mfcs.Add(Mfc40Data);
                if (V91.IsOpen) mfcs.Add(Mfc33Data);
                if (V90.IsOpen) mfcs.Add(Mfc31Data);
                if (V89.IsOpen) mfcs.Add(Mfc29Data);
                if (V88.IsOpen) mfcs.Add(Mfc28Data);
                if (V87.IsOpen) mfcs.Add(Mfc27Data);
            }

            if (V33s.IsOpen && V36.IsOpen)
            {
                if (V63.IsOpen)
                {
                    if (V90.IsOpen) mfcs.Add(Mfc25Data);
                    if (V89.IsOpen) mfcs.Add(Mfc26Data);
                    if (V88.IsOpen) mfcs.Add(Mfc15Data);

                    divFlowCount += IsC2H4RunMode ? GetC2H4MFCFlow(true) : 0;

                }

                if (V62.IsOpen)
                {
                    if (V89.IsOpen) mfcs.Add(Mfc23Data);
                    if (V90.IsOpen) mfcs.Add(Mfc22Data);
                    if (V88.IsOpen) mfcs.Add(Mfc9Data);

                    divFlowCount += IsSiH4RunMode ? GetSiH4MFCFlow(true) : 0;
                    divFlowCount += IsHCLRunMode ? GetHclMFCFlow(true) : 0;
                    divFlowCount += IsTCSRunMode ? GetTCSMFCFlow(true) : 0;

                }

                if (V61.IsOpen)
                {
                    if (V90.IsOpen) mfcs.Add(Mfc19Data);
                    if (V89.IsOpen) mfcs.Add(Mfc20Data);
                    if (V88.IsOpen) mfcs.Add(Mfc2Data);


                    divFlowCount += IsPN2RunMode ? GetPN2MFCFlow(true) : 0;
                    divFlowCount += IsTMARunMode ? GetTMAMFCFlow(true) : 0;

                }
            }

            if (V68.IsOpen)
            {
                if (V96.IsOpen) mfcs.Add(Mfc38Data);
                if (V95.IsOpen) mfcs.Add(Mfc37Data);
                if (V94.IsOpen) mfcs.Add(Mfc36Data);
                if (V93.IsOpen) mfcs.Add(Mfc35Data);
                if (V92.IsOpen) mfcs.Add(Mfc32Data);
            }

            var flowCount = mfcs.Sum(mfcData => mfcData.FeedBack);
            return (flowCount - divFlowCount > 0)? flowCount - divFlowCount:0 ;
        }


        private double CalH2FlowSum()
        {
            // 参与计算H2流量的MFC
            var mfcs = new List<AITMfcData>();

            if(!V31.IsOpen)
            {
                return 0;
            }

            if (V64.IsOpen != V65.IsOpen)
            {
                if (V64.IsOpen)
                {
                    if (V97.IsOpen) mfcs.Add(Mfc40Data);
                    if (V91.IsOpen) mfcs.Add(Mfc33Data);
                    if (V87.IsOpen) mfcs.Add(Mfc27Data);
                }
            }

            if (V33.IsOpen && V35.IsOpen)
            {
                if (V90.IsOpen) mfcs.Add(Mfc31Data);
                if (V89.IsOpen) mfcs.Add(Mfc29Data);
                if (V88.IsOpen) mfcs.Add(Mfc28Data);
            }

            if (V33.IsOpen && V35.IsOpen && V36.IsOpen && V63.IsOpen)
            {
                if (!V59.IsOpen)
                {
                    if (V90.IsOpen) mfcs.Add(Mfc26Data);
                    if (V89.IsOpen) mfcs.Add(Mfc25Data);
                    if (V88.IsOpen) mfcs.Add(Mfc15Data);
                }

                if (!V58.IsOpen && V59.IsOpen && !V60.IsOpen)
                {
                    if (V90.IsOpen) mfcs.Add(Mfc26Data);
                    if (V89.IsOpen) mfcs.Add(Mfc25Data);
                    if (V88.IsOpen) mfcs.Add(Mfc15Data);
                }

                if (V61.IsOpen)
                {
                    if (V90.IsOpen) mfcs.Add(Mfc19Data);
                    if (V89.IsOpen) mfcs.Add(Mfc20Data);
                    if (V88.IsOpen) mfcs.Add(Mfc2Data);
                }
            }

            var flowCount = mfcs.Sum(mfcData => mfcData.FeedBack);
            return flowCount;
        }
        #endregion

        private double GetC2H4MFCFlow(bool isRun)
        {
            double flow = 0;

            if (V58.IsOpen == false) // V58关闭时通载气，不计算C2H4流量
                return 0;

            if (isRun)
            {
                // 气体进入反应腔
                if (V59.IsOpen && !V60.IsOpen && V63.IsOpen && (V90.IsOpen || V89.IsOpen || V88.IsOpen))
                {
                    flow = Mfc16Data.FeedBack;
                }
            }
            else
            {
                // 气体直接排走
                if (!V59.IsOpen && V60.IsOpen)
                {
                    flow = Mfc16Data.FeedBack;
                }
            }
            return flow;
        }

        private double GetSiH4MFCFlow(bool isRun)
        {
            double flow = 0;

            if (V52.IsOpen == false) // V52关闭时通载气，不计算SiH4流量
                return 0;

            if (isRun)
            {
                // 气体进入反应腔
                if (V55.IsOpen && !V56.IsOpen && V62.IsOpen && (V90.IsOpen || V89.IsOpen || V88.IsOpen))
                {
                    flow += Mfc14Data.FeedBack;
                }
            }
            else
            {
                // 气体直接排走
                if (!V55.IsOpen && V56.IsOpen)
                {
                    flow += Mfc14Data.FeedBack;
                }
            }
            return flow;
        }


        private double GetHclMFCFlow(bool isRun)
        {
            double flow = 0;

            if (V51.IsOpen == false) // V51关闭时通载气，不计算HCL流量
                return 0;

            if (isRun)
            {
                // 气体进入反应腔
                if (V54.IsOpen && V62.IsOpen && (V90.IsOpen || V89.IsOpen || V88.IsOpen))
                {
                    flow += Mfc13Data.FeedBack;
                }
            }
            else
            {
                // 气体直接排走
                if (V51.IsOpen && !V54.IsOpen)
                {
                    flow += Mfc13Data.FeedBack;
                }
            }
            return flow;
        }

        private double GetPN2MFCFlow(bool isRun)
        {

            //新计算方式
            double flow = 0;
            if (IsPMProcess)
            {
                if (N2FlowMode == "Purge")
                {
                    flow = 0;
                }
                else if (N2FlowMode == "Vent")
                {
                    flow = Mfc4Data.FeedBack + Mfc5Data.FeedBack;
                }
                else if (N2FlowMode == "Run")
                {
                    flow = Mfc6Data.FeedBack * Mfc4Data.FeedBack / (Mfc3Data.FeedBack + Mfc4Data.FeedBack);

                    if (V40.IsOpen)
                    {
                        flow += Mfc5Data.FeedBack;
                    }
                }
            }

            return flow;
        }

        private double GetTCSMFCFlow(bool isRun)
        {
            double flow = 0;
            if (isRun)
            {
                if (!V49.IsOpen && V48.IsOpen && V53.IsOpen && V62.IsOpen && (V90.IsOpen || V89.IsOpen || V88.IsOpen)) { flow += Mfc10Data.FeedBack + Mfc11Data.FeedBack; }
            }
            else
            {
                if (!V49.IsOpen && V48.IsOpen && !V53.IsOpen) { flow += Mfc10Data.FeedBack + Mfc11Data.FeedBack; }
            }
            return flow;
        }

        private double GetTMAMFCFlow(bool isRun)
        {
            double flow = 0;
            if (isRun)
            {
                if (!V45.IsOpen && V43.IsOpen && V41.IsOpen && !V42.IsOpen && V61.IsOpen && (V90.IsOpen || V89.IsOpen || V88.IsOpen)) { flow += Mfc7Data.FeedBack; }
            }
            else
            {
                if (!V45.IsOpen && V43.IsOpen && !V41.IsOpen && V42.IsOpen) { flow += Mfc7Data.FeedBack; }
            }
            return flow;
        }

        /// <summary>
        /// 打开 ProcessMonitor
        /// </summary>
        public void ShowMonitorWindow()
        {
            // 给 OverviewViewModel 发消息，打开Monitor窗口。
            if (_eventAggregator?.HandlerExistsFor(typeof(ShowCloseMonitorWinEvent)) == true)
                _eventAggregator?.PublishOnUIThread(new ShowCloseMonitorWinEvent(true));
            else
                MessageBox.Show("The process has not been activated, please open Operation->Overview tab to activate the process.", "Warn",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        
    }
}
