using GalaSoft.MvvmLight;
using System;
using System.Collections.ObjectModel;

namespace RapidKnowledgeware.Models
{
    public class SessionModel : ObservableObject
    {
        private string _id;
        public string Id
        {
            get => _id;
            set { _id = value; RaisePropertyChanged(); }
        }

        private string _name;
        public string Name
        {
            get => _name;
            set { _name = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<MessageModel> _messages;
        public ObservableCollection<MessageModel> Messages
        {
            get => _messages;
            set { _messages = value; RaisePropertyChanged(); }
        }

        private SpaceAdjustModel _parameters;
        public SpaceAdjustModel Parameters
        {
            get => _parameters;
            set { _parameters = value; RaisePropertyChanged(); }
        }

        public SessionModel()
        {
            Id = Guid.NewGuid().ToString();
            Messages = new ObservableCollection<MessageModel>();
            Parameters = new SpaceAdjustModel();
        }
    }
}