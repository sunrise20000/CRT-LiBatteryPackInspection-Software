using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MECF.Framework.Common.CommonData;
using MECF.Framework.Common.DBCore;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.SubstrateTrackings;

namespace SicPM.RecipeExecutions
{
    class RecipeDBCallback : IRecipeDBCallback
    {
        public void RecipeStart(string module, int slot, string guid, string recipeName)
        {
            string waferId = "";
            if (!WaferManager.Instance.GetWafer(ModuleHelper.Converter(module), 0).IsEmpty)
                waferId = WaferManager.Instance.GetWafer(ModuleHelper.Converter(module), 0).InnerId.ToString();

            ProcessDataRecorder.Start(guid, recipeName, waferId, module.ToString());

        }

        public void RecipeUpdateStatus(string guid, string status)
        {
            ProcessDataRecorder.UpdateStatus(guid, status);
        }

        public void RecipeComplete(string guid)
        {
            ProcessDataRecorder.End(guid, "Succeed");
        }

        public void RecipeStepStart(string recipeGuid, int stepNumber, string stepName, float stepTime)
        {
            ProcessDataRecorder.StepStart(recipeGuid, stepNumber+1, stepName, stepTime);
        }

        public void RecipeStepEnd(string recipeGuid, int stepNumber, List<FdcDataItem> fdc)
        {
            ProcessDataRecorder.StepEnd(recipeGuid, stepNumber+1, fdc);
        }

        public void RecipeFailed(string guid)
        {
            ProcessDataRecorder.End(guid, "Failed");
        }
    }
}
