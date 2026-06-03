using IOC;
using RapidKnowledgeware.ViewModels;
using System.Windows.Controls;

namespace RapidKnowledgeware.Views
{
    public partial class LLMAdjustView : UserControl
    {
        public LLMAdjustView()
        {
            InitializeComponent();
            this.DataContext = BeanFactory.GetBean<ILLMAdjustViewModel>();
        }
    }
}
