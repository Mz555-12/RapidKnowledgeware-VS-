using IOC;
using RapidKnowledgeware.Base;
using RapidKnowledgeware.Models;
using RapidKnowledgeware.DAO;
using RapidKnowledgeware.Services;
using RapidKnowledgeware.ViewModels;
using RapidKnowledgeware.ViewModels.Impl;
using RapidKnowledgeware.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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


            var vm = BeanFactory.GetBean<IMainWindowModel>();
            this.DataContext = vm;

            this.Tag = vm;   // 将 ViewModel 存入 Tag，供菜单绑定使用

            // 传递 SpaceAdjustView 弹出层引用（注意这里的名称与 XAML 中 x:Name 一致）
            vm.SetSpaceAdjustViewReferences(this.SpaceAdjustOverlay, this.SpaceAdjustViewControl);

            // 加载设置到 KnowledgeBaseModel
            BeanFactory.GetBean<IAppSettingsRepository>().LoadSettings(KnowledgeBaseModel.Instance);
            foreach (var item in KnowledgeBaseModel.Instance.FileItems)
            {
                if (string.IsNullOrEmpty(item.ImportBlockRule))
                    item.ImportBlockRule = KnowledgeBaseModel.Instance.Default_BlockRule;
            }


            var settingsChangeTracker = BeanFactory.GetBean<ISettingsChangeTracker>();
            var llmAdjustService = BeanFactory.GetBean<ILLMAdjustService>();
            settingsChangeTracker.CaptureSnapshot(KnowledgeBaseModel.Instance);
            settingsChangeTracker.CaptureSnapshot(llmAdjustService.Current);

            this.Closing += MainWindow_Closing;


            BeanFactory.GetBean<IAppSettingsRepository>().LoadSettings(vm.MainModel);
            ApplyInitialLayout();

            System.Threading.Tasks.Task.Run(() =>
            {
                var instance = BeanFactory.GetBean<IKnowledgeBaseService>().RagServiceInstance;
                Debug.WriteLine($"[MainWindow] RagService 预热完成，索引块数 {instance.IndexedChunkCount}");
            });


            _ = BeanFactory.GetBean<IOllamaModelService>().LoadModelsAsync();


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
            if (vm == null) return;

            vm.OnWindowClosing();

            vm.MainModel.IsExpanded = _isExpanded;
            BeanFactory.GetBean<IAppSettingsRepository>().SaveSettings(vm.MainModel);
            BeanFactory.GetBean<IAppSettingsRepository>().SaveSettings(KnowledgeBaseModel.Instance);
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

            // 向上查找 Border
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
        /// 会话项右键菜单打开时检查：若空间参数视图处于显示状态，则阻止菜单弹出）
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

                // 标题行（窗口顶部）缩减
                var titleAnim = new GridLengthAnimation
                {
                    From = new GridLength(50, GridUnitType.Pixel),
                    To = new GridLength(0, GridUnitType.Pixel),
                    Duration = TimeSpan.FromMilliseconds(300)
                };
                Storyboard.SetTarget(titleAnim, TitleRow);
                Storyboard.SetTargetProperty(titleAnim, new PropertyPath("Height"));
                storyboard.Children.Add(titleAnim);

                // 信息行（窗口底部）缩减
                var infoAnim = new GridLengthAnimation
                {
                    From = new GridLength(30, GridUnitType.Pixel),
                    To = new GridLength(0, GridUnitType.Pixel),
                    Duration = TimeSpan.FromMilliseconds(300)
                };
                Storyboard.SetTarget(infoAnim, InfoRow);
                Storyboard.SetTargetProperty(infoAnim, new PropertyPath("Height"));
                storyboard.Children.Add(infoAnim);

                // 左侧列缩减
                var leftAnim = new GridLengthAnimation
                {
                    From = new GridLength(LeftColFixedWidth, GridUnitType.Pixel),
                    To = new GridLength(0, GridUnitType.Pixel),
                    Duration = TimeSpan.FromMilliseconds(300)
                };
                Storyboard.SetTarget(leftAnim, LeftCol);
                Storyboard.SetTargetProperty(leftAnim, new PropertyPath("Width"));
                storyboard.Children.Add(leftAnim);

                // 分隔条缩减
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
                var borderAnim = new ThicknessAnimation(new Thickness(0), TimeSpan.FromMilliseconds(300));
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
                ContentHeaderRow.Height = new GridLength(40);
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

        private void CodeBlock_Loaded(object sender, RoutedEventArgs e)
        {
            var rtb = sender as RichTextBox;
            var block = rtb?.DataContext as MessageBlock;
            if (block == null) return;

            string text = block.Content;

            // 色彩定义
            var keywordBrush = new SolidColorBrush(Color.FromRgb(0x8B, 0x00, 0xB4)); // 关键字 紫色
            var typeBrush = new SolidColorBrush(Color.FromRgb(0x2B, 0x8B, 0x57)); // 类型 青绿
            var builtinBrush = new SolidColorBrush(Color.FromRgb(0x00, 0x66, 0xCC)); // 内置函数/系统过程 亮蓝
            var commentBrush = new SolidColorBrush(Color.FromRgb(0x6B, 0x72, 0x80)); // 注释 灰色
            var stringBrush = new SolidColorBrush(Color.FromRgb(0x1A, 0x7F, 0x3A)); // 字符串 绿色
            var numberBrush = new SolidColorBrush(Color.FromRgb(0x8B, 0x00, 0xB4)); // 数字 紫色
            var operatorBrush = new SolidColorBrush(Color.FromRgb(0x3B, 0x3B, 0x3B)); // 操作符/标点 深灰
            var plainBrush = new SolidColorBrush(Color.FromRgb(0x1F, 0x29, 0x37)); // 普通标识符 近黑

            // 新增专属颜色
            var speedBrush = new SolidColorBrush(Color.FromRgb(0xC0, 0x5B, 0x0F)); // 速度 橙棕色
            var zoneBrush = new SolidColorBrush(Color.FromRgb(0xA0, 0x52, 0xCF)); // 转弯量 紫罗兰
            var toolBrush = new SolidColorBrush(Color.FromRgb(0x00, 0x7B, 0xA0)); // 工具/工件 蓝绿色

            // 关键字集
            var keywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "MODULE", "ENDMODULE", "PROC", "ENDPROC", "FUNC", "ENDFUNC",
        "IF", "THEN", "ELSE", "ELSEIF", "ENDIF",
        "FOR", "ENDFOR", "WHILE", "ENDWHILE", "DO", "ENDDO",
        "TEST", "ENDTEST", "CASE", "DEFAULT",
        "RETURN", "EXIT", "GOTO", "RETRY",
        "VAR", "CONST", "PERS", "LOCAL", "AND", "OR", "NOT",
        "TRUE", "FALSE",
        "MoveJ", "MoveL", "MoveC", "WaitTime", "SetDO", "SetAO", "WaitDI", "PulseDO",
        "FROM", "TO", "DOWNTO", "BY", "MOD"
    };

            // 数据类型
            var types = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "num", "bool", "string", "robtarget", "speeddata", "zonedata",
        "tooldata", "wobjdata"
    };

            // 内置函数
            var builtins = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Offs", "RelTool", "Sin", "Cos", "ATan2", "Sqrt", "Abs",
        "NumToStr", "BoolToStr", "TPWrite", "SetGO"
    };

            var paragraph = new Paragraph();
            int len = text.Length;
            int pos = 0;

            while (pos < len)
            {
                char c = text[pos];

                // 换行
                if (c == '\r' || c == '\n')
                {
                    if (c == '\r' && pos + 1 < len && text[pos + 1] == '\n') pos++;
                    paragraph.Inlines.Add(new LineBreak());
                    pos++;
                    continue;
                }

                // 注释
                if (c == '!')
                {
                    int start = pos;
                    while (pos < len && text[pos] != '\r' && text[pos] != '\n') pos++;
                    paragraph.Inlines.Add(new Run(text.Substring(start, pos - start))
                    {
                        Foreground = commentBrush,
                        FontStyle = FontStyles.Italic
                    });
                    continue;
                }

                // 字符串
                if (c == '"')
                {
                    int start = pos;
                    pos++;
                    while (pos < len && text[pos] != '"')
                    {
                        if (text[pos] == '\\') pos++;
                        if (pos < len) pos++;
                    }
                    if (pos < len) pos++;
                    paragraph.Inlines.Add(new Run(text.Substring(start, pos - start)) { Foreground = stringBrush });
                    continue;
                }

                // 数字（增强科学计数法识别）
                if (char.IsDigit(c))
                {
                    int start = pos;
                    while (pos < len && (char.IsDigit(text[pos]) || text[pos] == '.')) pos++;
                    // 检测科学计数法后缀：E/e [+-]? 数字
                    if (pos < len && (text[pos] == 'E' || text[pos] == 'e'))
                    {
                        pos++;
                        if (pos < len && (text[pos] == '+' || text[pos] == '-')) pos++;
                        while (pos < len && char.IsDigit(text[pos])) pos++;
                    }
                    paragraph.Inlines.Add(new Run(text.Substring(start, pos - start)) { Foreground = numberBrush });
                    continue;
                }

                // 操作符/标点
                if ("+-*/=:;,()[]{}<>@#".Contains(c))
                {
                    paragraph.Inlines.Add(new Run(c.ToString()) { Foreground = operatorBrush });
                    pos++;
                    continue;
                }

                // 标识符（单词）
                if (char.IsLetter(c) || c == '_')
                {
                    int start = pos;
                    while (pos < len && (char.IsLetterOrDigit(text[pos]) || text[pos] == '_')) pos++;
                    string word = text.Substring(start, pos - start);
                    var run = new Run(word);

                    // 匹配专用模式（优先于关键字/类型）
                    if (Regex.IsMatch(word, @"^v\d+$", RegexOptions.IgnoreCase))          // 速度 v500
                    {
                        run.Foreground = speedBrush;
                    }
                    else if (Regex.IsMatch(word, @"^z\d+$", RegexOptions.IgnoreCase) ||   // 转弯量 z50 / fine
                             word.Equals("fine", StringComparison.OrdinalIgnoreCase))
                    {
                        run.Foreground = zoneBrush;
                    }
                    else if (Regex.IsMatch(word, @"^(tool|wobj)\w*$", RegexOptions.IgnoreCase))  // 工具/工件
                    {
                        run.Foreground = toolBrush;
                    }
                    else if (keywords.Contains(word))
                    {
                        run.Foreground = keywordBrush;
                    }
                    else if (types.Contains(word))
                    {
                        run.Foreground = typeBrush;
                    }
                    else if (builtins.Contains(word))
                    {
                        run.Foreground = builtinBrush;
                    }
                    else
                    {
                        run.Foreground = plainBrush;
                    }
                    paragraph.Inlines.Add(run);
                    continue;
                }

                // 其他字符
                paragraph.Inlines.Add(new Run(c.ToString()) { Foreground = plainBrush });
                pos++;
            }

            rtb.Document = new FlowDocument(paragraph)
            {
                PagePadding = new Thickness(0),
                LineStackingStrategy = LineStackingStrategy.BlockLineHeight,
                LineHeight = 18
            };
        }

    }
}
