using System.Collections.ObjectModel;
using Caliburn.Micro.Core;
using MECF.Framework.UI.Client.ClientBase;
using OpenSEMI.ClientBase;

namespace MECF.Framework.UI.Client.CenterViews.Configs.Roles
{
    public class RoleItem : PropertyChangedBase
    {
        public RoleItem(Common.Account.Extends.Role r)
        {
            this._Role = r;
        }

        public RoleItem(string roleid)
        {
            this._Role = new Common.Account.Extends.Role(roleid, string.Empty, false, 0, string.Empty, string.Empty);
        }

        private Common.Account.Extends.Role _Role;
        public Common.Account.Extends.Role Role
        {
            get { return _Role; }
            set { _Role = value; }
        }

        public string RoleID
        {
            get { return _Role.RoleID; }
            set { _Role.RoleID = value; }
        }

        public string RoleName
        {
            get { return _Role.RoleName; }
            set { _Role.RoleName = value; }
        }

        public int AutoLogoutTime
        {
            get { return _Role.LogoutTime; }
            set { _Role.LogoutTime = value; }
        }

        public string Description
        {
            get { return _Role.Description; }
            set { _Role.Description = value; }
        }
        
        public bool IsAutoLogout
        {
            get { return _Role.IsAutoLogout; }
            set { _Role.IsAutoLogout = value; }
        }

        private string _DisplayRoleName;
        public string DisplayRoleName
        {
            get { return _DisplayRoleName; }
            set { _DisplayRoleName = value; NotifyOfPropertyChange("DisplayRoleName"); }
        }

        public int DisplayAutoLogoutTime
        {
            get;
            set;
        }

        public bool DisplayIsAutoLogout
        {
            get;
            set;
        }


        public string DisplayDescription
        {
            get;
            set;
        }


        private bool _IsSelected = false;
        public bool IsSelected
        {
            get { return _IsSelected; }
            set { _IsSelected = value; NotifyOfPropertyChange("IsSelected"); }
        }

        private bool _RoleNameTextSaved = true;
        public bool RoleNameTextSaved
        {
            get { return _RoleNameTextSaved; }
            set
            {
                if (_RoleNameTextSaved != value)
                {
                    _RoleNameTextSaved = value;
                    NotifyOfPropertyChange("RoleNameTextSaved");
                }
            }
        }

        private bool _TimeTextSaved = true;
        public bool TimeTextSaved
        {
            get { return _TimeTextSaved; }
            set
            {
                if (_TimeTextSaved != value)
                {
                    _TimeTextSaved = value;
                    NotifyOfPropertyChange("TimeTextSaved");
                }
            }
        }

        private bool _DescriptionTextSaved = true;
        public bool DescriptionTextSaved
        {
            get { return _DescriptionTextSaved; }
            set
            {
                if (_DescriptionTextSaved != value)
                {
                    _DescriptionTextSaved = value;
                    NotifyOfPropertyChange("DescriptionTextSaved");
                }
            }
        }
        
        private ObservableCollection<MenuInfo> _MenuCollection = new ObservableCollection<MenuInfo>();
        public ObservableCollection<MenuInfo> MenuCollection
        {
            get { return _MenuCollection; }
        }

        private ObservableCollection<RecipeInfo> _RecipeCollection = new ObservableCollection<RecipeInfo>();
        public ObservableCollection<RecipeInfo> RecipeCollection
        {
            get { return _RecipeCollection; }
        }

        private ObservableCollection<StepInfo> _RecipeStepCollection = new ObservableCollection<StepInfo>();
        public ObservableCollection<StepInfo> RecipeStepCollection
        {
            get { return _RecipeStepCollection; }
        }

        private ObservableCollection<ContentInfo> _ContentCollection = new ObservableCollection<ContentInfo>();
        public ObservableCollection<ContentInfo> ContentCollection
        {
            get { return _ContentCollection; }
        }

        public void AddMenuInfo(MenuInfo pInfo)
        {
            if (null == pInfo)
                return;

            MenuCollection.Add(pInfo);
        }

        public void AddRecipeInfo(RecipeInfo pInfo)
        {
            if (null == pInfo)
                return;

            RecipeCollection.Add(pInfo);
        }

        public void AddStepInfo(StepInfo pInfo)
        {
            if (null == pInfo)
                return;

            RecipeStepCollection.Add(pInfo);
        }

        public void AddContentInfo(ContentInfo pInfo)
        {
            if (null == pInfo)
                return;

            ContentCollection.Add(pInfo);
        }

