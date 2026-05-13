**1. 引入类库与依赖**

* 将 IOC.dll 添加到 WPF 项目的引用中。
* 在 WPF 项目中通过 NuGet 安装 Microsoft.Extensions.DependencyInjection（版本 6.0.1 或兼容 .NET Framework 4.8 的版本）。

**2. 在 App.xaml.cs 中启动容器**

csharp：
using IocLibrary;
using System.Reflection;
using YourNamespace.Views; // MainWindow 所在命名空间

public partial class App : Application

{
   protected override void OnStartup(StartupEventArgs e)
   {
       base.OnStartup(e);
       // 启动 IOC 容器，扫描当前程序集，并手动注册无法注解的窗口
       ServiceConfigurator.ConfigureServices(Assembly.GetExecutingAssembly(), services =>
       {
           services.AddTransient<MainWindow>();
       });

       // 获取主窗口并显示
       var mainWindow = BeanFactory.GetBean<MainWindow>();
       mainWindow.Show();
   }
}

**3. 使用注解标记业务类**

* [Service]：标记业务逻辑实现类（对应 Java @Service）。
* [Repository]：标记数据访问实现类（对应 Java @Repository）。
* [Controller]：标记 ViewModel 实现类（对应 Java @Controller）。

csharp：
using IocLibrary.Annotations;

[Repository]
public class UserRepository : IUserRepository { ... }

[Service]
public class UserService : IUserService
{
   private readonly IUserRepository _repo;

   // 构造函数注入（容器自动解析）
   public UserService(IUserRepository repo) { _repo = repo; }
   ...
}

[Controller]
public class MainViewModel : IMainViewModel
{
   private readonly IUserService _service;
   public MainViewModel(IUserService service) { _service = service; }
   ...

}
无需任何手动注册，容器会扫描这些注解并自动绑定。

**4. 获取 Bean**

在任何地方通过 BeanFactory.GetBean<T>() 获取实例：
var userService = BeanFactory.GetBean<IUserService>();

**5. 生命周期说明**

* [Service] 和 [Repository] → Singleton（整个应用程序共享一个实例）
* [Controller] → Transient（每次获取都创建新实例）

**6. 扩展性**

增加新功能只需创建接口、添加实现类并加上对应注解，无需修改容器配置：

[Service]
public class OrderService : IOrderService { ... }

该类会被自动扫描并注册为单例。

**7. 注意事项**

* Model 类不要加注解，仍然通过 new 创建。
* 仅扫描标记了 [Service]、[Repository]、[Controller] 的类，不能使用 [Component]。
* 获取 Bean 只能通过 BeanFactory.GetBean<T>()，禁止直接使用 IServiceProvider。

