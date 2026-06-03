using System;

namespace RapidKnowledgeware.Services
{
    public interface IDebugService
    {
        void Info(string message);
        void Error(string message, Exception ex = null);
    }
}
