using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Azure.Storage.Files.Shares;
using ABCFunc.Services;

// Code Attribution:
// Azure Functions Dependency Injection: Standard pattern for configuring services in .NET Worker — Microsoft Docs
var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        // Code Attribution:
        // Application Insights: Standard setup for Azure Functions monitoring — Microsoft Docs
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // Get connection string from environment variables
        // AzureWebJobsStorage is the default connection setting for functions storage bindings
        var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");

        // Register Azure Storage SDK Clients as Singletons
        // Code Attribution: Azure SDK Clients for DI: Registering the root service clients — Microsoft Docs
        services.AddSingleton(new TableServiceClient(connectionString));
        services.AddSingleton(new BlobServiceClient(connectionString));
        services.AddSingleton(new QueueServiceClient(connectionString));
        services.AddSingleton(new ShareServiceClient(connectionString));

        // Register custom Services (Scoped lifetime is typical for request-based functions)
        services.AddScoped<TableService>();
        services.AddScoped<BlobService>();
        services.AddScoped<QueueService>();
        services.AddScoped<FileService>();
    })
    .Build();

host.Run();
