﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Aitex.Core.RT.Log;
using MECF.Framework.Common.Account.Extends;
using MECF.Framework.Common.DataCenter;
using MECF.Framework.UI.Client.CenterViews.Editors;
using MECF.Framework.UI.Client.CenterViews.Editors.Recipe;
using MECF.Framework.UI.Client.ClientBase;

namespace MECF.Framework.UI.Client.CenterViews.Configs.Roles
{
    public class RoleManager
    {
        public RoleManager()
        {

        }

        private RecipeFormatBuilder _recipeBuilder = new RecipeFormatBuilder();

        public ObservableCollection<string> ChamberType { get; set; }

        public ObservableCollection<ProcessTypeFileItem> ProcessTypeFileList { get; set; }

        public bool Initialize()
        {
            _RoleContainer.Clear();
            List<Common.Account.Extends.Role> roles = RoleAccountProvider.Instance.GetRoles();
            if (roles == null)
            {
                LOG.Error("GetRoles method failed,in RoleManager");
                return false;
            }

            List<AppMenu> menus = BaseApp.Instance.MenuLoader.MenuList;
            this._listMenuItems = menus;
            if (menus == null)
            {
                LOG.Error("GetMenus method failed,in RoleManager");
                return false;
            }

            //每个Role下再挂一个Recipe权限
            //Recipe名称从RecipeFormat文件中获取
            var chamberType = QueryDataClient.Instance.Service.GetConfig("System.Recipe.SupportedChamberType");
            if (chamberType == null)
            {
                ChamberType = new ObservableCollection<string>() { "Default" };
            }
            else
            {
                ChamberType = new ObservableCollection<string>(((string)(chamberType)).Split(','));
            }

            var processType = QueryDataClient.Instance.Service.GetConfig("System.Recipe.SupportedProcessType");
            if (processType == null)
            {
                processType = "Process,Routine";
            }

            ProcessTypeFileList = new ObservableCollection<ProcessTypeFileItem>();
            string[] recipeProcessType = ((string)processType).Split(',');

            _recipeInfos = _recipeBuilder.GetRecipeColumnName($"{ChamberType[0]}\\{recipeProcessType[0]}", "PM1");

            _stepInfos = _recipeBuilder.GetRecipeStepNo();

            _permissionInfos = _recipeBuilder.GetContentName($"{ChamberType[0]}\\Content", "PM1");

            foreach (Common.Account.Extends.Role r in roles)
            {
                RoleItem role = new RoleItem(r);
                role.DisplayRoleName = role.RoleName;
                role.DisplayAutoLogoutTime = role.AutoLogoutTime;
                role.DisplayIsAutoLogout = role.IsAutoLogout;
                role.DisplayDescription = role.Description;

                //Parse menu permission
                MenuPermission menuPermissionParse = new MenuPermission();
                menuPermissionParse.ParsePermission(r.MenuPermission);

                MenuInfo mp = new MenuInfo() { ID = "Header" };
                mp.Name = "Header";
                mp.EnumPermission = menuPermissionParse.GetPermissionByPageID("Header");
                role.AddMenuInfo(mp);

                //Get Menu information
                foreach (AppMenu topMenuItem in menus)
                {
                    foreach (AppMenu subMenuItem in topMenuItem.MenuItems)
                    {
                        mp = new MenuInfo() { ID = subMenuItem.MenuID };
                        mp.Name = topMenuItem.ResKey + "." + subMenuItem.ResKey; //Application.Current.FindResource(subMenuItem.ResKey) as string;
                        mp.EnumPermission = menuPermissionParse.GetPermissionByPageID(subMenuItem.MenuID);
                        role.AddMenuInfo(mp);
                    }
                }

                RecipeInfo recipe;

                foreach (RecipeInfo recipeInfo in _recipeInfos)
                {
                    //需要去掉空格
                    recipe = new RecipeInfo();
                    recipe.Name = recipeInfo.Name;
                    recipeInfo.Name = recipeInfo.Name.Replace(" ", "");
                    recipe.EnumPermission = menuPermissionParse.GetPermissionByPageID(recipeInfo.Name);
                    role.AddRecipeInfo(recipe);
                }

                StepInfo step;

                foreach (StepInfo stepInfo in _stepInfos)
                {
                    //需要去掉空格
                    step = new StepInfo();
                    step.Name = stepInfo.Name;
                    stepInfo.Name = stepInfo.Name.Replace(" ", "");
                    step.EnumPermission = menuPermissionParse.GetPermissionByPageID(stepInfo.Name);
                    role.AddStepInfo(step);
                }

                ContentInfo content;

                foreach (ContentInfo contentInfo in _permissionInfos)
                {
                    content = new ContentInfo();
                    content.Name = contentInfo.Name;
                    content.EnumPermission = menuPermissionParse.GetPermissionByPageID(contentInfo.Name);
                    role.AddContentInfo(content);
                }

                _RoleContainer.Add(role);
            }
            return true;
        }

