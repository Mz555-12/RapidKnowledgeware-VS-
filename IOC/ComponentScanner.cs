using System;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using IOC.Annotations;

namespace IOC
{
    /// <summary>
    /// 模拟 Spring 组件扫描，自动注册标注了 @Service、@Repository、@Controller 的类。
    /// </summary>
    internal static class ComponentScanner
    {
        /// <summary>
        /// 扫描指定程序集中所有带衍生注解的类，并按规则注册到容器。
        /// </summary>
        /// <param name="services">服务集合</param>
        /// <param name="assembly">要扫描的程序集</param>
        internal static void ScanAndRegister(IServiceCollection services, Assembly assembly)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));
            if (assembly == null) throw new ArgumentNullException(nameof(assembly));

            var allTypes = assembly.GetTypes();
            foreach (var type in allTypes)
            {
                if (!type.IsClass || type.IsAbstract)
                    continue;

                if (type.GetCustomAttribute<ServiceAttribute>() != null)
                {
                    Register(services, type, ServiceLifetime.Singleton);
                    continue;
                }

                if (type.GetCustomAttribute<RepositoryAttribute>() != null)
                {
                    Register(services, type, ServiceLifetime.Singleton);
                    continue;
                }

                if (type.GetCustomAttribute<ControllerAttribute>() != null)
                {
                    Register(services, type, ServiceLifetime.Transient);
                }
            }
        }

        private static void Register(IServiceCollection services, Type implementationType, ServiceLifetime lifetime)
        {
            // 获取该类除 IDisposable、IBase 之外的第一个接口作为服务类型
            var serviceType = implementationType.GetInterfaces()
                .FirstOrDefault(i => i != typeof(IDisposable) && !i.Name.StartsWith("IBase"));

            if (serviceType != null)
            {
                switch (lifetime)
                {
                    case ServiceLifetime.Singleton:
                        services.AddSingleton(serviceType, implementationType);
                        break;
                    case ServiceLifetime.Transient:
                        services.AddTransient(serviceType, implementationType);
                        break;
                }
            }
            else
            {
                // 没有实现接口，注册自身
                switch (lifetime)
                {
                    case ServiceLifetime.Singleton:
                        services.AddSingleton(implementationType);
                        break;
                    case ServiceLifetime.Transient:
                        services.AddTransient(implementationType);
                        break;
                }
            }
        }
    }
}