using GalaSoft.MvvmLight;
using RapidKnowledgeware.Base;
using RapidKnowledgeware.Models;
using RapidKnowledgeware.Services;
using RapidKnowledgeware.ViewModels;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using IOC;
using IOC.Annotations;

namespace RapidKnowledgeware.ViewModels.Impl
{
    /// <summary>
    /// 空间参数设置视图模型
    /// </summary>
    [Controller]
    public class SpaceAdjustViewModel : ObservableObject, ISpaceAdjustViewModel
    {
        private readonly IOllamaModelService _ollamaModelService;
        private readonly ISpaceAdjustService _spaceAdjustService;
        private readonly ISettingsChangeTracker _settingsChangeTracker;
        private readonly ILoggingService _loggingService;

        public SpaceAdjustModel SpaceAdjustModel { get; private set; }

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


        /// <summary>
        /// 重置为默认参数命令
        /// </summary>
        public ICommand ResetToDefaultCommand { get; private set; }

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
        public ChatSessionModel Session { get; private set; }

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


                        // 记录操作日志
                        var session = Session;
                        _loggingService.WriteOperationLog(new OperationsLog
                        {
                            Timestamp = DateTime.Now,
                            Type = OperationType.ToggleKnowledgeLink,
                            ActionName = "切换知识库对接",
                            Target = session.DisplayName,
                            Success = true,
                            Details = session.SpaceParameters.IsLinkKnowledgeBase ? "已开启" : "已关闭"
                        });
                    });
                }
                return _isLinkKnowledgeBaseCommand;
            }
        }

        private CommandBase _saveAsDefaultCommand;
        /// <summary>
        /// 将当前会话参数设为全局默认命令
        /// </summary>
        public CommandBase SaveAsDefaultCommand
        {
            get
            {
                if (_saveAsDefaultCommand == null)
                {
                    _saveAsDefaultCommand = new CommandBase();
                    _saveAsDefaultCommand.DoExecute = new Action<object>(_ =>
                    {
                        _spaceAdjustService.SaveAsDefault(SpaceAdjustModel);
                        MainWindow.SetStatusMessage("当前参数已设为全局默认");
                    });
                }
                return _saveAsDefaultCommand;
            }
        }

 

        /// <summary>
        /// 初始化空间参数视图模型
        /// </summary>
        /// <param name="session">当前编辑的会话</param>
        /// <param name="closeCommand">关闭视图的命令</param>
        public SpaceAdjustViewModel()
        {
            _ollamaModelService = BeanFactory.GetBean<IOllamaModelService>();
            _spaceAdjustService = BeanFactory.GetBean<ISpaceAdjustService>();
            _settingsChangeTracker = BeanFactory.GetBean<ISettingsChangeTracker>();
            _loggingService = BeanFactory.GetBean<ILoggingService>();
        }

        public void Initialize(ChatSessionModel session, ICommand closeCommand)
        {
            Session = session;
            SpaceAdjustModel = session.SpaceParameters;
            CloseCommand = closeCommand;
            ResetToDefaultCommand = new CommandBase { DoExecute = _ => _spaceAdjustService.ResetToDefault(SpaceAdjustModel) };

            _settingsChangeTracker.CaptureSnapshot(SpaceAdjustModel);

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

    }
}