using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using AspNetCoreRateLimit;
using WaterWise.Infrastructure.Data;
using WaterWise.Core.Services;
using WaterWise.Infrastructure.Services;
using WaterWise.ML.Services;
using WaterWise.API.Extensions;
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

try
{
    // Configurar DbContext com configurações específicas para Oracle
    var connectionString = builder.Configuration.GetConnectionString("OracleConnection");

    Log.Information("Configurando conexão com Oracle: {Host}",
        connectionString?.Split(';')?[0] ?? "Connection string não encontrada");

    builder.Services.AddDbContext<WaterWiseContext>(options =>
    {
        options.UseOracle(connectionString, oracleOptions =>
        {
            // Configurações específicas para Oracle
            oracleOptions.UseOracleSQLCompatibility(OracleSQLCompatibility.DatabaseVersion19);
            oracleOptions.CommandTimeout(30);
        });

        // Configurações para desenvolvimento
        if (builder.Environment.IsDevelopment())
        {
            options.EnableDetailedErrors();
            options.EnableSensitiveDataLogging();
            options.LogTo(Console.WriteLine, LogLevel.Warning);
        }
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

        // Incluir comentários XML se disponível
        var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            c.IncludeXmlComments(xmlPath);
        }
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
        .AddDbContextCheck<WaterWiseContext>("database");

    var app = builder.Build();

    // Pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "WaterWise API V1");
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

        if (stopwatch.ElapsedMilliseconds > 1000) // Log apenas requests lentos
        {
            Log.Warning("Slow request {Method} {Path} completed in {ElapsedMs}ms with status code {StatusCode}",
                context.Request.Method,
                context.Request.Path,
                stopwatch.ElapsedMilliseconds,
                context.Response.StatusCode);
        }
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
        environment = app.Environment.EnvironmentName,
        database = "Oracle Database"
    }).WithTags("Info");

    // Endpoint para verificar conectividade do banco
    app.MapGet("/api/database/status", async (HttpContext httpContext) =>
    {
        using var scope = httpContext.RequestServices.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<WaterWiseContext>();

        try
        {
            var canConnect = await context.Database.CanConnectAsync();
            return Results.Json(new
            {
                connected = canConnect,
                timestamp = DateTime.UtcNow,
                message = canConnect ? "Conectado ao Oracle Database" : "Sem conexão com o banco"
            });
        }
        catch (Exception ex)
        {
            return Results.Json(new
            {
                connected = false,
                timestamp = DateTime.UtcNow,
                message = "Erro na conexão",
                error = ex.Message
            });
        }
    }).WithTags("Database");
    // Inicialização segura do banco e ML
    using (var scope = app.Services.CreateScope())
    {
        try
        {
            // Testar conexão com banco
            var context = scope.ServiceProvider.GetRequiredService<WaterWiseContext>();
            var canConnect = await context.Database.CanConnectAsync();

            if (canConnect)
            {
                Log.Information("✅ Conexão com Oracle Database estabelecida");

                // Verificar se as tabelas existem antes de fazer seed
                try
                {
                    var connection = context.Database.GetDbConnection();
                    if (connection.State != System.Data.ConnectionState.Open)
                        await connection.OpenAsync();

                    using var command = connection.CreateCommand();
                    command.CommandText = "SELECT COUNT(*) FROM USER_TABLES WHERE TABLE_NAME LIKE 'GS_WW_%'";
                    var tableCount = Convert.ToInt32(await command.ExecuteScalarAsync());

                    Log.Information("📊 Encontradas {TableCount} tabelas WaterWise no schema", tableCount);

                    // Fazer seed apenas se solicitado explicitamente
                    if (app.Environment.IsDevelopment() &&
                        builder.Configuration.GetValue<bool>("Database:EnableSeed", false))
                    {
                        Log.Information("🌱 Iniciando seed de dados...");
                        await DatabaseSeeder.SeedDatabaseAsync(scope.ServiceProvider);
                    }
                }
                catch (Exception ex)
                {
                    Log.Warning("⚠️ Erro ao verificar estrutura do banco: {Error}", ex.Message);
                    Log.Information("💡 Dica: Verifique se as tabelas GS_WW_* existem no schema");
                }
            }
            else
            {
                Log.Warning("⚠️ Não foi possível conectar ao Oracle Database");
            }
        }
        catch (Exception ex)
        {
            Log.Warning("⚠️ Erro na inicialização do banco: {Error}. API continuará sem banco.", ex.Message);
        }

        try
        {
            // Inicializar ML.NET model
            var mlService = scope.ServiceProvider.GetRequiredService<IMLPredictionService>();
            await mlService.TrainModelAsync();
            Log.Information("✅ Modelo ML.NET inicializado com sucesso");
        }
        catch (Exception ex)
        {
            Log.Warning("⚠️ Erro na inicialização do ML: {Error}. Funcionalidades ML podem estar limitadas.", ex.Message);
        }
    }

    Log.Information("🌊 WaterWise API iniciada com sucesso!");
    Log.Information("📍 Swagger UI: http://localhost:5072");
    Log.Information("🔗 Health check: http://localhost:5072/health");
    Log.Information("📊 Database status: http://localhost:5072/api/database/status");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "💥 Falha crítica na inicialização da aplicação");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { } // Torna o Program ACESSÍVEL PARA TESTES