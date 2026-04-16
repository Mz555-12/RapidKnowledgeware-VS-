using GalaSoft.MvvmLight;

public class MainModel : ObservableObject
{
    private string _viewInfo;
    public string ViewInfo
    {
        get => _viewInfo;
        set { _viewInfo = value; RaisePropertyChanged(); }
    }
}