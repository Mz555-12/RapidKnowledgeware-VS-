using RapidKnowledgeware.ViewModels;
using RapidKnowledgeware.ViewModels.Impl;
using System.Windows.Controls;

namespace RapidKnowledgeware.Views
{
    public partial class KnowledgeBaseView : UserControl
    {
        public KnowledgeBaseView()
        {
            InitializeComponent();
            var vm = KnowledgeBaseViewModel.Instance;
            this.DataContext = vm;
            vm.SetKnowledgeBaseView(this);
            vm.SetSlidingViewContainers(FuncViewContainer, SearchViewContainer, FuncView, SearchView);
        }
    }
}