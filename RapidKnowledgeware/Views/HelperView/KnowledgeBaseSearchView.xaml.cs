using RapidKnowledgeware.ViewModels;
using RapidKnowledgeware.ViewModels.Impl;
using System.Windows.Controls;

namespace RapidKnowledgeware.Views.HelperView
{
    public partial class KnowledgeBaseSearchView : UserControl
    {
        public KnowledgeBaseSearchView()
        {
            InitializeComponent();
            this.DataContext = KnowledgeBaseViewModel.Instance;
        }
    }
}
