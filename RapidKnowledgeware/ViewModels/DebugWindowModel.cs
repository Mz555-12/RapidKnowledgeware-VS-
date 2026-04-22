using RapidKnowledgeware.Base;
using RapidKnowledgeware.Models;
using System;
using System.Windows.Input;

namespace RapidKnowledgeware.ViewModels
{
    /// <summary>
    /// 调试窗口视图模型
    /// </summary>
    public class DebugWindowModel
    {
        public DebugModel DebugModel => DebugModel.Instance;

        private CommandBase _clearCommand;
        /// <summary>
        /// 清空所有日志命令
        /// </summary>
        public ICommand ClearCommand
        {
            get
            {
                if (_clearCommand == null)
                {
                    _clearCommand = new CommandBase();
                    _clearCommand.DoExecute = new Action<object>(_ =>
                    {
                        DebugModel.LogEntries.Clear();
                    });
                }
                return _clearCommand;
            }
        }
    }
}