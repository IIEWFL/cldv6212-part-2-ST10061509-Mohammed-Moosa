using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Files.Shares;
using Azure.Storage.Queues;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// FIX: Retrieve the connection string using the name "AzureStorage",
// which is defined in your appsettings.json file.
var connectionString = builder.Configuration.GetConnectionString("AzureStorage");

// Register Azure Storage clients
// NOTE: These constructors will now receive the correct, non-null string value.
builder.Services.AddSingleton(new TableServiceClient(connectionString));
builder.Services.AddSingleton(new BlobServiceClient(connectionString));
builder.Services.AddSingleton(new QueueServiceClient(connectionString));
builder.Services.AddSingleton(new ShareServiceClient(connectionString));

// ⭐ Register HttpClient for calling Azure Functions
builder.Services.AddHttpClient();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
