using System.Text;
using FluentValidation;
using IAM.Application.Behaviors;
using IAM.Application.Commands;
using IAM.Domain.Repositories;
using IAM.Infrastructure.Persistence;
using IAM.Infrastructure.Persistence.Repositories;
using IAM.Infrastructure.Persistence.Seed;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Platform.BuildingBlocks.Domain;
using Platform.Security.Cryptography;
using Platform.Security.Jwt;
using Platform.Security.Mfa;
using Platform.Security.Otpt;
using Platform.Security.RateLimiting;
using Platform.Security.Session;
using Prometheus;
using Serilog;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithProperty("Service", "IAM")
    .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// Add services
var services = builder.Services;

// Database
services.AddDbContext<IamDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Redis
services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(builder.Configuration.GetConnectionString("Redis")!));

// Repositories
services.AddScoped<IUserRepository, UserRepository>();
services.AddScoped<IRoleRepository, RoleRepository>();
services.AddScoped<IPolicyRepository, PolicyRepository>();
services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<IamDbContext>());

// Security services
services.AddSingleton<IPasswordHasher, CompositePasswordHasher>();
services.AddSingleton<ITotpService, TotpService>();

var masterKey = builder.Configuration["Security:MasterKey"] ?? "";
services.AddSingleton<IKeyManagementService>(new FileBasedKeyManagementService(masterKey));
services.AddSingleton<IDataEncryption, AesGcmDataEncryption>();

services.AddScoped<ISessionService, RedisSessionService>();
services.AddScoped<IOtptService, RedisOtptService>();
services.AddScoped<IRateLimiter, RedisRateLimiter>();

// JWT
services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
services.AddSingleton<IJwtTokenService, JwtTokenService>();

// MediatR
services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(LoginCommand).Assembly);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
});

// FluentValidation
services.AddValidatorsFromAssembly(typeof(LoginCommand).Assembly);

// Seed data
services.AddScoped<SeedData>();

// Authentication
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtConfig = builder.Configuration.GetSection(JwtOptions.SectionName);
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtConfig["Issuer"],
            ValidAudience = jwtConfig["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtConfig["SecretKey"]!)),
            ClockSkew = TimeSpan.Zero
        };
    });

services.AddAuthorization();

// OpenTelemetry
services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService("IAM.Service"))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddSource("IAM.Service");

        var jaegerEndpoint = builder.Configuration["Observability:JaegerEndpoint"];
        if (!string.IsNullOrEmpty(jaegerEndpoint))
        {
            tracing.AddOtlpExporter(options => options.Endpoint = new Uri(jaegerEndpoint));
        }
    });

// Controllers
services.AddControllers();

// Swagger
services.AddEndpointsApiExplorer();
services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "IAM Service API",
        Version = "v1",
        Description = "Identity and Access Management Service"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// CORS
services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[] { "http://localhost:3001" })
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// Health checks
services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!)
    .AddRedis(builder.Configuration.GetConnectionString("Redis")!);

var app = builder.Build();

// Apply migrations and seed
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<IamDbContext>();
    await db.Database.MigrateAsync();

    var seeder = scope.ServiceProvider.GetRequiredService<SeedData>();
    await seeder.SeedAsync();
}

// Configure pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();

app.UseCors("AllowFrontend");

app.UseAuthentication();
app.UseAuthorization();

// Prometheus metrics
app.UseHttpMetrics();

app.MapControllers();
app.MapHealthChecks("/health");
app.MapMetrics();

app.Run();
