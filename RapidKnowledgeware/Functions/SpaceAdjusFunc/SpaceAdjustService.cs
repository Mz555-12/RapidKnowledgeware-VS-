using RapidKnowledgeware.Models;

namespace RapidKnowledgeware.Functions.SpaceAdjustFunc
{
    /// <summary>
    /// 空间参数业务逻辑服务类
    /// </summary>
    public static class SpaceAdjustService
    {
        /// <summary>
        /// 将目标空间参数重置为全局默认值
        /// </summary>
        /// <param name="target">需要重置的空间参数模型</param>
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