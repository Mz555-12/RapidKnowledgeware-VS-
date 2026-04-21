using RapidKnowledgeware.Base;
using RapidKnowledgeware.Functions.SpaceParametersFunc;
using System;
using System.Windows.Controls;

namespace RapidKnowledgeware.ViewModels
{
    public class SpaceParametersViewModel
    {
        private ContentControl _viewContainer;

        public void SetViewContainer(ContentControl container)
        {
            _viewContainer = container;
            // 初始显示知识库视图
            ViewSwitcher.SwitchView(_viewContainer, "KnowledgeBaseView");
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
                        string viewKey = o as string;
                        ViewSwitcher.SwitchView(_viewContainer, viewKey);

                    });
                }
                return _viewChangedCommand;
            }
        }
    }
}