using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace RapidKnowledgeware.Services
{
    public interface IOllamaModelService
    {
        bool IsOllamaAvailable { get; }
        ObservableCollection<string> ChatModels { get; }
        ObservableCollection<string> EmbeddingModels { get; }
        event Action ModelsLoaded;
        event Action LoadFailed;
        Task LoadModelsAsync();
    }
}
