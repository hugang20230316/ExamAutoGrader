using Castle.DynamicProxy;
using ExamAutoGrader.Application.Abstractions;
using ExamAutoGrader.Domain.Attributes;
using ExamAutoGrader.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace ExamAutoGrader.Infrastructure.Persistence;

public class UnitOfWorkInterceptor : IInterceptor
{
    private readonly ILogger<UnitOfWorkInterceptor> _logger;

    public UnitOfWorkInterceptor( ILogger<UnitOfWorkInterceptor> logger)
    {
        _logger = logger;
    }

    public void Intercept(IInvocation invocation)
    {
        var unitOfWorkAttr = GetUnitOfWorkAttribute(invocation);
        if (unitOfWorkAttr == null || unitOfWorkAttr.IsDisabled)
        {
            _logger.LogDebug("未找到工作单元特性，直接执行方法: {Method}", invocation.Method.Name);
            invocation.Proceed();
            return;
        }

        // 关键步骤 1：获取被拦截的 Service 实例（Scoped 生命周期，已注入 IUnitOfWorkManager）
        var targetService = invocation.InvocationTarget;
        if (targetService == null)
        {
            _logger.LogError("无法获取被拦截的服务实例: {Method}", invocation.Method.Name);
            invocation.Proceed();
            return;
        }

        // 2. 从被拦截的服务获取Scoped容器（服务继承ScopedServiceBase，实现IServiceProviderAccessor）
        if (targetService is not IServiceProviderAccessor serviceProviderAccessor)
        {
            throw new InvalidOperationException($"服务{targetService.GetType().Name}必须继承ScopedServiceBase");
        }

        using var unitOfWork = serviceProviderAccessor.ServiceProvider.GetRequiredService<IUnitOfWork>();

        try
        {
            invocation.Proceed(); // 执行 Service 方法（仓储和拦截器共享同一个 DbContext）

            if (invocation.ReturnValue is Task task)
            {
                invocation.ReturnValue = InterceptAsync(task, unitOfWork, invocation.Method.Name);
            }
            else
            {
                unitOfWork.CompleteAsync().GetAwaiter().GetResult();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "工作单元执行失败: {Method}", invocation.Method.Name);
            unitOfWork.RollbackAsync().GetAwaiter().GetResult(); // 异常回滚
            throw;
        }
        finally
        {
            unitOfWork.Dispose();
            _logger.LogDebug("工作单元同步提交完成: {Method}", invocation.Method.Name);
        }
    }

    private async Task InterceptAsync(Task task, IUnitOfWork unitOfWork, string methodName)
    {
        try
        {
            await task.ConfigureAwait(false);
            await unitOfWork.CompleteAsync();

            Console.WriteLine($"【拦截器】 提交的 DbContext ID：{unitOfWork.GetHashCode()}");
        }
        catch
        {
            throw;
        }
    }

    private UnitOfWorkAttribute GetUnitOfWorkAttribute(IInvocation invocation)
    {
        _logger.LogDebug("=== 开始查找工作单元特性 ===");
        _logger.LogDebug("方法: {MethodName}", invocation.Method.Name);
        _logger.LogDebug("接口类型: {InterfaceType}", invocation.Method.DeclaringType?.Name);

        // 先查方法上的特性
        var methodAttr = invocation.Method.GetCustomAttribute<UnitOfWorkAttribute>();
        if (methodAttr != null) return methodAttr;

        // 方法上没有则查类上的特性
        var classAttr = invocation.TargetType.GetCustomAttribute<UnitOfWorkAttribute>();
        return classAttr;
    }

    // 从 Service 实例中反射获取 IUnitOfWorkManager（支持字段/属性,此方案是除了IServiceProviderAccessor接口以外的另一种方式）
    private IUnitOfWorkManager? GetUnitOfWorkManagerFromService(object targetService)
    {
        var serviceType = targetService.GetType();

        // 1. 先找私有字段（最常见：private readonly IUnitOfWorkManager _unitOfWorkManager;）
        var field = serviceType.GetField(
            "_unitOfWorkManager",
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (field != null && field.FieldType == typeof(IUnitOfWorkManager))
        {
            return field.GetValue(targetService) as IUnitOfWorkManager;
        }

        // 2. 再找公共字段（少见）
        field = serviceType.GetField(
            "_unitOfWorkManager",
            BindingFlags.Instance | BindingFlags.Public);
        if (field != null && field.FieldType == typeof(IUnitOfWorkManager))
        {
            return field.GetValue(targetService) as IUnitOfWorkManager;
        }

        // 3. 再找属性（比如：public IUnitOfWorkManager UnitOfWorkManager { get; }）
        var property = serviceType.GetProperty(
            "UnitOfWorkManager",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (property != null && property.PropertyType == typeof(IUnitOfWorkManager))
        {
            return property.GetValue(targetService) as IUnitOfWorkManager;
        }

        // 4. 若字段名不同（比如 _uowManager），可在这里扩展（添加其他可能的字段名）
        var alternativeField = serviceType.GetField(
            "_uowManager",
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (alternativeField != null && alternativeField.FieldType == typeof(IUnitOfWorkManager))
        {
            return alternativeField.GetValue(targetService) as IUnitOfWorkManager;
        }

        return null;
    }
}
