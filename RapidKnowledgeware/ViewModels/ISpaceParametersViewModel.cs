using RapidKnowledgeware.Base;

namespace RapidKnowledgeware.ViewModels
{
    public interface ISpaceParametersViewModel
    {
        CommandBase ViewChangedCommand { get; }
        void SetViewContainer(System.Windows.Controls.ContentControl container);
    }
}
