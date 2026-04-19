using RapidKnowledgeware.Base;
using RapidKnowledgeware.Functions.LLMAdjustFunc;
using RapidKnowledgeware.Models;
using System;

namespace RapidKnowledgeware.ViewModels
{
    /// <summary>
    /// LLM 默认参数设置视图模型
    /// </summary>
    public class LLMAdjustViewModel
    {
        /// <summary>
        /// LLM 全局默认参数模型（单例）
        /// </summary>
        public LLMAdjustModel LLMAdjustModel => LLMAdjustService.Current;

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