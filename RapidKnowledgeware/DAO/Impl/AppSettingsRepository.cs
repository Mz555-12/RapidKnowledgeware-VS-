using IOC.Annotations;
using Newtonsoft.Json;
using RapidKnowledgeware.Models;
using System;
using System.Diagnostics;
using System.IO;

namespace RapidKnowledgeware.DAO.Impl
{
    /// <summary>
    /// 应用程序设置管理器，负责将模型配置持久化到 JSON 文件
    /// </summary>
    [Repository]
    public class AppSettingsRepository : IAppSettingsRepository
    {
        /// <summary>
        /// 配置文件存储文件夹路径（位于应用程序目录下的 Json 文件夹）
        /// </summary>
        private readonly string SettingsFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Json");

        /// <summary>
        /// 确保配置文件存储文件夹存在，若不存在则创建
        /// </summary>
        private void EnsureFolderExists()
        {
            if (!Directory.Exists(SettingsFolder))
                Directory.CreateDirectory(SettingsFolder);
        }

        /// <summary>
        /// 获取指定类型的配置文件完整路径
        /// </summary>
        /// <typeparam name="T">模型类型</typeparam>
        /// <returns>配置文件完整路径</returns>
        private string GetFilePath<T>()
        {
            string fileName = $"{typeof(T).Name.ToLower()}_settings.json";
            return Path.Combine(SettingsFolder, fileName);
        }

        /// <summary>
        /// 保存任意模型配置到 JSON 文件
        /// </summary>
        /// <typeparam name="T">模型类型</typeparam>
        /// <param name="model">要保存的模型实例</param>
        public void SaveSettings<T>(T model) where T : class
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
                Debug.WriteLine($"[AppSettingsRepository] 保存 {typeof(T).Name} 失败: {ex.Message}");
            }

        }

        /// <summary>
        /// 从 JSON 文件加载模型配置，若文件不存在则返回 null
        /// </summary>
        /// <typeparam name="T">模型类型</typeparam>
        /// <returns>加载的模型实例，若文件不存在或加载失败则返回 null</returns>
        public T LoadSettings<T>() where T : class
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
                Debug.WriteLine($"[AppSettingsRepository] 加载 {typeof(T).Name} 失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 加载配置到已有模型实例中（填充属性）
        /// </summary>
        /// <typeparam name="T">模型类型</typeparam>
        /// <param name="target">目标模型实例</param>
        /// <exception cref="ArgumentNullException">当 target 为 null 时抛出</exception>
        public void LoadSettings<T>(T target) where T : class
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

        /// <summary>
        /// 保存知识库模型配置（便捷重载）
        /// </summary>
        /// <param name="model">知识库模型实例</param>
        public void SaveSettings(KnowledgeBaseModel model)
        {
            SaveSettings<KnowledgeBaseModel>(model);
        }

        /// <summary>
        /// 加载知识库模型配置到已有实例（便捷重载）
        /// </summary>
        /// <param name="model">目标知识库模型实例</param>
        public void LoadSettings(KnowledgeBaseModel model)
        {
            LoadSettings<KnowledgeBaseModel>(model);
        }
    }
}
