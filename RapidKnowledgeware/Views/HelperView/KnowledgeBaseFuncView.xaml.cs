using RapidKnowledgeware.ViewModels;
using RapidKnowledgeware.ViewModels.Impl;
using System.Windows.Controls;

namespace RapidKnowledgeware.Views.HelperView
{
    public partial class KnowledgeBaseFuncView : UserControl
    {
        public KnowledgeBaseFuncView()
        {
            InitializeComponent();
            this.DataContext = KnowledgeBaseViewModel.Instance;
        }
    }
}
