using RapidKnowledgeware.Base;
using RapidKnowledgeware.Functions.LLMAdjustFunc;
using RapidKnowledgeware.Models;
using System.Windows.Input;

namespace RapidKnowledgeware.ViewModels
{
    public class LLMAdjustViewModel
    {
        public LLMAdjustModel LLMAdjustModel => LLMAdjustService.Current;

        public ICommand SaveCommand { get; }

        public LLMAdjustViewModel()
        {
            SaveCommand = new CommandBase { DoExecute = _ => LLMAdjustService.Save() };
        }
    }
}