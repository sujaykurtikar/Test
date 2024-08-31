var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
// Add services to the container.

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

// Configure the HTTP request pipeline.
//if (app.Environment.IsDevelopment())
//{
    app.UseSwagger();
    app.UseSwaggerUI();
 

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
