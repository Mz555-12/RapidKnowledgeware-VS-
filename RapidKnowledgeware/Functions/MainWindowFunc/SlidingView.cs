using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace RapidKnowledgeware.Functions.MainWindowFunc
{
    public static class SlidingView
    {
        /// <summary>
        /// 从顶部滑落到底部（显示）
        /// </summary>
        public static void SlideDownToBottom(FrameworkElement view, FrameworkElement overlayContainer)
        {
            overlayContainer.Visibility = Visibility.Visible;
            var transform = view.RenderTransform as TranslateTransform;
            if (transform == null) return;

            // 确保在布局完成后启动动画
            view.Dispatcher.BeginInvoke(new Action(() =>
            {
                double height = view.ActualHeight;
                if (height > 0)
                {
                    transform.Y = -height;
                    var anim = new DoubleAnimation(0, TimeSpan.FromMilliseconds(350))
                    {
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    transform.BeginAnimation(TranslateTransform.YProperty, anim);
                }
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        /// <summary>
        /// 从底部滑回到顶部（隐藏），完成后将容器设为 Collapsed
        /// </summary>
        public static void SlideUpToTop(FrameworkElement view, FrameworkElement overlayContainer)
        {
            var transform = view.RenderTransform as TranslateTransform;
            if (transform == null) return;

            view.Dispatcher.BeginInvoke(new Action(() =>
            {
                double height = view.ActualHeight;
                if (height > 0)
                {
                    var anim = new DoubleAnimation(-height, TimeSpan.FromMilliseconds(350))
                    {
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    anim.Completed += (s, e) =>
                    {
                        overlayContainer.Visibility = Visibility.Collapsed;
                        Debug.WriteLine("[SlidingView] 滑回完成，容器已隐藏");
                    };
                    transform.BeginAnimation(TranslateTransform.YProperty, anim);
                }
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }
    
        /// <summary>
        /// 从左侧滑入（显示）
        /// </summary>
        public static void SlideInFromLeft(FrameworkElement view, FrameworkElement overlayContainer)
        {
            overlayContainer.Visibility = Visibility.Visible;
            var transform = view.RenderTransform as TranslateTransform;
            if (transform == null) return;

            view.Dispatcher.BeginInvoke(new Action(() =>
            {
                double width = view.ActualWidth;
                if (width > 0)
                {
                    transform.X = -width;
                    var anim = new DoubleAnimation(0, TimeSpan.FromMilliseconds(350))
                    {
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    transform.BeginAnimation(TranslateTransform.XProperty, anim);
                }
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        /// <summary>
        /// 向右侧滑出（隐藏），完成后将容器设为 Collapsed
        /// </summary>
        public static void SlideOutToRight(FrameworkElement view, FrameworkElement overlayContainer)
        {
            var transform = view.RenderTransform as TranslateTransform;
            if (transform == null) return;

            view.Dispatcher.BeginInvoke(new Action(() =>
            {
                double width = view.ActualWidth;
                if (width > 0)
                {
                    var anim = new DoubleAnimation(-width, TimeSpan.FromMilliseconds(350))
                    {
                        EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
                    };
                    anim.Completed += (s, e) =>
                    {
                        overlayContainer.Visibility = Visibility.Collapsed;
                        Debug.WriteLine("[SlidingView] 向右滑出完成，容器已隐藏");
                    };
                    transform.BeginAnimation(TranslateTransform.XProperty, anim);
                }
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }
    }
}