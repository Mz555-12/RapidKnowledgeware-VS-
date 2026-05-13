using System;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;

namespace IOC
{
    /// <summary>
    /// IOC 容器配置启动器。
    /// </summary>
    public static class ServiceConfigurator
    {
        /// <summary>
        /// 构建 IOC 容器并初始化 BeanFactory。
        /// </summary>
        /// <param name="entryAssembly">需要扫描的程序集（一般为宿主程序集）</param>
        /// <param name="configureServices">额外的服务注册委托（用于注册视图等无法扫描的类）</param>
        /// <returns>构建好的 ServiceProvider</returns>
        public static ServiceProvider ConfigureServices(Assembly entryAssembly, Action<IServiceCollection> configureServices = null)
        {
            if (entryAssembly == null)
                throw new ArgumentNullException(nameof(entryAssembly));

            var services = new ServiceCollection();

            // 1. 扫描注解并注册
            ComponentScanner.ScanAndRegister(services, entryAssembly);

            // 2. 允许宿主项目注册无法注解的组件（如 MainWindow）
            configureServices?.Invoke(services);

            var serviceProvider = services.BuildServiceProvider();

            // 3. 初始化全局 Bean 工厂，之后即可使用 GetBean
            BeanFactory.Initialize(serviceProvider);

            return serviceProvider;
        }
    }
}