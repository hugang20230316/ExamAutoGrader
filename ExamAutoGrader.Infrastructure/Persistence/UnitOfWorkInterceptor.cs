using Castle.DynamicProxy;
using ExamAutoGrader.Application.Abstractions;
using ExamAutoGrader.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace ExamAutoGrader.Infrastructure.Persistence;

/// <summary>
/// 工作单元拦截器，用于自动提交或回滚事务
/// </summary>
public class UnitOfWorkInterceptor : IInterceptor
{
    private readonly ILogger<UnitOfWorkInterceptor> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public UnitOfWorkInterceptor(
        ILogger<UnitOfWorkInterceptor> logger,
        IUnitOfWork unitOfWork)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public void Intercept(IInvocation invocation)
    {
        var method = invocation.MethodInvocationTarget ?? invocation.Method;
        var methodName = method.Name;
        var typeName = method.DeclaringType?.Name ?? "UnknownType";

        // 放行查询类方法（约定：以 Get/Find/Query/List 开头的方法不开启写事务）
        if (IsReadOnlyMethod(methodName))
        {
            invocation.Proceed();
            return;
        }

        var unitOfWorkAttr = GetUnitOfWorkAttribute(invocation);
        if (unitOfWorkAttr == null || unitOfWorkAttr.IsDisabled)
        {
            _logger.LogDebug("方法 {TypeName}.{MethodName} 未启用工作单元，直接执行。", typeName, methodName);
            invocation.Proceed();
            return;
        }

        try
        {
            invocation.Proceed();

            var returnType = method.ReturnType;

            if (returnType == typeof(Task))
            {
                // 处理 async Task（无返回值）
                var task = (Task)invocation.ReturnValue!;
                invocation.ReturnValue = HandleAsyncWithoutResult(task, typeName, methodName);
            }
            else if (returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>))
            {
                // 处理 async Task<T>（有返回值）
                var task = (Task)invocation.ReturnValue!;
                var resultType = returnType.GenericTypeArguments[0];
                var genericMethod = GetType()
                    .GetMethod(nameof(HandleAsyncWithResult), BindingFlags.NonPublic | BindingFlags.Instance)!
                    .MakeGenericMethod(resultType);

                invocation.ReturnValue = genericMethod.Invoke(this, new object[] { task, typeName, methodName });
            }
            else
            {
                // 同步方法（void 或 T）
                _unitOfWork.CompleteAsync().GetAwaiter().GetResult();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "工作单元执行失败：{TypeName}.{MethodName}", typeName, methodName);
            try
            {
                _unitOfWork.RollbackAsync().GetAwaiter().GetResult();
            }
            catch (Exception rollbackEx)
            {
                _logger.LogError(rollbackEx, "事务回滚失败：{TypeName}.{MethodName}", typeName, methodName);
            }
            throw;
        }
    }

    private bool IsReadOnlyMethod(string methodName)
    {
        return methodName.StartsWith("Get", StringComparison.OrdinalIgnoreCase) ||
               methodName.StartsWith("Find", StringComparison.OrdinalIgnoreCase) ||
               methodName.StartsWith("Query", StringComparison.OrdinalIgnoreCase) ||
               methodName.StartsWith("List", StringComparison.OrdinalIgnoreCase) ||
               methodName.StartsWith("Exists", StringComparison.OrdinalIgnoreCase);
    }

    private UnitOfWorkAttribute? GetUnitOfWorkAttribute(IInvocation invocation)
    {
        var method = invocation.MethodInvocationTarget ?? invocation.Method;
        return method.GetCustomAttribute<UnitOfWorkAttribute>()
               ?? method.DeclaringType?.GetCustomAttribute<UnitOfWorkAttribute>();
    }

    // 包装 async Task（无返回值）
    private async Task HandleAsyncWithoutResult(Task task, string typeName, string methodName)
    {
        try
        {
            await task.ConfigureAwait(false);
            await _unitOfWork.CompleteAsync().ConfigureAwait(false);
            _logger.LogDebug("工作单元提交成功：{TypeName}.{MethodName}", typeName, methodName);
        }
        catch
        {
            await _unitOfWork.RollbackAsync().ConfigureAwait(false);
            throw;
        }
    }

    // 包装 async Task<T>（有返回值）——实例方法，可访问 _unitOfWork
    private async Task<T> HandleAsyncWithResult<T>(Task<T> task, string typeName, string methodName)
    {
        try
        {
            T result = await task.ConfigureAwait(false);
            await _unitOfWork.CompleteAsync().ConfigureAwait(false);
            _logger.LogDebug("工作单元提交成功：{TypeName}.{MethodName}", typeName, methodName);
            return result;
        }
        catch
        {
            await _unitOfWork.RollbackAsync().ConfigureAwait(false);
            throw;
        }
    }
}