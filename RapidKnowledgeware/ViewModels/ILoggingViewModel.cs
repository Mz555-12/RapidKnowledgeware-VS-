using RapidKnowledgeware.Base;
using RapidKnowledgeware.Services.Impl;
using System.Collections.ObjectModel;

namespace RapidKnowledgeware.ViewModels
{
    public interface ILoggingViewModel
    {
        bool IsLogViewVisible { get; set; }
        string LogType { get; set; }
        string SelectedDate { get; set; }
        ObservableCollection<string> AvailableDates { get; }
        ObservableCollection<LogEntryViewModel> LogEntries { get; }
        string SearchKeyword { get; set; }
        CommandBase SwitchLogTypeCommand { get; }
        CommandBase SearchCommand { get; }
        CommandBase RefreshCommand { get; }
        CommandBase ExportCommand { get; }
        void Show(System.Windows.Controls.Grid overlayContainer, Views.LoggingView view);
        void Hide(System.Windows.Controls.Grid overlayContainer, Views.LoggingView view);
    }
}
