using RapidKnowledgeware.Base;
using RapidKnowledgeware.Functions.MainWindowFunc;
using RapidKnowledgeware.Models;
using RapidKnowledgeware.ViewModels;
using RapidKnowledgeware.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
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
        private bool is_check = false;
        private static System.Windows.Threading.DispatcherTimer _statusTimer;

        private bool _isExpanded = false;
        private double _leftColWidth, _splitColWidth;
        public MainWindow()
        {
            InitializeComponent();
            MainModel.Loading_Grid = this.Loading_Grid;
            MainModel.CloseWindow_Btn = this.CloseWindow_Btn;
            MainModel.TopWindow_Btn = this.TopWindow_Btn;
            MainModel.Minimum_Btn = this.Minimum_Btn;
            MainModel.OpenSpaceParametersView_Btn = this.OpenSpaceParametersView_Btn;
            MainModel.OpenLoggingView_Btn = this.OpenLoggingView_Btn;


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
                    item.ImportBlockRule = KnowledgeBaseModel.Instance.Default_BlockRule;
            }


            // === 捕获全局模型的初始快照 ===
            Functions.MainWindowFunc.SettingsChangeTracker.CaptureSnapshot(KnowledgeBaseModel.Instance);
            Functions.MainWindowFunc.SettingsChangeTracker.CaptureSnapshot(Functions.LLMAdjustFunc.LLMAdjustService.Current);

            this.Closing += MainWindow_Closing;


            // 加载持久化的窗口布局状态
            AppSettingsManager.LoadSettings(vm.MainModel);
            ApplyInitialLayout();

            // 后台预热 RagService，避免首次调用时的冷启动延迟
            System.Threading.Tasks.Task.Run(() =>
            {
                var instance = RapidKnowledgeware.Functions.KnowledgeBaseFunc.KnowledgeBaseService.RagServiceInstance;
                Debug.WriteLine($"[MainWindow] RagService 预热完成，索引块数: {instance.IndexedChunkCount}");
            });


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




        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var vm = DataContext as MainWindowModel;
            vm.MainModel.IsExpanded = _isExpanded;
            AppSettingsManager.SaveSettings(vm.MainModel);
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


        /// <summary>
        /// 输入框文本变化时动态调整父 Border 高度（带滞后避免频繁跳动）
        /// </summary>
        /// <summary>
        /// 输入框文本变化时动态调整父 Border 高度（检测滚动条是否出现）
        /// </summary>
        private void InputBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var textBox = sender as TextBox;
            if (textBox == null) return;

            // 向上查找父 Border
            DependencyObject parent = VisualTreeHelper.GetParent(textBox);
            while (parent != null && !(parent is Border))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            var border = parent as Border;
            if (border == null) return;

            // 延迟执行确保布局更新
            textBox.Dispatcher.BeginInvoke(new Action(() =>
            {
                textBox.UpdateLayout();

                // 获取滚动条是否可见：ExtentHeight > ViewportHeight 表示内容超出
                bool scrollBarVisible = textBox.ExtentHeight > textBox.ViewportHeight;
                double neededHeight = textBox.ExtentHeight;

                if (border.Height == 110 && scrollBarVisible)
                {
                    border.Height = 260;
                }
                else if (border.Height == 260 && neededHeight < 50)
                {
                    border.Height = 110;
                }
            }), System.Windows.Threading.DispatcherPriority.Background);
        }


        private void LogViewButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton btn)
            {
                is_check = !is_check;
            }
        }

        private void SpaceViewButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton btn)
            {
                is_check = !is_check;
            }
        }

        private void TopWindow_Click(object sender, RoutedEventArgs e)
        {
            is_top = !is_top;
            this.Topmost = is_top;

            if (sender is ToggleButton btn)
            {
                btn.IsChecked = is_top;
            }
        }

        /// <summary>
        /// 会话项右键菜单打开时检查：若空间参数视图处于显示状态，则阻止菜单弹出。
        /// </summary>
        private void ContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            var contextMenu = sender as ContextMenu;
            if (contextMenu == null) return;

            var placementTarget = contextMenu.PlacementTarget as FrameworkElement;
            if (placementTarget == null) return;

            // 通过 Tag 获取主窗口的 DataContext（即 MainWindowModel）
            var window = Window.GetWindow(placementTarget) as MainWindow;
            if (window == null) return;

            var vm = window.DataContext as MainWindowModel;
            if (vm != null && vm.IsSpaceAdjustVisible)
            {
                // 空间参数视图可见时，立即关闭右键菜单
                contextMenu.IsOpen = false;
                e.Handled = true;
            }
        }



        /// <summary>
        /// 窗口展开/恢复按钮点击
        /// </summary>
        private void ExpandButton_Click(object sender, RoutedEventArgs e)
        {
            const double LeftColFixedWidth = 229;
            double splitColWidth = SplitCol.ActualWidth;

            if (!_isExpanded)
            {
                // ===== 展开 =====
                // 切换头部
                NormalHeader.Visibility = Visibility.Collapsed;
                ExpandedHeader.Visibility = Visibility.Visible;

                var storyboard = new Storyboard();

                // 标题行（窗口顶部）缩到0
                var titleAnim = new GridLengthAnimation
                {
                    From = new GridLength(50, GridUnitType.Pixel),
                    To = new GridLength(0, GridUnitType.Pixel),
                    Duration = TimeSpan.FromMilliseconds(300)
                };
                Storyboard.SetTarget(titleAnim, TitleRow);
                Storyboard.SetTargetProperty(titleAnim, new PropertyPath("Height"));
                storyboard.Children.Add(titleAnim);

                // 信息行（窗口底部）缩到0
                var infoAnim = new GridLengthAnimation
                {
                    From = new GridLength(30, GridUnitType.Pixel),
                    To = new GridLength(0, GridUnitType.Pixel),
                    Duration = TimeSpan.FromMilliseconds(300)
                };
                Storyboard.SetTarget(infoAnim, InfoRow);
                Storyboard.SetTargetProperty(infoAnim, new PropertyPath("Height"));
                storyboard.Children.Add(infoAnim);

                // 左侧列缩到0
                var leftAnim = new GridLengthAnimation
                {
                    From = new GridLength(LeftColFixedWidth, GridUnitType.Pixel),
                    To = new GridLength(0, GridUnitType.Pixel),
                    Duration = TimeSpan.FromMilliseconds(300)
                };
                Storyboard.SetTarget(leftAnim, LeftCol);
                Storyboard.SetTargetProperty(leftAnim, new PropertyPath("Width"));
                storyboard.Children.Add(leftAnim);

                // 分隔条缩到0
                var splitAnim = new GridLengthAnimation
                {
                    From = new GridLength(splitColWidth, GridUnitType.Pixel),
                    To = new GridLength(0, GridUnitType.Pixel),
                    Duration = TimeSpan.FromMilliseconds(300)
                };
                Storyboard.SetTarget(splitAnim, SplitCol);
                Storyboard.SetTargetProperty(splitAnim, new PropertyPath("Width"));
                storyboard.Children.Add(splitAnim);

                // 右侧内容头部行从30变为50
                var headerHeightAnim = new GridLengthAnimation
                {
                    From = new GridLength(30, GridUnitType.Pixel),
                    To = new GridLength(50, GridUnitType.Pixel),
                    Duration = TimeSpan.FromMilliseconds(300)
                };
                Storyboard.SetTarget(headerHeightAnim, ContentHeaderRow);
                Storyboard.SetTargetProperty(headerHeightAnim, new PropertyPath("Height"));
                storyboard.Children.Add(headerHeightAnim);

                // 内容边框变为有圆角（展开状态）
                var borderAnim = new ThicknessAnimation(new Thickness(5), TimeSpan.FromMilliseconds(300));
                Storyboard.SetTarget(borderAnim, ContentBorder);
                Storyboard.SetTargetProperty(borderAnim, new PropertyPath("BorderThickness"));
                storyboard.Children.Add(borderAnim);

                storyboard.Completed += (s, ev) =>
                {
                    ContentBorder.CornerRadius = new CornerRadius(5);
                    // 无需再改按钮内容，因为ExpandedHeader里的按钮已经是展开图标
                };
                storyboard.Begin();
            }
            else
            {
                // ===== 恢复 =====
                // 切换头部
                ExpandedHeader.Visibility = Visibility.Collapsed;
                NormalHeader.Visibility = Visibility.Visible;

                var storyboard = new Storyboard();

                var titleAnim = new GridLengthAnimation
                {
                    From = new GridLength(0, GridUnitType.Pixel),
                    To = new GridLength(50, GridUnitType.Pixel),
                    Duration = TimeSpan.FromMilliseconds(300)
                };
                Storyboard.SetTarget(titleAnim, TitleRow);
                Storyboard.SetTargetProperty(titleAnim, new PropertyPath("Height"));
                storyboard.Children.Add(titleAnim);

                var infoAnim = new GridLengthAnimation
                {
                    From = new GridLength(0, GridUnitType.Pixel),
                    To = new GridLength(30, GridUnitType.Pixel),
                    Duration = TimeSpan.FromMilliseconds(300)
                };
                Storyboard.SetTarget(infoAnim, InfoRow);
                Storyboard.SetTargetProperty(infoAnim, new PropertyPath("Height"));
                storyboard.Children.Add(infoAnim);

                var leftAnim = new GridLengthAnimation
                {
                    From = new GridLength(0, GridUnitType.Pixel),
                    To = new GridLength(LeftColFixedWidth, GridUnitType.Pixel),
                    Duration = TimeSpan.FromMilliseconds(300)
                };
                Storyboard.SetTarget(leftAnim, LeftCol);
                Storyboard.SetTargetProperty(leftAnim, new PropertyPath("Width"));
                storyboard.Children.Add(leftAnim);

                var splitAnim = new GridLengthAnimation
                {
                    From = new GridLength(0, GridUnitType.Pixel),
                    To = new GridLength(splitColWidth, GridUnitType.Pixel),
                    Duration = TimeSpan.FromMilliseconds(300)
                };
                Storyboard.SetTarget(splitAnim, SplitCol);
                Storyboard.SetTargetProperty(splitAnim, new PropertyPath("Width"));
                storyboard.Children.Add(splitAnim);

                // 右侧内容头部行从50变回30
                var headerHeightAnim = new GridLengthAnimation
                {
                    From = new GridLength(50, GridUnitType.Pixel),
                    To = new GridLength(30, GridUnitType.Pixel),
                    Duration = TimeSpan.FromMilliseconds(300)
                };
                Storyboard.SetTarget(headerHeightAnim, ContentHeaderRow);
                Storyboard.SetTargetProperty(headerHeightAnim, new PropertyPath("Height"));
                storyboard.Children.Add(headerHeightAnim);

                var borderAnim = new ThicknessAnimation(new Thickness(0), TimeSpan.FromMilliseconds(300));
                Storyboard.SetTarget(borderAnim, ContentBorder);
                Storyboard.SetTargetProperty(borderAnim, new PropertyPath("BorderThickness"));
                storyboard.Children.Add(borderAnim);

                storyboard.Completed += (s, ev) =>
                {
                    LeftCol.Width = new GridLength(LeftColFixedWidth, GridUnitType.Pixel);
                    SplitCol.Width = GridLength.Auto;
                    ContentHeaderRow.Height = new GridLength(30, GridUnitType.Pixel);
                    ContentBorder.CornerRadius = new CornerRadius(0);
                };
                storyboard.Begin();
            }
            _isExpanded = !_isExpanded;
            ((MainWindowModel)DataContext).MainModel.IsExpanded = _isExpanded;
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn == null) return;

            var code = btn.CommandParameter as string;
            if (!string.IsNullOrEmpty(code))
            {
                try { Clipboard.SetText(code); }
                catch (Exception ex) { Debug.WriteLine($"复制失败: {ex.Message}"); }
            }

            // 保存原始图标，切换到 ✓
            object originalContent = btn.Content;
            btn.Content = "✓";

            // 2秒后恢复
            var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
            timer.Tick += (s, args) =>
            {
                btn.Content = originalContent;
                timer.Stop();
            };
            timer.Start();
        }

        private void ApplyInitialLayout()
        {
            var expanded = ((MainWindowModel)DataContext).MainModel.IsExpanded;
            if (expanded)
            {
                TitleRow.Height = new GridLength(0);
                InfoRow.Height = new GridLength(0);
                LeftCol.Width = new GridLength(0);
                SplitCol.Width = new GridLength(0);
                ContentHeaderRow.Height = new GridLength(50);
                ContentBorder.BorderThickness = new Thickness(5);
                ContentBorder.CornerRadius = new CornerRadius(5);
                NormalHeader.Visibility = Visibility.Collapsed;
                ExpandedHeader.Visibility = Visibility.Visible;
                _isExpanded = true;
            }
            else
            {
                TitleRow.Height = new GridLength(50);
                InfoRow.Height = new GridLength(30);
                LeftCol.Width = new GridLength(229);
                SplitCol.Width = GridLength.Auto;
                ContentHeaderRow.Height = new GridLength(30);
                ContentBorder.BorderThickness = new Thickness(0);
                ContentBorder.CornerRadius = new CornerRadius(0);
                NormalHeader.Visibility = Visibility.Visible;
                ExpandedHeader.Visibility = Visibility.Collapsed;
                _isExpanded = false;
            }
        }

    }
}
