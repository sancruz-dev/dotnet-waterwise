using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using AspNetCoreRateLimit;
using WaterWise.Infrastructure.Data;
using WaterWise.Core.Services;
using WaterWise.Infrastructure.Services;
using WaterWise.ML.Services;
using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Microsoft.AspNetCore.Mvc.Versioning;
using System.Runtime.CompilerServices;

// Torna os membros internal do WaterWise.API visíveis para o WaterWise.Tests.
[assembly: InternalsVisibleTo("WaterWise.Tests")]

var builder = WebApplication.CreateBuilder(args);

// Configurar Serilog
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .WriteTo.File("logs/waterwise-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Configurar DbContext
builder.Services.AddDbContext<WaterWiseContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? "Data Source=localhost:1521/XEPDB1;User Id=waterwise;Password=waterwise123;";
    options.UseOracle(connectionString);
});

// Rate Limiting
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(options =>
{
    options.EnableEndpointRateLimiting = true;
    options.StackBlockedRequests = false;
    options.HttpStatusCode = 429;
    options.RealIpHeader = "X-Real-IP";
    options.ClientIdHeader = "X-ClientId";
    options.GeneralRules = new List<RateLimitRule>
    {
        new RateLimitRule
        {
            Endpoint = "*",
            Period = "1m",
            Limit = 100
        },
        new RateLimitRule
        {
            Endpoint = "*/sensor-data",
            Period = "1s",
            Limit = 10
        }
    };
});
builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();

// API Versioning
builder.Services.AddApiVersioning(opt =>
{
    opt.DefaultApiVersion = new ApiVersion(1, 0);
    opt.AssumeDefaultVersionWhenUnspecified = true;
    opt.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new QueryStringApiVersionReader("version"),
        new HeaderApiVersionReader("X-Version"),
        new MediaTypeApiVersionReader("ver")
    );
});

builder.Services.AddVersionedApiExplorer(setup =>
{
    setup.GroupNameFormat = "'v'VVV";
    setup.SubstituteApiVersionInUrl = true;
});

// Services
builder.Services.AddScoped<IRabbitMQService, RabbitMQService>();
builder.Services.AddScoped<IMLPredictionService, MLPredictionService>();

// Controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.WriteIndented = true;
    });

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "WaterWise API",
        Version = "v1",
        Description = "API para monitoramento de propriedades rurais e prevenção de enchentes urbanas",
        Contact = new OpenApiContact
        {
            Name = "WaterWise Team",
            Email = "contato@waterwise.com",
            Url = new Uri("https://github.com/waterwise-team")
        },
        License = new OpenApiLicense
        {
            Name = "MIT License",
            Url = new Uri("https://opensource.org/licenses/MIT")
        }
    });

    c.SwaggerDoc("v2", new OpenApiInfo
    {
        Title = "WaterWise API",
        Version = "v2",
        Description = "Versão 2 da API WaterWise com funcionalidades estendidas"
    });

    // Incluir comentários XML
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);

    // Configurar esquemas de segurança (se necessário)
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<WaterWiseContext>();

var app = builder.Build();

// Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "WaterWise API V1");
        c.SwaggerEndpoint("/swagger/v2/swagger.json", "WaterWise API V2");
        c.RoutePrefix = string.Empty; // Swagger na raiz
        c.DisplayRequestDuration();
        c.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.None);
    });
}

// Rate Limiting
app.UseIpRateLimiting();

app.UseHttpsRedirection();
app.UseCors("AllowAll");

app.UseRouting();
app.UseAuthorization();

// Middleware customizado para logging
app.Use(async (context, next) =>
{
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    await next();
    stopwatch.Stop();

    Log.Information("Request {Method} {Path} completed in {ElapsedMs}ms with status code {StatusCode}",
        context.Request.Method,
        context.Request.Path,
        stopwatch.ElapsedMilliseconds,
        context.Response.StatusCode);
});

app.MapControllers();

// Health Check
app.MapHealthChecks("/health");

// Endpoint adicional para informações da API
app.MapGet("/api/info", () => new
{
    name = "WaterWise API",
    version = "1.0.0",
    description = "Sistema IoT para prevenção de enchentes urbanas",
    timestamp = DateTime.UtcNow,
    environment = app.Environment.EnvironmentName
}).WithTags("Info");

// Inicializar ML.NET model ao startup
using (var scope = app.Services.CreateScope())
{
    var mlService = scope.ServiceProvider.GetRequiredService<IMLPredictionService>();
    await mlService.TrainModelAsync();
    Log.Information("ML.NET model initialized successfully");
}

app.Run();

public partial class Program { } // Torna o Program ACESSÍVEL PARA TESTES