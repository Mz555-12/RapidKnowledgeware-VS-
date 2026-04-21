using Microsoft.Win32;
using RapidKnowledgeware.Functions.MainWindowFunc;
using RapidKnowledgeware.Models;
using RapidKnowledgeware.Views;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace RapidKnowledgeware.Functions.KnowledgeBaseFunc
{
    /// <summary>
    /// 知识库 UI 交互服务类（处理对话框、状态提示、动画等）
    /// </summary>
    public class KnowledgeBaseUIService
    {
        private readonly KnowledgeBaseModel _model;
        private readonly KnowledgeBaseService _knowledgeService;

        // 视图引用
        private KnowledgeBaseView _knowledgeBaseView;
        private KnowledgeFileItem _currentDisplayedFile;

        /// <summary>
        /// 初始化知识库 UI 服务
        /// </summary>
        /// <param name="model">知识库数据模型</param>
        public KnowledgeBaseUIService(KnowledgeBaseModel model)
        {
            _model = model;
            _knowledgeService = new KnowledgeBaseService(model);
        }

        /// <summary>
        /// 设置知识库视图引用（用于动画和查找子控件）
        /// </summary>
        /// <param name="view">KnowledgeBaseView 实例</param>
        public void SetKnowledgeBaseView(KnowledgeBaseView view)
        {
            _knowledgeBaseView = view;
        }

        #region 文件添加与索引

        /// <summary>
        /// 执行添加知识文件操作（打开对话框、显示进度、调用底层服务索引）
        /// </summary>
        public async Task AddKnowledgeAsync()
        {
            if (string.IsNullOrWhiteSpace(_model.BlockRule))
            {
                MessageBox.Show("分块规则不能为空，请先填写规则。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
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
                var existingFiles = selectedFiles.Where(f => _model.FileItems.Any(item => item.FilePath == f)).ToList();

                if (existingFiles.Any())
                {
                    string msg = $"以下文件已存在于知识库中：\n{string.Join("\n", existingFiles.Select(System.IO.Path.GetFileName))}\n\n是否覆盖这些文件？";
                    var result = MessageBox.Show(msg, "文件已存在", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        foreach (var file in existingFiles)
                        {
                            var item = _model.FileItems.First(f => f.FilePath == file);
                            _model.FileItems.Remove(item);
                            _knowledgeService.RemoveFileFromIndex(item.FilePath);
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
                MainWindow.SetStatusMessage($"开始索引 {selectedFiles.Length} 个文件...");

                var progress = new Progress<(string FileName, bool Success, int ChunkCount, string ErrorMessage)>(report =>
                {
                    if (report.Success)
                        MainWindow.SetStatusMessage($"✓ {report.FileName} 索引完成，{report.ChunkCount} 个块");
                    else
                        MainWindow.SetStatusMessage($"✗ {report.FileName} 索引失败: {report.ErrorMessage}");
                });

                int successCount = await _knowledgeService.IndexFilesAsync(selectedFiles, progress);


                // 记录操作日志
                LoggingFunc.LoggingService.WriteOperationLog(new OperationsLog
                {
                    Timestamp = DateTime.Now,
                    Type = OperationType.AddKnowledgeFile,
                    ActionName = "添加知识文件",
                    Target = string.Join(", ", selectedFiles.Select(System.IO.Path.GetFileName)),
                    Success = true
                });

                MainWindow.SetStatusMessage($"批量索引完成：成功 {successCount}/{selectedFiles.Length} 个文件。");
                AppSettingsManager.SaveSettings(_model);
                WindowControls.Hide_Loading();
            }
        }

        #endregion

        #region 文件块查看与删除

        /// <summary>
        /// 查看文件分块（加载并显示覆盖层）
        /// </summary>
        /// <param name="item">知识库文件项</param>
        public async Task ViewFileBlocksAsync(KnowledgeFileItem item)
        {
            if (item == null) return;
            try
            {
                MainWindow.SetStatusMessage($"正在加载分块内容: {item.FileName}...");
                _currentDisplayedFile = item;

                var chunks = await _knowledgeService.LoadFileChunksAsync(item);
                _model.FileBlocks.Clear();
                foreach (var c in chunks)
                    _model.FileBlocks.Add(c);

                ShowFileBlockOverlay();

                _model.CurrentFileName = item.FileName;
                _model.CurrentFileBlockRule = string.IsNullOrEmpty(item.ImportBlockRule)
                    ? _model.BlockRule
                    : item.ImportBlockRule;

                MainWindow.SetStatusMessage($"已加载 {item.FileName} 的分块，共 {chunks.Count} 个块");
            }
            catch (Exception ex)
            {
                MainWindow.SetStatusMessage($"加载分块失败: {ex.Message}");
            }
        }



        /// <summary>
        /// 删除单个分块（弹出确认框）
        /// </summary>
        /// <param name="chunkItem">分块项</param>
        public async Task DeleteChunkAsync(FileChunkItem chunkItem)
        {
            if (_currentDisplayedFile == null || chunkItem == null) return;
            var result = MessageBox.Show($"确定删除该块吗？", "确认删除", MessageBoxButton.YesNo);
            if (result != MessageBoxResult.Yes) return;

            WindowControls.Show_Loading();

            _currentDisplayedFile.DeletedChunkIndices.Add(chunkItem.OriginalIndex);
            _knowledgeService.RemoveChunkAndSave(_currentDisplayedFile.FilePath, chunkItem.OriginalIndex);
            _knowledgeService.RefreshDisplayedChunks(_currentDisplayedFile);

            AppSettingsManager.SaveSettings(_model);

            await Task.Delay(500);
            MainWindow.SetStatusMessage($"已删除块（原索引 {chunkItem.OriginalIndex}）");
            WindowControls.Hide_Loading();
        }

        #endregion

        #region 覆盖层动画控制

        /// <summary>
        /// 显示文件块覆盖层（从顶部滑下）
        /// </summary>
        public void ShowFileBlockOverlay()
        {
            if (_knowledgeBaseView == null) return;
            var overlay = _knowledgeBaseView.FindName("FileBlockOverlay") as Grid;
            var fileBlockView = _knowledgeBaseView.FindName("FileBlockViewControl") as FileBlockView;
            SlidingView.SlideDownToBottom(fileBlockView, overlay);
        }

        /// <summary>
        /// 关闭文件块覆盖层（滑回顶部）
        /// </summary>
        public void CloseFileBlockOverlay()
        {
            if (_knowledgeBaseView == null) return;
            var overlay = _knowledgeBaseView.FindName("FileBlockOverlay") as Grid;
            var fileBlockView = _knowledgeBaseView.FindName("FileBlockViewControl") as FileBlockView;
            SlidingView.SlideUpToTop(fileBlockView, overlay);

            _model.CurrentFileName = string.Empty;
            _model.CurrentFileBlockRule = string.Empty;
            AppSettingsManager.SaveSettings(_model);
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 获取当前显示的文件分块数量是否大于1（用于 UI 绑定）
        /// </summary>
        public bool HasMultipleChunks => _model.FileBlocks.Count > 1;

        #endregion
    }
}