using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MasterBackup_Worker.Application.Interfaces;
using MasterBackup_Worker.Application.Services;
using MasterBackup_Worker.Domain.Entities;
using MasterBackup_Worker.Infrastructure.Http;
using MasterBackup_Worker.Infrastructure.MessageQueue;
using MasterBackup_Worker.Infrastructure.Services;

// Build configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

// Load worker configuration
var workerConfig = new WorkerConfiguration
{
    WorkerId = Guid.Empty, // Will be assigned after registration
    TenantId = Guid.Empty, // Will be assigned after registration from API
    WorkerName = configuration["Worker:WorkerName"] ?? "Worker-Unnamed",
    ApiKey = configuration["Worker:ApiKey"] ?? throw new InvalidOperationException("Worker:ApiKey not configured"),
    Tags = configuration.GetSection("Worker:Tags").Get<string[]>() ?? Array.Empty<string>(),
    SupportedDatabaseTypes = configuration.GetSection("Worker:SupportedDatabaseTypes").Get<string[]>() ?? Array.Empty<string>(),
    MaxConcurrentJobs = int.Parse(configuration["Worker:MaxConcurrentJobs"] ?? "1")
};

// RabbitMQ configuration
var rabbitMQHost = configuration["RabbitMQ:Host"] ?? "localhost";
var rabbitMQPort = int.Parse(configuration["RabbitMQ:Port"] ?? "5672");

// API configuration
var apiBaseUrl = configuration["Api:BaseUrl"] ?? "http://localhost:7000";

Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
Console.WriteLine("║          MasterBackup Worker - Starting Up                   ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
Console.WriteLine();
Console.WriteLine($"Worker ID:       {workerConfig.WorkerId}");
Console.WriteLine($"Worker Name:     {workerConfig.WorkerName}");
Console.WriteLine($"Tenant ID:       {workerConfig.TenantId}");
Console.WriteLine($"Tags:            [{string.Join(", ", workerConfig.Tags)}]");
Console.WriteLine($"Database Types:  [{string.Join(", ", workerConfig.SupportedDatabaseTypes)}]");
Console.WriteLine($"Max Jobs:        {workerConfig.MaxConcurrentJobs}");
Console.WriteLine($"RabbitMQ:        {rabbitMQHost}:{rabbitMQPort}");
Console.WriteLine($"API:             {apiBaseUrl}");
Console.WriteLine();
Console.WriteLine("══════════════════════════════════════════════════════════════");
Console.WriteLine();

// Build and run host
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Register worker configuration as singleton
        services.AddSingleton(workerConfig);

        // Register HTTP client for API communication
        services.AddHttpClient<IBackupStatusReporter, BackupStatusReporter>(client =>
        {
            client.BaseAddress = new Uri(apiBaseUrl);
            client.DefaultRequestHeaders.Add("X-API-Key", workerConfig.ApiKey);
            client.Timeout = TimeSpan.FromMinutes(5);
        });

        // Register application services
        services.AddSingleton<IWorkerAuthorizationService, WorkerAuthorizationService>();
        services.AddSingleton<IConnectionTestService, ConnectionTestService>();
        services.AddSingleton<IBackupExecutorService, BackupExecutorService>();
        services.AddSingleton<IApiClient>(sp => new ApiClient(
            sp.GetRequiredService<ILogger<ApiClient>>(),
            apiBaseUrl,
            workerConfig.ApiKey));

        // Register Worker Registration and Heartbeat service
        services.AddHostedService<WorkerRegistrationService>();
        
        // Register RabbitMQ consumer as hosted service
        services.AddHostedService(sp => new RabbitMQConsumerService(
            sp.GetRequiredService<ILogger<RabbitMQConsumerService>>(),
            sp.GetRequiredService<IWorkerAuthorizationService>(),
            sp.GetRequiredService<IConnectionTestService>(),
            sp.GetRequiredService<IBackupExecutorService>(),
            sp.GetRequiredService<IApiClient>(),
            workerConfig,
            rabbitMQHost,
            rabbitMQPort));
    })
    .ConfigureLogging((context, logging) =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.AddDebug();
        logging.SetMinimumLevel(LogLevel.Information);
    })
    .Build();

Console.WriteLine("✓ Worker initialized successfully");
Console.WriteLine("✓ Listening for jobs on RabbitMQ...");
Console.WriteLine();

await host.RunAsync();
