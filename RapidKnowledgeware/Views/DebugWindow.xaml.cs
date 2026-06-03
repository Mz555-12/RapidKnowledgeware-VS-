using IOC;
using RapidKnowledgeware.Models;
using RapidKnowledgeware.ViewModels;
using System;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RapidKnowledgeware.Views
{
    public partial class DebugWindow : Window
    {
        private readonly NotifyCollectionChangedEventHandler _logEntriesHandler;

        public DebugWindow()
        {
            InitializeComponent();
            var vm = BeanFactory.GetBean<IDebugWindowModel>();
            this.DataContext = vm;

            var collectionView = System.Windows.Data.CollectionViewSource.GetDefaultView(vm.DebugModel.LogEntries);
            collectionView.SortDescriptions.Add(new System.ComponentModel.SortDescription("Timestamp", System.ComponentModel.ListSortDirection.Descending));

            _logEntriesHandler = (s, e) =>
            {
                if (AutoScrollCheck.IsChecked == true)
                {
                    LogScrollViewer.ScrollToHome();
                }
            };
            vm.DebugModel.LogEntries.CollectionChanged += _logEntriesHandler;

            this.Closed += (s, e) =>
            {
                vm.DebugModel.LogEntries.CollectionChanged -= _logEntriesHandler;
            };

            LogScrollViewer.PreviewMouseWheel += (s, e) =>
            {
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

        private void Close_DebugView(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void RowDefinition_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }
    }
}