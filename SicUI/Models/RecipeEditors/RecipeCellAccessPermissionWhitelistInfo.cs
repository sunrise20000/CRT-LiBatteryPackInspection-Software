using System;
using SicUI.Client;

namespace SicUI.Models.RecipeEditors
{
    internal class RecipeCellAccessPermissionWhitelistInfo
    {

        public RecipeCellAccessPermissionWhitelistInfo(string recipeName, string stepUid, string controlName, DateTime whenSet)
        {
            RecipeName = recipeName;
            StepUid = stepUid;
            ControlName = controlName;
            WhenSet = whenSet;

            WhoSet = ClientApp.Instance.UserContext.LoginName;
            Uuid = Guid.NewGuid().ToString("N");
        }

        public string Uuid { get; }

        public string RecipeName { get; }

        public string StepUid { get; }

        public string ControlName { get; }

        public string WhoSet { get; }

        public DateTime WhenSet { get; }

        public override string ToString()
        {
            return $"'{Uuid}', '{RecipeName}', '{StepUid}', '{ControlName}', '{WhoSet}', '{WhenSet}'";
        }
    }
}
