using IOC;
using RapidKnowledgeware.ViewModels;
using System.Windows.Controls;

namespace RapidKnowledgeware.Views
{
    public partial class LoggingView : UserControl
    {
        public ILoggingViewModel ViewModel { get; private set; }

        public LoggingView()
        {
            InitializeComponent();
            ViewModel = BeanFactory.GetBean<ILoggingViewModel>();
            this.DataContext = ViewModel;
        }
    }
}
