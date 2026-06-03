using RapidKnowledgeware.Services.Impl;
using System.Collections.Generic;

namespace RapidKnowledgeware.Services
{
    public interface ILoggingDisplayService
    {
        List<string> GetAvailableDates(string logType);
        List<LogEntryViewModel> LoadLogs(string logType, string selectedDate, string searchKeyword);
        string ExportLogs(string logType, string selectedDate);
    }
}
