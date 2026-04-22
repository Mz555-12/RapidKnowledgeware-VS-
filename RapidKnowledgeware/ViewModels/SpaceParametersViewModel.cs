using RapidKnowledgeware.Base;
using RapidKnowledgeware.Functions.SpaceParametersFunc;
using RapidKnowledgeware.Models;
using RapidKnowledgeware.Functions.MainWindowFunc;
using RapidKnowledgeware.Functions.LLMAdjustFunc;
using System;
using System.Windows.Controls;
using System.Diagnostics;

namespace RapidKnowledgeware.ViewModels
{
    public class SpaceParametersViewModel
    {
        private ContentControl _viewContainer;
        private string _currentViewKey = "KnowledgeBaseView"; // 记录当前显示的视图标识

        public void SetViewContainer(ContentControl container)
        {
            _viewContainer = container;
            // 初始显示知识库视图
            ViewSwitcher.SwitchView(_viewContainer, "KnowledgeBaseView");
            _currentViewKey = "KnowledgeBaseView";
        }

        private CommandBase _viewChangedCommand;
        public CommandBase ViewChangedCommand
        {
            get
            {
                if (_viewChangedCommand == null)
                {
                    _viewChangedCommand = new CommandBase();
                    _viewChangedCommand.DoExecute = new Action<object>((o) =>
                    {
                        if (_viewContainer == null) return;
                        string targetViewKey = o as string;
                        if (string.IsNullOrEmpty(targetViewKey) || targetViewKey == _currentViewKey)
                            return;

                        // --- 新增：离开当前视图前保存其变更日志 ---
                        SaveCurrentViewChanges(_currentViewKey);
                        // ----------------------------------------

                        // 切换视图
                        ViewSwitcher.SwitchView(_viewContainer, targetViewKey);
                        _currentViewKey = targetViewKey;
                    });
                }
                return _viewChangedCommand;
            }
        }

        /// <summary>
        /// 保存指定视图对应的参数变更日志，并重新捕获快照
        /// </summary>
        /// <param name="viewKey">视图标识（"KnowledgeBaseView" 或 "SpaceAdjustView"）</param>
        private void SaveCurrentViewChanges(string viewKey)
        {
            try
            {
                if (viewKey == "KnowledgeBaseView")
                {
                    var kbModel = KnowledgeBaseModel.Instance;
                    string changes = SettingsChangeTracker.GetChangesAndClear(kbModel);
                    if (!string.IsNullOrEmpty(changes))
                    {
                        string formatted = changes.Replace("; ", "\n");
                        Functions.LoggingFunc.LoggingService.WriteOperationLog(new OperationsLog
                        {
                            Timestamp = DateTime.Now,
                            Type = OperationType.EditKnowledgeBaseParams,
                            ActionName = "编辑知识库参数",
                            Target = "全局知识库设置",
                            Success = true,
                            Details = formatted
                        });
                        Debug.WriteLine($"[SpaceParametersVM] 知识库参数变更:\n{formatted}");
                    }
                    // 重新捕获快照，为后续编辑做准备
                    SettingsChangeTracker.CaptureSnapshot(kbModel);
                }
                else if (viewKey == "SpaceAdjustView")
                {
                    // LLM全局参数直接调用其内部保存方法（已含对比和日志）
                    LLMAdjustService.Save();
                    // LLMAdjustService.Save() 内部已维护快照，无需额外捕获
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SpaceParametersVM] 保存视图变更失败: {ex.Message}");
            }
        }
    }
}