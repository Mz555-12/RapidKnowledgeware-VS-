using GalaSoft.MvvmLight;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

/// <summary>
/// 主窗口静态控件引用及状态信息模型
/// </summary>
public class MainModel : ObservableObject
{
    /// <summary>
    /// 加载动画覆盖层
    /// </summary>
    public static Grid Loading_Grid { get; set; }



    /// <summary>
    /// 最小化按钮
    /// </summary>
    public static Button Minimum_Btn { get; set; }

    /// <summary>
    /// 关闭按钮
    /// </summary>
    public static Button CloseWindow_Btn { get; set; }

    /// <summary>
    /// 置顶按钮边框
    /// </summary>
    public static ToggleButton TopWindow_Btn { get; set; }

    /// <summary>
    /// 打开空间参数视图按钮
    /// </summary>
    public static ToggleButton OpenSpaceParametersView_Btn { get; set; }
    public static ToggleButton OpenLoggingView_Btn { get; set; }

    private string _viewInfo;
    /// <summary>
    /// 状态栏显示信息
    /// </summary>
    public string ViewInfo
    {
        get => _viewInfo;
        set { _viewInfo = value; RaisePropertyChanged(); }
    }

    private bool _isExpanded;
    /// <summary>
    /// 窗口是否处于展开状态
    /// </summary>
    public bool IsExpanded
    {
        get => _isExpanded;
        set { _isExpanded = value; RaisePropertyChanged(); }
    }


    /// <summary>
    /// 初始化主窗口模型，确保加载动画初始隐藏
    /// </summary>
    public MainModel()
    {
        Loading_Grid.Visibility = Visibility.Collapsed;
    }
}