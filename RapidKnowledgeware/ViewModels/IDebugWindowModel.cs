using RapidKnowledgeware.Models;
using System.Windows.Input;

namespace RapidKnowledgeware.ViewModels
{
    public interface IDebugWindowModel
    {
        DebugModel DebugModel { get; }
        ICommand ClearCommand { get; }
    }
}
