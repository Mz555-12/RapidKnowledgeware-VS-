// RapidKnowledgeware.Functions.MainWindowFunc.SettingsChangeTracker.cs
using IOC.Annotations;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RapidKnowledgeware.Services.Impl
{
    /// <summary>
    /// 设置变更追踪器：用于捕获对象快照并生成变更描述
    /// </summary>
    [Service]
    public class SettingsChangeTracker : ISettingsChangeTracker
    {
        private readonly Dictionary<object, string> _snapshots = new Dictionary<object, string>();

        // 需要排除的 KnowledgeBaseModel 属性名称（这些属性由文件操作管理，不应参与配置编辑日志）
        private readonly HashSet<string> _excludedPropertiesForKbModel = new HashSet<string>
        {
            "FileItems",
            "FileBlocks",
            "CurrentFileName",
            "CurrentFileBlockRule",
            "FileBlockContent"
        };

        /// <summary>
        /// 捕获对象的 JSON 快照（用于后续对比），KnowledgeBaseModel 会排除集合和临时状态字段
        /// </summary>
        /// <param name="target">要追踪的对象</param>
        public void CaptureSnapshot(object target)
        {
            if (target == null) return;

            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                ContractResolver = new PropertyExcludingContractResolver(GetExcludedPropertiesForType(target))
            };

            string json = JsonConvert.SerializeObject(target, Formatting.None, settings);
            _snapshots[target] = json;
        }

        /// <summary>
        /// 根据对象类型返回需要排除的属性名称集合
        /// </summary>
        private HashSet<string> GetExcludedPropertiesForType(object target)
        {
            if (target is RapidKnowledgeware.Models.KnowledgeBaseModel)
                return _excludedPropertiesForKbModel;
            return new HashSet<string>(); // 其他类型默认不排除任何属性
        }

        /// <summary>
        /// 对比当前对象与快照，返回变更描述字符串，并清除快照
        /// </summary>
        /// <param name="target">当前对象</param>
        /// <returns>变更描述，若无变化则返回 null</returns>
        public string GetChangesAndClear(object target)
        {
            if (target == null) return null;
            if (!_snapshots.TryGetValue(target, out string oldJson))
                return null;

            _snapshots.Remove(target);

            JsonSerializerSettings settings = new JsonSerializerSettings
            {
                ContractResolver = new PropertyExcludingContractResolver(GetExcludedPropertiesForType(target))
            };
            string newJson = JsonConvert.SerializeObject(target, Formatting.None, settings);

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

        private string FormatValue(object value)
        {
            if (value == null) return "null";
            if (value is string s && s.Length > 50)
                return s.Substring(0, 47) + "...";
            return value.ToString();
        }

        /// <summary>
        /// 自定义契约解析器，用于排除指定属性
        /// </summary>
        private class PropertyExcludingContractResolver : Newtonsoft.Json.Serialization.DefaultContractResolver
        {
            private readonly HashSet<string> _excludedProps;

            public PropertyExcludingContractResolver(HashSet<string> excludedProps)
            {
                _excludedProps = excludedProps ?? new HashSet<string>();
            }

            protected override IList<Newtonsoft.Json.Serialization.JsonProperty> CreateProperties(Type type, Newtonsoft.Json.MemberSerialization memberSerialization)
            {
                var properties = base.CreateProperties(type, memberSerialization);
                return properties.Where(p => !_excludedProps.Contains(p.PropertyName)).ToList();
            }
        }
    }
}
