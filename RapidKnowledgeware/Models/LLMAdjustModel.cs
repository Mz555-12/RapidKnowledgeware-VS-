using GalaSoft.MvvmLight;

namespace RapidKnowledgeware.Models
{
    /// <summary>
    /// LLM 全局默认参数模型
    /// </summary>
    public class LLMAdjustModel : ObservableObject
    {
        private string _defaultChatLLM = "qwen2.5:3b";
        /// <summary>
        /// 默认对话模型名称
        /// </summary>
        public string Default_ChatLLM
        {
            get => _defaultChatLLM;
            set { _defaultChatLLM = value; RaisePropertyChanged(); }
        }

        private string _defaultDeepThinkingLLM;
        /// <summary>
        /// 默认深度思考模型名称（可选，空字符串或null表示不使用）
        /// </summary>
        public string Default_DeepThinkingLLM
        {
            get => _defaultDeepThinkingLLM;
            set { _defaultDeepThinkingLLM = value; RaisePropertyChanged(); }
        }

        private string _defaultBaseURL = "http://localhost:11434";
        /// <summary>
        /// Ollama 服务地址
        /// </summary>
        public string Default_BaseURL
        {
            get => _defaultBaseURL;
            set { _defaultBaseURL = value; RaisePropertyChanged(); }
        }

        private int _defaultContextSize = 2000;
        /// <summary>
        /// 默认上下文窗口大小
        /// </summary>
        public int Default_ContextSize
        {
            get => _defaultContextSize;
            set { _defaultContextSize = value; RaisePropertyChanged(); }
        }

        private float _defaultTemperature = 0.3f;
        /// <summary>
        /// 默认温度参数（控制随机性）
        /// </summary>
        public float Default_Temperature
        {
            get => _defaultTemperature;
            set { _defaultTemperature = value; RaisePropertyChanged(); }
        }

        private float _defaultTopP = 0.5f;
        /// <summary>
        /// 默认 Top-P 核采样参数
        /// </summary>
        public float Default_TopP
        {
            get => _defaultTopP;
            set { _defaultTopP = value; RaisePropertyChanged(); }
        }

        private float _defaultRepeatPenalty = 1.0f;
        /// <summary>
        /// 默认重复惩罚系数
        /// </summary>
        public float Default_RepeatPenalty
        {
            get => _defaultRepeatPenalty;
            set { _defaultRepeatPenalty = value; RaisePropertyChanged(); }
        }

        private string _defaultSystemPrompt = "你是一个助理，解决用户的疑难杂症，要灵活对话，别这么死板。 \n\n 使用以下提供的上下文信息回答问题，请优先信赖和引用上下文中的相关描述。\n如果确实找不到任何相关信息，请根据提示词和历史聊天记录以及你知道的知识点结合来回答问题。";
        /// <summary>
        /// 默认系统提示词
        /// </summary>
        public string Default_SystemPrompt
        {
            get => _defaultSystemPrompt;
            set { _defaultSystemPrompt = value; RaisePropertyChanged(); }
        }

        private string _defaultRefusalResponse = "抱歉，我无法回答该问题。";
        /// <summary>
        /// 默认拒绝响应文本（当模型无法回答时返回）
        /// </summary>
        public string Default_RefusalResponse
        {
            get => _defaultRefusalResponse;
            set { _defaultRefusalResponse = value; RaisePropertyChanged(); }
        }
        private bool _globalIsDeepThinking;
        /// <summary>
        /// 全局深度思考开关（所有会话共享）
        /// </summary>
        public bool GlobalIsDeepThinking
        {
            get => _globalIsDeepThinking;
            set { _globalIsDeepThinking = value; RaisePropertyChanged(); }
        }

        private float _defaultChatHistoryPercentage = 0.25f;
        /// <summary>
        /// 历史记录占上下文窗口的最大比例（0~1）
        /// </summary>
        public float Default_ChatHistoryPercentage
        {
            get => _defaultChatHistoryPercentage;
            set { _defaultChatHistoryPercentage = value; RaisePropertyChanged(); }
        }

        private int _defaultChatHistoryMemory = 5;
        /// <summary>
        /// 最多保留的历史轮数
        /// </summary>
        public int Default_ChatHistoryMemory
        {
            get => _defaultChatHistoryMemory;
            set { _defaultChatHistoryMemory = value; RaisePropertyChanged(); }
        }



    }
}