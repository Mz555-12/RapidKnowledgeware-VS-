using Newtonsoft.Json;
using RapidKnowledgeware.Models;
using System;
using System.Diagnostics;
using System.IO;

namespace RapidKnowledgeware.Functions.MainWindowFunc
{
    public static class AppSettingsManager
    {
        private static readonly string SettingsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Json");

        private static void EnsureFolderExists()
        {
            if (!Directory.Exists(SettingsFolder))
                Directory.CreateDirectory(SettingsFolder);
        }

        private static string GetFilePath<T>()
        {
            string fileName = $"{typeof(T).Name.ToLower()}_settings.json";
            return Path.Combine(SettingsFolder, fileName);
        }

        /// <summary>
        /// 保存任意模型配置到 JSON 文件
        /// </summary>
        public static void SaveSettings<T>(T model) where T : class
        {
            if (model == null) return;
            try
            {
                EnsureFolderExists();
                string filePath = GetFilePath<T>();
                string json = JsonConvert.SerializeObject(model, Formatting.Indented);
                File.WriteAllText(filePath, json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AppSettingsManager] 保存 {typeof(T).Name} 失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 从 JSON 文件加载模型配置，若文件不存在则返回 null
        /// </summary>
        public static T LoadSettings<T>() where T : class
        {
            string filePath = GetFilePath<T>();
            if (!File.Exists(filePath))
                return null;

            try
            {
                string json = File.ReadAllText(filePath);
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[AppSettingsManager] 加载 {typeof(T).Name} 失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 加载配置到已有模型实例中（填充属性）
        /// </summary>
        public static void LoadSettings<T>(T target) where T : class
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            var loaded = LoadSettings<T>();
            if (loaded != null)
            {
                string json = JsonConvert.SerializeObject(loaded);
                JsonConvert.PopulateObject(json, target);
            }
        }

        // ---------- 便捷重载，保持原有调用不变 ----------
        public static void SaveSettings(KnowledgeBaseModel model)
        {
            SaveSettings<KnowledgeBaseModel>(model);
        }

        public static void LoadSettings(KnowledgeBaseModel model)
        {
            LoadSettings<KnowledgeBaseModel>(model);
        }
    }
}