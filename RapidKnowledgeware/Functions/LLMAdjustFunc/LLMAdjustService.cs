using RapidKnowledgeware.Functions.MainWindowFunc;
using RapidKnowledgeware.Models;

namespace RapidKnowledgeware.Functions.LLMAdjustFunc
{
    /// <summary>
    /// LLM 全局默认参数服务类
    /// </summary>
    public static class LLMAdjustService
    {
        private static LLMAdjustModel _current;
        /// <summary>
        /// 当前全局 LLM 参数配置（单例）
        /// </summary>
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

        /// <summary>
        /// 保存当前 LLM 配置到文件
        /// </summary>
        public static void Save()
        {
            AppSettingsManager.SaveSettings(Current);
        }

        /// <summary>
        /// 根据全局默认参数创建一个新的空间参数实例
        /// </summary>
        /// <returns>初始化为默认值的 SpaceAdjustModel 实例</returns>
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