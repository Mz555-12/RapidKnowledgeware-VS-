using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace IOC
{
    /// <summary>
    /// 全局 Bean 工厂，提供类似 Spring 的 getBean 方法。
    /// </summary>
    public static class BeanFactory
    {
        private static IServiceProvider _serviceProvider;

        /// <summary>
        /// 初始化 Bean 工厂（由 ServiceConfigurator 自动调用，无需手动调用）。
        /// </summary>
        internal static void Initialize(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <summary>
        /// 从 IOC 容器获取 Bean（对应 Java 的 context.getBean(xxx.class)）。
        /// </summary>
        public static T GetBean<T>() where T : class
        {
            if (_serviceProvider == null)
                throw new InvalidOperationException("BeanFactory 尚未初始化，请先调用 ServiceConfigurator.ConfigureServices。");
            return _serviceProvider.GetRequiredService<T>();
        }
    }
}