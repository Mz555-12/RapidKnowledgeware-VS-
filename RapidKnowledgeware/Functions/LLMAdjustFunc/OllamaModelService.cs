using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using OllamaSharp;

namespace RapidKnowledgeware.Functions.LLMAdjustFunc
{
    /// <summary>
    /// 管理本地 Ollama 模型列表的静态服务
    /// </summary>
    public static class OllamaModelService
    {
        private static readonly object _lock = new object();
        private static bool _isLoaded;

        /// <summary>
        /// Ollama 服务是否可用
        /// </summary>
        public static bool IsOllamaAvailable { get; private set; } = false;

        /// <summary>
        /// 大模型列表
        /// </summary>
        public static ObservableCollection<string> ChatModels { get; } = new ObservableCollection<string>();

        /// <summary>
        /// 嵌入模型列表
        /// </summary>
        public static ObservableCollection<string> EmbeddingModels { get; } = new ObservableCollection<string>();

        /// <summary>
        /// 模型加载成功事件
        /// </summary>
        public static event Action ModelsLoaded;

        /// <summary>
        /// 模型加载失败事件
        /// </summary>
        public static event Action LoadFailed;

        /// <summary>
        /// 异步加载模型列表（仅加载一次）
        /// </summary>
        public static async Task LoadModelsAsync()
        {
            if (_isLoaded) return;
            lock (_lock)
            {
                if (_isLoaded) return;
                _isLoaded = true;
            }

            string baseUrl = LLMAdjustService.Current.Default_BaseURL;
            var client = new OllamaApiClient(baseUrl);

            try
            {
                var models = await client.ListLocalModelsAsync();
                var names = models.Select(m => m.Name).ToList();

                var chatModels = names.Where(n =>
                    n.IndexOf("embed", StringComparison.OrdinalIgnoreCase) < 0 &&
                    n.IndexOf("bge", StringComparison.OrdinalIgnoreCase) < 0
                ).OrderBy(n => n);

                var embedModels = names.Where(n =>
                    n.IndexOf("embed", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    n.IndexOf("bge", StringComparison.OrdinalIgnoreCase) >= 0
                ).OrderBy(n => n);

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    ChatModels.Clear();
                    foreach (var m in chatModels) ChatModels.Add(m);

                    EmbeddingModels.Clear();
                    foreach (var m in embedModels) EmbeddingModels.Add(m);
                });

                IsOllamaAvailable = true;
                ModelsLoaded?.Invoke();
            }
            catch
            {
                IsOllamaAvailable = false;
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    ChatModels.Clear();
                    EmbeddingModels.Clear();
                });
                LoadFailed?.Invoke();
            }
        }
    }
}