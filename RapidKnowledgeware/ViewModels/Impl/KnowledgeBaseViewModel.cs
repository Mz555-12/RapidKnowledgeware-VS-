using GalaSoft.MvvmLight;
using RapidKnowledgeware.Base;
using RapidKnowledgeware.Helpers;
using RapidKnowledgeware.Models;
using RapidKnowledgeware.DAO;
using RapidKnowledgeware.Services;
using RapidKnowledgeware.ViewModels;
using RapidKnowledgeware.Views;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using IOC;
using IOC.Annotations;

namespace RapidKnowledgeware.ViewModels.Impl
{
    [Controller]
    public class KnowledgeBaseViewModel : ObservableObject, IKnowledgeBaseViewModel
    {
        private static IKnowledgeBaseViewModel _instance;
        public static IKnowledgeBaseViewModel Instance => _instance ?? (_instance = BeanFactory.GetBean<IKnowledgeBaseViewModel>());

        public KnowledgeBaseModel KnowledgeBaseModel { get; set; } = KnowledgeBaseModel.Instance;
        private readonly IKnowledgeBaseUIService _uiService;
        private readonly IKnowledgeBaseSearchService _searchService;
        private readonly ISettingsChangeTracker _settingsChangeTracker;
        private readonly IOllamaModelService _ollamaModelService;
        private readonly ILoggingService _loggingService;




        /// <summary>
        /// 可用的嵌入模型列表
        /// </summary>
        public ObservableCollection<string> EmbeddingModels => _ollamaModelService.EmbeddingModels;

        private string _embeddingModelHint = "正在加载模型...";
        /// <summary>
        /// 嵌入模型提示信息
        /// </summary>
        public string EmbeddingModelHint
        {
            get => _embeddingModelHint;
            set { _embeddingModelHint = value; RaisePropertyChanged(); }
        }

        private bool _isAnimating = false;

        private bool _hasMultipleChunks;
        /// <summary>
        /// 是否拥有多个分块（用于 UI 控制）
        /// </summary>
        public bool HasMultipleChunks
        {
            get => _hasMultipleChunks;
            set { _hasMultipleChunks = value; RaisePropertyChanged(); }
        }


        // 依 HasMultipleChunks 下方暴露的属性
        /// <summary>
        /// 当前文件总块数
        /// </summary>
        public int TotalBlockCount => KnowledgeBaseModel.FileBlocks.Count;

        /// <summary>
        /// 当前文件中可查看的分块的总有效字符数
        /// </summary>
        public int TotalWordCount => KnowledgeBaseModel.FileBlocks.Sum(b => b.EffectiveCharCount);



        private bool _isSearchViewVisible = false;

        /// <summary>
        /// 切换按钮显示的文本
        /// </summary>
        public string SwitchViewButtonText => IsSearchViewVisible ? "知识库功能" : "搜索";

        // 视图容器
        private Grid _funcViewContainer, _searchViewContainer;
        private FrameworkElement _funcView, _searchView;


        /// <summary>
        /// 当前是否显示搜索视图
        /// </summary>
        public bool IsSearchViewVisible
        {
            get => _isSearchViewVisible;
            set { _isSearchViewVisible = value; RaisePropertyChanged(); RaisePropertyChanged(nameof(SwitchViewButtonText)); }
        }

        private void UpdateEmbeddingHint()
        {
            if (!_ollamaModelService.IsOllamaAvailable)
                EmbeddingModelHint = "Ollama未启动";
            else if (_ollamaModelService.EmbeddingModels.Count == 0)
                EmbeddingModelHint = "无可用嵌入模型";
            else
                EmbeddingModelHint = "";
        }


        /// <summary>
        /// 设置滑动视图容器（功能视图、搜索视图，供 View 调用）
        /// </summary>
        public void SetSlidingViewContainers(Grid funcContainer, Grid searchContainer, FrameworkElement funcView, FrameworkElement searchView)
        {
            _funcViewContainer = funcContainer;
            _searchViewContainer = searchContainer;
            _funcView = funcView;
            _searchView = searchView;
            IsSearchViewVisible = false;
        }


