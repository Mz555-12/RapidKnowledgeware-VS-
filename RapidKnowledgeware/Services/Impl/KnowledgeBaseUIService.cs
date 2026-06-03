using IOC;
using IOC.Annotations;
using Microsoft.Win32;
using RapidKnowledgeware.Helpers;
using RapidKnowledgeware.Models;
using RapidKnowledgeware.DAO;
using RapidKnowledgeware.Services;
using RapidKnowledgeware.Views;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace RapidKnowledgeware.Services.Impl
{
    [Service]
    public class KnowledgeBaseUIService : IKnowledgeBaseUIService
    {
        private readonly IKnowledgeBaseService _knowledgeBaseService;
        private readonly IAppSettingsRepository _appSettingsManager;
        private readonly ILoggingService _loggingService;

        private KnowledgeBaseView _knowledgeBaseView;
        private KnowledgeFileItem _currentDisplayedFile;

        public KnowledgeBaseUIService(IKnowledgeBaseService knowledgeBaseService, IAppSettingsRepository appSettingsManager, ILoggingService loggingService)
        {
            _knowledgeBaseService = knowledgeBaseService;
            _appSettingsManager = appSettingsManager;
            _loggingService = loggingService;
        }

        public void SetKnowledgeBaseView(KnowledgeBaseView view)
        {
            _knowledgeBaseView = view;
        }

        #region 文件添加/导入相关

        public async Task AddKnowledgeAsync()
        {
            if (string.IsNullOrWhiteSpace(KnowledgeBaseModel.Instance.Default_BlockRule))
            {
                MessageBox.Show("分块规则不能为空，请先填写规则", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new OpenFileDialog
            {
                Filter = "文本文件|*.txt|所有文件|*.*",
                Multiselect = true
            };

            if (dialog.ShowDialog() == true && dialog.FileNames.Length > 0)
            {
                var selectedFiles = dialog.FileNames;
                var existingFiles = selectedFiles.Where(f => KnowledgeBaseModel.Instance.FileItems.Any(item => item.FilePath == f)).ToList();

                if (existingFiles.Any())
                {
                    string msg = $"以下文件已存在于知识库中：\n{string.Join("\n", existingFiles.Select(System.IO.Path.GetFileName))}\n\n是否覆盖这些文件？";
                    var result = MessageBox.Show(msg, "文件已存在", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        foreach (var file in existingFiles)
                        {
                            var item = KnowledgeBaseModel.Instance.FileItems.First(f => f.FilePath == file);
                            KnowledgeBaseModel.Instance.FileItems.Remove(item);
                            _knowledgeBaseService.RemoveFileFromIndex(item.FilePath);
                        }
                    }
                    else
                    {
                        selectedFiles = selectedFiles.Except(existingFiles).ToArray();
                        if (selectedFiles.Length == 0)
                            return;
                    }
                }

                WindowControls.Show_Loading();
                MainWindow.SetStatusMessage($"开始处理 {selectedFiles.Length} 个文件...");

                BeanFactory.GetBean<IDebugService>().Info($"[AddKnowledgeAsync] 用户选择了 {selectedFiles.Length} 个文件，分块规则: {KnowledgeBaseModel.Instance.Default_BlockRule}，嵌入模型: {KnowledgeBaseModel.Instance.Default_CurrentEmbeddingName}");

                var progress = new Progress<(string FileName, bool Success, int ChunkCount, string ErrorMessage)>(report =>
                {
                    if (report.Success)
                        MainWindow.SetStatusMessage($"✓ {report.FileName} 加载成功，{report.ChunkCount} 个块");
                    else
                        MainWindow.SetStatusMessage($"✗ {report.FileName} 加载失败: {report.ErrorMessage}");
                });

                int successCount = await _knowledgeBaseService.IndexFilesAsync(selectedFiles, progress);

                BeanFactory.GetBean<IDebugService>().Info($"[AddKnowledgeAsync] 处理完成，成功 {successCount}/{selectedFiles.Length} 个文件");


                _loggingService.WriteOperationLog(new OperationsLog
                {
                    Timestamp = DateTime.Now,
                    Type = OperationType.AddKnowledgeFile,
                    ActionName = "添加知识文件",
                    Target = string.Join(", ", selectedFiles.Select(System.IO.Path.GetFileName)),
                    Success = true
                });

                MainWindow.SetStatusMessage($"文件处理完成，成功 {successCount}/{selectedFiles.Length} 个文件。");
                _appSettingsManager.SaveSettings(KnowledgeBaseModel.Instance);
                WindowControls.Hide_Loading();
            }
        }

        #endregion

        #region 文件分块查看与删除

        public async Task ViewFileBlocksAsync(KnowledgeFileItem item)
        {
            if (item == null) return;
            try
            {
                MainWindow.SetStatusMessage($"正在加载分块内容: {item.FileName}...");
                _currentDisplayedFile = item;

                var chunks = await _knowledgeBaseService.LoadFileChunksAsync(item);
                KnowledgeBaseModel.Instance.FileBlocks.Clear();
                foreach (var c in chunks)
                    KnowledgeBaseModel.Instance.FileBlocks.Add(c);

                ShowFileBlockOverlay();

                KnowledgeBaseModel.Instance.CurrentFileName = item.FileName;
                KnowledgeBaseModel.Instance.CurrentFileBlockRule = string.IsNullOrEmpty(item.ImportBlockRule)
                    ? KnowledgeBaseModel.Instance.Default_BlockRule
                    : item.ImportBlockRule;

                MainWindow.SetStatusMessage($"已加载 {item.FileName} 的分块，共 {chunks.Count} 个块");
            }
            catch (Exception ex)
            {
                MainWindow.SetStatusMessage($"加载分块失败: {ex.Message}");
            }
        }



        public async Task DeleteChunkAsync(FileChunkItem chunkItem)
        {
            if (_currentDisplayedFile == null || chunkItem == null) return;
            var result = MessageBox.Show("确认删除该块？", "确认删除", MessageBoxButton.YesNo);
            if (result != MessageBoxResult.Yes) return;

            WindowControls.Show_Loading();

            _currentDisplayedFile.DeletedChunkIndices.Add(chunkItem.OriginalIndex);
            _knowledgeBaseService.RemoveChunkAndSave(_currentDisplayedFile.FilePath, chunkItem.OriginalIndex);
            _knowledgeBaseService.RefreshDisplayedChunks(_currentDisplayedFile);

            _appSettingsManager.SaveSettings(KnowledgeBaseModel.Instance);

            await Task.Delay(500);
            MainWindow.SetStatusMessage($"已删除块（原索引 {chunkItem.OriginalIndex}）");
            WindowControls.Hide_Loading();
        }

        #endregion

        #region 覆盖层动画控制

        public void ShowFileBlockOverlay()
        {
            if (_knowledgeBaseView == null) return;
            var overlay = _knowledgeBaseView.FindName("FileBlockOverlay") as Grid;
            var fileBlockView = _knowledgeBaseView.FindName("FileBlockViewControl") as FileBlockView;
            SlidingView.SlideDownToBottom(fileBlockView, overlay);
        }

        public void CloseFileBlockOverlay()
        {
            if (_knowledgeBaseView == null) return;
            var overlay = _knowledgeBaseView.FindName("FileBlockOverlay") as Grid;
            var fileBlockView = _knowledgeBaseView.FindName("FileBlockViewControl") as FileBlockView;
            SlidingView.SlideUpToTop(fileBlockView, overlay);

            KnowledgeBaseModel.Instance.CurrentFileName = string.Empty;
            KnowledgeBaseModel.Instance.CurrentFileBlockRule = string.Empty;
            _appSettingsManager.SaveSettings(KnowledgeBaseModel.Instance);
        }

        #endregion

        #region 其他属性

        public bool HasMultipleChunks => KnowledgeBaseModel.Instance.FileBlocks.Count > 1;

        #endregion
    }
}
