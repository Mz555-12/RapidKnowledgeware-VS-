using GalaSoft.MvvmLight;
using System;
using System.Collections.ObjectModel;

namespace RapidKnowledgeware.Models
{
    /// <summary>
    /// 聊天会话数据模型
    /// </summary>
    public class ChatSessionModel : ObservableObject
    {
        private string _sessionId;
        /// <summary>
        /// 会话唯一标识符
        /// </summary>
        public string SessionId
        {
            get => _sessionId;
            set { _sessionId = value; RaisePropertyChanged(); }
        }

        private string _displayName;
        /// <summary>
        /// 会话显示名称
        /// </summary>
        public string DisplayName
        {
            get => _displayName;
            set { _displayName = value; RaisePropertyChanged(); }
        }

        private bool _isSelected;
        /// <summary>
        /// 是否当前选中的会话
        /// </summary>
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; RaisePropertyChanged(); }
        }

        private SpaceAdjustModel _spaceParameters;
        /// <summary>
        /// 该会话专属的空间参数（LLM 模型、温度等）
        /// </summary>
        public SpaceAdjustModel SpaceParameters
        {
            get => _spaceParameters;
            set { _spaceParameters = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<ChatMessageModel> _messages;
        /// <summary>
        /// 该会话的消息列表
        /// </summary>
        public ObservableCollection<ChatMessageModel> Messages
        {
            get => _messages;
            set { _messages = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 初始化聊天会话，生成唯一ID和默认名称
        /// </summary>
        public ChatSessionModel()
        {
            SessionId = Guid.NewGuid().ToString();
            DisplayName = "新对话";
            Messages = new ObservableCollection<ChatMessageModel>();
            SpaceParameters = new SpaceAdjustModel();
        }
    }
}