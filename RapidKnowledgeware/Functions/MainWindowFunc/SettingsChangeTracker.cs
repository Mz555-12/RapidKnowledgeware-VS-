using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RapidKnowledgeware.Functions.MainWindowFunc
{
    /// <summary>
    /// 设置变更追踪器：用于捕获对象快照并生成变更描述
    /// </summary>
    public static class SettingsChangeTracker
    {
        private static readonly Dictionary<object, string> _snapshots = new Dictionary<object, string>();

        /// <summary>
        /// 捕获对象的 JSON 快照（用于后续对比）
        /// </summary>
        /// <param name="target">要追踪的对象</param>
        public static void CaptureSnapshot(object target)
        {
            if (target == null) return;
            string json = JsonConvert.SerializeObject(target);
            _snapshots[target] = json;
        }

        /// <summary>
        /// 对比当前对象与快照，返回变更描述字符串，并清除快照
        /// </summary>
        /// <param name="target">当前对象</param>
        /// <returns>变更描述，若无变化则返回 null</returns>
        public static string GetChangesAndClear(object target)
        {
            if (target == null) return null;
            if (!_snapshots.TryGetValue(target, out string oldJson))
                return null;

            _snapshots.Remove(target);
            string newJson = JsonConvert.SerializeObject(target);

            if (oldJson == newJson)
                return null;

            var oldDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(oldJson);
            var newDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(newJson);

            var changes = new List<string>();
            foreach (var kv in newDict)
            {
                if (!oldDict.TryGetValue(kv.Key, out object oldValue))
                {
                    changes.Add($"{kv.Key}: [新增] → {FormatValue(kv.Value)}");
                }
                else if (!Equals(oldValue, kv.Value))
                {
                    changes.Add($"{kv.Key}: {FormatValue(oldValue)} → {FormatValue(kv.Value)}");
                }
            }
            foreach (var kv in oldDict)
            {
                if (!newDict.ContainsKey(kv.Key))
                {
                    changes.Add($"{kv.Key}: {FormatValue(kv.Value)} → [删除]");
                }
            }

            return changes.Count > 0 ? string.Join("; ", changes) : null;
        }

        private static string FormatValue(object value)
        {
            if (value == null) return "null";
            if (value is string s && s.Length > 50)
                return s.Substring(0, 47) + "...";
            return value.ToString();
        }
    }
}