        public bool IsRoleChanged()
        {
            if (this.DisplayRoleName != this.Role.RoleName)
                return true;

            if (this.DisplayAutoLogoutTime != this.Role.LogoutTime)
                return true;

            if (this.DisplayIsAutoLogout != this.Role.IsAutoLogout)
                return true;

            if (this.DisplayDescription != this.Role.Description)
                return true;

            foreach (MenuInfo item in _MenuCollection)
            {
                if (item.DisplayIndexPermission != item.IndexPermission - 1)
                    return true;
            }

            foreach (RecipeInfo item in _RecipeCollection)
            {
                if (item.DisplayIndexPermission != item.IndexPermission - 1)
                    return true;
            }

            foreach (StepInfo item in _RecipeStepCollection)
            {
                if (item.DisplayIndexPermission != item.IndexPermission - 1)
                    return true;
            }

            foreach (ContentInfo item in ContentCollection)
            {
                if (item.DisplayIndexPermission != item.IndexPermission - 1)
                    return true;
            }

            return false;
        }
    }

    public class MenuInfo : PropertyChangedBase
    {
        private string _strMenuID;
        public string ID
        {
            get { return _strMenuID; }
            set
            {
                if (value != _strMenuID)
                    _strMenuID = value;
            }
        }

        private MenuPermissionEnum _EnummenuPermission;
        public MenuPermissionEnum EnumPermission
        {
            get { return _EnummenuPermission; }
            set
            {
                if (value != _EnummenuPermission)
                {
                    _EnummenuPermission = value;
                    _IndexMenuPermission = (int)_EnummenuPermission;
                }
            }
        }

        private string _StrMenuPermission;
        public string stringPermission
        {
            get { return _StrMenuPermission; }
            set
            {
                if (value != _StrMenuPermission)
                    _StrMenuPermission = value;
            }
        }

        private int _IndexMenuPermission;
        public int IndexPermission
        {
            get { return _IndexMenuPermission; }
            set
            {
                if (value != _IndexMenuPermission)
                {
                    _IndexMenuPermission = value;
                    EnumPermission = (MenuPermissionEnum)_IndexMenuPermission;
                }
            }
        }

        private int _DisplayIndexPermission;
        public int DisplayIndexPermission
        {
            get { return _DisplayIndexPermission; }
            set
            {
                if (value != _DisplayIndexPermission)
                {
                    _DisplayIndexPermission = value;
                }
            }
        }

        private bool _ComboBoxSaved = false;
        public bool ComboBoxSaved
        {
            get { return _ComboBoxSaved; }
            set
            {
                if (_ComboBoxSaved != value)
                {
                    _ComboBoxSaved = value;
                    NotifyOfPropertyChange("ComboBoxSaved");
                }
            }
        }

        private string _strMenuName;
        public string Name
        {
            get { return _strMenuName; }
            set
            {
                if (value != _strMenuName)
                    _strMenuName = value;
            }
        }

        public MenuInfo Clone()
        {
            MenuInfo menuInfo = new MenuInfo();

            menuInfo.Name = this.Name;
            menuInfo.stringPermission = this.stringPermission;
            menuInfo.EnumPermission = this.EnumPermission;
            menuInfo.ID = this.ID;
            menuInfo.IndexPermission = this.IndexPermission;
            menuInfo.DisplayIndexPermission = this.IndexPermission - 1;

            return menuInfo;
        }
    }

    public class RecipeInfo : PropertyChangedBase
    {
        private string _strRecipeID;
        public string ID
        {
            get { return _strRecipeID; }
            set
            {
                if (value != _strRecipeID)
                    _strRecipeID = value;
            }
        }

        private MenuPermissionEnum _EnummenuPermission;
        public MenuPermissionEnum EnumPermission
        {
            get { return _EnummenuPermission; }
            set
            {
                if (value != _EnummenuPermission)
                {
                    _EnummenuPermission = value;
                    _IndexRecipePermission = (int)_EnummenuPermission;
                }
            }
        }

        private string _StrRecipePermission;
        public string stringPermission
        {
            get { return _StrRecipePermission; }
            set
            {
                if (value != _StrRecipePermission)
                    _StrRecipePermission = value;
            }
        }

        private int _IndexRecipePermission;
        public int IndexPermission
        {
            get { return _IndexRecipePermission; }
            set
            {
                if (value != _IndexRecipePermission)
                {
                    _IndexRecipePermission = value;
                    EnumPermission = (MenuPermissionEnum)_IndexRecipePermission;
                }
            }
        }

        private int _DisplayIndexPermission;
        public int DisplayIndexPermission
        {
            get { return _DisplayIndexPermission; }
            set
            {
                if (value != _DisplayIndexPermission)
                {
                    _DisplayIndexPermission = value;
                }
            }
        }

        private bool _ComboBoxSaved = false;
        public bool ComboBoxSaved
        {
            get { return _ComboBoxSaved; }
            set
            {
                if (_ComboBoxSaved != value)
                {
                    _ComboBoxSaved = value;
                    NotifyOfPropertyChange("ComboBoxSaved");
                }
            }
        }

        private string _strRecipeName;
        public string Name
        {
            get { return _strRecipeName; }
            set
            {
                if (value != _strRecipeName)
                    _strRecipeName = value;
            }
        }

