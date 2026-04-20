using RapidKnowledgeware.ViewModels;
using System.Windows.Controls;

namespace RapidKnowledgeware.Views
{
    /// <summary>
    /// KnowledgeBaseView.xaml 的交互逻辑
    /// </summary>
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