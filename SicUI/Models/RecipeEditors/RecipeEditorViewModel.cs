using Aitex.Core.RT.Log;
using Caliburn.Micro;
using Caliburn.Micro.Core;
using MECF.Framework.Common.DataCenter;
using MECF.Framework.UI.Client.CenterViews.Editors;
using MECF.Framework.UI.Client.CenterViews.Editors.Recipe;
using MECF.Framework.UI.Client.CenterViews.Editors.Sequence;
using MECF.Framework.UI.Client.ClientBase;
using OpenSEMI.ClientBase;
using OpenSEMI.ClientBase.Command;
using RecipeEditorLib.DGExtension.CustomColumn;
using RecipeEditorLib.RecipeModel.Params;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using MECF.Framework.Common.OperationCenter;
using MECF.Framework.UI.Client.CenterViews.Configs.Roles;
using MECF.Framework.UI.Client.RecipeEditorLib.DGExtension;
using MECF.Framework.UI.Client.RecipeEditorLib.RecipeModel;
using MECF.Framework.UI.Client.RecipeEditorLib.RecipeModel.Params;
using Microsoft.Win32;
using Sicentury.Core;
using Action = System.Action;
using DataGridCellEditEndingEventArgs = ExtendedGrid.Microsoft.Windows.Controls.DataGridCellEditEndingEventArgs;
using DataGridRowEventArgs = ExtendedGrid.Microsoft.Windows.Controls.DataGridRowEventArgs;
using Sicentury.Core.Collections;

namespace SicUI.Models.RecipeEditors
{
    public class RecipeEditorViewModel : UiViewModelBase, IHandle<UserMode> //BaseModel
    {

        #region Variables

        private RecipeData _currentRecipe;
        private bool _isRunPurgeSwitch;
        private bool _isTmaNotInstall;
        private bool _isHClNotInstall;
        private bool _isHClFlowNotInstall;
        private bool _isLoading;
        private Window _winValidationInfo;
        private EditMode _editMode;
        private int _errorsCount;
        private int _cellAccessPermCount;
        private readonly RecipeFormatBuilder _columnBuilder;
        private readonly RecipeProvider _recipeProvider;
        private ObservableCollection<ProcessTypeFileItem> _processTypeFileList;
        private readonly ObservableRangeCollection<RecipeStepValidationInfo> _validationResultList;
        private readonly List<RecipeStep> _copiedStepList;
        private readonly Dictionary<string, ObservableCollection<RecipeStep>> _popCopySteps =
            new Dictionary<string, ObservableCollection<RecipeStep>>();

        private Point? _lastPosOfValidationWin = null;
        
        #endregion

        #region Constructors

        public RecipeEditorViewModel()
        {
            _eventAggregator?.Subscribe(this);

            _columnBuilder = new RecipeFormatBuilder();
            _recipeProvider = new RecipeProvider();
            _validationResultList = new ObservableRangeCollection<RecipeStepValidationInfo>();
            _copiedStepList = new List<RecipeStep>();
        }

        #endregion
        
        #region Commands

        private ICommand _RenameFolderCommand;

        public ICommand RenameFolderCommand
        {
            get
            {
                if (_RenameFolderCommand == null)
                    _RenameFolderCommand = new BaseCommand(() => RenameFolder());
                return _RenameFolderCommand;
            }
        }

        private ICommand _DeleteFolderCommand;

        public ICommand DeleteFolderCommand
        {
            get
            {
                if (_DeleteFolderCommand == null)
                    _DeleteFolderCommand = new BaseCommand(() => DeleteFolder());
                return _DeleteFolderCommand;
            }
        }

        private ICommand _NewFolderCommand;

        public ICommand NewFolderCommand
        {
            get
            {
                if (_NewFolderCommand == null)
                    _NewFolderCommand = new BaseCommand(() => NewFolder());
                return _NewFolderCommand;
            }
        }

        private ICommand _NewFolderRootCommand;

        public ICommand NewFolderRootCommand
        {
            get
            {
                if (_NewFolderRootCommand == null)
                    _NewFolderRootCommand = new BaseCommand(() => NewFolderRoot());
                return _NewFolderRootCommand;
            }
        }

        private ICommand _NewRecipeCommand;

        public ICommand NewRecipeCommand
        {
            get
            {
                if (_NewRecipeCommand == null)
                    _NewRecipeCommand = new BaseCommand(() => NewRecipe());
                return _NewRecipeCommand;
            }
        }

        private ICommand _NewRecipeRootCommand;

        public ICommand NewRecipeRootCommand
        {
            get
            {
                if (_NewRecipeRootCommand == null)
                    _NewRecipeRootCommand = new BaseCommand(() => NewRecipeRoot());
                return _NewRecipeRootCommand;
            }
        }

        private ICommand _RenameRecipeCommand;

        public ICommand RenameRecipeCommand
        {
            get
            {
                if (_RenameRecipeCommand == null)
                    _RenameRecipeCommand = new BaseCommand(() => RenameRecipe());
                return _RenameRecipeCommand;
            }
        }

        private ICommand _DeleteRecipeCommand;

        public ICommand DeleteRecipeCommand
        {
            get
            {
                if (_DeleteRecipeCommand == null)
                    _DeleteRecipeCommand = new BaseCommand(() => DeleteRecipe());
                return _DeleteRecipeCommand;
            }
        }

        private ICommand _SaveAsRecipeCommand;

        public ICommand SaveAsRecipeCommand
        {
            get
            {
                if (_SaveAsRecipeCommand == null)
                    _SaveAsRecipeCommand = new BaseCommand(() => SaveAsRecipe());
                return _SaveAsRecipeCommand;
            }
        }

        #endregion

        #region Properties
        
        public ObservableCollection<ProcessTypeFileItem> ProcessTypeFileList
        {
            get => _processTypeFileList;
            set
            {
                _processTypeFileList = value;
                NotifyOfPropertyChange();
            }
        }
        
        public RecipeData CurrentRecipe
        {
            get => _currentRecipe;
            set
            {
                _currentRecipe = value;
                NotifyOfPropertyChange();
            }
        }
        
        public bool IsPermission => Permission == 3; //&& RtStatus != "AutoRunning";

        private bool _isCellAccessPermEditMode = false;

        /// <summary>
        /// 设置或返回是否在Cell访问权限编辑模式。
        /// </summary>
        public bool IsCellAccessPermissionEditMode
        {
            get => _isCellAccessPermEditMode;
            private set
            {
                _isCellAccessPermEditMode = value;
                NotifyOfPropertyChange();
            }
        }

        public FileNode CurrentFileNode { get; set; }

        private bool IsChanged => _editMode == EditMode.Edit || CurrentRecipe.IsChanged;

        public List<EditorDataGridTemplateColumnBase> Columns { get; set; }

        public Dictionary<string, List<EditorDataGridTemplateColumnBase>> DicColunms { get; set; }

        public bool EnableNew { get; set; }
        
        public bool EnableReName { get; set; }
        
        public bool EnableCopy { get; set; }
        
        public bool EnableDelete { get; set; }
        
        public bool EnableSave { get; set; }
        
        public bool EnableImportExport { get; set; }

        public bool EnableStep { get; set; }

        public bool EnableReload { get; set; }

        public bool EnableSaveToAll { get; set; }

        public bool EnableSaveTo { get; set; }

        public bool EnableLeftTabPanel { get; set; }
        
        public bool EnableRefreshRecipeList { get; set; }
        
        public bool EnableFilterTreeList { get; set; }

        public bool EnableCellPermButton { get; set; }
        
        /// <summary>
        /// 返回是否显示单元格访问权限编辑按钮。
        /// </summary>
        public bool IsShowCellAccessPermEditButton { get; private set; }

        private int ErrorsCount 
        {
            set
            {
                // 如果错误数没发生变化，不要修改Badge的值，以免触发Badge动画。
                if (value == _errorsCount)
                    return;
                
                if(value == 0)
                    ((RecipeEditorView)View).txtErrorCount.Badge = null;
                else
                    ((RecipeEditorView)View).txtErrorCount.Badge = value;

                _errorsCount = value;
            }
        }

        private int CellAccessPermCount
        {
            set
            {
                if (value == _cellAccessPermCount)
                    return;

                if (value == 0)
                    ((RecipeEditorView)View).txtCellAccessPremCount.Badge = null;
                else
                    ((RecipeEditorView)View).txtCellAccessPremCount.Badge = value;

                _cellAccessPermCount = value;
            }
        }

