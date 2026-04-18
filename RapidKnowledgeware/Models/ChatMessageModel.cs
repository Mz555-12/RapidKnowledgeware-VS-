using GalaSoft.MvvmLight;

namespace RapidKnowledgeware.Models
{
    public class ChatMessageModel : ObservableObject
    {
        private bool _isUserMessage;
        public bool IsUserMessage
        {
            get => _isUserMessage;
            set { _isUserMessage = value; RaisePropertyChanged(); }
        }

        // 对于AI消息，Content为思考内容，Content2为正式回答
        private string _content;
        public string Content
        {
            get => _content;
            set { _content = value; RaisePropertyChanged(); }
        }

        private string _content2;
        public string Content2
        {
            get => _content2;
            set { _content2 = value; RaisePropertyChanged(); }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; RaisePropertyChanged(); }
        }

        // 辅助属性
        public bool HasThinkContent => !string.IsNullOrEmpty(Content);
        public bool HasMainContent => !string.IsNullOrEmpty(Content2);
    }
}