using RapidKnowledgeware.Models;
using RapidKnowledgeware.ViewModels;
using System;
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

            LogScrollViewer.PreviewMouseWheel += (s, e) =>
            {
                // 根据滚轮增量计算滚动行数（Delta 通常为 ±120，此处每 40 单位滚动 1 行）
                int lines = Math.Max(1, Math.Abs(e.Delta) / 40);
                if (e.Delta > 0)
                {
                    for (int i = 0; i < lines; i++)
                        LogScrollViewer.LineUp();
                }
                else
                {
                    for (int i = 0; i < lines; i++)
                        LogScrollViewer.LineDown();
                }
                e.Handled = true;
            };
        }
    }
}