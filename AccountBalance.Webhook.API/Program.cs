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

builder.Configuration.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Webhook API", Version = "v1" });

    c.AddSecurityDefinition("CompanyId", new OpenApiSecurityScheme
    {
        Name = "X-Company-Id",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Description = "Company identifier provided by APIM (transformed from subscription key)"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "CompanyId" },
                In = ParameterLocation.Header
            },
            new string[] {}
        }
    });
});

// Infrastructure
builder.Services.Configure<MongoDbOptions>(builder.Configuration.GetSection("MongoDb"));
builder.Services.Configure<AzureServiceBusOptions>(builder.Configuration.GetSection("AzureServiceBus"));

builder.Services.AddSingleton<IMongoDbContext, MongoDbContext>();
builder.Services.AddSingleton<IMessagePublisher, AzureServiceBusPublisher>();

builder.Services.AddScoped<IIngestionRepository, IngestionRepository>();
builder.Services.AddScoped<ITransactionPublisher, TransactionPublisher>();

// Application
builder.Services.AddScoped<ITransactionIngestionService, TransactionIngestionService>();

builder.Services.AddProblemDetails();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();
