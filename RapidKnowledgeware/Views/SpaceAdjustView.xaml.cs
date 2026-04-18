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

namespace RapidKnowledgeware.Views
{
    /// <summary>
    /// SpaceAdjustView.xaml 的交互逻辑
    /// </summary>
    public partial class SpaceAdjustView : UserControl
    {
        public SpaceAdjustView()
        {
            InitializeComponent();
            this.DataContextChanged += (s, e) =>
            {
                System.Diagnostics.Debug.WriteLine($"[SpaceAdjustView] DataContext 变为: {DataContext?.GetType().Name ?? "null"}");
            };
        }
    }
}
