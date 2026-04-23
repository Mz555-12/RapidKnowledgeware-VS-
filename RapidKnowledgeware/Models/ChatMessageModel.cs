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

        private string _thingking_content;
        /// <summary>
        /// AI消息的思考内容（think标签内容），用户消息时存储用户输入
        /// </summary>
        public string ThinkingContent
        {
            get => _thingking_content;
            set
            {
                _thingking_content = value;
                RaisePropertyChanged();
                RaisePropertyChanged(nameof(HasThinkContent));
            }
        }
        // 在 ChatMessageModel 类中添加以下属性
        private string _userContent;
        /// <summary>
        /// 用户消息的文本内容（仅当 IsUserMessage=true 时使用）
        /// </summary>
        public string UserContent
        {
            get => _userContent;
            set { _userContent = value; RaisePropertyChanged(); }
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
        public bool HasThinkContent => !string.IsNullOrEmpty(ThinkingContent);

        /// <summary>
        /// 是否有正式回答内容（用于控制回答区域显示）
        /// </summary>
        public bool HasMainContent => !string.IsNullOrEmpty(Content2);
    }
}