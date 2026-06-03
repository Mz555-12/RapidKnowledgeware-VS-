using GalaSoft.MvvmLight;
using RapidKnowledgeware.Base;
using RapidKnowledgeware.Models;
using RapidKnowledgeware.Services;
using RapidKnowledgeware.ViewModels;
using System;
using System.Collections.ObjectModel;
using IOC;
using IOC.Annotations;

namespace RapidKnowledgeware.ViewModels.Impl
{
    /// <summary>
    /// LLM 默认参数设置视图模型
    /// </summary>
    [Controller]
    public class LLMAdjustViewModel : ObservableObject, ILLMAdjustViewModel
    {
        private readonly IOllamaModelService _ollamaModelService;
        private readonly ILLMAdjustService _llmAdjustService;

        public LLMAdjustModel LLMAdjustModel => _llmAdjustService.Current;

        public ObservableCollection<string> ChatModels => _ollamaModelService.ChatModels;

        private string _chatModelHint = "正在检查模型...";
        /// <summary>
        /// 聊天模型提示信息
        /// </summary>
        public string ChatModelHint
        {
            get => _chatModelHint;
            set { _chatModelHint = value; RaisePropertyChanged(); }
        }

        public LLMAdjustViewModel(IOllamaModelService ollamaModelService, ILLMAdjustService llmAdjustService)
        {
            _ollamaModelService = ollamaModelService;
            _llmAdjustService = llmAdjustService;
            UpdateHint();
            _ollamaModelService.ModelsLoaded += UpdateHint;
            _ollamaModelService.LoadFailed += UpdateHint;
        }

        private void UpdateHint()
        {
            if (!_ollamaModelService.IsOllamaAvailable)
                ChatModelHint = "Ollama未部署";
            else if (_ollamaModelService.ChatModels.Count == 0)
                ChatModelHint = "请下载大模型";
            else
                ChatModelHint = "";
        }

        /// <summary>
        /// 保存设置命令
        /// </summary>
        private CommandBase _saveCommand;
        public CommandBase SaveCommand
        {
            get
            {
                if (_saveCommand == null)
                {
                    _saveCommand = new CommandBase();
                    _saveCommand.DoExecute = new Action<object>(_ =>
                    {
                        _llmAdjustService.Save();
                    });
                }
                return _saveCommand;
            }
        }
    }
}