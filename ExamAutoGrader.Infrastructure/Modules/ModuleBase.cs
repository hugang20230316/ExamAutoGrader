using Microsoft.Extensions.DependencyInjection;

namespace ExamAutoGrader.Infrastructure.Modules;

/// <summary>
/// 模仿 ABP 的模块基类，用于组织启动逻辑。
/// </summary>
public abstract class ModuleBase
{
    /// <summary>
    /// 模块初始化前，可用于配置全局选项。
    /// </summary>
    public virtual void PreConfigureServices(IServiceCollection services) { }

    /// <summary>
    /// 注册服务。
    /// </summary>
    public virtual void ConfigureServices(IServiceCollection services) { }

    /// <summary>
    /// 初始化后，可用于验证或扩展。
    /// </summary>
    public virtual void PostConfigureServices(IServiceCollection services) { }
}