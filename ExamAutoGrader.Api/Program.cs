using Castle.DynamicProxy;
using ExamAutoGrader.Application.Interfaces;
using ExamAutoGrader.Application.Services;
using ExamAutoGrader.Domain.Interfaces;
using ExamAutoGrader.Domain.Repositories;
using ExamAutoGrader.Infrastructure;
using ExamAutoGrader.Infrastructure.AI;
using ExamAutoGrader.Infrastructure.OCR;
using ExamAutoGrader.Infrastructure.Parsing;
using ExamAutoGrader.Infrastructure.Persistence;
using ExamAutoGrader.Infrastructure.Persistence.Repositories;
using ExamAutoGrader.Infrastructure.Services;
using ExamAutoGrader.Infrastructure.Similarity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// 添加详细日志
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.SetMinimumLevel(LogLevel.Debug);

// 或者在开发环境中启用详细日志
if (builder.Environment.IsDevelopment())
{
    builder.Logging.AddFilter("Microsoft.EntityFrameworkCore", LogLevel.Debug);
    builder.Logging.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Information);
}

// 1. 控制器服务
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = builder.Environment.IsDevelopment();
    });

// 2. API探索和Swagger文档
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 3. 数据库上下文配置
builder.Services.AddDbContext<ExamAutoGraderDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrEmpty(connectionString))
    {
        throw new InvalidOperationException("数据库连接字符串未配置");
    }

    // 添加详细日志以便调试
    options.EnableSensitiveDataLogging();
    options.EnableDetailedErrors();
    options.LogTo(message =>
    {
        // 输出到控制台
        Console.WriteLine(message);
    }, LogLevel.Information);

    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 21)) // 根据您的MySQL版本调整
    );

    // 仅在开发环境忽略迁移警告
    if (builder.Environment.IsDevelopment())
    {
        options.EnableDetailedErrors();
        options.EnableSensitiveDataLogging();
    }
}, ServiceLifetime.Scoped);

builder.Services.AddHostedService<StartupService>();

// 4. 核心服务注册 - 按照依赖顺序注册
//builder.Services.AddUnitOfWorkCore();

// AOP 支持
builder.Services.AddScoped<IUnitOfWorkManager, UnitOfWorkManager>(); // 替换为你的实现
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
// AOP 支持
builder.Services.AddSingleton<IProxyGenerator, ProxyGenerator>();
builder.Services.AddSingleton<UnitOfWorkInterceptor>();
builder.Services.AddScoped<UnitOfWorkManager>();

// 注册通用仓储（开放泛型）
builder.Services.AddScoped(typeof(IRepository<,>), typeof(EfCoreRepository<,>));
builder.Services.AddScoped<IFeedbackRecordRepository, FeedbackRecordRepository>();

builder.Services.AddSingleton<IProxyGenerator, ProxyGenerator>();
builder.Services.AddSingleton<UnitOfWorkInterceptor>(); // ✅ 单例，但现在不直接依赖 Scoped 服务

// 4.1 首先注册基础设施层服务
builder.Services.AddHttpClient<BaiduOCRService>();
builder.Services.AddScoped<IOCRService, BaiduOCRService>();

// 4.2 注册文件存储服务 - 必须在OCR处理服务之前注册！
builder.Services.AddScoped<IFileStorageService, SimpleFileStorageService>();

// 4.3 然后注册应用层服务（依赖基础设施层服务）
builder.Services.AddScoped<IKnowledgeBaseService, KnowledgeBaseService>();
builder.Services.AddScoped<IGradingService, GradingService>();
builder.Services.AddScoped<IOCRProcessingService, OCRProcessingService>();
builder.Services.AddHttpClient<GradingService>();


// 4.4 注册其他基础设施服务
builder.Services.AddScoped<IQuestionParserService, AlibabaAIParserService>();
builder.Services.AddHttpClient<AlibabaAIParserService>();

builder.Services.AddScoped<ILlmService, LlmService>();
builder.Services.AddHttpClient<LlmService>();

builder.Services.AddScoped<IEmbeddingService, DashScopeEmbeddingService>();
builder.Services.AddHttpClient<DashScopeEmbeddingService>();
builder.Services.Configure<DashScopeSettings>(builder.Configuration.GetSection("DashScope"));

// 工作单元核心服务
// 注册工作单元核心组件（类似 ABP 的 UnitOfWork 模块）
builder.Services.AddExamAutoGraderInfrastructure();

// 应用服务

// 5. 注册必要的基础服务
builder.Services.AddHttpContextAccessor(); // 重要：用于文件URL构建
builder.Services.AddMemoryCache();

// 6. 健康检查
builder.Services.AddHealthChecks();

// 7. 跨域策略
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:8080")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// 配置HTTP请求管道
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
// 启用静态文件服务
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads")),
    RequestPath = "/uploads"
});

// 确保上传目录存在
var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "ocr-temp");
if (!Directory.Exists(uploadsPath))
{
    Directory.CreateDirectory(uploadsPath);
    app.Logger.LogInformation("创建上传目录：{UploadsPath}", uploadsPath);
}

app.UseRouting();
app.UseCors("AllowFrontend");

// 健康检查端点
app.UseHealthChecks("/health");

app.MapControllers();

app.Logger.LogInformation("应用程序启动完成");

await app.RunAsync();