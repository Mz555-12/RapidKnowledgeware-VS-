using RapidKnowledgeware.ViewModels;
using RapidKnowledgeware.ViewModels.Impl;
using System.Windows.Controls;

namespace RapidKnowledgeware.Views
{
    public partial class FileBlockView : UserControl
    {
        public FileBlockView()
        {
            InitializeComponent();
            this.DataContext = KnowledgeBaseViewModel.Instance;
        }
    }
}
