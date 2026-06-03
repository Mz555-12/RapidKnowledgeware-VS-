using RapidKnowledgeware.Base;
using RapidKnowledgeware.Models;
using System.Collections.ObjectModel;

namespace RapidKnowledgeware.ViewModels
{
    public interface IKnowledgeBaseViewModel
    {
        KnowledgeBaseModel KnowledgeBaseModel { get; set; }
        ObservableCollection<string> EmbeddingModels { get; }
        string EmbeddingModelHint { get; set; }
        bool HasMultipleChunks { get; set; }
        int TotalBlockCount { get; }
        int TotalWordCount { get; }
        bool IsSearchViewVisible { get; set; }
        string SwitchViewButtonText { get; }
        ObservableCollection<KnowledgeFileItem> DisplayFileItems { get; }
        CommandBase AddKnowledgeCommand { get; }
        CommandBase ViewFileBlocksCommand { get; }
        CommandBase ReindexFileCommand { get; }
        CommandBase DeleteFileCommand2 { get; }
        CommandBase CloseFileBlockViewCommand { get; }
        CommandBase DeleteChunkCommand { get; }
        CommandBase SwitchViewCommand { get; }
        string SearchKeyword { get; set; }
        int CurrentPage { get; }
        int TotalPages { get; }
        bool HasPreviousPage { get; }
        bool HasNextPage { get; }
        CommandBase SearchCommand { get; }
        CommandBase PreviousPageCommand { get; }
        CommandBase NextPageCommand { get; }
        void SetKnowledgeBaseView(Views.KnowledgeBaseView view);
        void SetSlidingViewContainers(System.Windows.Controls.Grid funcContainer, System.Windows.Controls.Grid searchContainer, System.Windows.FrameworkElement funcView, System.Windows.FrameworkElement searchView);
    }
}
