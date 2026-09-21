using CryptoPlatform.Worker;

var builder = Host.CreateApplicationBuilder(args);

// 注册后台 Worker 服务
builder.Services.AddHostedService<KeyExpirationWorker>();
builder.Services.AddHostedService<KeyRotationWorker>();
builder.Services.AddHostedService<SecurityDetectionWorker>();
builder.Services.AddHostedService<CompensationWorker>();
builder.Services.AddHostedService<HealthCheckWorker>();

var host = builder.Build();
host.Run();
