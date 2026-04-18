using GalaSoft.MvvmLight;

namespace RapidKnowledgeware.Models
{
    public class SpaceAdjustModel : ObservableObject
    {
        private string _chatLLM = "qwen2.5:3b";
        public string ChatLLM
        {
            get => _chatLLM;
            set { _chatLLM = value; RaisePropertyChanged(); }
        }

        private float _temperature = 0.3f;
        public float Temperature
        {
            get => _temperature;
            set { _temperature = value; RaisePropertyChanged(); }
        }

        private float _topP = 0.5f;
        public float TopP
        {
            get => _topP;
            set { _topP = value; RaisePropertyChanged(); }
        }

        private float _repeatPenalty = 1.0f;
        public float RepeatPenalty
        {
            get => _repeatPenalty;
            set { _repeatPenalty = value; RaisePropertyChanged(); }
        }

        private string _queryRefusalResponse = "抱歉，我无法回答该问题。";
        public string QueryRefusalResponse
        {
            get => _queryRefusalResponse;
            set { _queryRefusalResponse = value; RaisePropertyChanged(); }
        }

        private string _systemPrompt = "You are a helpful assistant.";
        public string SystemPrompt
        {
            get => _systemPrompt;
            set { _systemPrompt = value; RaisePropertyChanged(); }
        }
    }
}