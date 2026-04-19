using GalaSoft.MvvmLight;
using RapidKnowledgeware.Base;
using RapidKnowledgeware.Functions.SpaceAdjustFunc;
using RapidKnowledgeware.Models;
using System.Windows.Input;

namespace RapidKnowledgeware.ViewModels
{
    /// <summary>
    /// 空间参数设置视图模型
    /// </summary>
    public class SpaceAdjustViewModel : ObservableObject
    {
        /// <summary>
        /// 空间参数数据模型
        /// </summary>
        public SpaceAdjustModel SpaceAdjustModel { get; }

        /// <summary>
        /// 重置为默认参数命令
        /// </summary>
        public ICommand ResetToDefaultCommand { get; }

        private ICommand _closeCommand;
        /// <summary>
        /// 关闭视图命令
        /// </summary>
        public ICommand CloseCommand
        {
            get => _closeCommand;
            set { _closeCommand = value; RaisePropertyChanged(); }
        }

        /// <summary>
        /// 当前编辑的会话对象
        /// </summary>
        public ChatSessionModel Session { get; }

        /// <summary>
        /// 初始化空间参数视图模型
        /// </summary>
        /// <param name="session">当前编辑的会话</param>
        /// <param name="closeCommand">关闭视图的命令</param>
        public SpaceAdjustViewModel(ChatSessionModel session, ICommand closeCommand)
        {
            Session = session;
            SpaceAdjustModel = session.SpaceParameters;
            CloseCommand = closeCommand;
            ResetToDefaultCommand = new CommandBase { DoExecute = _ => SpaceAdjustService.ResetToDefault(SpaceAdjustModel) };
        }
    }
}