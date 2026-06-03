using RapidKnowledgeware.Models;
using System;
using System.Collections.Generic;

namespace RapidKnowledgeware.DAO
{
    public interface ILogRepository
    {
        string ChatLogDir { get; }
        string OpsLogDir { get; }
        void EnsureDirectoriesExist();
        void AppendLog<T>(string baseDir, DateTime timestamp, T entry) where T : class;
        List<string> GetAvailableMonths(string baseDir);
        List<T> LoadLogs<T>(string baseDir, string month) where T : class;
        void CleanOldFiles(string baseDir, int keepCount);
    }
}
