using RapidKnowledgeware.ViewModels;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;

namespace RapidKnowledgeware.Views
{
    /// <summary>
    /// DebugWindow.xaml 的交互逻辑
    /// </summary>
    public partial class DebugWindow : Window
    {
        private readonly NotifyCollectionChangedEventHandler _logEntriesHandler;

        public DebugWindow()
        {
            InitializeComponent();
            this.DataContext = new DebugWindowModel();

            // 对日志集合进行降序排序（最新日志显示在顶部）
            var collectionView = System.Windows.Data.CollectionViewSource.GetDefaultView(((DebugWindowModel)DataContext).DebugModel.LogEntries);
            collectionView.SortDescriptions.Add(new System.ComponentModel.SortDescription("Timestamp", System.ComponentModel.ListSortDirection.Descending));

            // 保存事件处理委托以便取消订阅
            _logEntriesHandler = (s, e) =>
            {
                if (AutoScrollCheck.IsChecked == true)
                {
                    LogScrollViewer.ScrollToHome();
                }
            };
            ((DebugWindowModel)DataContext).DebugModel.LogEntries.CollectionChanged += _logEntriesHandler;

            // 窗口关闭时移除事件订阅
            this.Closed += (s, e) =>
            {
                ((DebugWindowModel)DataContext).DebugModel.LogEntries.CollectionChanged -= _logEntriesHandler;
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