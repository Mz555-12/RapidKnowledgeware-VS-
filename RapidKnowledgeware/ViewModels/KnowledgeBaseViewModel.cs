using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using RapidKnowledgeware.Base;
using RapidKnowledgeware.Functions.KnowledgeBaseFunc;
using RapidKnowledgeware.Functions.MainWindowFunc;
using RapidKnowledgeware.Models;
using RapidKnowledgeware.Views;
using System;
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
        private readonly KnowledgeBaseService _service;

        private static KnowledgeBaseViewModel _instance;
        public static KnowledgeBaseViewModel Instance => _instance ?? (_instance = new KnowledgeBaseViewModel());

        private KnowledgeFileItem _currentDisplayedFile;


        private bool _hasMultipleChunks;
        public bool HasMultipleChunks
        {
            get => _hasMultipleChunks;
            set { _hasMultipleChunks = value; RaisePropertyChanged(); }
        }

        private KnowledgeBaseViewModel()
        {
            _service = new KnowledgeBaseService(KnowledgeBaseModel);

            // 命令初始化
            AddKnowledgeCommand = new RelayCommand(AddKnowledgeExecute);
            ViewFileBlocksCommand = new RelayCommand<KnowledgeFileItem>(ViewFileBlocksExecute);
            DeleteFileCommand = new RelayCommand<KnowledgeFileItem>(DeleteFileExecute);
            CloseFileBlockViewCommand = new RelayCommand(CloseFileBlockViewExecute);
            DeleteChunkCommand = new RelayCommand<FileChunkItem>(DeleteChunkExecute);
        }

        public ICommand AddKnowledgeCommand { get; }
        public ICommand ViewFileBlocksCommand { get; }
        public ICommand DeleteFileCommand { get; }
        public ICommand CloseFileBlockViewCommand { get; }
        public ICommand DeleteChunkCommand { get; }

        private async void AddKnowledgeExecute()
        {

            // 添加这段检查
            if (string.IsNullOrWhiteSpace(KnowledgeBaseModel.BlockRule))
            {
                MessageBox.Show("分块规则不能为空，请先填写规则。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "文本文件|*.txt|所有文件|*.*",
                Multiselect = true
            };

            if (dialog.ShowDialog() == true && dialog.FileNames.Length > 0)
            {
                LoadingAnimation.Show_Loading();
                var files = dialog.FileNames;
                MainWindow.SetStatusMessage($"开始索引 {files.Length} 个文件...");

                var progress = new Progress<(string FileName, bool Success, int ChunkCount, string ErrorMessage)>(report =>
                {
                    if (report.Success)
                        MainWindow.SetStatusMessage($"✓ {report.FileName} 索引完成，{report.ChunkCount} 个块");
                    else
                        MainWindow.SetStatusMessage($"✗ {report.FileName} 索引失败: {report.ErrorMessage}");
                });

                int successCount = await _service.IndexFilesAsync(files, progress);

                MainWindow.SetStatusMessage($"批量索引完成：成功 {successCount}/{files.Length} 个文件。");
                AppSettingsManager.SaveSettings(KnowledgeBaseModel);
                LoadingAnimation.Hide_Loading();
            }
        }

        private async void ViewFileBlocksExecute(KnowledgeFileItem item)
        {
            if (item == null) return;
            try
            {
                MainWindow.SetStatusMessage($"正在加载分块内容: {item.FileName}...");
                _currentDisplayedFile = item;

                var chunks = await _service.LoadFileChunksAsync(item);
                KnowledgeBaseModel.FileBlocks.Clear();
                foreach (var c in chunks)
                    KnowledgeBaseModel.FileBlocks.Add(c);

                ShowFileBlockOverlay();
                HasMultipleChunks = KnowledgeBaseModel.FileBlocks.Count > 1;

                KnowledgeBaseModel.CurrentFileName = item.FileName;
                KnowledgeBaseModel.CurrentFileBlockRule = string.IsNullOrEmpty(item.ImportBlockRule)
                    ? KnowledgeBaseModel.BlockRule
                    : item.ImportBlockRule;


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
                LoadingAnimation.Show_Loading();

                // 立即从 UI 列表移除
                KnowledgeBaseModel.FileItems.Remove(item);
                AppSettingsManager.SaveSettings(KnowledgeBaseModel);

                // 从内存索引中移除并保存（瞬间完成）
                _service.RemoveFileFromIndex(item.FilePath);

                // 保证动画至少显示 0.5 秒
                await Task.Delay(500);

                MainWindow.SetStatusMessage($"已删除文件 {item.FileName}");
                LoadingAnimation.Hide_Loading();
            }
        }

        private async void DeleteChunkExecute(FileChunkItem chunkItem)
        {
            if (_currentDisplayedFile == null || chunkItem == null) return;
            var result = MessageBox.Show($"确定删除该块吗？", "确认删除", MessageBoxButton.YesNo);
            if (result != MessageBoxResult.Yes) return;

            LoadingAnimation.Show_Loading();

            _currentDisplayedFile.DeletedChunkIndices.Add(chunkItem.OriginalIndex);
            _service.RemoveChunkAndSave(_currentDisplayedFile.FilePath, chunkItem.OriginalIndex);
            _service.RefreshDisplayedChunks(_currentDisplayedFile);

            HasMultipleChunks = KnowledgeBaseModel.FileBlocks.Count > 1;
            AppSettingsManager.SaveSettings(KnowledgeBaseModel);

            await Task.Delay(500);  // 同样保证动画最短显示时间
            MainWindow.SetStatusMessage($"已删除块（原索引 {chunkItem.OriginalIndex}）");
            LoadingAnimation.Hide_Loading();
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
            KnowledgeBaseModel.CurrentFileName = string.Empty;
            KnowledgeBaseModel.CurrentFileBlockRule = string.Empty;
            AppSettingsManager.SaveSettings(KnowledgeBaseModel);

        }

        private void ShowFileBlockOverlay()
        {
            var knowledgeView = GetKnowledgeBaseView();
            if (knowledgeView != null)
            {
                var overlay = knowledgeView.FindName("FileBlockOverlay") as Grid;
                var fileBlockView = knowledgeView.FindName("FileBlockViewControl") as FileBlockView;
                SlidingView.SlideDownToBottom(fileBlockView, overlay);
            }
        }

        private KnowledgeBaseView GetKnowledgeBaseView()
        {
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
    }
}