        public List<RoleItem> GetAllRoles()
        {
            return _RoleContainer.ToList();
        }

        public bool AddRole(RoleItem r)
        {
            RoleItem existRole = GetRoleByName(r.RoleName);
            if (existRole != null)
            {
                LOG.Info("the role already exists, in RoleManager");
                return false;
            }
            existRole = GetRoleByID(r.RoleID);
            if (existRole != null)
            {
                LOG.Info("the role already exists, in RoleManager");
                return false;
            }
            _RoleContainer.Add(r);

            return true;
        }

        public string GenerateRoleID()
        {
            RoleItem r = GetRoleByID(_RoleNum.ToString());
            while (r != null)
            {
                _RoleNum++;
                r = GetRoleByID(_RoleNum.ToString());
            }

            return _RoleNum.ToString();
        }

        public RoleItem GetRoleByID(string ID)
        {
            return _RoleContainer.FirstOrDefault(t => t.RoleID == ID);
        }

        public RoleItem GetRoleByName(string name)
        {
            return _RoleContainer.FirstOrDefault(t => t.RoleName == name);
        }

        public RoleItem CreateRole()
        {
            RoleItem r = new RoleItem(GenerateRoleID());

            r.DisplayRoleName = r.RoleName = string.Empty;
            r.DisplayAutoLogoutTime = r.AutoLogoutTime = 10;
            r.DisplayIsAutoLogout = r.IsAutoLogout = false;
            r.DisplayDescription = r.Description = string.Empty;

            r.MenuCollection.Add(new MenuInfo() { ID = "Header", Name = "Header", EnumPermission = MenuPermissionEnum.MP_NONE });

            foreach (AppMenu topMenuItem in _listMenuItems)
            {
                foreach (AppMenu subMenuItem in topMenuItem.MenuItems)
                {
                    MenuInfo mp = new MenuInfo()
                    {
                        ID = subMenuItem.MenuID,
                        Name = topMenuItem.ResKey + "." + subMenuItem.ResKey, //Application.Current.FindResource(subMenuItem.ResKey) as string,
                        EnumPermission = MenuPermissionEnum.MP_NONE
                    };

                    r.MenuCollection.Add(mp);
                }
            }

            foreach (RecipeInfo recipeInfo in _recipeInfos)
            {
                RecipeInfo recipe = new RecipeInfo()
                {
                    Name = recipeInfo.Name,
                    EnumPermission = MenuPermissionEnum.MP_NONE
                };
                r.AddRecipeInfo(recipe);
            }

            foreach (StepInfo sInfo in _stepInfos)
            {
                StepInfo step = new StepInfo()
                {
                    Name = sInfo.Name,
                    EnumPermission = MenuPermissionEnum.MP_NONE
                };
                r.AddStepInfo(step);
            }

            foreach (ContentInfo permissionInfo in _permissionInfos)
            {
                ContentInfo permission = new ContentInfo()
                {
                    Name = permissionInfo.Name,
                    EnumPermission = MenuPermissionEnum.MP_NONE
                };
                r.AddContentInfo(permission);
            }

            return r;
        }

        public RoleItem CreateRole(RoleItem role)
        {
            RoleItem newRole = new RoleItem(GenerateRoleID())
            {
                RoleName = role.RoleName,
                AutoLogoutTime = role.AutoLogoutTime,
                IsAutoLogout = role.IsAutoLogout,
                Description = role.Description,
                DisplayRoleName = role.DisplayRoleName,
                DisplayAutoLogoutTime = role.DisplayAutoLogoutTime,
                DisplayIsAutoLogout = role.DisplayIsAutoLogout,
                DisplayDescription = role.DisplayDescription
            };

            foreach (MenuInfo mInfo in role.MenuCollection)
            {
                newRole.AddMenuInfo(mInfo.Clone());
            }

            foreach (RecipeInfo rInfo in role.RecipeCollection)
            {
                newRole.AddRecipeInfo(rInfo.Clone());
            }

            foreach (StepInfo sInfo in role.RecipeStepCollection)
            {
                newRole.AddStepInfo(sInfo.Clone());
            }

            foreach (ContentInfo pInfo in role.ContentCollection)
            {
                newRole.AddContentInfo(pInfo.Clone());
            }

            return newRole;
        }

