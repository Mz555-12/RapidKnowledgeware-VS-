using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OllamaFramework.Embedding
{
    public interface IAnalysesFile
    {
        string EmbeddingModelName { get; }

        Task<string> LoadFileAsync(string filePath);
        List<string> SplitIntoChunks(string content, string[] separators, bool trimChunks = true);
        void LogChunks(IEnumerable<string> chunks);
        Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
        Task<List<float[]>> GenerateEmbeddingsAsync(IEnumerable<string> chunks, CancellationToken cancellationToken = default);
        Task<List<float[]>> ProcessFileAsync(string filePath, string[] chunkSeparator);
    }
}
