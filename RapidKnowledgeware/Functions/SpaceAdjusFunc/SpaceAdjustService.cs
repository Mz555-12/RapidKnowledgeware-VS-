using RapidKnowledgeware.Functions.MainWindowFunc;
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

        /// <summary>
        /// 将指定空间参数保存为全局默认设置
        /// </summary>
        /// <param name="source">源空间参数模型</param>
        public static void SaveAsDefault(SpaceAdjustModel source)
        {
            var def = LLMAdjustFunc.LLMAdjustService.Current;
            def.Default_ChatLLM = source.ChatLLM;
            def.Default_DeepThinkingLLM = source.DeepThinkingLLM;
            def.Default_Temperature = source.Temperature;
            def.Default_TopP = source.TopP;
            def.Default_RepeatPenalty = source.RepeatPenalty;
            def.Default_SystemPrompt = source.SystemPrompt;
            def.Default_RefusalResponse = source.QueryRefusalResponse;

            KnowledgeBaseModel.Instance.Default_SearchQuantity = source.SearchQuantity;
            KnowledgeBaseModel.Instance.Default_IndexSimilarityThreshold = source.IndexSimilarityThreshold;

            LLMAdjustFunc.LLMAdjustService.Save();
            AppSettingsManager.SaveSettings(KnowledgeBaseModel.Instance);
        }
    }
}