        public KnowledgeBaseViewModel(
            IKnowledgeBaseUIService uiService,
            IKnowledgeBaseSearchService searchService,
            ISettingsChangeTracker settingsChangeTracker,
            IOllamaModelService ollamaModelService,
            ILoggingService loggingService)
        {
            _uiService = uiService;
            _searchService = searchService;
            _settingsChangeTracker = settingsChangeTracker;
            _ollamaModelService = ollamaModelService;
            _loggingService = loggingService;

            _settingsChangeTracker.CaptureSnapshot(KnowledgeBaseModel);

            RefreshDisplayFileItems();

            KnowledgeBaseModel.FileItems.CollectionChanged += (s, e) => RefreshDisplayFileItems();

            KnowledgeBaseModel.FileBlocks.CollectionChanged += (s, e) =>
            {
                RaisePropertyChanged(nameof(TotalBlockCount));
                RaisePropertyChanged(nameof(TotalWordCount));
            };
        }

        /// <summary>
        /// 设置知识库视图（由 KnowledgeBaseView 在加载时调用）
        /// </summary>
        /// <param name="view">KnowledgeBaseView 实例</param>
        public void SetKnowledgeBaseView(KnowledgeBaseView view)
        {
            _uiService.SetKnowledgeBaseView(view);
        }

        #region 命令定义

        /// <summary>
        /// 添加知识文件命令
        /// </summary>
        private CommandBase _addKnowledgeCommand;
        public CommandBase AddKnowledgeCommand
        {
            get
            {
                if (_addKnowledgeCommand == null)
                {
                    _addKnowledgeCommand = new CommandBase();
                    _addKnowledgeCommand.DoExecute = new Action<object>(async _ =>
                    {
                        await _uiService.AddKnowledgeAsync();
                    });
                }
                return _addKnowledgeCommand;
            }
        }

        /// <summary>
        /// 查看文件分块命令
        /// </summary>
        private CommandBase _viewFileBlocksCommand;
        public CommandBase ViewFileBlocksCommand
        {
            get
            {
                if (_viewFileBlocksCommand == null)
                {
                    _viewFileBlocksCommand = new CommandBase();
                    _viewFileBlocksCommand.DoExecute = new Action<object>(async param =>
                    {
                        if (param is KnowledgeFileItem item)
                        {
                            await _uiService.ViewFileBlocksAsync(item);
                            HasMultipleChunks = _uiService.HasMultipleChunks;
                        }
                    });
                }
                return _viewFileBlocksCommand;
            }
        }

