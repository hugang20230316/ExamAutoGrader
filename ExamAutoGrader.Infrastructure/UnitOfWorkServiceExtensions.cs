using Castle.DynamicProxy;
using ExamAutoGrader.Domain.Interfaces;
using ExamAutoGrader.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace ExamAutoGrader.Infrastructure;

public static class UnitOfWorkServiceExtensions
{
    /// <summary>
    /// 添加工作单元核心支持（类似 ABP 的 AddUnitOfWork()）
    /// </summary>
    public static IServiceCollection AddUnitOfWorkCore(this IServiceCollection services)
    {
        // 核心服务
        services.AddScoped<IUnitOfWorkManager, UnitOfWorkManager>(); // 替换为你的实现
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // AOP 支持
        services.AddSingleton<IProxyGenerator, ProxyGenerator>();
        services.AddSingleton<UnitOfWorkInterceptor>();

        return services;
    }
}
