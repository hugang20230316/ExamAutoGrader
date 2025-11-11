namespace ExamAutoGrader.Application.Abstractions;

/// <summary>
/// 所有Scoped业务服务的基类（实现IServiceProviderAccessor，持有当前作用域的容器）
/// </summary>
public abstract class ScopedServiceBase : IServiceProviderAccessor
{
    /// <summary>
    /// 当前作用域的IServiceProvider（不是根容器）
    /// </summary>
    public IServiceProvider ServiceProvider { get; }

    /// <summary>
    /// 构造函数注入当前作用域的IServiceProvider（DI自动提供）
    /// </summary>
    protected ScopedServiceBase(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }
}

/// <summary>
/// 容器访问接口（让拦截器能获取Scoped容器）
/// </summary>
public interface IServiceProviderAccessor
{
    IServiceProvider ServiceProvider { get; }
}