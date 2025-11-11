// Modules/ExamAutoGraderInfrastructureModule.cs
using Castle.DynamicProxy;
using ExamAutoGrader.Domain.Attributes;
using ExamAutoGrader.Domain.Interfaces;
using ExamAutoGrader.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace ExamAutoGrader.Infrastructure;

/// <summary>
/// 模仿 ABP 模块系统，封装基础设施层的自动注册逻辑。
/// </summary>
public static class ExamAutoGraderInfrastructureModule
{
    public static IServiceCollection AddExamAutoGraderInfrastructure(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // 注册工作单元核心服务（类似 ABP 的 Conventional Registration）
        services.AddUnitOfWorkCore();

        // 扫描并注册所有需要事务代理的服务
        RegisterUnitOfWorkProxiedServices(services, assembly);

        return services;
    }

    private static void RegisterUnitOfWorkProxiedServices(IServiceCollection services, Assembly assembly)
    {
        // 找出所有可能需要 UnitOfWork 拦截的类型
        var typesToProxy = assembly.GetTypes()
            .Where(t => t.IsClass &&
                        !t.IsAbstract &&
                        ImplementsServiceInterface(t) &&
                        (HasUnitOfWorkOnClassOrMethods(t)))
            .ToList();

        foreach (var type in typesToProxy)
        {
            // 优先使用接口代理（ABP 风格）
            var serviceInterfaces = type.GetInterfaces()
                .Where(i => i.Name.EndsWith("Service") ||
                            i.Name.EndsWith("Repository") ||
                            i.Name.StartsWith("I"))
                .ToArray();

            if (serviceInterfaces.Length > 0)
            {
                foreach (var iface in serviceInterfaces)
                {
                    services.AddTransient(iface, provider =>
                    {
                        var proxyGenerator = provider.GetRequiredService<IProxyGenerator>();
                        var interceptor = provider.GetRequiredService<UnitOfWorkInterceptor>();

                        var targetInstance = ActivatorUtilities.CreateInstance(provider, type);

                        return proxyGenerator.CreateInterfaceProxyWithTarget(
                            interfaceToProxy: iface,
                            target: targetInstance,
                            interceptors: new IInterceptor[] { interceptor }
                        );
                    });
                }
            }
            else
            {
                // 无接口时代理类（要求 virtual 方法）
                services.AddTransient(type, provider =>
                {
                    var proxyGenerator = provider.GetRequiredService<IProxyGenerator>();
                    var interceptor = provider.GetRequiredService<UnitOfWorkInterceptor>();
                    return proxyGenerator.CreateClassProxy(type, interceptor);
                });
            }
        }
    }

    private static bool ImplementsServiceInterface(Type type)
    {
        return type.GetInterfaces().Any(i =>
            i.Name.EndsWith("Service") ||
            i.Name.EndsWith("Repository") ||
            typeof(IUnitOfWork).IsAssignableFrom(i) == false); // 排除 UoW 自身
    }

    private static bool HasUnitOfWorkOnClassOrMethods(Type type)
    {
        // 类上有 [UnitOfWork]
        if (type.GetCustomAttribute<UnitOfWorkAttribute>() != null)
            return true;

        // 任意方法上有 [UnitOfWork]
        return type.GetMethods(BindingFlags.Public | BindingFlags.Instance)
                   .Any(m => m.GetCustomAttribute<UnitOfWorkAttribute>() != null);
    }
}