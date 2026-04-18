
using RapidKnowledgeware.Base;
using RapidKnowledgeware.Functions.MainWindowFunc;
using RapidKnowledgeware.Models;
using RapidKnowledgeware.Properties;
using RapidKnowledgeware.Views;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;


namespace RapidKnowledgeware.ViewModels
{
    public class MainWindowModel
    {
        public MainModel MainModel { get; set; } = new MainModel();
        private CommandBase _closeMainWindowCommand;

        public CommandBase CloseMainWindowCommand
        {
            get
            {
                if (_closeMainWindowCommand == null)
                {
                    _closeMainWindowCommand = new CommandBase();
                    _closeMainWindowCommand.DoExecute = new Action<object>((o) =>
                    {
                        AppSettingsManager.SaveSettings(KnowledgeBaseModel.Instance);
                        (o as Window).Close();
                    });
                }
                return _closeMainWindowCommand;
            }
        }


        private CommandBase _openSpaceParametersViewCommand;
        private bool _isSpaceViewVisible = false;  // 状态标志
        public CommandBase OpenSpaceParametersViewCommand
        {
            get
            {
                if (_openSpaceParametersViewCommand == null)
                {
                    _openSpaceParametersViewCommand = new CommandBase();
                    _openSpaceParametersViewCommand.DoExecute = new Action<object>((o) =>
                    {

                        var mainWindow = Application.Current.MainWindow as MainWindow;
                        if (mainWindow == null)
                        {
                            return;
                        }

                        var overlay = mainWindow.FindName("OverlayContainer") as Grid;
                        var spaceView = mainWindow.FindName("SpaceView") as SpaceParametersView;

                        if (overlay == null || spaceView == null)
                        {
                            return;
                        }

                        if (!_isSpaceViewVisible)
                        {   
                            
                            WindowControls.Show_Title();

                            // 当前隐藏 → 显示（滑落）
                            Debug.WriteLine("[命令] 执行滑落动画");
                            SlidingView.SlideInFromLeft(spaceView, overlay);


                            _isSpaceViewVisible = true;
                        }
                        else
                        {
                            WindowControls.Hide_Title();

                            // 当前显示 → 隐藏
                            Debug.WriteLine("[命令] 执行滑回动画");
                            SlidingView.HideImmediately(spaceView, overlay);
                            AppSettingsManager.SaveSettings(KnowledgeBaseModel.Instance);
                            _isSpaceViewVisible = false;
                        }
                    });
                }
                return _openSpaceParametersViewCommand;
            }
        }


       
    }
}
