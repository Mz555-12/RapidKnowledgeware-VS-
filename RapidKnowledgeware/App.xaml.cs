using IOC;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace RapidKnowledgeware
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ServiceConfigurator.ConfigureServices(
                System.Reflection.Assembly.GetExecutingAssembly());

            var mainWindow = new MainWindow();
            mainWindow.Show();
        }
    }
}
