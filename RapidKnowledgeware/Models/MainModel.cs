using GalaSoft.MvvmLight;
using System.Windows;
using System.Windows.Controls;

public class MainModel : ObservableObject
{
    public static Grid Loading_Grid { get; set; }

    private string _viewInfo;
    public string ViewInfo
    {
        get => _viewInfo;
        set { _viewInfo = value; RaisePropertyChanged(); }
    }


    public MainModel()
    {
        Loading_Grid.Visibility = Visibility.Collapsed;
    }
}