        public RoleItem CloneRole(RoleItem role)
        {
            RoleItem newRole = new RoleItem(role.RoleID)
            {
                RoleName = role.RoleName,
                AutoLogoutTime = role.AutoLogoutTime,
                IsAutoLogout = role.IsAutoLogout,
                Description = role.Description,
                DisplayRoleName = role.DisplayRoleName,
                DisplayAutoLogoutTime = role.DisplayAutoLogoutTime,
                DisplayIsAutoLogout = role.DisplayIsAutoLogout,
                DisplayDescription = role.DisplayDescription
            };

            foreach (MenuInfo mInfo in role.MenuCollection)
            {
                newRole.AddMenuInfo(mInfo.Clone());
            }

            foreach (RecipeInfo rInfo in role.RecipeCollection)
            {
                newRole.AddRecipeInfo(rInfo.Clone());
            }

            foreach (StepInfo sInfo in role.RecipeStepCollection)
            {
                newRole.AddStepInfo(sInfo.Clone());
            }

            foreach (ContentInfo pInfo in role.ContentCollection)
            {
                newRole.AddContentInfo(pInfo.Clone());
            }

            return newRole;
        }

        public RoleItem CloneRole(string strRoleID)
        {
            RoleItem orignalRole = GetRoleByID(strRoleID);
            if (null == orignalRole)
                return null;

            RoleItem newRole = new RoleItem(strRoleID)
            {
                AutoLogoutTime = orignalRole.AutoLogoutTime,
                IsAutoLogout = orignalRole.IsAutoLogout,
                RoleName = orignalRole.RoleName,
                Description = orignalRole.Description,
                DisplayRoleName = orignalRole.DisplayRoleName,
                DisplayAutoLogoutTime = orignalRole.DisplayAutoLogoutTime,
                DisplayIsAutoLogout = orignalRole.DisplayIsAutoLogout,
                DisplayDescription = orignalRole.DisplayDescription
            };

            foreach (MenuInfo mInfo in orignalRole.MenuCollection)
            {
                newRole.AddMenuInfo(mInfo.Clone());
            }

            foreach (RecipeInfo rInfo in orignalRole.RecipeCollection)
            {
                newRole.AddRecipeInfo(rInfo.Clone());
            }

            foreach (ContentInfo pInfo in orignalRole.ContentCollection)
            {
                newRole.AddContentInfo(pInfo.Clone());
            }

            foreach (StepInfo sInfo in orignalRole.RecipeStepCollection)
            {
                newRole.AddStepInfo(sInfo.Clone());
            }

            return newRole;
        }

        public bool CheckAvailable(RoleItem role)
        {
            if (role == null)
                return false;

            foreach (RoleItem r in _RoleContainer)
            {
                if (role.RoleName == r.RoleName && role.RoleID != r.RoleID)
                {
                    LOG.Info("Check account available fail because the name already exists, in RoleManager");
                    return false;
                }
            }

            return true;
        }

        public bool SaveRole(RoleItem pRoletoSave)
        {
            if (!CheckAvailable(pRoletoSave))
                return false;


            Common.Account.Extends.Role r = new Common.Account.Extends.Role(
                pRoletoSave.RoleID,
                pRoletoSave.RoleName,
                pRoletoSave.IsAutoLogout,
                pRoletoSave.AutoLogoutTime,
                MenuPermission.PermissionToString(pRoletoSave.MenuCollection, pRoletoSave.RecipeCollection,
                pRoletoSave.ContentCollection,pRoletoSave.RecipeStepCollection),
                pRoletoSave.Description
                );

            if (RoleAccountProvider.Instance.UpdateRole(r))
            {
                RoleItem orignalRole = GetRoleByID(pRoletoSave.RoleID);
                if (null == orignalRole)
                {
                    RoleItem NewRole = CloneRole(pRoletoSave);
                    AddRole(NewRole);
                    return true;
                }

                orignalRole.RoleName = pRoletoSave.RoleName;
                orignalRole.IsAutoLogout = pRoletoSave.IsAutoLogout;
                orignalRole.AutoLogoutTime = pRoletoSave.AutoLogoutTime;
                orignalRole.Description = pRoletoSave.Description;
                orignalRole.MenuCollection.Clear();
                foreach (MenuInfo mInfo in pRoletoSave.MenuCollection)
                {
                    orignalRole.AddMenuInfo(mInfo.Clone());
                    mInfo.ComboBoxSaved = true;
                }

                foreach (RecipeInfo rInfo in pRoletoSave.RecipeCollection)
                {
                    orignalRole.AddRecipeInfo(rInfo.Clone());
                    rInfo.ComboBoxSaved = true;
                }

                foreach (ContentInfo pInfo in pRoletoSave.ContentCollection)
                {
                    orignalRole.AddContentInfo(pInfo.Clone());
                    pInfo.ComboBoxSaved = true;
                }

                foreach (StepInfo sInfo in pRoletoSave.RecipeStepCollection)
                {
                    orignalRole.AddStepInfo(sInfo.Clone());
                    sInfo.ComboBoxSaved = true;
                }
            }
            else
            {
                LOG.Error("UpdateRoles method failed, in RoleManager");
                return false;
            }

            return true;
        }

