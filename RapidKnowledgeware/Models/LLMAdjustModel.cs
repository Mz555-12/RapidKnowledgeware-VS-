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

        private string _defaultBaseURL = "http://localhost:11434";
        /// <summary>
        /// Ollama 服务地址
        /// </summary>
        public string Default_BaseURL
        {
            get => _defaultBaseURL;
            set { _defaultBaseURL = value; RaisePropertyChanged(); }
        }

        private int _defaultContextSize = 4096;
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

        private string _defaultSystemPrompt = "You are a helpful assistant.";
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
    }
}