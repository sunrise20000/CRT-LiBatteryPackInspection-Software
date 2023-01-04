using System;
using System.Collections.Generic;


namespace SicPM.RecipeExecutions
{
    public enum EnumRecipeRunningState
    {
        PrepareProcess,
        InProcess,
        PostProcess,
        NormalComplete,
        Failed,
    }

    public class RecipeRunningInfo
    {
        public Guid InnerId { get; set; }

        public RecipeHead Head { get; set; }
        public List<RecipeStep> RecipeStepList { get; set; }

        public string RecipeName { get; set; }

        public DateTime BeginTime { get; set; }
        public DateTime EndTime { get; set; }

        public int StepNumber { get; set; }
        public string StepName { get; set; }

        public double StepTime { get; set; }
        public double StepElapseTime { get; set; }

        public double TotalTime { get; set; }
        public double TotalElapseTime { get; set; }

        public double StepElapseTime2 { get; set; }
        public double TotalElapseTime2 { get; set; }

        public string ArH2Switch { get; set; }
        public string N2FlowMode { get; set; }

        public bool IsRoutineAbort { get; set; }
        public bool NeedReloadRecipe { get; set; }

        public string XmlRecipeToReload { get; set; }
        
    }
}
