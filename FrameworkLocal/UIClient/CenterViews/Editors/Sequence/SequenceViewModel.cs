using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Caliburn.Micro;
using Caliburn.Micro.Core;
using MECF.Framework.UI.Client.CenterViews.Editors.Recipe;
using MECF.Framework.UI.Client.ClientBase;
using OpenSEMI.ClientBase;
using OpenSEMI.ClientBase.Command;
using RecipeEditorLib.DGExtension.CustomColumn;
using RecipeEditorLib.RecipeModel.Params;

namespace MECF.Framework.UI.Client.CenterViews.Editors.Sequence
{
    public class SequenceViewModel : BaseModel
    {
        public bool IsPermission => Permission == 3; //&& RtStatus != "AutoRunning";

        public ObservableCollection<FileNode> Files { get; set; }

        protected override void OnInitialize()
        {
            base.OnInitialize();
            CurrentSequence = new SequenceData();
            Columns = columnBuilder.Build();

            editMode = EditMode.None;
            IsSavedDesc = true;

            RefreshSequenceFileTree();
           
        }

        protected override void OnActivate()
        {
            base.OnActivate();
        }

        protected override void OnDeactivate(bool close)
        {
            base.OnDeactivate(close);
            if (IsChanged)
            {
                if (DialogBox.Confirm("This sequence is changed,do you want to save it?"))
                    SaveSequence();
                LoadData(CurrentSequence.Name);
            }
        }

        protected override void OnViewLoaded(object view)
        {
            base.OnViewLoaded(view);
            SequenceColumnBuilder.ApplyTemplate((UserControl)view, Columns);
            var u = (SequenceView)view;

            Columns.Apply((c) =>
            {
                c.Header = c;
                u.dgCustom.Columns.Add(c);
            });

            u.dgCustom.ItemsSource = CurrentSequence.Steps;

            UpdateView();
        }

        #region Sequence selection

        public void TreeSelectChanged(FileNode file)
        {
            if (file != null && file.IsFile)
            {
                if (IsChanged)
                    if (DialogBox.Confirm("This sequence is changed,do you want to save it?"))
                        Save(CurrentSequence);
                LoadData(file.FullPath);
                CurrentFileNode = file;
            }
            else
            {
                ClearData();
                editMode = EditMode.None;
                CurrentSequence.Steps.Clear();
            }
            UpdateView();
        }

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

        #endregion

        private ICommand _NotImplementCommand;
        public ICommand NotImplementCommand
        {
            get
            {
                if (_NotImplementCommand == null)
                    _NotImplementCommand = new BaseCommand(() => NotImplement());
                return _NotImplementCommand;
            }
        }

        public void NotImplement()
        {
            DialogBox.ShowInfo("Not Implement");
        }

        #region Sequence operation

