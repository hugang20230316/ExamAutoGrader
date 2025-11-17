using Castle.DynamicProxy;
using ExamAutoGrader.Application.Abstractions;
using ExamAutoGrader.Application.Interfaces;
using ExamAutoGrader.Domain.Entities;
using ExamAutoGrader.Domain.Events;
using ExamAutoGrader.Domain.Interfaces;
using ExamAutoGrader.Domain.Repositories;
using ExamAutoGrader.Infrastructure.AI;
using ExamAutoGrader.Infrastructure.Events;
using ExamAutoGrader.Infrastructure.ExtenalServices;
using ExamAutoGrader.Infrastructure.File;
using ExamAutoGrader.Infrastructure.Grading;
using ExamAutoGrader.Infrastructure.OCR;
using ExamAutoGrader.Infrastructure.Parsing;
using ExamAutoGrader.Infrastructure.Persistence;
using ExamAutoGrader.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;

namespace ExamAutoGrader.Infrastructure;

/// <summary>
/// 模仿 ABP 模块系统，封装基础设施层的自动注册逻辑。
/// </summary>
public static class ExamAutoGraderInfrastructureModule
{// 方法签名添加 IConfiguration 参数
    public static IServiceCollection AddExamAutoGraderInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IEventBus, EventBus>();
        // 注册拦截器相关服务
        services.AddSingleton<IProxyGenerator, ProxyGenerator>();// 🔥 添加缺失的仓储注册
        services.AddScoped<IFeedbackRecordRepository, FeedbackRecordRepository>();
        services.AddScoped<IRepository<GradingRecord, Guid>, EfCoreRepository<GradingRecord, Guid>>();

        services.AddScoped<UnitOfWorkInterceptor>();

        // 注册工作单元管理器
        services.AddScoped<IUnitOfWorkManager, UnitOfWorkManager>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddHttpClient<BaiduOCRService>();
        services.AddScoped<IOCRService, BaiduOCRService>();

        services.AddScoped<IFileStorageService, FileStorageService>();
        services.AddScoped<IOCRProcessingService, OCRProcessingService>();
        services.AddScoped<IQuestionParserService, AlibabaAIParserService>();
        services.AddHttpClient<AlibabaAIParserService>();
        services.AddScoped<ILlmService, LlmService>();
        services.AddHttpClient<LlmService>();
        services.AddScoped<IEmbeddingService, DashScopeEmbeddingService>();
        services.AddHttpClient<DashScopeEmbeddingService>();
        services.Configure<DashScopeSettings>(configuration.GetSection("DashScope"));
        services.AddHostedService<StartupService>();

        // 🔥 只保留这一套自动注册逻辑（删除其他重复的）
        RegisterAllProxiedServices(services);

        return services;
    }

    // 统一的自动注册方法
    private static void RegisterAllProxiedServices(IServiceCollection services)
    {
        var allAssemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.FullName.Contains("ExamAutoGrader")).ToList();

        foreach (var assembly in allAssemblies)
        {
            try
            {
                var serviceTypes = assembly.GetTypes()
                    .Where(t => t.IsClass && !t.IsAbstract &&
                               t.GetInterfaces().Any(i => (i.Name.EndsWith("Service") && !i.Name.Contains("HostedService")) || i.Name.EndsWith("Repository")))
                    .ToList();

                foreach (var serviceType in serviceTypes)
                {
                    var interfaces = serviceType.GetInterfaces()
                        .Where(i => (i.Name.StartsWith("I") && i.Name.EndsWith("Service")) || (i.Name.StartsWith("I") && i.Name.EndsWith("Repository")))
                        .ToList();

                    foreach (var interfaceType in interfaces)
                    {
                        // 注册具体实现
                        services.AddScoped(serviceType);

                        // 注册代理接口
                        services.AddScoped(interfaceType, provider =>
                        {
                            var proxyGenerator = provider.GetRequiredService<IProxyGenerator>();
                            var target = provider.GetRequiredService(serviceType);
                            var interceptor = provider.GetRequiredService<UnitOfWorkInterceptor>();
                            return proxyGenerator.CreateInterfaceProxyWithTarget(interfaceType, target, interceptor);
                        });

                        Console.WriteLine($"✅ 注册代理服务: {interfaceType.Name} -> {serviceType.Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠️ 扫描程序集 {assembly.FullName} 失败: {ex.Message}");
            }
        }
    }
}