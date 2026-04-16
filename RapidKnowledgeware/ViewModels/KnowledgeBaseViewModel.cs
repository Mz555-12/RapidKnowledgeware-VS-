// KnowledgeBaseViewModel.cs
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using RapidKnowledgeware.Base;
using RapidKnowledgeware.Functions.MainWindowFunc;
using RapidKnowledgeware.Models;
using RapidKnowledgeware.Views;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RapidKnowledgeware.ViewModels
{
    public class KnowledgeBaseViewModel : ViewModelBase
    {
        public KnowledgeBaseModel KnowledgeBaseModel { get; set; } = KnowledgeBaseModel.Instance;

        private static KnowledgeBaseViewModel _instance;
        public static KnowledgeBaseViewModel Instance => _instance ?? (_instance = new KnowledgeBaseViewModel());

        private KnowledgeBaseViewModel()
        {
            // 初始化命令
            AddKnowledgeCommand = new RelayCommand(AddKnowledgeExecute, () => !string.IsNullOrWhiteSpace(KnowledgeBaseModel.BlockRule));
            ViewFileBlocksCommand = new RelayCommand<KnowledgeFileItem>(ViewFileBlocksExecute);
            DeleteFileCommand = new RelayCommand<KnowledgeFileItem>(DeleteFileExecute);
            CloseFileBlockViewCommand = new RelayCommand(CloseFileBlockViewExecute);
        }

        public ICommand AddKnowledgeCommand { get; }
        public ICommand ViewFileBlocksCommand { get; }
        public ICommand DeleteFileCommand { get; }
        public ICommand CloseFileBlockViewCommand { get; }

        private string[] ParseSeparators()
        {
            var separators = KnowledgeBaseModel.BlockRule
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToArray();

            return separators.Length > 0 ? separators : new[] { "###" };
        }


        private async void AddKnowledgeExecute()
        {
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "文本文件|*.txt|所有文件|*.*"
            };
            if (dialog.ShowDialog() == true)
            {
                string filePath = dialog.FileName;
                MainWindow.SetStatusMessage($"正在索引文件: {System.IO.Path.GetFileName(filePath)}...");
                try
                {
                    var ragService = new RagService(
                        ollamaEndpoint: "http://localhost:11434",
                        embeddingModel: KnowledgeBaseModel.CurrentEmbeddingName
                    );

                    // 解析分隔符：支持逗号分隔，自动去除空白
                    var separators = KnowledgeBaseModel.BlockRule
                        .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(s => s.Trim())
                        .Where(s => !string.IsNullOrEmpty(s))
                        .ToArray();

                    // 如果解析后为空，使用默认分隔符
                    if (separators.Length == 0)
                        separators = new[] { "###" };

                    int chunkCount = await ragService.IndexDocumentAsync(filePath, separators, clearExisting: false);

                    var item = new KnowledgeFileItem
                    {
                        FilePath = filePath,
                        ChunkCount = chunkCount,
                        IsIndexed = true
                    };
                    KnowledgeBaseModel.FileItems.Add(item);
                    MainWindow.SetStatusMessage($"索引完成，共 {chunkCount} 个块。");

                    AppSettingsManager.SaveSettings(KnowledgeBaseModel);
                }
                catch (Exception ex)
                {
                    MainWindow.SetStatusMessage($"索引失败: {ex.Message}");
                }
            }
        }

        private async void ViewFileBlocksExecute(KnowledgeFileItem item)
        {
            if (item == null) return;
            try
            {
                MainWindow.SetStatusMessage($"正在加载分块内容: {item.FileName}...");

                var analysis = new OllamaFramework.Embedding.AnalysesFile();
                string content = await analysis.LoadFileAsync(item.FilePath);
                var chunks = await Task.Run(() =>
                    analysis.SplitIntoChunks(content, ParseSeparators())
                );

                // 清空原有并填充新分块

                KnowledgeBaseModel.FileBlocks.Clear();
                foreach (var chunk in chunks)
                    KnowledgeBaseModel.FileBlocks.Add(chunk);

                // 显示浮层
                var knowledgeView = GetKnowledgeBaseView();
                if (knowledgeView != null)
                {
                    var overlay = knowledgeView.FindName("FileBlockOverlay") as Grid;
                    var fileBlockView = knowledgeView.FindName("FileBlockViewControl") as FileBlockView;
                    SlidingView.SlideDownToBottom(fileBlockView, overlay);
                }

                MainWindow.SetStatusMessage($"已加载 {item.FileName} 的分块，共 {chunks.Count} 个块");
            }
            catch (Exception ex)
            {
                MainWindow.SetStatusMessage($"加载分块失败: {ex.Message}");
            }
        }

        private async void DeleteFileExecute(KnowledgeFileItem item)
        {
            if (item == null) return;
            var result = MessageBox.Show($"确定删除文件 {item.FileName} 及其索引吗？", "确认删除", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.Yes)
            {
                // 从RAG索引中移除（简单起见重新构建索引，或从内部列表移除）
                // 这里假设你维护一个全局 RagService，简化起见我们直接删除并清空重索引剩余文件
                KnowledgeBaseModel.FileItems.Remove(item);

                // 重新索引剩余文件（耗时操作可后台执行）
                await ReindexAllFiles();

                MainWindow.SetStatusMessage($"已删除文件 {item.FileName}");
                AppSettingsManager.SaveSettings(KnowledgeBaseModel);
            }
        }

        private async Task ReindexAllFiles()
        {
            var ragService = new RagService(embeddingModel: KnowledgeBaseModel.CurrentEmbeddingName);
            ragService.ClearIndex();
            foreach (var file in KnowledgeBaseModel.FileItems)
            {
                var separators = ParseSeparators();
                int count = await ragService.IndexDocumentAsync(file.FilePath, separators, clearExisting: false);
                file.ChunkCount = count;
                file.IsIndexed = true;
            }
        }

        private KnowledgeBaseView GetKnowledgeBaseView()
        {
            // 遍历可视化树获取当前显示的 KnowledgeBaseView 实例
            var mainWindow = Application.Current.MainWindow as MainWindow;
            var spaceView = mainWindow?.FindName("SpaceView") as SpaceParametersView;
            if (spaceView != null)
            {
                var container = spaceView.FindName("ViewContainer") as ContentControl;
                if (container?.Content is KnowledgeBaseView kbView)
                    return kbView;
            }
            return null;
        }

        private void CloseFileBlockViewExecute()
        {
            var knowledgeView = GetKnowledgeBaseView();
            if (knowledgeView != null)
            {
                var overlay = knowledgeView.FindName("FileBlockOverlay") as Grid;
                var fileBlockView = knowledgeView.FindName("FileBlockViewControl") as FileBlockView;
                SlidingView.SlideUpToTop(fileBlockView, overlay);
            }
        }
    }
}