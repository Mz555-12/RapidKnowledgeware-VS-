using RapidKnowledgeware.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RapidKnowledgeware.Services
{
    public interface ILoggingService
    {
        Task WriteChatLogAsync(ChatLog log);
        Task WriteOperationLogAsync(OperationsLog log);
        void WriteOperationLog(OperationsLog log);
        List<string> GetAvailableMonths(string logType);
        List<ChatLog> LoadChatLogs(string month);
        List<OperationsLog> LoadOperationLogs(string month);
        void CleanOldLogs();
        string ExportChatLogsToTxt(string month);
        string ExportOperationLogsToTxt(string month);
    }
}