        /// <summary>
        /// 重新索引文件命令
        /// </summary>
        private CommandBase _reindexFileCommand;
        public CommandBase ReindexFileCommand
        {
            get
            {
                if (_reindexFileCommand == null)
                {
                    _reindexFileCommand = new CommandBase();
                    _reindexFileCommand.DoExecute = new Action<object>(async param =>
                    {
                        if (param is KnowledgeFileItem item)
                        {
                            // 检查源文件是否存在
                            if (!System.IO.File.Exists(item.FilePath))
                            {
                                var deleteResult = MessageBox.Show(
                                    $"文件「{item.FileName}」已不存在，可能被移动或删除。\n是否从知识库中移除该文件？",
                                    "文件已丢失",
                                    MessageBoxButton.YesNo,
                                    MessageBoxImage.Warning);
                                if (deleteResult == MessageBoxResult.Yes)
                                {
                                    // 从UI列表移除
                                    KnowledgeBaseModel.FileItems.Remove(item);
                                    // 从索引中移除
                                    var service = BeanFactory.GetBean<IKnowledgeBaseService>();
                                    service.RemoveFileFromIndex(item.FilePath);
                                    RefreshDisplayFileItems();
                                    MainWindow.SetStatusMessage($"已移除文件「{item.FileName}」");
                                }
                                return;
                            }

                            var result = MessageBox.Show($"确认重新索引文件「{item.FileName}」？",
                                "重新索引", MessageBoxButton.YesNo, MessageBoxImage.Question);
                            if (result != MessageBoxResult.Yes) return;

                            WindowControls.Show_Loading();
                            MainWindow.SetStatusMessage($"正在重新索引 {item.FileName}...");

                            var reindexService = BeanFactory.GetBean<IKnowledgeBaseService>();
                            await System.Threading.Tasks.Task.Run(async () =>
                            {
                                await reindexService.ReindexSingleFileAsync(item);
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    RefreshDisplayFileItems();

                                    // 记录操作日志
                                    _loggingService.WriteOperationLog(new OperationsLog
                                    {
                                        Timestamp = DateTime.Now,
                                        Type = OperationType.ReindexFile,
                                        ActionName = "重新索引知识文件",
                                        Target = item.FileName,
                                        Success = true
                                    });
                                    MainWindow.SetStatusMessage($"重新索引完成！{item.FileName}");
                                    WindowControls.Hide_Loading();
                                });
                            });
                        }
                    });


                }
                return _reindexFileCommand;
            }
        }

        /// <summary>
        /// 删除文件命令（带确认提示）
        /// </summary>
        private CommandBase _deleteFileCommand2;
        public CommandBase DeleteFileCommand2
        {
            get
            {
                if (_deleteFileCommand2 == null)
                {
                    _deleteFileCommand2 = new CommandBase();
                    _deleteFileCommand2.DoExecute = new Action<object>(async param =>
                    {
                        if (param is KnowledgeFileItem item)
                        {
                            string message = System.IO.File.Exists(item.FilePath)
                                ? $"确认删除文件「{item.FileName}」？将从知识库中移除该索引"
                                : $"文件「{item.FileName}」已不存在，是否从知识库中移除该索引？";

                            var result = MessageBox.Show(message, "确认删除", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                            if (result != MessageBoxResult.Yes) return;

                            WindowControls.Show_Loading();
                            MainWindow.SetStatusMessage($"正在删除 {item.FileName}...");

                            await System.Threading.Tasks.Task.Run(() =>
                            {
                                // 从UI列表移除
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    KnowledgeBaseModel.FileItems.Remove(item);
                                    BeanFactory.GetBean<IAppSettingsRepository>().SaveSettings(KnowledgeBaseModel);
                                });

                                var service = BeanFactory.GetBean<IKnowledgeBaseService>();
                                service.RemoveFileFromIndex(item.FilePath);

                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    RefreshDisplayFileItems();
                                    // 记录操作日志
                                    _loggingService.WriteOperationLog(new OperationsLog
                                    {
                                        Timestamp = DateTime.Now,
                                        Type = OperationType.DeleteKnowledgeFile,
                                        ActionName = "删除知识文件",
                                        Target = item.FileName,
                                        Success = true
                                    });
                                    MainWindow.SetStatusMessage($"已删除文件「{item.FileName}」");
                                    WindowControls.Hide_Loading();
                                });
                            });
                        }
                    });

                }
                return _deleteFileCommand2;
            }
        }

        /// <summary>
        /// 关闭文件分块视图命令
        /// </summary>
        private CommandBase _closeFileBlockViewCommand;
        public CommandBase CloseFileBlockViewCommand
        {
            get
            {
                if (_closeFileBlockViewCommand == null)
                {
                    _closeFileBlockViewCommand = new CommandBase();
                    _closeFileBlockViewCommand.DoExecute = new Action<object>(_ =>
                    {
                        _uiService.CloseFileBlockOverlay();
                    });
                }
                return _closeFileBlockViewCommand;
            }
        }

        /// <summary>
        /// 删除分块命令
        /// </summary>
        private CommandBase _deleteChunkCommand;
        public CommandBase DeleteChunkCommand
        {
            get
            {
                if (_deleteChunkCommand == null)
                {
                    _deleteChunkCommand = new CommandBase();
                    _deleteChunkCommand.DoExecute = new Action<object>(async param =>
                    {
                        if (param is FileChunkItem chunkItem)
                        {
                            await _uiService.DeleteChunkAsync(chunkItem);
                            HasMultipleChunks = _uiService.HasMultipleChunks;
                        }
                    });
                }
                return _deleteChunkCommand;
            }
        }

        /// <summary>
        /// 切换视图命令（绑定到按钮或双击触发）
        /// </summary>
        private CommandBase _switchViewCommand;
        public CommandBase SwitchViewCommand
        {
            get
            {
                if (_switchViewCommand == null)
                {
                    _switchViewCommand = new CommandBase();
                    _switchViewCommand.DoExecute = new Action<object>(_ =>
                    {
                        // 如果正在动画中则直接返回
                        if (_isAnimating)
                            return;
                        if (_funcViewContainer == null || _searchViewContainer == null)
                            return;

                        _isAnimating = true;

                        if (!IsSearchViewVisible)
                        {
                            // 切换到搜索视图
                            SlidingView.SlideOutToRight2(_funcView, _funcViewContainer);
                            SlidingView.SlideInFromRight(_searchView, _searchViewContainer, onCompleted: () =>
                            {
                                _isAnimating = false; // 动画完成后解锁
                            });
                            IsSearchViewVisible = true;
                            _searchService.PerformSearch();
                            RefreshDisplayFileItems();
                            RaiseSearchPropertiesChanged();
                        }
                        else
                        {
                            // 切换回功能视图
                            SlidingView.SlideOutToRight2(_searchView, _searchViewContainer);
                            SlidingView.SlideInFromRight(_funcView, _funcViewContainer, onCompleted: () =>
                            {
                                _isAnimating = false; // 动画完成后解锁
                            });
                            IsSearchViewVisible = false;
                            _searchService.Keyword = string.Empty;
                            RaisePropertyChanged(nameof(SearchKeyword));
                            _searchService.PerformSearch();
                            RefreshDisplayFileItems();
                        }
                    });
                }
                return _switchViewCommand;
            }
        }




        #endregion

        #region 搜索

        /// <summary>
        /// 搜索关键词
        /// </summary>
        public string SearchKeyword
        {
            get => _searchService.Keyword;
            set
            {
                if (_searchService.Keyword != value)
                {
                    _searchService.Keyword = value;
                    RaisePropertyChanged();
                }
            }
        }

        /// <summary>
        /// 当前页码
        /// </summary>
        public int CurrentPage => _searchService.CurrentPage;

        /// <summary>
        /// 总页数
        /// </summary>
        public int TotalPages => _searchService.TotalPages;

        /// <summary>
        /// 是否有上一页
        /// </summary>
        public bool HasPreviousPage => _searchService.HasPreviousPage;

        /// <summary>
        /// 是否有下一页
        /// </summary>
        public bool HasNextPage => _searchService.HasNextPage;


        /// <summary>
        /// 显示文件列表的文件集合（供列表视图绑定）
        /// </summary>
        public ObservableCollection<KnowledgeFileItem> DisplayFileItems { get; } = new ObservableCollection<KnowledgeFileItem>();

        /// <summary>
        /// 刷新显示文件列表（显示全部文件）
        /// </summary>
        private void RefreshDisplayFileItems()
        {
            DisplayFileItems.Clear();
            IEnumerable<KnowledgeFileItem> items;
            if (IsSearchViewVisible)
                items = _searchService.GetCurrentPageItems();
            else
                items = KnowledgeBaseModel.FileItems;

            foreach (var item in items)
                DisplayFileItems.Add(item);
        }

        /// <summary>
        /// 刷新分页状态属性
        /// </summary>
        private void RaiseSearchPropertiesChanged()
        {
            RaisePropertyChanged(nameof(CurrentPage));
            RaisePropertyChanged(nameof(TotalPages));
            RaisePropertyChanged(nameof(HasPreviousPage));
            RaisePropertyChanged(nameof(HasNextPage));
        }

        /// <summary>
        /// 搜索命令
        /// </summary>
        private CommandBase _searchCommand;
        public CommandBase SearchCommand
        {
            get
            {
                if (_searchCommand == null)
                {
                    _searchCommand = new CommandBase();
                    _searchCommand.DoExecute = new Action<object>(_ =>
                    {
                        _searchService.PerformSearch();
                        RefreshDisplayFileItems();      // 刷新列表
                        RaiseSearchPropertiesChanged();
                    });
                }
                return _searchCommand;
            }
        }

        /// <summary>
        /// 上一页命令
        /// </summary>
        private CommandBase _previousPageCommand;
        public CommandBase PreviousPageCommand
        {
            get
            {
                if (_previousPageCommand == null)
                {
                    _previousPageCommand = new CommandBase();
                    _previousPageCommand.DoExecute = new Action<object>(_ =>
                    {
                        if (_searchService.HasPreviousPage)
                        {
                            _searchService.CurrentPage--;
                            RefreshDisplayFileItems();
                            RaiseSearchPropertiesChanged();
                        }
                    });
                }
                return _previousPageCommand;
            }
        }

        /// <summary>
        /// 下一页命令
        /// </summary>
        private CommandBase _nextPageCommand;
        public CommandBase NextPageCommand
        {
            get
            {
                if (_nextPageCommand == null)
                {
                    _nextPageCommand = new CommandBase();
                    _nextPageCommand.DoExecute = new Action<object>(_ =>
                    {
                        if (_searchService.HasNextPage)
                        {
                            _searchService.CurrentPage++;
                            RefreshDisplayFileItems();
                            RaiseSearchPropertiesChanged();
                        }
                    });
                }
                return _nextPageCommand;
            }
        }

        #endregion
    }
}
