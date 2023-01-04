using System.Collections.Generic;

namespace SicPM.RecipeExecutions
{
    public enum EnumEndByCondition
    {
        ByTime,

        ByStable,

        ByPurge,

        ByStep,

        ByEndpoint,

        ByPressure,//Routine增加该条件跳转步,压力值到达后直接跳下一步
    }

    public class RecipeStep
    {
        public string StepName;
        public double StepTime;
        public bool IsJumpStep;
        public bool IsLoopStartStep;
        public bool IsLoopEndStep;
        public bool IsDummyStep;
        public int LoopCount;
        public EnumEndByCondition EndBy;
        public double EndByValue;
        public Dictionary<string, string> RecipeCommands = new Dictionary<string, string>();
    }
}
