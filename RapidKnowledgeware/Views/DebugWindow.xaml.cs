using RapidKnowledgeware.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace RapidKnowledgeware.Views
{
    /// <summary>
    /// DebugWindow.xaml 的交互逻辑
    /// </summary>
    public partial class DebugWindow : Window
    {
        public DebugWindow()
        {
            InitializeComponent();
            this.DataContext = new DebugWindowModel();

            // 对日志集合进行降序排序（最新日志显示在顶部）
            var collectionView = System.Windows.Data.CollectionViewSource.GetDefaultView(((DebugWindowModel)DataContext).DebugModel.LogEntries);
            collectionView.SortDescriptions.Add(new System.ComponentModel.SortDescription("Timestamp", System.ComponentModel.ListSortDirection.Descending));

            // 自动滚动：当有新日志添加时滚动到顶部
            ((DebugWindowModel)DataContext).DebugModel.LogEntries.CollectionChanged += (s, e) =>
            {
                if (AutoScrollCheck.IsChecked == true)
                {
                    LogScrollViewer.ScrollToHome();
                }
            };

            // 新增：将滚轮事件转发给外层 ScrollViewer，使列表可滚动
            LogScrollViewer.PreviewMouseWheel += (s, e) =>
            {
                if (e.Delta > 0)
                    LogScrollViewer.LineUp();
                else
                    LogScrollViewer.LineDown();
                e.Handled = true;
            };
        }
    }
}