        public bool DeleteRole(string strRoleID)
        {
            RoleItem r = GetRoleByID(strRoleID);
            if (r != null)
            {
                if (RoleAccountProvider.Instance.DeleteRole(strRoleID))
                {
                    _RoleContainer.Remove(r);
                    return true;
                }
                else
                {
                    LOG.Error("DeleteRole method failed, in RoleManager");
                }
            }
            else
            {
                LOG.Warning("Can not find the role to delete, in RoleManager");
            }

            return false;
        }


        private static RoleManager _Instance = null;
        public static RoleManager Instance
        {
            get
            {
                if (_Instance == null)
                {
                    _Instance = new RoleManager();
                }

                return _Instance;
            }
        }

        private ObservableCollection<RoleItem> _RoleContainer = new ObservableCollection<RoleItem>();
        public ObservableCollection<RoleItem> RoleContainer
        {
            get { return _RoleContainer; }
        }

        private static int _RoleNum = 0;
        private List<AppMenu> _listMenuItems = null;
        private ObservableCollection<RecipeInfo> _recipeInfos = null;
        private ObservableCollection<StepInfo> _stepInfos = null;
        private ObservableCollection<ContentInfo> _permissionInfos = null;
    }

    public class MenuPermission
    {
        public void ParsePermission(string strPermission)
        {
            try
            {
                strPermission = strPermission.Replace("\n", "").Replace(" ", "");
                int groupIndex = strPermission.IndexOf(';');
                if (groupIndex == -1)
                {
                    AddToDictionary(strPermission);
                    return;
                }

                string strGroup = strPermission.Substring(0, groupIndex);
                AddToDictionary(strGroup);

                ParsePermission(strPermission.Substring(groupIndex + 1));

            }
            catch (System.Exception ex)
            {
                LOG.Write(ex);
            }
        }

        public MenuPermissionEnum GetPermissionByPageID(string strPageID)
        {
            if (MenuPermissionDictionary.ContainsKey(strPageID))
            {
                return MenuPermissionDictionary[strPageID];
            }

            return MenuPermissionEnum.MP_NONE;
        }

        public static string PermissionToString(ObservableCollection<MenuInfo> listPermission, 
            ObservableCollection<RecipeInfo> recipeListPermission, ObservableCollection<ContentInfo> permissionListPermission,
            ObservableCollection<StepInfo> recipeStepPermission)
        {
            string strResult = "";

            if (listPermission == null)
                return strResult;

            foreach (MenuInfo info in listPermission)
            {
                strResult += info.ID;
                strResult += ",";
                strResult += info.IndexPermission;
                strResult += ";";
            }

            foreach (RecipeInfo info in recipeListPermission)
            {
                strResult += info.Name;
                strResult += ",";
                strResult += info.IndexPermission;
                strResult += ";";
            }

            foreach (ContentInfo info in permissionListPermission)
            {
                strResult += info.Name;
                strResult += ",";
                strResult += info.IndexPermission;
                strResult += ";";
            }

            foreach (StepInfo info in recipeStepPermission)
            {
                strResult += info.Name;
                strResult += ",";
                strResult += info.IndexPermission;
                strResult += ";";
            }

            return strResult;
        }

        private void AddToDictionary(string strGroupName)
        {
            int Index = strGroupName.IndexOf(',');
            if (Index == -1)
                return;

            string strMenuID = strGroupName.Substring(0, Index);
            string strMenuPermision = strGroupName.Substring(Index + 1);

            MenuPermissionDictionary.Add(strMenuID, (MenuPermissionEnum)Convert.ToInt32(strMenuPermision));
        }

        public MenuPermissionEnum this[string strPageID]
        {
            get
            {
                return GetPermissionByPageID(strPageID);
            }

        }

        private Dictionary<string, MenuPermissionEnum> _dicPermission = new Dictionary<string, MenuPermissionEnum>();
        public Dictionary<string, MenuPermissionEnum> MenuPermissionDictionary
        {
            get { return _dicPermission; }
        }
    }
}
