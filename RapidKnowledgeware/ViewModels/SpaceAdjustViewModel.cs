using GalaSoft.MvvmLight;
using RapidKnowledgeware.Base;
using RapidKnowledgeware.Functions.SpaceAdjustFunc;
using RapidKnowledgeware.Models;
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;

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
        /// 切换知识库对接命令（仅保存状态，动画由 ToggleButton 触发器自动处理）
        /// </summary>
        private CommandBase _isLinkKnowledgeBaseCommand;
        public CommandBase Is_LinkKnowledgeBaseCommand
        {
            get
            {
                if (_isLinkKnowledgeBaseCommand == null)
                {
                    _isLinkKnowledgeBaseCommand = new CommandBase();
                    _isLinkKnowledgeBaseCommand.DoExecute = new Action<object>(_ =>
                    {
                        // 立即保存会话
                        var mainWin = Application.Current.MainWindow as MainWindow;
                        var mainVM = mainWin?.DataContext as MainWindowModel;
                        mainVM?.SaveSessions();
                    });
                }
                return _isLinkKnowledgeBaseCommand;
            }
        }

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

        /// <summary>
        /// 切换知识库对接状态并播放动画
        /// </summary>
        /// <param name="element">触发动画的按钮元素</param>
        private void ToggleKnowledgeBaseLink(FrameworkElement element)
        {
            // 切换状态
            SpaceAdjustModel.IsLinkKnowledgeBase = !SpaceAdjustModel.IsLinkKnowledgeBase;

            // 播放对应动画
            if (SpaceAdjustModel.IsLinkKnowledgeBase)
            {
                var storyboard = element.FindResource("StartAnimation") as Storyboard;
                storyboard?.Begin();
            }
            else
            {
                var storyboard = element.FindResource("StopAnimation") as Storyboard;
                storyboard?.Begin();
            }

            // 保存会话设置（持久化）
            var mainWin = Application.Current.MainWindow as MainWindow;
            var mainVM = mainWin?.DataContext as MainWindowModel;
            mainVM?.OnWindowClosing(); // 复用保存逻辑
        }
    }
}