using GalaSoft.MvvmLight;

namespace RapidKnowledgeware.Models
{
    public class LLMAdjustModel : ObservableObject
    {
        private string _defaultChatLLM = "qwen2.5:3b";
        public string Default_ChatLLM
        {
            get => _defaultChatLLM;
            set { _defaultChatLLM = value; RaisePropertyChanged(); }
        }

        private string _defaultBaseURL = "http://localhost:11434";
        public string Default_BaseURL
        {
            get => _defaultBaseURL;
            set { _defaultBaseURL = value; RaisePropertyChanged(); }
        }

        private int _defaultContextSize = 4096;
        public int Default_ContextSize
        {
            get => _defaultContextSize;
            set { _defaultContextSize = value; RaisePropertyChanged(); }
        }

        private float _defaultTemperature = 0.3f;
        public float Default_Temperature
        {
            get => _defaultTemperature;
            set { _defaultTemperature = value; RaisePropertyChanged(); }
        }

        private float _defaultTopP = 0.5f;
        public float Default_TopP
        {
            get => _defaultTopP;
            set { _defaultTopP = value; RaisePropertyChanged(); }
        }

        private float _defaultRepeatPenalty = 1.0f;
        public float Default_RepeatPenalty
        {
            get => _defaultRepeatPenalty;
            set { _defaultRepeatPenalty = value; RaisePropertyChanged(); }
        }

        private string _defaultSystemPrompt = "You are a helpful assistant.";
        public string Default_SystemPrompt
        {
            get => _defaultSystemPrompt;
            set { _defaultSystemPrompt = value; RaisePropertyChanged(); }
        }

        private string _defaultRefusalResponse = "抱歉，我无法回答该问题。";
        public string Default_RefusalResponse
        {
            get => _defaultRefusalResponse;
            set { _defaultRefusalResponse = value; RaisePropertyChanged(); }
        }
    }
}