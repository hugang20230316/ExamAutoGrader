# 构建阶段
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# 复制项目文件
COPY ["ExamAutoGrader.API/ExamAutoGrader.API.csproj", "ExamAutoGrader.API/"]
COPY ["ExamAutoGrader.Application/ExamAutoGrader.Application.csproj", "ExamAutoGrader.Application/"]
COPY ["ExamAutoGrader.Domain/ExamAutoGrader.Domain.csproj", "ExamAutoGrader.Domain/"]
COPY ["ExamAutoGrader.Infrastructure/ExamAutoGrader.Infrastructure.csproj", "ExamAutoGrader.Infrastructure/"]
COPY ["ExamAutoGrader.Shared/ExamAutoGrader.Shared.csproj", "ExamAutoGrader.Shared/"]

# 恢复NuGet包
RUN dotnet restore "ExamAutoGrader.API/ExamAutoGrader.API.csproj"

# 复制所有源代码
COPY . .

# 发布应用
WORKDIR "/src/ExamAutoGrader.API"
RUN dotnet publish "ExamAutoGrader.API.csproj" -c Release -o /app/publish

# 运行阶段
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# 安装curl用于健康检查
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# 创建非root用户
RUN groupadd -r appuser && useradd -r -g appuser appuser
RUN mkdir -p /app/wwwroot/uploads /app/logs && chown -R appuser:appuser /app

# 切换用户
USER appuser

# 复制发布文件
COPY --from=build --chown=appuser:appuser /app/publish .

# 暴露端口
EXPOSE 8080

# 设置环境变量
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# 健康检查
HEALTHCHECK --interval=30s --timeout=10s --start-period=40s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# 启动应用
ENTRYPOINT ["dotnet", "ExamAutoGrader.API.dll"]