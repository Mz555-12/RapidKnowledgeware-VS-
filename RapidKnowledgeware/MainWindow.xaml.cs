using RapidKnowledgeware.Functions.MainWindowFunc;
using RapidKnowledgeware.Models;
using RapidKnowledgeware.ViewModels;
using RapidKnowledgeware.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RapidKnowledgeware
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool is_top = false;
        private static System.Windows.Threading.DispatcherTimer _statusTimer;


        public MainWindow()
        {
            InitializeComponent();
            MainModel.Loading_Grid = this.Loading_Grid;
            MainModel.CloseWindow_Btn = this.CloseWindow_Btn;
            MainModel.TopWindow_Border = this.TopWindow_Border;
            MainModel.Minimum_Btn = this.Minimum_Btn;
            MainModel.OpenSpaceParametersView_Btn = this.OpenSpaceParametersView_Btn;


            var vm = new MainWindowModel();
            this.DataContext = vm;

            this.Tag = vm;   // 将 ViewModel 存入 Tag，供菜单绑定使用

            // 传递 SpaceAdjustView 弹出层引用（注意这里的名称与 XAML 中 x:Name 一致）
            vm.SetSpaceAdjustViewReferences(this.SpaceAdjustOverlay, this.SpaceAdjustViewControl);

            // 加载设置到 KnowledgeBaseModel
            AppSettingsManager.LoadSettings(KnowledgeBaseModel.Instance);
            foreach (var item in KnowledgeBaseModel.Instance.FileItems)
            {
                if (string.IsNullOrEmpty(item.ImportBlockRule))
                    item.ImportBlockRule = KnowledgeBaseModel.Instance.BlockRule;
            }

            this.Closing += MainWindow_Closing;
        }

        private void RowDefinition_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }
        private void Minimum_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void TopWindow_Click(object sender, RoutedEventArgs e)
        {
            is_top = !is_top;
            this.Topmost = !this.Topmost;
            this.border_top.BorderThickness = is_top ? new Thickness(0, 0, 0, 2) : new Thickness(0);
        }



        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            AppSettingsManager.SaveSettings(KnowledgeBaseModel.Instance);
        }

        // 提供静态方法设置状态栏信息
        public static void SetStatusMessage(string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var mainWin = Application.Current.MainWindow as MainWindow;
                if (mainWin != null)
                {
                    var vm = mainWin.DataContext as MainWindowModel;
                    if (vm != null)
                        vm.MainModel.ViewInfo = message;
                }

                // 重置定时器：5秒后清空状态信息
                if (_statusTimer == null)
                {
                    _statusTimer = new System.Windows.Threading.DispatcherTimer();
                    _statusTimer.Interval = TimeSpan.FromSeconds(5);
                    _statusTimer.Tick += (s, e) =>
                    {
                        _statusTimer.Stop();
                        var win = Application.Current.MainWindow as MainWindow;
                        if (win != null)
                        {
                            var viewModel = win.DataContext as MainWindowModel;
                            if (viewModel != null)
                                viewModel.MainModel.ViewInfo = string.Empty;
                        }
                    };
                }
                else
                {
                    _statusTimer.Stop();
                }
                _statusTimer.Start();
            });
        }

        public static void ScrollChatToEnd()
        {
            Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                var mainWin = Application.Current.MainWindow as MainWindow;
                mainWin?.ChatScrollViewer?.ScrollToEnd();
            }), System.Windows.Threading.DispatcherPriority.Background);
        }


    }
}
