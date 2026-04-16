using RapidKnowledgeware.Functions.SpaceParametersFunc;
using RapidKnowledgeware.ViewModels;
using System.Windows.Controls;

namespace RapidKnowledgeware.Views
{
    public partial class SpaceParametersView : UserControl
    {
        public SpaceParametersView()
        {
            InitializeComponent();
            this.DataContext = new SpaceParametersViewModel();

            // 预加载两个子视图（仅在首次实例化时执行一次）
            PreloadViews();

            // 将 ContentControl 引用传递给 ViewModel（或通过命令参数直接操作）
            var vm = (SpaceParametersViewModel)this.DataContext;
            vm.SetViewContainer(ViewContainer);
        }

        private void PreloadViews()
        {
            // 创建并缓存两个视图实例
            ViewSwitcher.PreloadView("KnowledgeBaseView", new KnowledgeBaseView());
            ViewSwitcher.PreloadView("SpaceAdjustView", new SpaceAdjustView());
        }
    }
}