        public RecipeInfo Clone()
        {
            RecipeInfo recipeInfo = new RecipeInfo();

            recipeInfo.Name = this.Name;
            recipeInfo.stringPermission = this.stringPermission;
            recipeInfo.EnumPermission = this.EnumPermission;
            recipeInfo.ID = this.ID;
            recipeInfo.IndexPermission = this.IndexPermission;
            recipeInfo.DisplayIndexPermission = this.IndexPermission - 1;

            return recipeInfo;
        }
    }

    public class StepInfo : PropertyChangedBase
    {
        private string _strStepID;
        public string ID
        {
            get { return _strStepID; }
            set
            {
                if (value != _strStepID)
                    _strStepID = value;
            }
        }

        private MenuPermissionEnum _EnummenuPermission;
        public MenuPermissionEnum EnumPermission
        {
            get { return _EnummenuPermission; }
            set
            {
                if (value != _EnummenuPermission)
                {
                    _EnummenuPermission = value;
                    _IndexStepPermission = (int)_EnummenuPermission;
                }
            }
        }

        private string _StrStepPermission;
        public string stringPermission
        {
            get { return _StrStepPermission; }
            set
            {
                if (value != _StrStepPermission)
                    _StrStepPermission = value;
            }
        }

        private int _IndexStepPermission;
        public int IndexPermission
        {
            get { return _IndexStepPermission; }
            set
            {
                if (value != _IndexStepPermission)
                {
                    _IndexStepPermission = value;
                    EnumPermission = (MenuPermissionEnum)_IndexStepPermission;
                }
            }
        }

        private int _DisplayIndexPermission;
        public int DisplayIndexPermission
        {
            get { return _DisplayIndexPermission; }
            set
            {
                if (value != _DisplayIndexPermission)
                {
                    _DisplayIndexPermission = value;
                }
            }
        }

        private bool _ComboBoxSaved = false;
        public bool ComboBoxSaved
        {
            get { return _ComboBoxSaved; }
            set
            {
                if (_ComboBoxSaved != value)
                {
                    _ComboBoxSaved = value;
                    NotifyOfPropertyChange("ComboBoxSaved");
                }
            }
        }

        private string _strStepName;
        public string Name
        {
            get { return _strStepName; }
            set
            {
                if (value != _strStepName)
                    _strStepName = value;
            }
        }

        public StepInfo Clone()
        {
            StepInfo stepInfo = new StepInfo();

            stepInfo.Name = this.Name;
            stepInfo.stringPermission = this.stringPermission;
            stepInfo.EnumPermission = this.EnumPermission;
            stepInfo.ID = this.ID;
            stepInfo.IndexPermission = this.IndexPermission;
            stepInfo.DisplayIndexPermission = this.IndexPermission - 1;

            return stepInfo;
        }
    }

    public class ContentInfo : PropertyChangedBase
    {
        private string _strPermissionID;
        public string ID
        {
            get { return _strPermissionID; }
            set
            {
                if (value != _strPermissionID)
                    _strPermissionID = value;
            }
        }

        private MenuPermissionEnum _EnummenuPermission;
        public MenuPermissionEnum EnumPermission
        {
            get { return _EnummenuPermission; }
            set
            {
                if (value != _EnummenuPermission)
                {
                    _EnummenuPermission = value;
                    _IndexPermission = (int)_EnummenuPermission;
                }
            }
        }

        private string _StrPermission;
        public string stringPermission
        {
            get { return _StrPermission; }
            set
            {
                if (value != _StrPermission)
                    _StrPermission = value;
            }
        }

        private int _IndexPermission;
        public int IndexPermission
        {
            get { return _IndexPermission; }
            set
            {
                if (value != _IndexPermission)
                {
                    _IndexPermission = value;
                    EnumPermission = (MenuPermissionEnum)_IndexPermission;
                }
            }
        }

        private int _DisplayIndexPermission;
        public int DisplayIndexPermission
        {
            get { return _DisplayIndexPermission; }
            set
            {
                if (value != _DisplayIndexPermission)
                {
                    _DisplayIndexPermission = value;
                }
            }
        }

        private bool _ComboBoxSaved = false;
        public bool ComboBoxSaved
        {
            get { return _ComboBoxSaved; }
            set
            {
                if (_ComboBoxSaved != value)
                {
                    _ComboBoxSaved = value;
                    NotifyOfPropertyChange("ComboBoxSaved");
                }
            }
        }

        private string _strName;
        public string Name
        {
            get { return _strName; }
            set
            {
                if (value != _strName)
                    _strName = value;
            }
        }

        public ContentInfo Clone()
        {
            ContentInfo contentInfo = new ContentInfo();

            contentInfo.Name = this.Name;
            contentInfo.stringPermission = this.stringPermission;
            contentInfo.EnumPermission = this.EnumPermission;
            contentInfo.ID = this.ID;
            contentInfo.IndexPermission = this.IndexPermission;
            contentInfo.DisplayIndexPermission = this.IndexPermission - 1;

            return contentInfo;
        }
    }
}
