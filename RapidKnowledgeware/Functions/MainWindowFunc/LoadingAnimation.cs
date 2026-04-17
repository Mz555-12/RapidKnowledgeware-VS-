using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RapidKnowledgeware.Functions.MainWindowFunc
{
    public class LoadingAnimation
    {
        public static void Show_Loading()
        {
            MainModel.Loading_Grid.Visibility = Visibility.Visible;
        }

        public static  void Hide_Loading()
        {
            MainModel.Loading_Grid.Visibility = Visibility.Collapsed;
        }
    }
}
