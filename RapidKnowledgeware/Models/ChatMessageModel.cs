using GalaSoft.MvvmLight;

namespace RapidKnowledgeware.Models
{
    /// <summary>
    /// 聊天消息数据模型
    /// </summary>
    public class ChatMessageModel : ObservableObject
    {
        private bool _isUserMessage;
        /// <summary>
        /// 是否为用户消息（true：用户消息，显示在右侧；false：AI消息，显示在左侧）
        /// </summary>
        public bool IsUserMessage
        {
            get => _isUserMessage;
            set { _isUserMessage = value; RaisePropertyChanged(); }
        }

        private string _content;
        /// <summary>
        /// AI消息的思考内容（think标签内容），用户消息时存储用户输入
        /// </summary>
        public string Content
        {
            get => _content;
            set
            {
                _content = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(HasThinkContent));
            }
        }

        private string _content2;
        /// <summary>
        /// AI消息的正式回答内容
        /// </summary>
        public string Content2
        {
            get => _content2;
            set
            {
                _content2 = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(HasMainContent));
            }
        }

        private bool _isLoading;
        /// <summary>
        /// 是否正在加载（显示思考动画）
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 是否有思考内容（用于控制思考区域显示）
        /// </summary>
        public bool HasThinkContent => !string.IsNullOrEmpty(Content);

        /// <summary>
        /// 是否有正式回答内容（用于控制回答区域显示）
        /// </summary>
        public bool HasMainContent => !string.IsNullOrEmpty(Content2);
    }
}