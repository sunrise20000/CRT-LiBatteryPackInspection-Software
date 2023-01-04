using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SicPM.RecipeExecutions
{
    public interface IRecipeExecutor
    {
        bool IsError { get; }
        bool IsPaused { get; set; }

        RecipeRunningInfo RecipeRunningInfo { get;}

        bool CheckEnableRunProcess(out string reason);

        void ResetToleranceChecker();
        void OnProcessStart(string v1, string recipeName, bool v2);
        void PauseRecipe(out string reason);
        bool CheckEndPoint();
        bool CheckAllDevicesStable(float v1, float v2, float v3, float v4, float v5, float v6, float v7, float v8, float v9);
        void AbortRunProcess(out string reason);
    }

}
