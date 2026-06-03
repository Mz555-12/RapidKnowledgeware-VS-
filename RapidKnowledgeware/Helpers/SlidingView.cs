using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace RapidKnowledgeware.Helpers
{
    /// <summary>
    /// 提供视图滑动动画（支持上、下、左、右方向），自动处理 TranslateTransform 和布局尺寸
    /// </summary>
    public static class SlidingView
    {
        private const double AnimationDurationMs = 350;
        private static readonly IEasingFunction DefaultEase = new QuadraticEase { EasingMode = EasingMode.EaseOut };

        /// <summary>
        /// 获取或创建视图的 TranslateTransform，并确保其被设置为 RenderTransform
        /// </summary>
        private static TranslateTransform EnsureTranslateTransform(FrameworkElement view)
        {
            var transform = view.RenderTransform as TranslateTransform;
            if (transform == null)
            {
                transform = new TranslateTransform();
                view.RenderTransform = transform;
                view.RenderTransformOrigin = new Point(0.5, 0.5);
            }
            return transform;
        }

        /// <summary>
        /// 安全获取视图的实际宽度（布局后），若为 0 则强制更新布局并测量
        /// </summary>
        private static double GetActualWidth(FrameworkElement view)
        {
            view.UpdateLayout();
            double width = view.ActualWidth;
            if (width <= 0)
            {
                view.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                width = view.DesiredSize.Width;
            }
            return width;
        }

        /// <summary>
        /// 安全获取视图的实际高度（布局后），若为 0 则强制更新布局并测量
        /// </summary>
        private static double GetActualHeight(FrameworkElement view)
        {
            view.UpdateLayout();
            double height = view.ActualHeight;
            if (height <= 0)
            {
                view.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                height = view.DesiredSize.Height;
            }
            return height;
        }

        /// <summary>
        /// 执行水平方向的平移动画（从 from 到 to）
        /// </summary>
        private static void AnimateX(TranslateTransform transform, double from, double to, Action onCompleted = null)
        {
            transform.BeginAnimation(TranslateTransform.XProperty, null);
            transform.X = from;

            var anim = new DoubleAnimation(to, TimeSpan.FromMilliseconds(AnimationDurationMs))
            {
                EasingFunction = DefaultEase
            };
            if (onCompleted != null)
                anim.Completed += (s, e) => onCompleted();

            transform.BeginAnimation(TranslateTransform.XProperty, anim);
        }

        /// <summary>
        /// 执行垂直方向的平移动画（从 from 到 to）
        /// </summary>
        private static void AnimateY(TranslateTransform transform, double from, double to, Action onCompleted = null)
        {
            transform.BeginAnimation(TranslateTransform.YProperty, null);
            transform.Y = from;

            var anim = new DoubleAnimation(to, TimeSpan.FromMilliseconds(AnimationDurationMs))
            {
                EasingFunction = DefaultEase
            };
            if (onCompleted != null)
                anim.Completed += (s, e) => onCompleted();

            transform.BeginAnimation(TranslateTransform.YProperty, anim);
        }

        /// <summary>
        /// 从顶部滑落到底部（显示），先显示容器，再将视图从 -height 滑动到 0
        /// </summary>
        public static void SlideDownToBottom(FrameworkElement view, FrameworkElement overlayContainer)
        {
            if (view == null || overlayContainer == null) return;
            overlayContainer.Visibility = Visibility.Visible;

            view.Dispatcher.BeginInvoke(new Action(() =>
            {
                var transform = EnsureTranslateTransform(view);
                double height = GetActualHeight(view);
                if (height <= 0) return;

                AnimateY(transform, -height, 0);
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        /// <summary>
        /// 从底部滑回到顶部（隐藏），完成后隐藏容器
        /// </summary>
        public static void SlideUpToTop(FrameworkElement view, FrameworkElement overlayContainer)
        {
            if (view == null || overlayContainer == null) return;

            view.Dispatcher.BeginInvoke(new Action(() =>
            {
                var transform = EnsureTranslateTransform(view);
                double height = GetActualHeight(view);
                if (height <= 0) return;

                AnimateY(transform, 0, -height, () =>
                {
                    overlayContainer.Visibility = Visibility.Collapsed;
                });
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        /// <summary>
        /// 从左侧滑入（显示），先显示容器，再将视图从 -width 滑动到 0
        /// </summary>
        public static void SlideInFromLeft(FrameworkElement view, FrameworkElement overlayContainer)
        {
            if (view == null || overlayContainer == null) return;
            overlayContainer.Visibility = Visibility.Visible;

            view.Dispatcher.BeginInvoke(new Action(() =>
            {
                var transform = EnsureTranslateTransform(view);
                double width = GetActualWidth(view);
                if (width <= 0) return;

                AnimateX(transform, -width, 0);
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        /// <summary>
        /// 向左侧滑出（隐藏），注意：方法名虽为 SlideOutToRight，但实际行为是向左滑出（-width）
        /// 这是为了保持与旧代码兼容，完成后隐藏容器
        /// </summary>
        public static void SlideOutToRight(FrameworkElement view, FrameworkElement overlayContainer)
        {
            if (view == null || overlayContainer == null) return;

            view.Dispatcher.BeginInvoke(new Action(() =>
            {
                var transform = EnsureTranslateTransform(view);
                double width = GetActualWidth(view);
                if (width <= 0) return;

                AnimateX(transform, 0, -width, () =>
                {
                    overlayContainer.Visibility = Visibility.Collapsed;
                });
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        /// <summary>
        /// 立即隐藏视图（无动画），并将容器设为 Collapsed
        /// </summary>
        public static void HideImmediately(FrameworkElement view, FrameworkElement overlayContainer)
        {
            if (view == null || overlayContainer == null) return;

            var transform = EnsureTranslateTransform(view);
            // 停止任何正在进行的动画
            transform.BeginAnimation(TranslateTransform.XProperty, null);
            transform.BeginAnimation(TranslateTransform.YProperty, null);

            // 将视图移出屏幕（根据现有方向优先处理 X 偏移）
            double width = GetActualWidth(view);
            if (width > 0)
                transform.X = -width;
            else
                transform.X = -1000; // 保底偏移

            overlayContainer.Visibility = Visibility.Collapsed;
        }

        /// <summary>
        /// 向右侧滑出（隐藏），完成后隐藏容器。这是真正的向右滑出（+width）
        /// </summary>
        public static void SlideOutToRight2(FrameworkElement view, FrameworkElement overlayContainer)
        {
            if (view == null || overlayContainer == null) return;

            view.Dispatcher.BeginInvoke(new Action(() =>
            {
                var transform = EnsureTranslateTransform(view);
                double width = GetActualWidth(view);
                if (width <= 0) return;

                AnimateX(transform, 0, width, () =>
                {
                    overlayContainer.Visibility = Visibility.Collapsed;
                });
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        /// <summary>
        /// 从右侧滑入（显示），先显示容器，再将视图从 +width 滑动到 0
        /// </summary>
        /// <param name="view">要滑入的视图</param>
        /// <param name="overlayContainer">容器</param>
        /// <param name="onCompleted">动画完成后的回调（可选）</param>
        public static void SlideInFromRight(FrameworkElement view, FrameworkElement overlayContainer, Action onCompleted = null)
        {
            if (view == null || overlayContainer == null) return;
            overlayContainer.Visibility = Visibility.Visible;

            view.Dispatcher.BeginInvoke(new Action(() =>
            {
                var transform = EnsureTranslateTransform(view);
                double width = GetActualWidth(view);
                if (width <= 0)
                if (width <= 0)
                {
                    onCompleted?.Invoke();
                    return;
                }

                AnimateX(transform, width, 0, onCompleted);
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }
    }
}