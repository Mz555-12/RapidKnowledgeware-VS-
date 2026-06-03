using RapidKnowledgeware.Models;
using RapidKnowledgeware.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace RapidKnowledgeware.Services
{
    public interface IMainWindowService
    {
        ChatSessionModel CreateNewSession(ObservableCollection<ChatSessionModel> sessions);
        Task SendMessageAsync(ChatSessionModel session, string userInput, Action onCompleted);
        ChatSessionModel DeleteSession(ChatSessionModel session, ObservableCollection<ChatSessionModel> sessions);
        void RenameSession(ChatSessionModel session, ObservableCollection<ChatSessionModel> sessions);
        void StopMessage();
        void ForceStopAndFinalizeAllSessions(ObservableCollection<ChatSessionModel> sessions);
        ObservableCollection<ChatSessionModel> LoadSessions();
        void SaveSessions(ObservableCollection<ChatSessionModel> sessions);
        void SaveAllSettings(ObservableCollection<ChatSessionModel> sessions);
        void RecordAndSaveAllSettingsChanges();
        void RecordSpaceAdjustChanges(ISpaceAdjustViewModel spaceAdjustVM);
        string GetGreetingByTime();
    }
}
