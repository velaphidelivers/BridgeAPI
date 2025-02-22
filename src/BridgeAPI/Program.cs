using Handlers;
using Helpers;
using Helpers.Interfaces;
using Services;
using Services.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register dependencies for ITokenService and IAllowUrls
builder.Services.AddHttpClient<ITokenService, TokenService>();

// Assuming AllowedUrls requires some configuration
builder.Services.AddSingleton<IAllowedUrls>(provider =>
{
    // You can configure AllowedUrls based on your application settings or parameters.
    // Here we create an instance directly.
    return new AllowedUrls();
});

builder.Services.AddCors(options =>
    {
        options.AddDefaultPolicy(builder =>
        {
            builder.AllowAnyOrigin()
                   .AllowAnyHeader()
                   .AllowAnyMethod();
        });
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RoutingMiddleware>();
//app.ConfigureExceptionHandler();

app.Run();

