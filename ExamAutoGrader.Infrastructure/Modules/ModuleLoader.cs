using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace ExamAutoGrader.Infrastructure.Modules;

/// <summary>
/// 自动扫描并加载所有 ModuleBase 派生类，模仿 ABP 的模块发现机制。
/// </summary>
public static class ModuleLoader
{
    public static void LoadModules(IServiceCollection services, Assembly assembly)
    {
        var moduleTypes = assembly.GetTypes()
            .Where(t => typeof(ModuleBase).IsAssignableFrom(t) &&
                        t.IsClass &&
                        !t.IsAbstract)
            .OrderBy(t => GetOrder(t)) // 可支持 [DependsOn] 排序
            .ToList();

        var modules = new List<ModuleBase>();

        foreach (var type in moduleTypes)
        {
            var module = (ModuleBase)Activator.CreateInstance(type)!;
            modules.Add(module);

            module.PreConfigureServices(services);
        }

        foreach (var module in modules)
        {
            module.ConfigureServices(services);
        }

        foreach (var module in modules)
        {
            module.PostConfigureServices(services);
        }
    }

    private static int GetOrder(Type type)
    {
        // 后续可支持 [DependsOn] 解析依赖顺序
        return 0;
    }
}