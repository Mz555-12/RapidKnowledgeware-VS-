using RapidKnowledgeware.Functions.MainWindowFunc;
using RapidKnowledgeware.Models;

namespace RapidKnowledgeware.Functions.LLMAdjustFunc
{
    public static class LLMAdjustService
    {
        private static LLMAdjustModel _current;
        public static LLMAdjustModel Current
        {
            get
            {
                if (_current == null)
                {
                    _current = AppSettingsManager.LoadSettings<LLMAdjustModel>() ?? new LLMAdjustModel();
                }
                return _current;
            }
        }

        public static void Save()
        {
            AppSettingsManager.SaveSettings(Current);
        }

        public static SpaceAdjustModel CreateSpaceParametersFromDefault()
        {
            return new SpaceAdjustModel
            {
                ChatLLM = Current.Default_ChatLLM,
                Temperature = Current.Default_Temperature,
                TopP = Current.Default_TopP,
                RepeatPenalty = Current.Default_RepeatPenalty,
                SystemPrompt = Current.Default_SystemPrompt,
                QueryRefusalResponse = Current.Default_RefusalResponse
            };
        }
    }
}