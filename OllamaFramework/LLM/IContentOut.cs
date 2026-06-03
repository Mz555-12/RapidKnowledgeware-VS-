using OllamaFramework.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace OllamaFramework.LLM
{
    public interface IContentOut
    {
        LLMParameters DefaultParameters { get; set; }

        Task<string> GenerateAsync(string prompt, LLMParameters parameters = null, CancellationToken cancellationToken = default);
        Task<string> GenerateStreamingAsync(string prompt, Action<string> onChunkReceived, LLMParameters parameters = null, CancellationToken cancellationToken = default);
        CancellationTokenSource CreateCancellationTokenSource();
    }
}
