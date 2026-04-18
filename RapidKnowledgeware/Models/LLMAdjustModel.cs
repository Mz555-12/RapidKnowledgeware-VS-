using GalaSoft.MvvmLight;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RapidKnowledgeware.Models
{
    public class LLMAdjustModel : ObservableObject
    {
        private float _chatLLM;
        public float ChatLLM
        {
            get => _chatLLM;
            set { _chatLLM = value; RaisePropertyChanged(); }
        }

    }
}
