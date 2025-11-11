using Castle.DynamicProxy;
using ExamAutoGrader.Domain.Attributes;
using ExamAutoGrader.Domain.Interfaces;
using ExamAutoGrader.Infrastructure.Persistence;
using System.Reflection;

namespace ExamAutoGrader.Api.Extensions;

public static class UnitOfWorkServiceCollectionExtensions
{
    /// <summary>
    /// 启用工作单元支持：自动注册 IUnitOfWorkManager、拦截器，
    /// 并为所有标记 [UnitOfWork] 的服务启用动态代理。
    /// 模仿 ABP 的模块化自动注册行为。
    /// </summary>
    public static IServiceCollection AddUnitOfWorkSupport(this IServiceCollection services)
    {
        // 1. 注册核心服务（类似 ABP 的模块内部注册）
        services.AddScoped<IUnitOfWorkManager, UnitOfWorkManager>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();              
        services.AddSingleton<UnitOfWorkInterceptor>();
        services.AddSingleton<IProxyGenerator, ProxyGenerator>();

        // 2. 扫描当前程序集（或指定程序集）中所有服务
        var assembly = Assembly.GetCallingAssembly(); // 可改为 EntryAssembly 或指定领域层

        var serviceTypes = assembly.GetTypes()
            .Where(t => t.IsClass &&
                        !t.IsAbstract &&
                        t.GetCustomAttributes<UnitOfWorkAttribute>().Any() &&
                        t.GetInterfaces().Any(i => i.Name.EndsWith("Service") ||
                                                  i.Name.EndsWith("Repository")));

        foreach (var serviceType in serviceTypes)
        {
            var interfaceType = serviceType.GetInterfaces()
                .FirstOrDefault(i => i.Name.EndsWith("Service") ||
                                    i.Name.EndsWith("Repository"));

            if (interfaceType != null)
            {
                // 注册服务接口 -> 实现，并用代理包装
                services.AddScoped(interfaceType, provider =>
                {
                    var proxyGenerator = provider.GetRequiredService<IProxyGenerator>();
                    var interceptor = provider.GetRequiredService<UnitOfWorkInterceptor>();
                    return proxyGenerator.CreateClassProxy(serviceType, interceptor);
                });
            }
            else
            {
                // 如果没有接口，直接代理类（需注意：只能代理 virtual 方法）
                services.AddScoped(serviceType, provider =>
                {
                    var proxyGenerator = provider.GetRequiredService<IProxyGenerator>();
                    var interceptor = provider.GetRequiredService<UnitOfWorkInterceptor>();
                    return proxyGenerator.CreateClassProxy(serviceType, interceptor);
                });
            }
        }

        return services;
    }
}