var builder = WebApplication.CreateBuilder(args);
var port = Environment.GetEnvironmentVariable("PORT") ?? "2593";
builder.WebHost.UseUrls($"http://+:{port}");

var app = builder.Build();
app.MapGet("/", () => $"Hello from {Environment.MachineName}\n");
app.MapGet("/health", () => Results.Ok());
app.Run();
