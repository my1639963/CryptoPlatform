using CryptoPlatform.Api.Middleware;
using CryptoPlatform.Application;
using CryptoPlatform.Audit;
using CryptoPlatform.Authentication;
using CryptoPlatform.Authorization;
using CryptoPlatform.Crypto.Abstractions;
using CryptoPlatform.Crypto.Software;
using CryptoPlatform.Persistence;
using CryptoPlatform.Security;
using Scalar.AspNetCore;
using Serilog;

namespace CryptoPlatform.Api;

public class Program
{
    public static int Main(string[] args)
    {
        // ── Serilog 引导 ──
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        try
        {
            Log.Information("国密加密服务平台启动中...");

            var builder = WebApplication.CreateBuilder(args);

            // ── Serilog 配置 ──
            builder.Host.UseSerilog((context, services, configuration) => configuration
                .ReadFrom.Configuration(context.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}")
                .WriteTo.File(
                    path: "logs/crypto-platform-.log",
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: 30,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}"));

            // ── JSON 序列化 ──
            builder.Services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                    options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
                });

            // ── OpenAPI / Scalar ──
            builder.Services.AddOpenApi(options =>
            {
                options.AddDocumentTransformer((document, context, cancellationToken) =>
                {
                    document.Info.Title = "Encryption Service API";
                    document.Info.Description = "国密加密服务平台 API 文档";
                    document.Info.Version = "v1";
                    return Task.CompletedTask;
                });
            });
            // ── 健康检查 ──
            builder.Services.AddHealthChecks();

            // ── 基础设施层 ──
            builder.Services.AddPersistence(builder.Configuration);

            // ── 密码学层 ──
            builder.Services.AddCryptoAbstractions();
            builder.Services.AddSoftwareCryptoProvider();

            // ── 业务层 ──
            builder.Services.AddApplication();
            builder.Services.AddPlatformAuthorization();

            // ── 安全层 ──
            builder.Services.AddDistributedMemoryCache(); // 开发阶段使用内存缓存，生产环境替换为 Redis
            builder.Services.AddAuthenticationServices();
            builder.Services.AddAuditServices();
            builder.Services.AddSecurityServices();

            // ── CORS（开发阶段允许全部） ──
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            var app = builder.Build();

            // ── 中间件管道 ──
            app.UseSerilogRequestLogging();
            app.UseMiddleware<RequestIdMiddleware>();
            app.UseMiddleware<ExceptionHandlingMiddleware>();
            app.UseMiddleware<IdempotencyMiddleware>();

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
                app.MapScalarApiReference(options =>
                {
                    options.Title = "国密加密服务平台 API";
                    options.WithTheme(ScalarTheme.Default);
                });
            }

            app.UseCors();
            app.UseHealthChecks("/health");

            app.MapControllers();

            app.Run();

            return 0;
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "应用程序启动失败");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
