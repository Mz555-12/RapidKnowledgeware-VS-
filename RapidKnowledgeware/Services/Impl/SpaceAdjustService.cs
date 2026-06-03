using IOC.Annotations;
using RapidKnowledgeware.Models;
using RapidKnowledgeware.DAO;
using RapidKnowledgeware.Services;

namespace RapidKnowledgeware.Services.Impl
{
    [Service]
    public class SpaceAdjustService : ISpaceAdjustService
    {
        private readonly ILLMAdjustService _llmAdjustService;
        private readonly IAppSettingsRepository _appSettingsManager;

        public SpaceAdjustService(ILLMAdjustService llmAdjustService, IAppSettingsRepository appSettingsManager)
        {
            _llmAdjustService = llmAdjustService;
            _appSettingsManager = appSettingsManager;
        }

        public void ResetToDefault(SpaceAdjustModel target)
        {
            var def = _llmAdjustService.Current;
            target.ChatLLM = def.Default_ChatLLM;
            target.Temperature = def.Default_Temperature;
            target.TopP = def.Default_TopP;
            target.RepeatPenalty = def.Default_RepeatPenalty;
            target.SystemPrompt = def.Default_SystemPrompt;
            target.QueryRefusalResponse = def.Default_RefusalResponse;
        }

        public void SaveAsDefault(SpaceAdjustModel source)
        {
            var def = _llmAdjustService.Current;
            def.Default_ChatLLM = source.ChatLLM;
            def.Default_DeepThinkingLLM = source.DeepThinkingLLM;
            def.Default_Temperature = source.Temperature;
            def.Default_TopP = source.TopP;
            def.Default_RepeatPenalty = source.RepeatPenalty;
            def.Default_SystemPrompt = source.SystemPrompt;
            def.Default_RefusalResponse = source.QueryRefusalResponse;

            KnowledgeBaseModel.Instance.Default_SearchQuantity = source.SearchQuantity;
            KnowledgeBaseModel.Instance.Default_IndexSimilarityThreshold = source.IndexSimilarityThreshold;

            _llmAdjustService.Save();
            _appSettingsManager.SaveSettings(KnowledgeBaseModel.Instance);
        }
    }
}
