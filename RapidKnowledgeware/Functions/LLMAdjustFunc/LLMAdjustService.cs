using Newtonsoft.Json;
using RapidKnowledgeware.Functions.MainWindowFunc;
using RapidKnowledgeware.Models;
using System;
using System.Collections.Generic;

namespace RapidKnowledgeware.Functions.LLMAdjustFunc
{
    /// <summary>
    /// LLM 全局默认参数服务类
    /// </summary>
    public static class LLMAdjustService
    {
        private static LLMAdjustModel _current;
        private static string _lastSavedJson;   // 用于对比
        /// <summary>
        /// 当前全局 LLM 参数配置（单例）
        /// </summary>
        public static LLMAdjustModel Current
        {
            get
            {
                if (_current == null)
                {
                    _current = AppSettingsManager.LoadSettings<LLMAdjustModel>() ?? new LLMAdjustModel();

                    _lastSavedJson = JsonConvert.SerializeObject(_current);   // 初始化快照
                }
                return _current;
            }
        }

        /// <summary>
        /// 保存当前 LLM 配置到文件
        /// </summary>
        public static void Save()
        {
            string newJson = JsonConvert.SerializeObject(Current);
            if (_lastSavedJson != newJson)
            {
                string changes = GenerateChanges(_lastSavedJson, newJson);
                if (!string.IsNullOrEmpty(changes))
                {
                    string formattedChanges = changes.Replace("; ", "\n");
                    LoggingFunc.LoggingService.WriteOperationLog(new OperationsLog
                    {
                        Timestamp = DateTime.Now,
                        Type = OperationType.EditLLMParams,
                        ActionName = "编辑全局LLM参数",
                        Target = "全局默认设置",
                        Success = true,
                        Details = formattedChanges
                    });
                }
                _lastSavedJson = newJson;
            }
            AppSettingsManager.SaveSettings(Current);
        }

        private static string GenerateChanges(string oldJson, string newJson)
        {
            // 复用 SettingsChangeTracker 的逻辑，或直接实现简单对比
            // 为了避免循环依赖，这里直接实现简易版本
            try
            {
                var oldDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(oldJson);
                var newDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(newJson);
                var changes = new List<string>();
                foreach (var kv in newDict)
                {
                    if (!oldDict.TryGetValue(kv.Key, out object oldValue))
                        changes.Add($"{kv.Key}: [新增] → {kv.Value}");
                    else if (!Equals(oldValue, kv.Value))
                        changes.Add($"{kv.Key}: {oldValue} → {kv.Value}");
                }
                foreach (var kv in oldDict)
                {
                    if (!newDict.ContainsKey(kv.Key))
                        changes.Add($"{kv.Key}: {kv.Value} → [删除]");
                }
                return changes.Count > 0 ? string.Join("; ", changes) : null;
            }
            catch
            {
                return "参数已变更（详情无法解析）";
            }
        }

        /// <summary>
        /// 根据全局默认参数创建一个新的空间参数实例
        /// </summary>
        /// <returns>初始化为默认值的 SpaceAdjustModel 实例</returns>
        public static SpaceAdjustModel CreateSpaceParametersFromDefault()
        {
            return new SpaceAdjustModel
            {
                ChatLLM = Current.Default_ChatLLM,
                Temperature = Current.Default_Temperature,
                TopP = Current.Default_TopP,
                RepeatPenalty = Current.Default_RepeatPenalty,
                SystemPrompt = Current.Default_SystemPrompt,
                QueryRefusalResponse = Current.Default_RefusalResponse,
                DeepThinkingLLM = Current.Default_DeepThinkingLLM,
                IsDeepThinking = false
            };
        }
    }
}