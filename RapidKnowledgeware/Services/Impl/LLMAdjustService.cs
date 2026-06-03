using IOC;
using IOC.Annotations;
using Newtonsoft.Json;
using RapidKnowledgeware.Models;
using RapidKnowledgeware.DAO;
using RapidKnowledgeware.Services;
using System;
using System.Collections.Generic;

namespace RapidKnowledgeware.Services.Impl
{
    /// <summary>
    /// LLM 全局默认参数服务类
    /// </summary>
    [Service]
    public class LLMAdjustService : ILLMAdjustService
    {
        private readonly IAppSettingsRepository _appSettingsManager;
        private readonly ILoggingService _loggingService;
        private LLMAdjustModel _current;
        private string _lastSavedJson;

        public LLMAdjustService(IAppSettingsRepository appSettingsManager, ILoggingService loggingService)
        {
            _appSettingsManager = appSettingsManager;
            _loggingService = loggingService;
        }
        /// <summary>
        /// 当前全局 LLM 参数配置（单例）
        /// </summary>
        public LLMAdjustModel Current
        {
            get
            {
                if (_current == null)
                {
                    _current = _appSettingsManager.LoadSettings<LLMAdjustModel>() ?? new LLMAdjustModel();

                    _lastSavedJson = JsonConvert.SerializeObject(_current);
                }
                return _current;
            }
        }

        /// <summary>
        /// 保存当前 LLM 配置到文件
        /// </summary>
        public void Save()
        {
            string newJson = JsonConvert.SerializeObject(Current);
            if (_lastSavedJson != newJson)
            {
                string changes = GenerateChanges(_lastSavedJson, newJson);
                if (!string.IsNullOrEmpty(changes))
                {
                    string formattedChanges = changes.Replace("; ", "\n");
                    _loggingService.WriteOperationLog(new OperationsLog
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
            _appSettingsManager.SaveSettings(Current);
        }

        private string GenerateChanges(string oldJson, string newJson)
        {
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
        public SpaceAdjustModel CreateSpaceParametersFromDefault()
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
                IsDeepThinking = false,
                SearchQuantity = KnowledgeBaseModel.Instance.Default_SearchQuantity,
                IndexSimilarityThreshold = KnowledgeBaseModel.Instance.Default_IndexSimilarityThreshold
            };
        }
    }
}
