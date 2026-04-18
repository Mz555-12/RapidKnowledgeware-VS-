using RapidKnowledgeware.Models;

namespace RapidKnowledgeware.Functions.SpaceAdjustFunc
{
    public static class SpaceAdjustService
    {
        public static void ResetToDefault(SpaceAdjustModel target)
        {
            var def = LLMAdjustFunc.LLMAdjustService.Current;
            target.ChatLLM = def.Default_ChatLLM;
            target.Temperature = def.Default_Temperature;
            target.TopP = def.Default_TopP;
            target.RepeatPenalty = def.Default_RepeatPenalty;
            target.SystemPrompt = def.Default_SystemPrompt;
            target.QueryRefusalResponse = def.Default_RefusalResponse;
        }
    }
}