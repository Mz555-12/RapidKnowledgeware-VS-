using GalaSoft.MvvmLight;
using System;
using System.Collections.ObjectModel;

namespace RapidKnowledgeware.Models
{
    public class ChatSessionModel : ObservableObject
    {
        private string _sessionId;
        public string SessionId
        {
            get => _sessionId;
            set { _sessionId = value; RaisePropertyChanged(); }
        }

        private string _displayName;
        public string DisplayName
        {
            get => _displayName;
            set { _displayName = value; RaisePropertyChanged(); }
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; RaisePropertyChanged(); }
        }

        private SpaceAdjustModel _spaceParameters;
        public SpaceAdjustModel SpaceParameters
        {
            get => _spaceParameters;
            set { _spaceParameters = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<ChatMessageModel> _messages;
        public ObservableCollection<ChatMessageModel> Messages
        {
            get => _messages;
            set { _messages = value; RaisePropertyChanged(); }
        }

        public ChatSessionModel()
        {
            SessionId = Guid.NewGuid().ToString();
            DisplayName = "新对话";
            Messages = new ObservableCollection<ChatMessageModel>();
            SpaceParameters = new SpaceAdjustModel();
        }
    }
}