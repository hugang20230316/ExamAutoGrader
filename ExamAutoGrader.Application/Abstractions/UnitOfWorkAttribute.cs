namespace ExamAutoGrader.Application.Abstractions;

/// <summary>
/// 标记方法或类启用工作单元（事务）管理
/// 支持自动开启、提交、回滚数据库事务
/// 可用于类或方法上
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public class UnitOfWorkAttribute : Attribute
{
    /// <summary>
    /// 是否禁用当前作用域的事务（用于嵌套调用中跳过事务）
    /// </summary>
    public bool IsDisabled { get; set; }

    /// <summary>
    /// 是否强制新建一个工作单元（即使外部已有）
    /// 默认为 false：复用现有事务（推荐）
    /// 设置为 true：开启独立事务（慎用）
    /// </summary>
    public bool IsolationLevel { get; set; } = false;

    /// <summary>
    /// 是否自动保存更改（SaveChanges）
    /// 默认 true：在 Complete 时自动 SaveChanges
    /// 可设为 false 手动控制（高级场景）
    /// </summary>
    public bool AutoSaveChanges { get; set; } = true;

    /// <summary>
    /// 超时时间（秒），null 表示使用默认超时
    /// </summary>
    public int? Timeout { get; set; }

    /// <summary>
    /// 默认构造函数：启用事务，自动保存
    /// </summary>
    public UnitOfWorkAttribute()
    {
    }

    /// <summary>
    /// 快捷方式：传入是否禁用
    /// </summary>
    /// <param name="isDisabled">true 表示禁用事务</param>
    public UnitOfWorkAttribute(bool isDisabled)
    {
        IsDisabled = isDisabled;
    }
}

