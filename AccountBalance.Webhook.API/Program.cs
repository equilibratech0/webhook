using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using AccountBalance.Webhook.Application.Interfaces;
using AccountBalance.Webhook.Application.Services;
using AccountBalance.Webhook.Infrastructure.Messaging;
using AccountBalance.Webhook.Infrastructure.Persistence;
using Shared.Infrastructure.Persistence.Abstractions;
using Shared.Infrastructure.Persistence.Mongo;
using Shared.Infrastructure.Messaging.Abstractions;
using Shared.Infrastructure.Messaging.AzureServiceBus;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Add API Key from appsettings.json
builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();

// 1. Add presentation layer (Controllers / JSON / Swagger)
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Webhook API", Version = "v1" });

    // Add API key authorization config for swagger
    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Name = "X-Client-Key",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Description = "Authorization by X-Client-Key inside request's header",
        Scheme = "ApiKeyScheme"
    });

    // Requirement for idempotency key on every call in swagger
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ApiKey" },
                In = ParameterLocation.Header
            },
            new string[] {}
        }
    });
});

// 2. Add Infrastructure layer configurations
builder.Services.Configure<MongoDbOptions>(builder.Configuration.GetSection("MongoDb"));
builder.Services.Configure<AzureServiceBusOptions>(builder.Configuration.GetSection("AzureServiceBus"));

// 2.1 Dependencies from Shared (Persistence and Messaging)
builder.Services.AddSingleton<IMongoDbContext, MongoDbContext>();
builder.Services.AddSingleton<IMessagePublisher, AzureServiceBusPublisher>();

// 2.2 Dependencies from AccountBalance.Webhook.Infrastructure
builder.Services.AddScoped<IIngestionRepository, IngestionRepository>();
builder.Services.AddScoped<ITransactionPublisher, TransactionPublisher>();

// 3. Add Application layer services
builder.Services.AddScoped<ITransactionIngestionService, TransactionIngestionService>();

// Configure Global Error Handling/Logging (simple built-in handling)
builder.Services.AddProblemDetails();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler(); // Maps to ProblemDetails automatically
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
