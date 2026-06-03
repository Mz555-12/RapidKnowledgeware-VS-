using RapidKnowledgeware.Models;
using System.Threading.Tasks;

namespace RapidKnowledgeware.Services
{
    public interface IKnowledgeBaseUIService
    {
        bool HasMultipleChunks { get; }
        void SetKnowledgeBaseView(Views.KnowledgeBaseView view);
        Task AddKnowledgeAsync();
        Task ViewFileBlocksAsync(KnowledgeFileItem item);
        Task DeleteChunkAsync(FileChunkItem chunkItem);
        void ShowFileBlockOverlay();
        void CloseFileBlockOverlay();
    }
}