        /// <summary>
        /// 是否正在加载配方。
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                _isLoading = value;
                NotifyOfPropertyChange();
            }
        }

        public List<RecipeStep> SelectedRecipeSteps { get; set; }
        
        public IList<ExtendedGrid.Microsoft.Windows.Controls.DataGridCellInfo> SelectedGridCellCollection
        {
            get;
            set;
        }

        public ObservableCollection<string> ChamberType { get; set; }

        public int ChamberTypeIndexSelection { get; set; }

        public int ProcessTypeIndexSelection { get; set; }

        public string CurrentChamberType => ChamberType[ChamberTypeIndexSelection];

        public string CurrentProcessType => ProcessTypeFileList[ProcessTypeIndexSelection].ProcessType;

        public Visibility MultiChamberVisibility { get; set; }

        public ObservableCollection<string> Chambers { get; set; }

        public string SelectedChamber { get; set; }

        public object View { get; set; }
        
        
        #endregion

        protected override void OnInitialize()
        {
            base.OnInitialize();

            var chamberType = QueryDataClient.Instance.Service.GetConfig("System.Recipe.SupportedChamberType");
            if (chamberType == null)
            {
                ChamberType = new ObservableCollection<string>() { "Default" };
            }
            else
            {
                ChamberType = new ObservableCollection<string>(((string)(chamberType)).Split(','));
            }

            ChamberTypeIndexSelection = 0;

            var processType = "Process,Clean";

            ProcessTypeFileList = new ObservableCollection<ProcessTypeFileItem>();
            var recipeProcessType = ((string)processType).Split(',');

            for (var i = 0; i < recipeProcessType.Length; i++)
            {
                var type = new ProcessTypeFileItem();
                type.ProcessType = recipeProcessType[i];
                var prefix = $"{ChamberType[ChamberTypeIndexSelection]}\\{recipeProcessType[i]}";
                var recipes = _recipeProvider.GetXmlRecipeList(prefix);
                type.FileListByProcessType =
                    RecipeSequenceTreeBuilder.BuildFileNode(prefix, "", false, recipes)[0].Files;
                type.FilterFileListByProcessType = type.FileListByProcessType;
                ProcessTypeFileList.Add(type);
            }

            DicColunms = new Dictionary<string, List<EditorDataGridTemplateColumnBase>>();
            UpdateRecipeFormat();
        }

        protected override void OnActivate()
        {
            // 是否隐藏参数值
            SetValueHideByCurrentRole();

            // 设置各列的权限。
            SetColumnPermissionByCurrentRole();

            UpdateView();
            
            // 根据角色权限设置判断是否显示按钮。
            IsShowCellAccessPermEditButton = GetRoleConfigItemRecipeEditorAllowEditCellAccessPerm();
            NotifyOfPropertyChange(nameof(IsShowCellAccessPermEditButton));

            base.OnActivate();
        }

        /// <summary>
        /// 获取角色权限配置中“Recipe.Behaviour.ShowValuesInRecipeEditor”的配置。
        /// </summary>
        /// <returns></returns>
        private int GetRoleConfigItemRecipeEditorValuesInEditor()
        {
            var roleID = BaseApp.Instance.UserContext.RoleID;
            var isHideValue = RoleAccountProvider.Instance.GetMenuPermission(roleID, "Recipe.Behaviour.ShowValuesInRecipeEditor");
            return isHideValue;
        }

        /// <summary>
        /// 获取角色权限配置中“Recipe.Behaviour.AllowEditCellAccessPerm”的配置。
        /// </summary>
        /// <returns></returns>
        private bool GetRoleConfigItemRecipeEditorAllowEditCellAccessPerm()
        {
            var roleID = BaseApp.Instance.UserContext.RoleID;
            var val = RoleAccountProvider.Instance.GetMenuPermission(roleID, "Recipe.Behaviour.AllowEditCellAccessPerm");
            return val != (int)MenuPermissionEnum.MP_NONE;
        }
        
        private void SetValueHideByCurrentRole()
        {
            // 是否在编辑器中隐藏Recipe参数值
            CurrentRecipe.Steps.IsHideValue = GetRoleConfigItemRecipeEditorValuesInEditor() == (int)MenuPermissionEnum.MP_NONE; // 当权限为None时隐藏，其它时显示。

            // 白名单的Param显示出来
            ShowValueOfAccessPermissionWhitelist();
        }

        private void SetColumnPermissionByCurrentRole()
        {
            // 初始化RoleManager
            var roleManager = new RoleManager();
            roleManager.Initialize();

            //得到当前登录的RoleItem
            var roleItem = roleManager.GetRoleByName(BaseApp.Instance.UserContext.RoleName);
            var menuPermission = new MenuPermission();
            menuPermission.ParsePermission(roleItem.Role.MenuPermission);

            foreach (var col in Columns)
            {
                if (col.ControlName == "StepNo" || col.ControlName == "StepUid")
                    continue;

                RecipeFormatBuilder.SetPermission(col, menuPermission);
            }
        }

        protected override void OnDeactivate(bool close)
        {
            base.OnDeactivate(close);

            if (IsChanged)
            {
                if (DialogBox.ShowDialog(DialogButton.Yes | DialogButton.No, DialogType.CONFIRM,
                        $"Recipe {CurrentRecipe.Name} content is changed, do you want to save it?") == DialogButton.Yes)
                {
                    SaveRecipe();
                }
            }

            _winValidationInfo?.Close();
        }

        protected override void OnViewLoaded(object view)
        {
            View = view;
            base.OnViewLoaded(view);
            RecipeFormatBuilder.ApplyTemplate((UserControl)view, Columns);
            var u = (RecipeEditorView)view;
            u.dgCustom.Columns.Clear();

            Columns.Apply((c) =>
            {
                c.Header = c;
                u.dgCustom.Columns.Add(c);

            });

            u.dgCustom.LostFocus += DgCustom_LostFocus;
            u.dgCustom.CellEditEnding += DgCustomOnCellEditEnding;
            u.dgCustom.LoadingRow += DgCustomOnLoadingRow;
            u.dgCustom.FrozenColumnCount = 5;
        }

        private void DgCustomOnLoadingRow(object sender, DataGridRowEventArgs e)
        {
            if (sender is XDataGrid dg && e.Row.DataContext is RecipeStep step)
            {
                for (var i = 0; i < step.Count; i++)
                {
                    step[i].RowOwner = e.Row;
                    step[i].ColumnOwner = dg.Columns[i];
                }
            }
        }

        private void DgCustomOnCellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            // ValidateEntireRecipe();
            if (SelectedRecipeSteps != null && SelectedRecipeSteps.Count == 1)
            {
                SelectedRecipeSteps[0].CalRecipeParameterForRunVent();
                CheckIfAllCellsValid(out _);
            }
        }

        private void DgCustom_LostFocus(object sender, RoutedEventArgs e)
        {
            // ValidateEntireRecipe();
            if (SelectedRecipeSteps != null && SelectedRecipeSteps.Count == 1)
            {
                SelectedRecipeSteps[0].CalRecipeParameterForRunVent();
                CheckIfAllCellsValid(out _);
            }
        }

        /// <summary>
        /// 校验整个Recipe。
        /// </summary>
        private void ValidateEntireRecipe()
        {
            _isRunPurgeSwitch = false;
            _isTmaNotInstall = false;
            _isHClNotInstall = false;
            _isHClFlowNotInstall = false;

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

                //如果是Routine则在下面代码不执行，在这里continue
                if (CurrentProcessType == ProcessTypeFileList[1].ProcessType)
                {
                    //如果是Routine,则总时间需*循环次数
                    var loopTimes = ((DoubleParam)currentStep[9]).Value <= 0
                        ? 1
                        : (int)((DoubleParam)currentStep[8]).Value;
                    CurrentRecipe.RecipeTotalTime +=
                        (int)((DoubleParam)currentStep[(int)RecipColNo.Time]).Value * loopTimes;

                    continue;
                }
                else
                {
                    //如果是Recipe则总时间直接加
                    CurrentRecipe.RecipeTotalTime +=
                        (int)((DoubleParam)currentStep[(int)RecipColNo.Time]).Value;
                }

                NotInstallDevice(currentStep);
                currentStep.CalRecipeParameterForRunVent(); // 自动计算比例值
                currentStep.Validate(); // 校验单元格

                //var stepTime = ((DoubleParam)currentStep[(int)RecipColNo.Time]).Value;
                //PurgeRunSwitch(previousStep, currentStep, nextStep);
                /*if (stepTime > 0)
                {
                    CalRecipeParameterForRunVent(currentStep);
                }*/

                // 校验单元格内容
                
            }

            //如果是Routine则在下面代码不执行，在这里返回
            if (CurrentProcessType == ProcessTypeFileList[1].ProcessType)
            {
                return;
            }

            if (_isRunPurgeSwitch)
                MessageBox.Show("Flow mode 'Run' and 'Purge' can't switch directly", "Warning", MessageBoxButton.OK,
                    MessageBoxImage.Warning);

            if (_isTmaNotInstall)
                MessageBox.Show("TMA is not install, only Purge mode is valid", "Warning", MessageBoxButton.OK,
                    MessageBoxImage.Warning);

            if (_isHClNotInstall)
                MessageBox.Show("GR Purge HCl is not install, only Disable is valid", "Warning", MessageBoxButton.OK,
                    MessageBoxImage.Warning);

            if (_isHClFlowNotInstall)
                MessageBox.Show("GR Purge HCl Flow is not install, lock the value as zero", "Warning",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
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

        private void NotInstallDevice(RecipeStep currentStep)
        {
            if (!(currentStep[(int)RecipColNo.TMAFlowMode] is ComboxParam currentTmaFlowMode))
                throw new InvalidOperationException(
                    $"The column {RecipColNo.TMAFlowMode} is not defined in recipe format.");
            
            if (string.Equals(currentTmaFlowMode.Value,
                    FlowModeParam.FlowModeEnum.Purge.ToString(), StringComparison.OrdinalIgnoreCase))
                return;

            currentTmaFlowMode.Value = FlowModeParam.FlowModeEnum.Purge.ToString();
            _isTmaNotInstall = true;
        }

        public void TabSelectionChanged()
        {
            UpdateRecipeFormat();
            OnViewLoaded(View);
        }

        public void UpdateRecipeFormat()
        {
            var chamber = QueryDataClient.Instance.Service.GetConfig("System.Recipe.ChamberModules");
            if (chamber == null)
            {
                chamber = "PM1";
            }

            Chambers = new ObservableCollection<string>(((string)chamber).Split(','));
            if (Chambers.Count > 1)
            {
                for (var i = 0; i < Chambers.Count; i++)
                {
                    var isPmInstall =
                        (bool)QueryDataClient.Instance.Service.GetConfig(
                            $"System.SetUp.Is{Chambers[i].ToString()}Installed");
                    {
                        if (!isPmInstall)
                        {
                            Chambers.RemoveAt(i);
                        }
                    }
                }
            }

            if (Chambers.Count == 0)
            {
                Chambers = new ObservableCollection<string>(new string[] { "PM1" });
            }

            SelectedChamber = Chambers[0];

            if (DicColunms.Keys.Contains(CurrentProcessType))
            {
                Columns = DicColunms[CurrentProcessType];
            }
            else
            {
                Columns = _columnBuilder.Build($"{CurrentChamberType}\\{CurrentProcessType}", SelectedChamber, true,
                    BaseApp.Instance.UserContext.RoleName).ToList();

                DicColunms[CurrentProcessType] = Columns;
            }

            CurrentRecipe = new RecipeData();
            CurrentRecipe.RecipeChamberType = _columnBuilder.RecipeChamberType;
            CurrentRecipe.RecipeVersion = _columnBuilder.RecipeVersion;

            _editMode = EditMode.None;

            MultiChamberVisibility = Chambers.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>
        /// Chamber变更时重新加载Recipe。
        /// </summary>
        public async void ChamberSelectionChanged()
        {
            if (IsChanged)
            {
                var selection = DialogBox.ShowDialog(DialogButton.Yes | DialogButton.No,
                    DialogType.CONFIRM,
                    $"Recipe {CurrentRecipe.Name} is changed, do you want to save it?");

                if (selection == DialogButton.Yes)
                {
                    CurrentRecipe.Revisor = BaseApp.Instance.UserContext.LoginName;
                    CurrentRecipe.ReviseTime = DateTime.Now;
                    Save(CurrentRecipe, false);
                }
            }

            IsLoading = true;

            await CurrentRecipe.ChangeChamber(Columns
                , _columnBuilder.Configs, SelectedChamber, GetLoadingDispatcher());

            IsLoading = false;
        }

        /// <summary>
        /// 左侧Recipe列表选择改变时，重新加载Recipe。
        /// </summary>
        /// <param name="node"></param>
        public async void TreeSelectChanged(FileNode node)
        {
            if (IsChanged)
            {
                var selection = DialogBox.ShowDialog(DialogButton.Yes | DialogButton.No,
                    DialogType.CONFIRM,
                    $"Recipe {CurrentRecipe.Name} is changed, do you want to save it?");

                if (selection == DialogButton.Yes)
                {
                    CurrentRecipe.Revisor = BaseApp.Instance.UserContext.LoginName;
                    CurrentRecipe.ReviseTime = DateTime.Now;
                    Save(CurrentRecipe, false);
                }
            }

            CurrentFileNode = node;

            if (node != null && node.IsFile)
            {
                await LoadRecipe(node.PrefixPath, node.FullPath);
            }
            else
            {
                ClearData();
                _editMode = EditMode.None;
            }

            UpdateView();
        }

        #region folder

        public void NewFolder()
        {
            if (IsChanged)
            {
                var selection = DialogBox.ShowDialog(DialogButton.Yes | DialogButton.No | DialogButton.Cancel,
                    DialogType.CONFIRM, $"Recipe {CurrentRecipe.Name} is changed, do you want to save it?");
                if (selection == DialogButton.Cancel)
                    return;

                if (selection == DialogButton.Yes)
                {
                    CurrentRecipe.Revisor = BaseApp.Instance.UserContext.LoginName;
                    CurrentRecipe.ReviseTime = DateTime.Now;
                    Save(CurrentRecipe, false);
                }
            }

            var dialog = new InputFileNameDialogViewModel("Input New Folder Name");
            dialog.FileName = "new folder";
            var wm = new WindowManager();
            var dialogReturn = wm.ShowDialog(dialog);
            if (!dialogReturn.HasValue || !dialogReturn.Value)
                return;

            var name = dialog.FileName.Trim();
            if (string.IsNullOrEmpty(name))
            {
                DialogBox.ShowWarning("Folder name should not be empty");
                return;
            }

            var prefix = ChamberType[ChamberTypeIndexSelection] + "\\" +
                         ProcessTypeFileList[ProcessTypeIndexSelection].ProcessType;
            var processType = string.Empty;
            var newFolder = string.Empty;
            if (CurrentFileNode != null)
            {
                prefix = CurrentFileNode.PrefixPath;
                var folder = CurrentFileNode.FullPath;
                if (CurrentFileNode.IsFile)
                {
                    folder = folder.Substring(0, folder.LastIndexOf("\\") + 1);
                    if (!string.IsNullOrEmpty(folder))
                        newFolder = folder;
                }
                else
                {
                    newFolder = folder + "\\";
                }

            }

            newFolder = newFolder + name;

            if (IsExist(newFolder, false))
            {
                DialogBox.ShowWarning($"Can not create folder {newFolder}, Folder with the same name already exist.");
                return;
            }

            if (newFolder.Length > 200)
            {
                DialogBox.ShowWarning($"Can not create folder {newFolder}, Folder name too long, should be less 200.");
                return;
            }

            _recipeProvider.CreateRecipeFolder(prefix, newFolder);

            ReloadRecipeFileList(CurrentChamberType, CurrentProcessType, newFolder, true);
        }

        public void NewFolderRoot()
        {
            if (IsChanged)
            {
                var selection = DialogBox.ShowDialog(DialogButton.Yes | DialogButton.No | DialogButton.Cancel,
                    DialogType.CONFIRM, $"Recipe {CurrentRecipe.Name} is changed, do you want to save it?");
                if (selection == DialogButton.Cancel)
                    return;

                if (selection == DialogButton.Yes)
                {
                    CurrentRecipe.Revisor = BaseApp.Instance.UserContext.LoginName;
                    CurrentRecipe.ReviseTime = DateTime.Now;
                    Save(CurrentRecipe, false);
                }
            }

            var dialog = new InputFileNameDialogViewModel("Input New Folder Name");
            dialog.FileName = "new folder";
            var wm = new WindowManager();
            var dialogReturn = wm.ShowDialog(dialog);
            if (!dialogReturn.HasValue || !dialogReturn.Value)
                return;

            var name = dialog.FileName.Trim();
            if (string.IsNullOrEmpty(name))
            {
                DialogBox.ShowWarning("Folder name should not be empty");
                return;
            }


            if (IsExist(name, false))
            {
                DialogBox.ShowWarning($"Can not create folder {name}, Folder with the same name already exist.");
                return;
            }

            if (name.Length > 200)
            {
                DialogBox.ShowWarning($"Can not create folder {name}, Folder name too long, should be less 200.");
                return;
            }

            var prefix = ChamberType[ChamberTypeIndexSelection] + "\\" +
                         ProcessTypeFileList[ProcessTypeIndexSelection].ProcessType;

            _recipeProvider.CreateRecipeFolder(prefix, name);

            ReloadRecipeFileList(CurrentChamberType, CurrentProcessType, name, true);
        }

        public void DeleteFolder()
        {
            if (CurrentFileNode == null || CurrentFileNode.IsFile)
                return;

            if (CurrentFileNode.Files.Count > 0)
            {
                DialogBox.ShowWarning(
                    $"Can not delete non-empty folder, Remove the files or folders under \r\n{CurrentFileNode.FullPath}.");
                return;
            }

            var selection = DialogBox.ShowDialog(DialogButton.Yes | DialogButton.No, DialogType.CONFIRM,
                $"Are you sure you want to delete \r\n {CurrentFileNode.FullPath}?");
            if (selection == DialogButton.No)
                return;

            var nextFocus = CurrentFileNode.Parent.FullPath;
            var isFolder = true;
            if (CurrentFileNode.Parent.Files.Count > 1)
            {
                for (var i = 0; i < CurrentFileNode.Parent.Files.Count; i++)
                {
                    if (CurrentFileNode.Parent.Files[i] == CurrentFileNode)
                    {
                        if (i == 0)
                        {
                            nextFocus = CurrentFileNode.Parent.Files[i + 1].FullPath;
                            isFolder = !CurrentFileNode.Parent.Files[i + 1].IsFile;
                        }
                        else
                        {
                            nextFocus = CurrentFileNode.Parent.Files[i - 1].FullPath;
                            isFolder = !CurrentFileNode.Parent.Files[i - 1].IsFile;
                        }
                    }
                }
            }

            _recipeProvider.DeleteRecipeFolder(CurrentFileNode.PrefixPath, CurrentFileNode.FullPath);

            ReloadRecipeFileList(CurrentChamberType, CurrentProcessType, nextFocus, isFolder);
        }

        public void RenameFolder()
        {
            if (CurrentFileNode == null || CurrentFileNode.IsFile)
                return;

            var dialog = new InputFileNameDialogViewModel("Input New Folder Name");
            dialog.FileName = CurrentFileNode.Name;
            var wm = new WindowManager();
            var dialogReturn = wm.ShowDialog(dialog);
            if (!dialogReturn.HasValue || !dialogReturn.Value)
                return;

            var name = dialog.FileName.Trim();
            if (string.IsNullOrEmpty(name))
                return;

            var newFolder = CurrentFileNode.FullPath.Substring(0, CurrentFileNode.FullPath.LastIndexOf("\\") + 1);
            if (!string.IsNullOrEmpty(newFolder))
                newFolder = newFolder + name;
            else
                newFolder = name;

            if (newFolder == CurrentFileNode.FullPath)
                return;

            if (IsExist(newFolder, false))
            {
                DialogBox.ShowWarning($"Can not rename to {newFolder}, Folder with the same name already exist.");
                return;
            }

            if (newFolder.Length > 200)
            {
                DialogBox.ShowWarning($"Can not create folder {newFolder}, Folder name too long, should be less 200.");
                return;
            }

            _recipeProvider.RenameFolder(CurrentFileNode.PrefixPath, CurrentFileNode.FullPath, newFolder);

            ReloadRecipeFileList(CurrentChamberType, CurrentProcessType, newFolder, true);
        }

        #endregion

        #region Recipe Edit

        /// <summary>
        /// 检查Recipe文件夹或文件是否存在
        /// </summary>
        /// <param name="fullPath"></param>
        /// <param name="isFile"></param>
        /// <returns></returns>
        private bool IsExist(string fullPath, bool isFile)
        {
            for (var i = 0; i < ProcessTypeFileList.Count; i++)
            {
                if (ProcessTypeFileList[i].ProcessType == CurrentProcessType)
                {
                    if (ProcessTypeFileList[i].FileListByProcessType.Count == 0)
                        return false;

                    return FindFile(fullPath, ProcessTypeFileList[i].FileListByProcessType[0].Parent, isFile);
                }
            }

            return true;
        }

        private bool FindFile(string path, FileNode root, bool isFile)
        {
            if (root.FullPath == path && !isFile)
            {
                return true;
            }

            foreach (var node in root.Files)
            {
                if (isFile && node.IsFile && node.FullPath == path)
                    return true;

                if (!node.IsFile && FindFile(path, node, isFile))
                    return true;
            }

            return false;
        }

        public void NewRecipe()
        {
            if (IsChanged)
            {
                var selection = DialogBox.ShowDialog(DialogButton.Yes | DialogButton.No | DialogButton.Cancel,
                    DialogType.CONFIRM, $"Recipe {CurrentRecipe.Name} is changed, do you want to save it?");
                if (selection == DialogButton.Cancel)
                    return;

                if (selection == DialogButton.Yes)
                {
                    CurrentRecipe.Revisor = BaseApp.Instance.UserContext.LoginName;
                    CurrentRecipe.ReviseTime = DateTime.Now;
                    Save(CurrentRecipe, false);
                }
            }

            var dialog = new InputFileNameDialogViewModel("Input New Name");
            dialog.FileName = "";
            var wm = new WindowManager();
            var dialogReturn = wm.ShowDialog(dialog);
            if (!dialogReturn.HasValue || !dialogReturn.Value)
                return;

            var recipeName = dialog.FileName.Trim();
            if (string.IsNullOrEmpty(dialog.FileName))
            {
                DialogBox.ShowWarning("Recipe file name should not be empty");
                return;
            }

            var prefix = CurrentChamberType + "\\" + CurrentProcessType;
            var processType = string.Empty;
            if (CurrentFileNode != null)
            {
                var folder = CurrentFileNode.FullPath;


                if (CurrentFileNode.IsFile)
                {
                    folder = folder.Substring(0, folder.LastIndexOf("\\") + 1);
                    //if (!string.IsNullOrEmpty(folder))
                    //    folder = folder;
                }
                else
                {
                    folder = folder + "\\";
                }

                recipeName = folder + recipeName;
            }

            if (IsExist(recipeName, true))
            {
                DialogBox.ShowWarning($"Can not create {recipeName}, Recipe with the same name already exist.");
                return;
            }


            if (recipeName.Length > 200)
            {
                DialogBox.ShowWarning($"Can not create folder {recipeName}, Folder name too long, should be less 200.");
                return;
            }

            var recipe = new RecipeData();
            recipe.Name = recipeName;
            recipe.PrefixPath = prefix;
            recipe.Creator = BaseApp.Instance.UserContext.LoginName;
            recipe.CreateTime = DateTime.Now;
            recipe.Revisor = BaseApp.Instance.UserContext.LoginName;
            recipe.ReviseTime = DateTime.Now;
            recipe.Description = string.Empty;

            if (!Save(recipe, true))
                return;

            var types = prefix.Split('\\');

            ReloadRecipeFileList(types[0], types[1], recipeName, false);

        }

        public void NewRecipeRoot()
        {
            if (IsChanged)
            {
                var selection = DialogBox.ShowDialog(DialogButton.Yes | DialogButton.No | DialogButton.Cancel,
                    DialogType.CONFIRM, $"Recipe {CurrentRecipe.Name} is changed, do you want to save it?");
                if (selection == DialogButton.Cancel)
                    return;

                if (selection == DialogButton.Yes)
                {
                    CurrentRecipe.Revisor = BaseApp.Instance.UserContext.LoginName;
                    CurrentRecipe.ReviseTime = DateTime.Now;
                    Save(CurrentRecipe, false);
                }
            }

            var dialog = new InputFileNameDialogViewModel("Input New Recipe Name");
            dialog.FileName = "new recipe";
            var wm = new WindowManager();
            var dialogReturn = wm.ShowDialog(dialog);
            if (!dialogReturn.HasValue || !dialogReturn.Value)
                return;

            var recipeName = dialog.FileName.Trim();
            if (string.IsNullOrEmpty(dialog.FileName))
            {
                DialogBox.ShowWarning("Recipe file name should not be empty");
                return;
            }

            if (IsExist(recipeName, true))
            {
                DialogBox.ShowWarning($"Can not create {recipeName}, Recipe with the same name already exist.");
                return;
            }


            if (recipeName.Length > 200)
            {
                DialogBox.ShowWarning($"Can not create folder {recipeName}, Folder name too long, should be less 200.");
                return;
            }

            var recipe = new RecipeData();
            recipe.Name = recipeName;
            recipe.PrefixPath = CurrentChamberType + "\\" + CurrentProcessType;
            recipe.Creator = BaseApp.Instance.UserContext.LoginName;
            recipe.CreateTime = DateTime.Now;
            recipe.Revisor = BaseApp.Instance.UserContext.LoginName;
            recipe.ReviseTime = DateTime.Now;
            recipe.Description = string.Empty;

            if (!Save(recipe, true))
                return;


            ReloadRecipeFileList(CurrentChamberType, CurrentProcessType, recipeName, false);
        }

        private void ReloadRecipeFileList(string chamberType, string processType, string selectedFile,
            bool selectionIsFolder)
        {
            var item = ProcessTypeFileList.FirstOrDefault(x => x.ProcessType == processType);
            if (item == null)
            {
                LOG.Write("error reload recipe file list, type = " + processType);
            }

            var prefix = $"{ChamberType[ChamberTypeIndexSelection]}\\{item.ProcessType}";
            var recipes = _recipeProvider.GetXmlRecipeList(prefix);

            item.FileListByProcessType =
                RecipeSequenceTreeBuilder.BuildFileNode(prefix, selectedFile, selectionIsFolder, recipes)[0].Files;
            item.FilterFileListByProcessType = item.FileListByProcessType;

            item.InvokePropertyChanged();
        }

        public void SaveAsRecipe()
        {
            if (CurrentFileNode == null || !CurrentFileNode.IsFile)
                return;

            if (IsChanged)
            {
                var selection = DialogBox.ShowDialog(DialogButton.Yes | DialogButton.No | DialogButton.Cancel,
                    DialogType.CONFIRM, $"Recipe {CurrentRecipe.Name} is changed, do you want to save it?");
                if (selection == DialogButton.Cancel)
                    return;

                if (selection == DialogButton.Yes)
                {
                    CurrentRecipe.Revisor = BaseApp.Instance.UserContext.LoginName;
                    CurrentRecipe.ReviseTime = DateTime.Now;
                    Save(CurrentRecipe, false);
                }
            }

            var dialog = new InputFileNameDialogViewModel("Input New Recipe Name");
            dialog.FileName = CurrentFileNode.Name;
            var wm = new WindowManager();
            var dialogReturn = wm.ShowDialog(dialog);
            if (!dialogReturn.HasValue || !dialogReturn.Value)
                return;

            var recipeName = dialog.FileName.Trim();
            if (string.IsNullOrEmpty(dialog.FileName))
            {
                DialogBox.ShowWarning("Recipe file name should not be empty");
                return;
            }

            var prefix = CurrentChamberType + "\\" + CurrentProcessType;
            var processType = string.Empty;

            var folder = CurrentFileNode.FullPath;
            if (CurrentFileNode.IsFile)
            {
                folder = folder.Substring(0, folder.LastIndexOf("\\") + 1);
            }

            if (!string.IsNullOrEmpty(folder))
                recipeName = folder + "\\" + recipeName;

            if (CurrentFileNode.FullPath == recipeName)
                return;

            if (IsExist(recipeName, true))
            {
                DialogBox.ShowWarning($"Can not copy to {recipeName}, Recipe with the same name already exist.");
                return;
            }


            if (recipeName.Length > 200)
            {
                DialogBox.ShowWarning($"Can not create folder {recipeName}, Folder name too long, should be less 200.");
                return;
            }

            CurrentRecipe.Creator = BaseApp.Instance.UserContext.LoginName;
            CurrentRecipe.CreateTime = DateTime.Now;
            CurrentRecipe.Revisor = BaseApp.Instance.UserContext.LoginName;
            CurrentRecipe.ReviseTime = DateTime.Now;
            CurrentRecipe.Description = CurrentRecipe.Description + ". Renamed from " + CurrentFileNode.Name;

            _recipeProvider.SaveAsRecipe(prefix, recipeName, CurrentRecipe.GetXmlString());

            ReloadRecipeFileList(CurrentChamberType, CurrentProcessType, recipeName, false);
        }

        public void RenameRecipe()
        {
            if (CurrentFileNode == null || !CurrentFileNode.IsFile)
                return;

            if (IsChanged)
            {
                var selection = DialogBox.ShowDialog(DialogButton.Yes | DialogButton.No | DialogButton.Cancel,
                    DialogType.CONFIRM, $"Recipe {CurrentRecipe.Name} is changed, do you want to save it?");
                if (selection == DialogButton.Cancel)
                    return;

                if (selection == DialogButton.Yes)
                {
                    CurrentRecipe.Revisor = BaseApp.Instance.UserContext.LoginName;
                    CurrentRecipe.ReviseTime = DateTime.Now;
                    Save(CurrentRecipe, false);
                }
            }

            var dialog = new InputFileNameDialogViewModel("Input New Recipe Name");
            dialog.FileName = CurrentFileNode.Name;
            var wm = new WindowManager();
            var dialogReturn = wm.ShowDialog(dialog);
            if (!dialogReturn.HasValue || !dialogReturn.Value)
                return;

            var recipeName = dialog.FileName.Trim();
            if (string.IsNullOrEmpty(dialog.FileName))
            {
                DialogBox.ShowWarning("Recipe file name should not be empty");
                return;
            }

            var prefix = CurrentChamberType + "\\" + CurrentProcessType;
            var processType = string.Empty;

            var newName = CurrentFileNode.FullPath.Substring(0, CurrentFileNode.FullPath.LastIndexOf("\\") + 1);
            if (!string.IsNullOrEmpty(newName))
                newName = newName + recipeName;
            else
                newName = recipeName;

            if (newName == CurrentFileNode.FullPath)
                return;

            if (IsExist(newName, true))
            {
                DialogBox.ShowWarning($"Can not rename to {newName}, Recipe with the same name already exist.");
                return;
            }


            if (newName.Length > 200)
            {
                DialogBox.ShowWarning($"Can not create folder {newName}, Folder name too long, should be less 200.");
                return;
            }

            _recipeProvider.RenameRecipe(prefix, CurrentFileNode.FullPath, newName);

            ReloadRecipeFileList(CurrentChamberType, CurrentProcessType, newName, false);
        }

        public void DeleteRecipe()
        {
            if (CurrentFileNode == null || !CurrentFileNode.IsFile)
                return;

            var selection = DialogBox.ShowDialog(DialogButton.Yes | DialogButton.No, DialogType.CONFIRM,
                $"Are you sure you want to delete \r\n {CurrentFileNode.FullPath}?");
            if (selection == DialogButton.No)
                return;

            var nextFocus = CurrentFileNode.Parent.FullPath;
            var isFolder = true;
            if (CurrentFileNode.Parent.Files.Count > 1)
            {
                for (var i = 0; i < CurrentFileNode.Parent.Files.Count; i++)
                {
                    if (CurrentFileNode.Parent.Files[i] == CurrentFileNode)
                    {
                        if (i == 0)
                        {
                            nextFocus = CurrentFileNode.Parent.Files[i + 1].FullPath;
                            isFolder = !CurrentFileNode.Parent.Files[i + 1].IsFile;
                        }
                        else
                        {
                            nextFocus = CurrentFileNode.Parent.Files[i - 1].FullPath;
                            isFolder = !CurrentFileNode.Parent.Files[i - 1].IsFile;
                        }
                    }
                }
            }

            _recipeProvider.DeleteRecipe(CurrentFileNode.PrefixPath, CurrentFileNode.FullPath);
            DelAccessPermWhiteList();
            ReloadRecipeFileList(CurrentChamberType, CurrentProcessType, nextFocus, isFolder);
        }

        public void RefreshRecipe()
        {
            ReloadRecipeFileList(CurrentChamberType, CurrentProcessType, "", false);
        }

        public async void ReloadRecipe()
        {
            if (_editMode == EditMode.Normal || _editMode == EditMode.Edit)
            {
                if (CurrentRecipe == null)
                    return;
                
                if (CurrentRecipe.IsChanged)
                {
                    var ret = DialogBox.ShowDialog(DialogButton.Yes | DialogButton.No,
                        DialogType.CONFIRM,
                        $"Recipe {CurrentRecipe.Name} has changed, discard and reload?");
                    if (ret == DialogButton.No)
                        return;
                }
                
                await LoadRecipe(CurrentRecipe.PrefixPath, CurrentRecipe.Name);
                UpdateView();
            }
        }

        public void SaveToAll()
        {
            ValidateEntireRecipe();

            if (!CurrentRecipe.IsCompatibleWithCurrentFormat)
            {
                DialogBox.ShowWarning($"Saving failed, {CurrentRecipe.Name} is not a valid recipe file");
                return;
            }

            var selection = DialogBox.ShowDialog(DialogButton.Yes | DialogButton.No,
                DialogType.CONFIRM,
                $"Do you want to save to all chambers? \r\n This will replace all the other chambers recipe content");
            if (selection == DialogButton.No)
                return;

            CurrentRecipe.SaveTo(Chambers.ToArray());

            Save(CurrentRecipe, false);
        }

        public void SaveTo()
        {
            ValidateEntireRecipe();

            if (!CurrentRecipe.IsCompatibleWithCurrentFormat)
            {
                DialogBox.ShowWarning($"Saving failed, {CurrentRecipe.Name} is not a valid recipe file.");
                return;
            }

            var dialog =
                new SaveToDialogViewModel("Select the chamber to copy to", SelectedChamber, Chambers.ToList());

            var wm = new WindowManager();
            var dialogReturn = wm.ShowDialog(dialog);
            if (!dialogReturn.HasValue || !dialogReturn.Value)
                return;

            var chambers = new List<string>();
            foreach (var dialogChamber in dialog.Chambers)
            {
                if (dialogChamber.IsEnabled && dialogChamber.IsChecked)
                    chambers.Add(dialogChamber.Name);
            }

            if (chambers.Count == 0)
                return;

            CurrentRecipe.SaveTo(chambers.ToArray());

            Save(CurrentRecipe, false);
        }

        /// <summary>
        /// 导入Recipe。
        /// </summary>
        public  void ImportRecipe()
        {
            try
            {
                IsLoading = true;

                // 选择文件
                var openFileDialog = new OpenFileDialog()
                {
                    Filter = "Sic Recipe File|*.srp",
                    Title = "Import Recipe",
                    DefaultExt = ".srp",
                    Multiselect = true
                };

                var prefix = ProcessTypeFileItem.GetProcessFilesPrefix(ProcessTypeFileItem.ProcessFileTypes.Process);

                if (openFileDialog.ShowDialog() != true)
                    return;

                #region 选择要导入的文件夹

                var dialog = new RecipeSelectDialogViewModel(true, true, ProcessTypeFileItem.ProcessFileTypes.Process)
                {
                    DisplayName = "Select Folder to Import ..."
                };
                
                var wm = new WindowManager();
                var bret = wm.ShowDialog(dialog);

                #endregion

                if (bret != true)
                    return;

                var recipeFolderToImport
                    = dialog.DialogResult;

                // 从文件夹名移除Prefix
                recipeFolderToImport = recipeFolderToImport.Replace(prefix + "\\", "");

                var fns = openFileDialog.FileNames;

                // 是否覆盖所有Recipe，该选项通过Overwrite对话框给值。
                var isOverwriteAll = false;

                foreach (var fn in fns)
                {
                    try
                    {
                        var recipePath = $"{recipeFolderToImport}\\{Path.GetFileNameWithoutExtension(fn)}";

                        // 检查Recipe是否已存在
                        if (IsExist(recipePath.TrimStart('\\'), true) && !isOverwriteAll)
                        {

                            var ret = DialogBox.ShowDialog(
                                DialogButton.Yes | DialogButton.YesToAll | DialogButton.Cancel,
                                DialogType.WARNING,
                                $"{recipePath} has existed, overwrite?\r\n\r\nATTENTION: Press 'Yes To All' to overwrite all files.");
                            if (ret == DialogButton.Cancel)
                                return;

                            if (ret == DialogButton.YesToAll)
                                isOverwriteAll = true;
                        }

                        RecipeData.ImportRecipe(fn, out var xmlContent);

                        // 保存文件。
                        _recipeProvider.WriteRecipeFile(prefix, recipePath, xmlContent);
                    }
                    catch (Exception ex)
                    {
                        DialogBox.ShowError($"Unable to import {fn}, {ex}");
                    }
                }

                // 刷新Recipe列表
                ReloadRecipeFileList("Sic", "Process", "", false);


            }
            finally
            {
                IsLoading = false;
            }

        }

        /// <summary>
        /// 到处Recipe。
        /// </summary>
        public void ExportRecipe()
        {
            if (CurrentRecipe == null)
            {
                MessageBox.Show("No recipe selected.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }


            if (string.IsNullOrEmpty(CurrentRecipe.Name))
            {
                MessageBox.Show("No recipe loaded.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // 选择保存路径和文件名
            var dialog = new SaveFileDialog
            {
                Filter = "Sic Recipe File|*.srp",
                Title = "Export Recipe",
                DefaultExt = ".srp",
                FileName = CurrentRecipe.Name.Split('\\').Last()
            };
            var ret = dialog.ShowDialog();
            if (!ret.HasValue || ret == false)
                return;

            // 保存文件
            try
            {
                CurrentRecipe.ExportRecipe(dialog.FileName);
                DialogBox.ShowInfo($"Export done! {dialog.FileName}");
            }
            catch (Exception ex)
            {
                DialogBox.ShowError($"Unable to export recipe, {ex}");
            }
            
           
        }


        #endregion
        
        #region Steps

        /// <summary>
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

        /// <summary>
        /// 检查是否所有的参数均有效，并创建错误清单。
        /// </summary>
        /// <returns></returns>
        private bool CheckIfAllCellsValid(out string errorMessage)
        {
            errorMessage = string.Empty;

            if (CurrentProcessType == ProcessTypeFileList[1].ProcessType)
            {
                return true;
            }

            /*var lstError = new List<string>();*/
            _validationResultList.Clear();

            foreach (var step in CurrentRecipe.Steps)
            {
                var invalidParams = step.Where(x => x is IParam p && p.IsValidated == false).ToList();
 
                if(invalidParams.Any() == false)
                    continue;
                
                _validationResultList.AddRange(invalidParams.Cast<IParam>()
                    .Select(param => new RecipeStepValidationInfo(
                        step,
                        param,
                        param.ValidationError)));
            }

            if (_validationResultList.Count <= 0)
            {
                ErrorsCount = 0;
                return true;
            }
            
            ErrorsCount = _validationResultList.Count;

            var errCnt = 0;
            var strInfo = new StringBuilder();
            foreach (var t in _validationResultList)
            {
                strInfo.AppendLine(t.ToString());
                errCnt++;

                if (errCnt > 10)
                    break;
            }

            if (errCnt < _validationResultList.Count)
                strInfo.AppendLine("\r\n<please check for more errors...>");

            errorMessage = strInfo.ToString();
            return false;
        }

        public void SaveRecipe()
        {
            /*if (Save(CurrentRecipe, false))
                MessageBox.Show($"The recipe has been saved successfully!", "Succeeded", MessageBoxButton.OK,
                    MessageBoxImage.Information);*/

            Save(CurrentRecipe, false);
        }

        public void PopSetting(string controlName, Param paramData)
        {
            var stepNum = Convert.ToInt32(((StepParam)paramData.Parent[1]).Value);

            var dialog = new PublicPopSettingDialogViewModel();
            dialog.DisplayName = paramData.DisplayName;
            var Parameters = new RecipeStep(null);

            Parameters = CurrentRecipe.PopSettingSteps[controlName][stepNum - 1];

            var ControlParameters = new RecipeStep(null);
            var BrandParameters = new ObservableCollection<BandParam>();
            foreach (var item in Parameters)
            {
                if (item.Name.Contains("Band"))
                {
                    var name = item.Name.Replace("Wavelength", "").Replace("Bandwidth", "");
                    var displayName = item.DisplayName.Replace("Wavelength", "").Replace("Bandwidth", "");

                    if (BrandParameters.Where(x => x.Name == name).Count() == 0)
                    {
                        BrandParameters.Add(new BandParam()
                        {
                            Name = name,
                            DisplayName = displayName
                        });
                    }

                    if (item.Name.Contains("Wavelength"))
                    {
                        BrandParameters.First(x => x.Name == name).WavelengthDoubleParam = item;
                    }
                    else if (item.Name.Contains("Bandwidth"))
                    {
                        BrandParameters.First(x => x.Name == name).BandwidthDoubleParam = item;
                    }
                }
                else
                    ControlParameters.Add(item);
            }

            dialog.Parameters = Parameters;
            dialog.ControlParameters = ControlParameters;
            dialog.BandParameters = BrandParameters;

            var wm = new WindowManager();
            var bret = wm.ShowDialog(dialog);
            if (bret == true)
            {
                CurrentRecipe.PopSettingSteps[controlName][stepNum - 1] = dialog.Parameters;
            }
        }

        private bool Save(RecipeData recipe, bool createNew)
        {
            if (string.IsNullOrEmpty(recipe.Name))
            {
                MessageBox.Show("Recipe name can't be empty", "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            ValidateEntireRecipe();

            if (CheckIfAllCellsValid(out var errors) == false)
            {
                var mbr = MessageBox.Show($"{errors}\r\n Are you sure to continue to save?", "Warning",
                    MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (mbr == MessageBoxResult.No)
                    return false;
            }

            var result = false;


            recipe.Revisor = BaseApp.Instance.UserContext.LoginName;
            recipe.ReviseTime = DateTime.Now;

            result = _recipeProvider.WriteRecipeFile(recipe.PrefixPath, recipe.Name, recipe.GetXmlString());

            if (result)
            {
                recipe.DataSaved();

                _editMode = EditMode.Normal;

                UpdateView();
            }
            else
            {
                MessageBox.Show("Save failed!");
            }

            return result;
        }

        public void AddStep()
        {
            CurrentRecipe.Steps.Add(CurrentRecipe.CreateStep(Columns));

            if (_editMode != EditMode.New && _editMode != EditMode.ReName)
                _editMode = EditMode.Edit;

            ValidateEntireRecipe();
            CheckIfAllCellsValid(out _);
            UpdateView();
        }

        public void InsertStepToLeft()
        {
            if (SelectedRecipeSteps.Count != 1)
                return;

            var stepNo = SelectedRecipeSteps[0]?.StepNo;
            if (RecipeStep.ValidateStepNo(stepNo, out var vStepNo) == false)
                return;


            var index = vStepNo - RecipeStep.START_INDEX;

            if (_editMode != EditMode.New && _editMode != EditMode.ReName)
                _editMode = EditMode.Edit;

            CurrentRecipe.Steps.Insert(index, CurrentRecipe.CreateStep(Columns));
        }

        public void InsertStepToRight()
        {
            if (SelectedRecipeSteps.Count != 1)
                return;

            var stepNo = SelectedRecipeSteps[0]?.StepNo;
            if (RecipeStep.ValidateStepNo(stepNo, out var vStepNo) == false)
                return;

            var index = vStepNo - RecipeStep.START_INDEX;

            
            if (_editMode != EditMode.New && _editMode != EditMode.ReName)
                _editMode = EditMode.Edit;

            if (index < _currentRecipe.Steps.Count - 1)
                CurrentRecipe.Steps.Insert(index + 1, CurrentRecipe.CreateStep(Columns));
            else
                CurrentRecipe.Steps.Add(CurrentRecipe.CreateStep(Columns));

        }

     
       /// <summary>
        /// 拷贝所有选中的Steps。
        /// </summary>
        public void CopyStep()
       {
           _copiedStepList.Clear();
            _popCopySteps.Clear();

            if (SelectedRecipeSteps == null || SelectedRecipeSteps.Count <= 0)
                return;
            
            foreach (var step in SelectedRecipeSteps)
                _copiedStepList.Add(_currentRecipe.CloneStep(Columns, step));

            CurrentRecipe.ValidLoopData();
        }

       /// <summary>
       /// 在左侧粘贴Step。
       /// </summary>
       public async void PasteStepToLeft()
       {
           await Paste(true);
       }

       /// <summary>
       /// 在右侧粘贴Step。
       /// </summary>
       public async void PasteStepToRight()
       {
           await Paste(false);
       }

        /// <summary>
        /// 粘贴Step。
        /// <para>注意：async Task 标记的方法不能直接应用于Caliburn.Message.Attach 方法。</para>
        /// </summary>
        /// <param name="isToLeft">是否粘贴到当前选中的Step的左侧。</param>
        /// <returns></returns>
        private async Task Paste(bool isToLeft)
        {
            if (SelectedRecipeSteps == null || SelectedRecipeSteps.Count != 1)
                return;

            if (_copiedStepList.Count <= 0)
                return;

            if (_editMode != EditMode.New && _editMode != EditMode.ReName)
                _editMode = EditMode.Edit;

            var stepNo = SelectedRecipeSteps[0].StepNo;
            if (RecipeStep.ValidateStepNo(stepNo, out var vStepNo) == false)
                return;

            foreach (var t in _copiedStepList)
            {
                var cloned = CurrentRecipe.CloneStep(Columns, t);

                var pos = isToLeft ? vStepNo - RecipeStep.START_INDEX : vStepNo;

                await Dispatcher.CurrentDispatcher.BeginInvoke(DispatcherPriority.Background,
                    (Action)(() => { CurrentRecipe.Steps.Insert(pos, cloned); }));
            }

            ValidateEntireRecipe();
            CurrentRecipe.ValidLoopData();
            CheckIfAllCellsValid(out _);
            UpdateView();
        }

        /// <summary>
        /// 在左侧复制Step。
        /// </summary>
        public async void DuplicateStepToLeft()
        {
            await Duplicate(true);
        }

        /// <summary>
        /// 在右侧复制Step。
        /// </summary>
        public async void DuplicateStepToRight()
        {
            await Duplicate(false);
        }

        /// <summary>
        /// 复制选中的Step。
        /// </summary>
        /// <param name="isToLeft">是否复制到选中的Step的左侧。</param>
        private async Task Duplicate(bool isToLeft)
        {
            if (SelectedRecipeSteps == null || SelectedRecipeSteps.Count != 1)
                return;

            CopyStep();
            await Paste(isToLeft);
        }

        /// <summary>
        /// 删除步骤。
        /// </summary>
        public void DeleteStep()
        {

            if (_editMode != EditMode.New && _editMode != EditMode.ReName)
                _editMode = EditMode.Edit;

            for (var i = SelectedRecipeSteps.Count - 1; i >= 0; i--)
            {
                _currentRecipe.Steps.Remove(SelectedRecipeSteps[i]);
            }

            CheckIfAllCellsValid(out _);

        }

        public void ReloadRecipeWhileProcess()
        {
            if (CurrentRecipe.IsChanged || _editMode != EditMode.Normal)
            {
                DialogBox.ShowError("The recipe has change, please save it before doing Reload-In-Process.");
                return;
            }

            InvokeClient.Instance.Service.DoOperation($"PM1.ReloadRecipe");
            InvokeClient.Instance.Service.DoOperation($"PM2.ReloadRecipe");
        }

        private void CreateValidationDetailWindow()
        {
            _winValidationInfo = new RecipeEditorValidationDetailWindow(_validationResultList, _lastPosOfValidationWin);
            _winValidationInfo.DataContext = this;
            _winValidationInfo.Closed += (sender, args) =>
            {
                _lastPosOfValidationWin = new Point(_winValidationInfo.Left, _winValidationInfo.Top);
                ResetHighlight();
            };
        }

        public void ShowValidationDetailWindow()
        {
            ValidateEntireRecipe();
            CheckIfAllCellsValid(out _);

            if (Application.Current.Windows.OfType<RecipeEditorValidationDetailWindow>().Any())
                _winValidationInfo.Activate();
            else
            {
                CreateValidationDetailWindow();
                _winValidationInfo.Show();
            }
        }

        /*public void ShowHideParamValues()
        {
            CurrentRecipe.Steps.IsHideValue = !CurrentRecipe.Steps.IsHideValue;
        }*/

        private TreeViewItem GetParentObjectEx<TreeViewItem>(DependencyObject obj) where TreeViewItem : FrameworkElement
        {
            var parent = VisualTreeHelper.GetParent(obj);
            while (parent != null)
            {
                if (parent is TreeViewItem)
                {
                    return (TreeViewItem)parent;
                }

                parent = VisualTreeHelper.GetParent(parent);
            }

            return null;
        }

        public void TreeRightMouseDown(MouseButtonEventArgs e)
        {
            var item = GetParentObjectEx<TreeViewItem>(e.OriginalSource as DependencyObject) as TreeViewItem;
            if (item != null)
            {
                item.Focus();
            }
        }

        /// <summary>
        /// 在DataGrid中聚焦并高亮指定的参数。
        /// </summary>
        /// <param name="param"></param>
        public void FocusToParam(IParam param)
        {
            if (param == null)
                return;

            ResetHighlight();

            var row = param.RowOwner;
            var col = param.ColumnOwner;

            if (row != null && col != null)
            {
                ((RecipeEditorView)View).dgCustom.ScrollIntoView(param.Parent, col);
                param.Highlight();
            }
        }


        public void ResetHighlight()
        {
            _currentRecipe.Steps.ResetHighlight();
        }

        #endregion

        #region Cell访问权限编辑

        /// <summary>
        /// 计算具有访问权限白名单的参数的总数。
        /// </summary>
        public void CountHaveAccessPermParams()
        {
            var total = CurrentRecipe.Steps.GetParamsCountWhoHaveAccessPerm();
            CellAccessPermCount = total;
        }

        public async Task SwitchCellAccessPermEditMode()
        {
            if (IsCellAccessPermissionEditMode)
                await LeaveCellAccessPermissionEditMode();
            else
                await EnterCellAccessPermissionEditMode();
        }

        /// <summary>
        /// 进入Cell访问白名单编辑模式。
        /// </summary>
        private async Task EnterCellAccessPermissionEditMode()
        {
            if (CurrentRecipe == null || CurrentRecipe.Steps.Count <= 0)
            {
                await new Task(() => { });
                return;
            }

            // 如果Recipe已更改但未保存，禁止编辑
            if (CurrentRecipe.IsChanged)
            {
                DialogBox.ShowError("The recipe has changed, please save it before entering Cell-Access-Permission-Mode.");
                return;
            }

            IsCellAccessPermissionEditMode = true;
            

            // 清除高亮显示
            CurrentRecipe.Steps.ResetHighlight();

            // 加载白名单
            HighlightAccessPermissionWhitelist();

            // 禁止编辑单元格内容
            Columns.ToList().ForEach(c => c.IsReadOnly = true);

            UpdateView();

            CountHaveAccessPermParams();

            await new Task(() => { });
        }


        /// <summary>
        /// 离开Cell访问白名单编辑模式。
        /// </summary>
        private async Task LeaveCellAccessPermissionEditMode()
        {
            // 恢复列访问权限
            SetColumnPermissionByCurrentRole();

            // 保存白名单
            SaveAccessPermissionWhitelist();

            // 清除高亮显示
            CurrentRecipe.Steps.ResetHighlight();

            // 根据角色配置隐藏参数值。
            SetValueHideByCurrentRole();

            // 显示白名单允许访问的Cell
            ShowValueOfAccessPermissionWhitelist();

            IsCellAccessPermissionEditMode = false;

            UpdateView();

            CellAccessPermCount = 0;

            await new Task(() => { });

        }

        /// <summary>
        /// 从数据库读取单元格访问白名单。
        /// </summary>
        /// <returns></returns>
        private DataTable ReadAccessPermissionWhitelist()
        {
            if (CurrentRecipe == null)
                return null;

            var cmd = $"select * from recipe_cell_access_permission_whitelist where \"recipeName\" = '{_currentRecipe.FullName}'";
            var dt = QueryDataClient.Instance.Service.QueryData(cmd);
            return dt;
        }

        private void ShowValueOfAccessPermissionWhitelist()
        {
            var dt = ReadAccessPermissionWhitelist();

            if (dt != null && dt.Rows.Count > 0)
            {
                foreach (DataRow dataRow in dt.Rows)
                {
                    var stepUid = dataRow["stepUid"].ToString();
                    var controlName = dataRow["columnName"].ToString();

                    var step = CurrentRecipe.Steps.FirstOrDefault(s => s.StepUid == stepUid);
                    var param = step?.FirstOrDefault(p => p.Name == controlName);
                    if(param != null)
                        param.IsHideValue = false;
                }
            }
        }

        /// <summary>
        /// 加载Cell访问权限白名单。
        /// </summary>
        private void HighlightAccessPermissionWhitelist()
        {
            var dt = ReadAccessPermissionWhitelist();

            if (dt != null && dt.Rows.Count > 0)
            {
                foreach (DataRow dataRow in dt.Rows)
                {
                    var stepUid = dataRow["stepUid"].ToString();
                    var controlName = dataRow["columnName"].ToString();

                    var step = CurrentRecipe.Steps.FirstOrDefault(s => s.StepUid == stepUid);
                    var param = step?.FirstOrDefault(p=>p.Name == controlName);
                    param?.Highlight();
                }
            }
        }

        /// <summary>
        /// 保存Cell访问权限白名单。
        /// </summary>
        private void SaveAccessPermissionWhitelist()
        {
            var cmdStr = new StringBuilder();
            var insertExp =
                "INSERT into recipe_cell_access_permission_whitelist (\"uid\", \"recipeName\",\"stepUid\",\"columnName\",\"whoSet\",\"whenSet\") VALUES ";

            var whenSet = DateTime.Now;

            // 枚举所有高亮的Recipe参数。
            foreach (var step in CurrentRecipe.Steps)
            {
                var lst = step.GetHighlightedParams();
                lst.ForEach(p =>
                {
                    cmdStr.AppendLine($"({(new RecipeCellAccessPermissionWhitelistInfo(CurrentRecipe.FullName, step.StepUid, p.Name, whenSet))}),");
                });
            }

            // 是否有需要保存的Cell信息？如果没有，cmdStr留空，则之前的白名单会被删除。
            if (cmdStr.Length > 0)
                cmdStr.Insert(0, insertExp);

            // 刷新白名单
            QueryDataClient.Instance.Service.ExcuteTransAction(new List<string>(new string[]
            {
                // 删除原有的白名单
                $"delete from recipe_cell_access_permission_whitelist where \"recipeName\" = '{CurrentRecipe.FullName}'",
                
                // 写入新的白名单
                cmdStr.ToString().TrimEnd('\n').TrimEnd('\r').TrimEnd(',')
            }));
        }

        /// <summary>
        /// 删除Cell访问权限白名单。
        /// </summary>
        private void DelAccessPermWhiteList()
        {
            // 刷新白名单
            QueryDataClient.Instance.Service.ExcuteTransAction(new List<string>(new string[]
            {
                // 删除原有的白名单
                $"delete from recipe_cell_access_permission_whitelist where \"recipeName\" = '{CurrentRecipe.FullName}'",
            }));
        }

        #endregion

        private void ClearData()
        {
            _editMode = EditMode.None;
            CurrentRecipe.Clear();
            CurrentRecipe.Name = string.Empty;
            CurrentRecipe.Description = string.Empty;
        }

        /// <summary>
        /// 根据配置获取Cascade方式呈现Recipe的Dispatcher，或返回null。
        /// </summary>
        /// <returns></returns>
        private static Dispatcher GetLoadingDispatcher()
        {
            // 判断Step呈现方式
            var isCascadeLoading = (bool)QueryDataClient.Instance.Service.GetConfig("System.RecipeCascadeLoading");
            var dispatcher = isCascadeLoading ? Dispatcher.CurrentDispatcher : null;
            if (dispatcher == null || !dispatcher.CheckAccess())
                return null;

            return dispatcher;
        }


        private async Task LoadRecipe(string prefixPath, string recipeName)
        {
            IsLoading = true;

            var isHideValueWhenLoading = GetRoleConfigItemRecipeEditorValuesInEditor();

            CurrentRecipe.Clear();
            CurrentRecipe.Steps.Save(); // 重置为已保存状态
            ErrorsCount = 0;

            var recipeXmlString = _recipeProvider.ReadRecipeFile(prefixPath, recipeName);

            if (string.IsNullOrEmpty(recipeXmlString))
            {
                MessageBox.Show($"{prefixPath}\\{recipeName} is empty, please confirm the file is valid.",
                    "Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            CurrentRecipe.RecipeChamberType = _columnBuilder.RecipeChamberType;
            CurrentRecipe.RecipeVersion = _columnBuilder.RecipeVersion;

            await CurrentRecipe.InitData(prefixPath, recipeName, recipeXmlString, Columns,
                _columnBuilder.Configs, SelectedChamber, GetLoadingDispatcher(), 
                isHideValueWhenLoading == (int)MenuPermissionEnum.MP_NONE);

            _editMode = EditMode.Normal;

            IsLoading = false;

            // 如果当前Recipe非本机Recipe，比例相关的参数可能由于算法不同导致结果不同，
            // 重新计算一次Recipe，以更新比例计算值，并校验其合法性。
            ValidateEntireRecipe();
            CheckIfAllCellsValid(out var _);

            // ValidateEntireRecipe()会重新计算比例相关的值，导致IsChanged == true, 刷新一下。
            foreach (var step in CurrentRecipe.Steps)
                step.Save();

            // 显示Cell访问白名单中的参数
            SetValueHideByCurrentRole();
        }

        private void UpdateView()
        {
            var isFileSelected = CurrentFileNode != null && CurrentFileNode.IsFile;

            EnableImportExport = true;
            EnableLeftTabPanel = true;
            EnableFilterTreeList = true;
            EnableRefreshRecipeList = true;
            EnableNew = true;

            EnableCellPermButton = isFileSelected;
            EnableReload = isFileSelected;
            EnableNew = isFileSelected;
            EnableReName = isFileSelected;
            EnableCopy = isFileSelected;
            EnableDelete = isFileSelected;
            EnableSave = isFileSelected;
            EnableStep = isFileSelected;
            EnableSaveTo = isFileSelected;
            EnableSaveToAll = isFileSelected;

            if (_editMode == EditMode.None)
            {
                EnableNew = true;
                EnableReName = false;
                EnableCopy = false;
                EnableDelete = false;
                EnableStep = false;
                EnableSave = false;
                EnableReload = true;

            }
            else if (IsCellAccessPermissionEditMode)
            {
                EnableNew = false;
                EnableReName = false;
                EnableCopy = false;
                EnableDelete = false;
                EnableStep = false;
                EnableSave = false;
                EnableReload = false;
                EnableSaveTo = false;
                EnableSaveToAll = false;
                EnableLeftTabPanel = false;
                EnableFilterTreeList = false;
                EnableRefreshRecipeList = false;
            }

            NotifyOfPropertyChange(nameof(EnableNew));
            NotifyOfPropertyChange(nameof(EnableReName));
            NotifyOfPropertyChange(nameof(EnableCopy));
            NotifyOfPropertyChange(nameof(EnableDelete));
            NotifyOfPropertyChange(nameof(EnableSave));
            NotifyOfPropertyChange(nameof(EnableImportExport));
            NotifyOfPropertyChange(nameof(EnableStep));
            NotifyOfPropertyChange(nameof(EnableSaveTo));
            NotifyOfPropertyChange(nameof(EnableSaveToAll));
            NotifyOfPropertyChange(nameof(EnableLeftTabPanel));
            NotifyOfPropertyChange(nameof(EnableFilterTreeList));
            NotifyOfPropertyChange(nameof(EnableRefreshRecipeList));
            NotifyOfPropertyChange(nameof(EnableCellPermButton));
            NotifyOfPropertyChange(nameof(CurrentRecipe));
        }

        private string _currentCriteria = string.Empty;

        public string CurrentCriteria
        {
            get => _currentCriteria;
            set
            {
                if (value == _currentCriteria)
                    return;

                _currentCriteria = value;
                NotifyOfPropertyChange();
                ApplyFilter();
            }
        }

        private void ApplyFilter()
        {
            ProcessTypeFileList[ProcessTypeIndexSelection].FilterFileListByProcessType =
                new ObservableCollection<FileNode>(ProcessTypeFileList[ProcessTypeIndexSelection].FileListByProcessType
                    .Where(d => d.Name.IndexOf(CurrentCriteria, StringComparison.OrdinalIgnoreCase) >= 0));
        }

        public void ClearFilter()
        {
            CurrentCriteria = "";
        }

        /// <summary>
        /// 当主窗口状态Login状态发生变化时处理当前窗口状态。
        /// </summary>
        /// <param name="message"></param>
        public void Handle(UserMode message)
        {
            switch (message)
            {
                case UserMode.None:
                    break;
                case UserMode.Normal:
                    break;
                case UserMode.Lock:
                case UserMode.Logoff:
                case UserMode.Exit:
                case UserMode.Shutdown:
                case UserMode.Breakdown:
                    _winValidationInfo?.Close();
                    break;

                default:
                    // ignore
                    break;
            }
        }
    }
}
