using RapidKnowledgeware.Base;
using RapidKnowledgeware.Models;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace RapidKnowledgeware.ViewModels
{
    public interface IMainWindowModel
    {
        MainModel MainModel { get; set; }
        LLMAdjustModel LLMAdjustModel { get; }
        bool HasMessages { get; }
        string GreetingText { get; }
        string GreetingDisplayText { get; set; }
        bool IsSending { get; set; }
        CommandBase CloseMainWindowCommand { get; }
        CommandBase OpenSpaceParametersViewCommand { get; }
        CommandBase LogCommand { get; }
        CommandBase OpenDebugWindowCommand { get; }
        ObservableCollection<ChatSessionModel> Sessions { get; set; }
        ChatSessionModel SelectedSession { get; set; }
        ObservableCollection<ChatMessageModel> CurrentMessages { get; }
        string InputText { get; set; }
        bool IsSpaceAdjustVisible { get; set; }
        ISpaceAdjustViewModel SpaceAdjustVM { get; set; }
        CommandBase NewSessionCommand { get; }
        CommandBase SendMessageCommand { get; }
        CommandBase EditSessionCommand { get; }
        CommandBase CloseSpaceAdjustCommand { get; }
        CommandBase DeleteSessionCommand { get; }
        CommandBase RenameSessionCommand { get; }
        CommandBase StopMessageCommand { get; }
        CommandBase DeepThinkingCommand { get; }
        CommandBase CopyCodeCommand { get; }
        void SetSpaceAdjustViewReferences(System.Windows.Controls.Grid overlay, Views.SpaceAdjustView view);
        void OnWindowClosing();
        void RefreshUIAssistProperties();
        void SaveSessions();
        void StopGreetingAnimation();
        void ResetAndStartGreetingAnimation();
    }
}
