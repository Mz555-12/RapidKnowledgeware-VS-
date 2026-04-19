using GalaSoft.MvvmLight;
using RapidKnowledgeware.Base;
using RapidKnowledgeware.Functions.KnowledgeBaseFunc;
using RapidKnowledgeware.Models;
using RapidKnowledgeware.Views;
using System;

namespace RapidKnowledgeware.ViewModels
{
    public class KnowledgeBaseViewModel : ViewModelBase
    {
        public KnowledgeBaseModel KnowledgeBaseModel { get; set; } = KnowledgeBaseModel.Instance;
        private readonly KnowledgeBaseUIService _uiService;

        private static KnowledgeBaseViewModel _instance;
        public static KnowledgeBaseViewModel Instance => _instance ?? (_instance = new KnowledgeBaseViewModel());

        private bool _hasMultipleChunks;
        /// <summary>
        /// 是否包含多个分块（用于 UI 控制）
        /// </summary>
        public bool HasMultipleChunks
        {
            get => _hasMultipleChunks;
            set { _hasMultipleChunks = value; RaisePropertyChanged(); }
        }

        private KnowledgeBaseViewModel()
        {
            _uiService = new KnowledgeBaseUIService(KnowledgeBaseModel);
        }

        /// <summary>
        /// 设置知识库视图引用（由 KnowledgeBaseView 在加载时调用）
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
        /// 删除文件命令
        /// </summary>
        private CommandBase _deleteFileCommand;
        public CommandBase DeleteFileCommand
        {
            get
            {
                if (_deleteFileCommand == null)
                {
                    _deleteFileCommand = new CommandBase();
                    _deleteFileCommand.DoExecute = new Action<object>(async param =>
                    {
                        if (param is KnowledgeFileItem item)
                        {
                            await _uiService.DeleteFileAsync(item);
                        }
                    });
                }
                return _deleteFileCommand;
            }
        }

        /// <summary>
        /// 关闭文件块视图命令
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

        #endregion
    }
}