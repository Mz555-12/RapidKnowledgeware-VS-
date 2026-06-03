using RapidKnowledgeware.Base;
using RapidKnowledgeware.Models;
using System.Collections.ObjectModel;

namespace RapidKnowledgeware.ViewModels
{
    public interface ILLMAdjustViewModel
    {
        LLMAdjustModel LLMAdjustModel { get; }
        ObservableCollection<string> ChatModels { get; }
        string ChatModelHint { get; set; }
        CommandBase SaveCommand { get; }
    }
}
