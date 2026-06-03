using RapidKnowledgeware.Base;
using RapidKnowledgeware.Models;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace RapidKnowledgeware.ViewModels
{
    public interface ISpaceAdjustViewModel
    {
        SpaceAdjustModel SpaceAdjustModel { get; }
        ObservableCollection<string> ChatModels { get; }
        string ChatModelHint { get; set; }
        ICommand ResetToDefaultCommand { get; }
        ICommand CloseCommand { get; set; }
        ChatSessionModel Session { get; }
        CommandBase Is_LinkKnowledgeBaseCommand { get; }
        CommandBase SaveAsDefaultCommand { get; }
        void Initialize(ChatSessionModel session, ICommand closeCommand);
    }
}
