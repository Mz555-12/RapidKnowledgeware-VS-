using IOC.Annotations;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using OllamaSharp;

namespace RapidKnowledgeware.Services.Impl
{
    /// <summary>
    /// 管理本地 Ollama 模型列表的静态服务
    /// </summary>
    [Service]
    public class OllamaModelService : IOllamaModelService
    {
        private readonly ILLMAdjustService _llmAdjustService;
        private readonly object _lock = new object();
        private bool _isLoaded;

        public OllamaModelService(ILLMAdjustService llmAdjustService)
        {
            _llmAdjustService = llmAdjustService;
        }

        /// <summary>
        /// Ollama 服务是否可用
        /// </summary>
        public bool IsOllamaAvailable { get; private set; } = false;

        /// <summary>
        /// 大模型列表
        /// </summary>
        public ObservableCollection<string> ChatModels { get; } = new ObservableCollection<string>();

        /// <summary>
        /// 嵌入模型列表
        /// </summary>
        public ObservableCollection<string> EmbeddingModels { get; } = new ObservableCollection<string>();

        /// <summary>
        /// 模型加载成功事件
        /// </summary>
        public event Action ModelsLoaded;

        /// <summary>
        /// 模型加载失败事件
        /// </summary>
        public event Action LoadFailed;

        /// <summary>
        /// 异步加载模型列表（仅加载一次）
        /// </summary>
        public async Task LoadModelsAsync()
        {
            if (_isLoaded) return;
            lock (_lock)
            {
                if (_isLoaded) return;
                _isLoaded = true;
            }

            string baseUrl = _llmAdjustService.Current.Default_BaseURL;
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
