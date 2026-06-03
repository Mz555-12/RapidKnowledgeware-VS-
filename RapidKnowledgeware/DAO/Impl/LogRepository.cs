using IOC.Annotations;
using Newtonsoft.Json;
using RapidKnowledgeware.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RapidKnowledgeware.DAO.Impl
{
    /// <summary>
    /// 日志数据访问对象：负责日志文件的读写和清理
    /// </summary>
    [Repository]
    public class LogRepository : ILogRepository
    {
        private readonly string _logRoot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Json", "Log");

        public void EnsureDirectoriesExist()
        {
            var chatDir = Path.Combine(_logRoot, "Chat");
            var opsDir = Path.Combine(_logRoot, "Operations");
            if (!Directory.Exists(chatDir))
                Directory.CreateDirectory(chatDir);
            if (!Directory.Exists(opsDir))
                Directory.CreateDirectory(opsDir);
        }

        public void AppendLog<T>(string baseDir, DateTime timestamp, T entry) where T : class
        {
            try
            {
                string month = timestamp.ToString("yyyy-MM");
                string filePath = Path.Combine(baseDir, $"{month}.json");

                List<T> logs;
                if (File.Exists(filePath))
                {
                    string existingJson = File.ReadAllText(filePath);
                    logs = JsonConvert.DeserializeObject<List<T>>(existingJson) ?? new List<T>();
                }
                else
                {
                    logs = new List<T>();
                }
                logs.Add(entry);
                string newJson = JsonConvert.SerializeObject(logs, Formatting.Indented);
                File.WriteAllText(filePath, newJson);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LogRepository] 写入日志失败: {ex.Message}");
            }
        }

        public List<string> GetAvailableMonths(string baseDir)
        {
            if (!Directory.Exists(baseDir)) return new List<string>();
            return Directory.GetFiles(baseDir, "*.json")
                .Select(f => Path.GetFileNameWithoutExtension(f))
                .OrderByDescending(m => m)
                .ToList();
        }

        public List<T> LoadLogs<T>(string baseDir, string month) where T : class
        {
            string filePath = Path.Combine(baseDir, $"{month}.json");
            if (!File.Exists(filePath)) return new List<T>();
            try
            {
                string json = File.ReadAllText(filePath);
                return JsonConvert.DeserializeObject<List<T>>(json) ?? new List<T>();
            }
            catch
            {
                return new List<T>();
            }
        }

        public void CleanOldFiles(string baseDir, int keepCount)
        {
            if (!Directory.Exists(baseDir)) return;
            var files = Directory.GetFiles(baseDir, "*.json")
                .Select(f => new { Path = f, Month = Path.GetFileNameWithoutExtension(f) })
                .Where(x => DateTime.TryParseExact(x.Month + "-01", "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out _))
                .OrderByDescending(x => x.Month)
                .ToList();

            var toDelete = files.Skip(keepCount).ToList();
            foreach (var file in toDelete)
            {
                try { File.Delete(file.Path); } catch { }
            }
        }

        /// <summary>
        /// 获取聊天日志目录路径
        /// </summary>
        public string ChatLogDir => Path.Combine(_logRoot, "Chat");

        /// <summary>
        /// 获取操作日志目录路径
        /// </summary>
        public string OpsLogDir => Path.Combine(_logRoot, "Operations");
    }
}
