using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
// Add services to the container.
Log.Logger = new LoggerConfiguration()
    .WriteTo.File("Logs/app-log.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();


//builder.Host.UseSerilog();

builder.Services.AddControllers();

builder.Services.AddSingleton<Test_Project.Controllers.ControlController.ITaskStateService, Test_Project.Controllers.ControlController.TaskStateService>();
builder.Services.AddSingleton<PeriodicTaskService>(); // Register PeriodicTaskService
builder.Services.AddSingleton<HistoricalDataFetcher>();
// Register the background service
builder.Services.AddHostedService<PeriodicTaskService>();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.MapGet("/", () =>
{
    Log.Information("This is an information log message.");  // Log information
    return "Hello World!";
});
// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
app.UseSwagger();
    app.UseSwaggerUI();
 

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
