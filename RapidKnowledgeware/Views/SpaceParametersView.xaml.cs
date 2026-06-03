using IOC;
using RapidKnowledgeware.Helpers;
using RapidKnowledgeware.ViewModels;
using System.Windows.Controls;

namespace RapidKnowledgeware.Views
{
    public partial class SpaceParametersView : UserControl
    {
        public SpaceParametersView()
        {
            InitializeComponent();
            this.DataContext = BeanFactory.GetBean<ISpaceParametersViewModel>();

            PreloadViews();

            var vm = (ISpaceParametersViewModel)this.DataContext;
            vm.SetViewContainer(ViewContainer);
        }

        private void PreloadViews()
        {
            ViewSwitcher.PreloadView("KnowledgeBaseView", new KnowledgeBaseView());
            ViewSwitcher.PreloadView("SpaceAdjustView", new LLMAdjustView());
        }
    }
}