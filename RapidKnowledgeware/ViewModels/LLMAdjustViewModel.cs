using GalaSoft.MvvmLight;
using RapidKnowledgeware.Base;
using RapidKnowledgeware.Functions.LLMAdjustFunc;
using RapidKnowledgeware.Models;
using System;
using System.Collections.ObjectModel;

namespace RapidKnowledgeware.ViewModels
{
    /// <summary>
    /// LLM 默认参数设置视图模型
    /// </summary>
    public class LLMAdjustViewModel : ObservableObject
    {
        /// <summary>
        /// LLM 全局默认参数模型（单例）
        /// </summary>
        public LLMAdjustModel LLMAdjustModel => LLMAdjustService.Current;

        /// <summary>
        /// 可用的聊天模型列表
        /// </summary>
        public ObservableCollection<string> ChatModels => OllamaModelService.ChatModels;

        private string _chatModelHint = "正在检查模型...";
        /// <summary>
        /// 聊天模型提示信息
        /// </summary>
        public string ChatModelHint
        {
            get => _chatModelHint;
            set { _chatModelHint = value; RaisePropertyChanged(); }
        }

        public LLMAdjustViewModel()
        {
            UpdateHint();
            OllamaModelService.ModelsLoaded += UpdateHint;
            OllamaModelService.LoadFailed += UpdateHint;
        }

        private void UpdateHint()
        {
            if (!OllamaModelService.IsOllamaAvailable)
                ChatModelHint = "Ollama未部署";
            else if (OllamaModelService.ChatModels.Count == 0)
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
                        LLMAdjustService.Save();
                    });
                }
                return _saveCommand;
            }
        }
    }
}