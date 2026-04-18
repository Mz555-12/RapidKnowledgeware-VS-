using GalaSoft.MvvmLight;
using RapidKnowledgeware.Base;
using RapidKnowledgeware.Functions.SpaceAdjustFunc;
using RapidKnowledgeware.Models;
using System.Windows.Input;

namespace RapidKnowledgeware.ViewModels
{
    public class SpaceAdjustViewModel : ObservableObject
    {
        public SpaceAdjustModel SpaceAdjustModel { get; }

        public ICommand ResetToDefaultCommand { get; }

        private ICommand _closeCommand;
        public ICommand CloseCommand
        {
            get => _closeCommand;
            set { _closeCommand = value; RaisePropertyChanged(); }
        }

        public ChatSessionModel Session { get; }

        public SpaceAdjustViewModel(ChatSessionModel session, ICommand closeCommand)
        {
            Session = session;
            SpaceAdjustModel = session.SpaceParameters;
            CloseCommand = closeCommand;
            ResetToDefaultCommand = new CommandBase { DoExecute = _ => SpaceAdjustService.ResetToDefault(SpaceAdjustModel) };
        }
    }
}