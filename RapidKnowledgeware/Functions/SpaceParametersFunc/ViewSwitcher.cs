using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using RapidKnowledgeware.Views;

namespace RapidKnowledgeware.Functions.SpaceParametersFunc
{
    public static class ViewSwitcher
    {
        // 缓存视图实例，避免重复创建
        private static readonly Dictionary<string, UserControl> _viewCache = new Dictionary<string, UserControl>();

        /// <summary>
        /// 预加载指定视图（通常在程序启动时调用）
        /// </summary>
        public static void PreloadView(string viewKey, UserControl viewInstance)
        {
            if (!_viewCache.ContainsKey(viewKey))
            {
                _viewCache[viewKey] = viewInstance;
            }
        }

        /// <summary>
        /// 切换 ContentControl 显示的视图（使用缓存）
        /// </summary>
        public static void SwitchView(ContentControl container, string viewKey)
        {
            if (container == null) return;

            if (_viewCache.TryGetValue(viewKey, out var cachedView))
            {
                container.Content = cachedView;
            }
            else
            {
                // 如果缓存不存在，尝试动态创建（保底措施）
                var newView = CreateViewByKey(viewKey);
                if (newView != null)
                {
                    _viewCache[viewKey] = newView;
                    container.Content = newView;
                }
            }
        }

        // 根据类名字符串反射创建实例（备用）
        private static UserControl CreateViewByKey(string viewKey)
        {
            try
            {
                var type = Type.GetType(viewKey);
                if (type != null && typeof(UserControl).IsAssignableFrom(type))
                {
                    return (UserControl)Activator.CreateInstance(type);
                }
            }
            catch
            {
                // 忽略异常
            }
            return null;
        }




    }
}