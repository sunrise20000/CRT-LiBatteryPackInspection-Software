using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MECF.Framework.Common.CommonData;

namespace SicPM.RecipeExecutions
{
    public interface IRecipeDBCallback
    {
        void RecipeStart(string module, int slot, string guid, string recipeName);
        void RecipeComplete(string guid);

        void RecipeStepStart(string recipeGuid, int stepNumber, string stepName, float stepTime);
        void RecipeStepEnd(string recipeGuid, int stepNumber, List<FdcDataItem> data);

        void RecipeFailed(string guid);
    }
}
