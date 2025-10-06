using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Azure.Storage.Files.Shares;
using ABCFunc.Services;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices(services =>
    {
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();

        // Get connection string from environment variables
        var connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");

        // Register Azure Storage clients as Singletons
        services.AddSingleton(new TableServiceClient(connectionString));
        services.AddSingleton(new BlobServiceClient(connectionString));
        services.AddSingleton(new QueueServiceClient(connectionString));
        services.AddSingleton(new ShareServiceClient(connectionString));

        // Register custom Services
        services.AddScoped<TableService>();
        services.AddScoped<BlobService>();
        services.AddScoped<QueueService>();
        services.AddScoped<FileService>();
    })
    .Build();

host.Run();