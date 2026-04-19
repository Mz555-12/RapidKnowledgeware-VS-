using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RapidKnowledgeware.Functions.MainWindowFunc
{
    public class WindowControls
    {
        public static void Show_Loading()
        {
            MainModel.Loading_Grid.Visibility = Visibility.Visible;
        }

        public static  void Hide_Loading()
        {
            MainModel.Loading_Grid.Visibility = Visibility.Collapsed;
        }

        public static void Show_Title()
        {
           MainModel.CloseWindow_Btn.Visibility = Visibility.Visible;
            MainModel.Minimum_Btn.Visibility = Visibility.Visible;
            MainModel.TopWindow_Border.Visibility = Visibility.Visible;
            MainModel.OpenSpaceParametersView_Btn.Visibility = Visibility.Visible;

        }

        public static void Hide_Title(int count = 1)
        {

            switch (count)
            {
                case 0:
                    MainModel.CloseWindow_Btn.Visibility = Visibility.Collapsed;
                    MainModel.Minimum_Btn.Visibility = Visibility.Collapsed;
                    MainModel.TopWindow_Border.Visibility = Visibility.Collapsed;
                    MainModel.OpenSpaceParametersView_Btn.Visibility = Visibility.Collapsed;
                    break;

                case 1:
                    MainModel.CloseWindow_Btn.Visibility = Visibility.Collapsed;
                    MainModel.Minimum_Btn.Visibility = Visibility.Collapsed;
                    MainModel.TopWindow_Border.Visibility = Visibility.Collapsed;
                    break;

            }

        }
    }
}