        public void RefreshSequenceFileTree()
        {
            var names = provider.GetSequences();

            Files = new ObservableCollection<FileNode>(RecipeSequenceTreeBuilder.GetFiles("", names));
            CurrentFileNode = Files[0];

            SelectDefault(CurrentFileNode);
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
        public void NewFolder()
        {
            if (!CurrentFileNode.IsFile)
            {
                var dialog = new InputFileNameDialogViewModel("Input Folder Name");
                var wm = new WindowManager();
                var bret = wm.ShowDialog(dialog);
                if (!(bool)bret)
                    return;
                var fullpath = (CurrentFileNode.FullPath == "" ? dialog.DialogResult : CurrentFileNode.FullPath + "\\" + dialog.DialogResult);
                if (provider.CreateSequenceFolder(fullpath))
                {
                    var folder = new FileNode();
                    folder.Name = dialog.DialogResult;
                    folder.FullPath = fullpath;
                    folder.Parent = CurrentFileNode;
                    CurrentFileNode.Files.Add(folder);
                }
                else
                    DialogBox.ShowError("Create folder failed!");
            }
        }

        private ICommand _NewFolderInParentCommand;
        public ICommand NewFolderInParentCommand
        {
            get
            {
                if (_NewFolderInParentCommand == null)
                    _NewFolderInParentCommand = new BaseCommand(() => NewFolderInParent());
                return _NewFolderInParentCommand;
            }
        }
        public void NewFolderInParent()
        {
            var dialog = new InputFileNameDialogViewModel("Input Folder Name");
            var wm = new WindowManager();
            var bret = wm.ShowDialog(dialog);
            if (!(bool)bret)
                return;
            var fullpath = dialog.DialogResult;
            if (provider.CreateSequenceFolder(fullpath))
            {
                var folder = new FileNode();
                folder.Name = dialog.DialogResult;
                folder.FullPath = fullpath;
                folder.Parent = Files[0];
                Files[0].Files.Add(folder);
            }
            else
                DialogBox.ShowError("Create folder failed!");
        }

        private ICommand _NewSequenceInParentCommand;
        public ICommand NewSequenceInParentCommand
        {
            get
            {
                if (_NewSequenceInParentCommand == null)
                    _NewSequenceInParentCommand = new BaseCommand(() => NewSequenceInParent());
                return _NewSequenceInParentCommand;
            }
        }
        public void NewSequenceInParent()
        {
            var dialog = new InputFileNameDialogViewModel("Input New Sequence Name");
            var wm = new WindowManager();
            var bret = wm.ShowDialog(dialog);
            if ((bool)bret)
            {
                if (IsChanged)
                {
                    if (DialogBox.Confirm("This sequence is changed,do you want to save it?"))
                    {
                        CurrentSequence.Revisor = BaseApp.Instance.UserContext.LoginName;
                        CurrentSequence.ReviseTime = DateTime.Now;
                        Save(CurrentSequence);
                    }
                }
                else
                {
                    var fullpath = dialog.DialogResult;
                    if (string.IsNullOrEmpty(fullpath))
                    {
                        DialogBox.ShowError("SequenceName cannot be null or empty!");
                        return;
                    }
                    if (IsExist(fullpath))
                    {
                        DialogBox.ShowError("Sequence already existed!");
                    }
                    else
                    {
                        var sequence = new SequenceData
                        {
                            Name = fullpath,
                            Creator = BaseApp.Instance.UserContext.LoginName,
                            CreateTime = DateTime.Now,
                            Revisor = BaseApp.Instance.UserContext.LoginName,
                            ReviseTime = DateTime.Now,
                            Description = string.Empty
                        };

                        if (Save(sequence))
                        {
                            CurrentSequence.Name = sequence.Name;
                            CurrentSequence.Creator = sequence.Creator;
                            CurrentSequence.CreateTime = sequence.CreateTime;
                            CurrentSequence.Revisor = sequence.Revisor;
                            CurrentSequence.ReviseTime = sequence.ReviseTime;
                            CurrentSequence.Description = sequence.Description;
                            CurrentSequence.Steps.Clear();

                            var file = new FileNode();
                            file.Name = dialog.DialogResult;
                            file.FullPath = CurrentSequence.Name;
                            file.IsFile = true;
                            file.Parent = Files[0];
                            Files[0].Files.Insert(findInsertPosition(Files[0].Files), file);
                            editMode = EditMode.Normal;
                            UpdateView();
                        }
                    }
                }
            }
        }

        public int findInsertPosition(ObservableCollection<FileNode> files)
        {
            var pos = -1;
            if (files.Count == 0)
                pos = 0;
            else
            {
                var foundfolder = false;
                for (var index = 0; index < files.Count; index++)
                {
                    if (!files[index].IsFile)
                    {
                        foundfolder = true;
                        pos = index;
                        break;
                    }
                }

                if (!foundfolder)
                    pos = files.Count;
            }
            return pos;
        }

        private ICommand _SaveAsCommand;
        public ICommand SaveAsCommand
        {
            get
            {
                if (_SaveAsCommand == null)
                    _SaveAsCommand = new BaseCommand(() => SaveAsSequence());
                return _SaveAsCommand;
            }
        }

        public void SaveAsSequence()
        {
            if (CurrentFileNode.IsFile)
            {
                var dialog = new InputFileNameDialogViewModel("SaveAs Sequence");
                var wm = new WindowManager();
                var bret = wm.ShowDialog(dialog);
                if (!(bool)bret)
                    return;
                var fullpath = (CurrentFileNode.Parent.FullPath == "" ? dialog.DialogResult : CurrentFileNode.Parent.FullPath + "\\" + dialog.DialogResult);

                if (string.IsNullOrEmpty(fullpath))
                {
                    DialogBox.ShowError("SequenceName cannot be null or empty!");
                    return;
                }
                if (IsExist(fullpath))
                {
                    DialogBox.ShowError("Sequence already existed!");
                }
                else
                {
                    if (IsChanged)
                    {
                        if (DialogBox.Confirm("This sequence is changed,do you want to save it?"))
                        {
                            Save(CurrentSequence);
                        }
                    }

                    CurrentSequence.Revisor = BaseApp.Instance.UserContext.LoginName;
                    CurrentSequence.ReviseTime = DateTime.Now;
                    var tempName = CurrentSequence.Name;
                    CurrentSequence.Name = fullpath;
                    if (provider.SaveAs(fullpath, CurrentSequence))
                    {
                        var node = new FileNode
                        {
                            Parent = CurrentFileNode.Parent,
                            Name = dialog.DialogResult,
                            FullPath = fullpath,
                            IsFile = true
                        };
                        CurrentFileNode.Parent.Files.Add(node);
                    }
                    else
                    {
                        CurrentSequence.Name = tempName;
                        DialogBox.ShowError("SaveAs failed!");
                    }
                }
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
        public void DeleteFolder()
        {
            if (!CurrentFileNode.IsFile && CurrentFileNode.FullPath != "")
            {
                if (DialogBox.Confirm("Do you want to delete this folder? this operation will delete all files in this folder."))
                {
                    if (provider.DeleteSequenceFolder(CurrentFileNode.FullPath))
                    {
                        CurrentFileNode.Parent.Files.Remove(CurrentFileNode);
                    }
                    else
                        DialogBox.ShowInfo("Delete folder failed!");
                }
            }
        }

        private ICommand _NewSequenceCommand;
        public ICommand NewSequenceCommand
        {
            get
            {
                if (_NewSequenceCommand == null)
                    _NewSequenceCommand = new BaseCommand(() => NewSequence());
                return _NewSequenceCommand;
            }
        }
        public void NewSequence()
        {
            var dialog = new InputFileNameDialogViewModel("Input New Sequence Name");
            var wm = new WindowManager();
            var bret = wm.ShowDialog(dialog);
            if ((bool)bret)
            {
                if (IsChanged)
                {
                    if (DialogBox.Confirm("This sequence is changed,do you want to save it?"))
                    {
                        CurrentSequence.Revisor = BaseApp.Instance.UserContext.LoginName;
                        CurrentSequence.ReviseTime = DateTime.Now;
                        Save(CurrentSequence);
                    }
                }
                else
                {
                    var fullpath = dialog.DialogResult;
                    if (CurrentFileNode.IsFile)
                        fullpath = (string.IsNullOrEmpty(CurrentFileNode.Parent.FullPath) ? dialog.DialogResult : CurrentFileNode.Parent.FullPath + "\\" + dialog.DialogResult);
                    else
                        fullpath = (string.IsNullOrEmpty(CurrentFileNode.FullPath) ? dialog.DialogResult : CurrentFileNode.FullPath + "\\" + dialog.DialogResult);

                    if (string.IsNullOrEmpty(fullpath))
                    {
                        DialogBox.ShowError("SequenceName cannot be null or empty!");
                        return;
                    }
                    if (IsExist(fullpath))
                    {
                        DialogBox.ShowError("Sequence already existed!");
                    }
                    else
                    {
                        var sequence = new SequenceData
                        {
                            Name = fullpath,
                            Creator = BaseApp.Instance.UserContext.LoginName,
                            CreateTime = DateTime.Now,
                            Revisor = BaseApp.Instance.UserContext.LoginName,
                            ReviseTime = DateTime.Now,
                            Description = string.Empty
                        };

                        if (Save(sequence))
                        {
                            CurrentSequence.Name = sequence.Name;
                            CurrentSequence.Creator = sequence.Creator;
                            CurrentSequence.CreateTime = sequence.CreateTime;
                            CurrentSequence.Revisor = sequence.Revisor;
                            CurrentSequence.ReviseTime = sequence.ReviseTime;
                            CurrentSequence.Description = sequence.Description;
                            CurrentSequence.Steps.Clear();

                            var file = new FileNode();
                            file.Name = dialog.DialogResult;
                            file.FullPath = CurrentSequence.Name;
                            file.IsFile = true;
                            file.Parent = CurrentFileNode.IsFile ? CurrentFileNode.Parent : CurrentFileNode;
                            if (CurrentFileNode.IsFile)
                                CurrentFileNode.Parent.Files.Insert(findInsertPosition(CurrentFileNode.Parent.Files), file);
                            else
                                CurrentFileNode.Files.Insert(findInsertPosition(CurrentFileNode.Files), file);
                            editMode = EditMode.Normal;
                            UpdateView();
                        }
                    }
                }
            }
        }

        private ICommand _RenameCommand;
        public ICommand RenameCommand
        {
            get
            {
                if (_RenameCommand == null)
                    _RenameCommand = new BaseCommand(() => RenameSequence());
                return _RenameCommand;
            }
        }
        public void RenameSequence()
        {
            if (CurrentFileNode.IsFile)
            {
                var dialog = new InputFileNameDialogViewModel("Rename Sequence");
                var wm = new WindowManager();
                dialog.FileName = CurrentFileNode.Name;
                var bret = wm.ShowDialog(dialog);
                if (!(bool)bret)
                    return;
                var fullpath = CurrentFileNode.Parent.FullPath == "" ? dialog.DialogResult : CurrentFileNode.Parent.FullPath + "\\" + dialog.DialogResult;

                if (string.IsNullOrEmpty(fullpath))
                {
                    DialogBox.ShowError("SequenceName cannot be null or empty!");
                    return;
                }
                if (IsExist(fullpath))
                {
                    DialogBox.ShowError("Sequence already existed!");
                }
                else
                {
                    if (IsChanged)
                    {
                        if (DialogBox.Confirm("This sequence is changed,do you want to save it?", "") == true)
                        {
                            Save(CurrentSequence);
                        }
                    }
                    CurrentSequence.Revisor = BaseApp.Instance.UserContext.LoginName;
                    CurrentSequence.ReviseTime = DateTime.Now;
                    if (provider.Rename(CurrentSequence.Name, fullpath))
                    {
                        CurrentFileNode.Name = dialog.DialogResult;
                        CurrentFileNode.FullPath = fullpath;
                        CurrentSequence.Name = fullpath;
                    }
                    else
                        DialogBox.ShowError("Rename failed!");
                }
            }
        }

        public void SaveSequence()
        {
            if (IsChanged)
            {
                Save(CurrentSequence);
            }
        }

        private ICommand _DeleteCommand;
        public ICommand DeleteSequenceCommand
        {
            get
            {
                if (_DeleteCommand == null)
                    _DeleteCommand = new BaseCommand(() => DeleteSequence());
                return _DeleteCommand;
            }
        }
        public void DeleteSequence()
        {
            if (CurrentFileNode.IsFile)
            {
                if (DialogBox.Confirm("Do you want to delete this sequence?"))
                {
                    if (provider.Delete(CurrentSequence.Name))
                    {
                        CurrentFileNode.Parent.Files.Remove(CurrentFileNode);
                    }
                }
            }
        }

        public void ReloadSequence()
        {
            if (editMode == EditMode.Normal || editMode == EditMode.Edit)
            {
                LoadData(CurrentSequence.Name);
                UpdateView();
            }
        }

        private bool Save(SequenceData seq)
        {
            var result = false;
            if (string.IsNullOrEmpty(seq.Name))
            {
                DialogBox.ShowError("Sequence name can't be empty");
                return false;
            }

            string ruleCheckMessage = null;
            if (!ValidateSequenceRules(seq, ref ruleCheckMessage))
            {
                DialogBox.ShowError(string.Format($"{seq.Name} rules check failed, don't allow to save: {ruleCheckMessage}"));
                return false;
            }

            seq.Revisor = BaseApp.Instance.UserContext.LoginName;
            seq.ReviseTime = DateTime.Now;
            result = provider.Save(seq);

            if (result)
            {
                foreach (var parameters in seq.Steps)
                {
                    parameters.Apply(param => param.IsSaved = true);
                }
                editMode = EditMode.Normal;
                IsSavedDesc = true;
                NotifyOfPropertyChange("IsSavedDesc");
                UpdateView();
            }
            else
                DialogBox.ShowError("Save failed!");
            return result;
        }

        private bool IsExist(string sequenceName)
        {
            var existed = false;
            var fileNode = CurrentFileNode.IsFile ? CurrentFileNode.Parent : CurrentFileNode;
            for (var index = 0; index < fileNode.Files.Count; index++)
            {
                if (string.Equals(fileNode.Files[index].FullPath, sequenceName, StringComparison.CurrentCultureIgnoreCase))
                {
                    existed = true;
                    break;
                }
            }
            return existed;
        }

        private static bool ValidateSequenceRules(SequenceData currentSequence, ref string ruleCheckMessage)
        {
            foreach (var stepInfo in currentSequence.Steps)
            {
                if (string.IsNullOrEmpty((stepInfo[1] as PositionParam).Value))
                {
                    ruleCheckMessage = $"Step{(stepInfo[0] as StepParam).Value}, Position Is Empty";

                    return false;
                }

                //if (((stepInfo[4] as PathFileParam).Visible == Visibility.Visible && string.IsNullOrEmpty((stepInfo[4] as PathFileParam).Value)) ||
                //    ((stepInfo[5] as PathFileParam).Visible == Visibility.Visible && string.IsNullOrEmpty((stepInfo[5] as PathFileParam).Value)))
                //{
                //    ruleCheckMessage = $"Step{(stepInfo[0] as StepParam).Value}, Recipe Is Empty";

                //    return false;
                //}
            }
            return true;
        }

        #endregion

        #region Steps operation

        public void AddStep()
        {
            CurrentSequence.Steps.Add(CurrentSequence.CreateStep(Columns));

            if (editMode != EditMode.New && editMode != EditMode.ReName)
                editMode = EditMode.Edit;

            UpdateView();
        }

        public void AppendStep()
        {
            var index = -1;
            var found = false;
            for (var i = 0; i < CurrentSequence.Steps.Count; i++)
            {
                if (CurrentSequence.Steps[i][0] is StepParam && ((StepParam)CurrentSequence.Steps[i][0]).IsChecked)
                {
                    index = i;
                    found = true;
                    break;
                }
            }
            if (found)
            {
                if (editMode != EditMode.New && editMode != EditMode.ReName)
                    editMode = EditMode.Edit;

                CurrentSequence.Steps.Insert(index, CurrentSequence.CreateStep(Columns));
            }
        }

        private RecipeStepCollection copySteps = new RecipeStepCollection();
        public void CopyStep()
        {
            copySteps.Clear();

            for (var i = 0; i < CurrentSequence.Steps.Count; i++)
            {
                if (CurrentSequence.Steps[i][0] is StepParam && ((StepParam)CurrentSequence.Steps[i][0]).IsChecked)
                {
                    copySteps.Add(CurrentSequence.CloneStep(Columns, CurrentSequence.Steps[i]));
                }
            }
        }

        public void PasteStep()
        {
            if (copySteps.Count > 0)
            {
                if (editMode != EditMode.New && editMode != EditMode.ReName)
                    editMode = EditMode.Edit;
                for (var i = 0; i < CurrentSequence.Steps.Count; i++)
                {
                    if (CurrentSequence.Steps[i][0] is StepParam && ((StepParam)CurrentSequence.Steps[i][0]).IsChecked)
                    {
                        for (var copyindex = 0; copyindex < copySteps.Count; copyindex++)
                        {
                            CurrentSequence.Steps.Insert(i, CurrentSequence.CloneStep(Columns, copySteps[copyindex]));
                            i++;
                        }
                        break;
                    }
                }
                
                UpdateView();
            }
        }

        public void DeleteStep()
        {
            if (editMode != EditMode.New && editMode != EditMode.ReName)
                editMode = EditMode.Edit;

            var steps = CurrentSequence.Steps.ToList();

            for (var i = 0; i < steps.Count; i++)
            {
                if (steps[i][0] is StepParam && ((StepParam)steps[i][0]).IsChecked)
                {
                    CurrentSequence.Steps.Remove(steps[i]);
                }
            }
        }

        public void SelectRecipe(PathFileParam param)
        {
            var dialog = new RecipeSelectDialogViewModel
            {
                DisplayName = "Select Recipe"
            };
            var parameters = param.Parent;
            PositionParam posParam = null;
            for (var index = 0; index < parameters.Count; index++)
            {
                if (parameters[index] is PositionParam)
                {
                    posParam = parameters[index] as PositionParam;
                    break;
                }
            }

            var wm = new WindowManager();
            var bret = wm.ShowDialog(dialog);
            if ((bool)bret)
            {
                param.Value = dialog.DialogResult;

                var path = param.Value;
                var index = path.LastIndexOf("\\");
                if (index > -1)
                {
                    param.FileName = path.Substring(index + 1);
                }
                else
                {
                    param.FileName = path;
                }

                param.IsSaved = false;
            }
        }

        #endregion

        private void SelectDefault(FileNode node)
        {
            if (!node.IsFile)
            {
                if (node.Files.Count > 0)
                {
                    foreach (var file in node.Files)
                    {
                        if (file.IsFile)
                        {
                            TreeSelectChanged(file);
                            //break;
                        }
                    }
                }
            }
        }

        private void LoadData(string newSeqName)
        {
            var Sequence = provider.GetSequenceByName(Columns, newSeqName);

            CurrentSequence.Name = Sequence.Name;
            CurrentSequence.Creator = Sequence.Creator;
            CurrentSequence.CreateTime = Sequence.CreateTime;
            CurrentSequence.Revisor = Sequence.Revisor;
            CurrentSequence.ReviseTime = Sequence.ReviseTime;
            CurrentSequence.Description = Sequence.Description;

            CurrentSequence.Steps.Clear();
            Sequence.Steps.ToList().ForEach(step =>
                CurrentSequence.Steps.Add(step));

            //int index = 1;
            //foreach (RecipeStep parameters in this.CurrentSequence.Steps)
            //{
            //    (parameters[0] as StepParam).Value = index.ToString();
            //    index++;

            //    foreach (var para in parameters)
            //    {
            //        var pathFile = para as PathFileParam;
            //        if (pathFile != null)
            //        {
            //            pathFile.Value = pathFile.Value.Replace($"{pathFile.PrefixPath}\\", "");
            //        }
            //    }
            //}

            IsSavedDesc = true;
            NotifyOfPropertyChange("IsSavedDesc");

            editMode = EditMode.Normal;
        }

        private void ClearData()
        {
            editMode = EditMode.None;
            CurrentSequence.Steps.Clear();
            CurrentSequence.Name = string.Empty;
            CurrentSequence.Description = string.Empty;

            IsSavedDesc = true;

            NotifyOfPropertyChange("IsSavedGroup");
            NotifyOfPropertyChange("IsSavedDesc");
        }

        public void UpdateView()
        {
            EnableSequenceName = false;
            NotifyOfPropertyChange("EnableSequenceName");

            EnableNew = true;
            EnableReName = true;
            EnableCopy = true;
            EnableDelete = true;
            EnableSave = true;
            EnableStep = true;

            NotifyOfPropertyChange("EnableNew");
            NotifyOfPropertyChange("EnableReName");
            NotifyOfPropertyChange("EnableCopy");
            NotifyOfPropertyChange("EnableDelete");
            NotifyOfPropertyChange("EnableSave");
            NotifyOfPropertyChange("EnableStep");

            if (editMode == EditMode.None)
            {
                EnableNew = true;
                EnableReName = false;
                EnableCopy = false;
                EnableDelete = false;
                EnableStep = false;
                EnableSave = false;

                NotifyOfPropertyChange("EnableNew");
                NotifyOfPropertyChange("EnableReName");
                NotifyOfPropertyChange("EnableCopy");
                NotifyOfPropertyChange("EnableDelete");
                NotifyOfPropertyChange("EnableSave");
                NotifyOfPropertyChange("EnableStep");
            }
        }

        private bool IsChanged
        {
            get
            {
                var changed = false;
                if (!IsSavedDesc || editMode == EditMode.Edit)
                    changed = true;
                else
                {
                    foreach (var parameters in CurrentSequence.Steps)
                    {
                        if (parameters.Where(param => param.IsSaved == false && param.Name != "Step").Count() > 0)
                        {
                            changed = true;
                            break;
                        }
                    }
                }
                return changed;
            }
        }

        public bool EnableNew { get; set; }
        public bool EnableReName { get; set; }
        public bool EnableCopy { get; set; }
        public bool EnableDelete { get; set; }
        public bool EnableSave { get; set; }
        public bool EnableStep { get; set; }
        public bool IsSavedDesc { get; set; }


        public FileNode CurrentFileNode { get; private set; }
        public SequenceData CurrentSequence { get; private set; }
        public ObservableCollection<EditorDataGridTemplateColumnBase> Columns { get; set; }

        public bool EnableSequenceName { get; set; }

        private string selectedSequenceName = string.Empty;
        private string SequenceNameBeforeRename = string.Empty;
        private string lastSequenceName = string.Empty;

        private SequenceColumnBuilder columnBuilder = new SequenceColumnBuilder();
        private EditMode editMode;
        private SequenceDataProvider provider = new SequenceDataProvider();
    }
}
