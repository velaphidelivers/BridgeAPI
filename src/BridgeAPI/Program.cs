var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

//app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RoutingMiddleware>();
app.MapGet("/health", () =>
{
    var healthStatus = new HealthStatus("Healthy", DateTime.Now);
    return healthStatus;
})
.WithName("GetHealthStatus")
.WithOpenApi();



app.Run();

record HealthStatus(string Status, DateTime CheckedAt);
