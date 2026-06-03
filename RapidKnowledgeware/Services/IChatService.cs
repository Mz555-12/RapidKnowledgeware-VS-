using RapidKnowledgeware.Models;
using System;
using System.Threading.Tasks;

namespace RapidKnowledgeware.Services
{
    public interface IChatService
    {
        void Initialize(ChatSessionModel session);
        Task SendMessageAsync(string userInput, Action<string> onTokenReceived);
        void StopGeneration();
    }
}
