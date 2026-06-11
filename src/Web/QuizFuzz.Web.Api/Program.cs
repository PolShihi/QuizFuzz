using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Microsoft.Extensions.FileProviders;
using QuizFuzz.Infrastructure;
using QuizFuzz.Web.Api.Services;
using System.Text;
using Serilog;
using Serilog.Events;

// Configure Serilog BEFORE building the app.
// Verbose logging is enabled only for DEBUG builds. Release builds keep logging silent.
#if DEBUG
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/quizfuzz-.log",
        rollingInterval: RollingInterval.Day,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}",
        retainedFileCountLimit: 7,
        fileSizeLimitBytes: 10_000_000,
        rollOnFileSizeLimit: true)
    .CreateLogger();
#else
Log.Logger = new LoggerConfiguration().CreateLogger();
#endif

try
{
    #if DEBUG
    Log.Debug("Starting QuizFuzz Web API...");
#endif

var builder = WebApplication.CreateBuilder(args);

// Add Serilog
builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();

// HTTP Context Accessor
builder.Services.AddHttpContextAccessor();

// Current User Service
builder.Services.AddScoped<QuizFuzz.Application.Common.Interfaces.Services.ICurrentUserService, 
    QuizFuzz.Web.Api.Services.CurrentUserService>();

// Round hint scheduling
builder.Services.AddSingleton<IHintRevealScheduler, HintRevealScheduler>();

// User stats and achievements
builder.Services.AddScoped<IUserStatsService, UserStatsService>();

// Room cleanup
builder.Services.Configure<RoomCleanupOptions>(builder.Configuration.GetSection("RoomCleanup"));
builder.Services.AddScoped<IRoomCleanupService, RoomCleanupService>();
builder.Services.AddHostedService<RoomCleanupBackgroundService>();

// Infrastructure Layer
builder.Services.AddInfrastructure(builder.Configuration);

// MediatR - CRITICAL for Moderation Commands/Queries
builder.Services.AddMediatR(cfg => 
{
    cfg.RegisterServicesFromAssembly(typeof(QuizFuzz.Application.Moderation.Commands.ApproveQuestion.ApproveQuestionCommand).Assembly);
});

// CORS
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
    ?? new[] { "http://localhost:5173", "https://localhost:5002", "http://localhost:5003" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials()
              .SetIsOriginAllowedToAllowWildcardSubdomains();
    });
});

// JWT Authentication
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"] 
    ?? throw new InvalidOperationException("JWT SecretKey not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "QuizFuzz";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "QuizFuzz";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };

    // SignalR support
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
            {
                context.Token = accessToken;
            }
            
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "QuizFuzz API",
        Version = "v1",
        Description = "API для платформы многопользовательских викторин с нечеткими критериями ответов",
        Contact = new OpenApiContact
        {
            Name = "QuizFuzz Team",
            Email = "support@quizfuzz.com"
        }
    });

    // JWT Authentication in Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token",
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

// SignalR
builder.Services.AddSignalR();

// Health Checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "QuizFuzz API v1");
        c.RoutePrefix = string.Empty; // Swagger at root
    });
}

app.UseHttpsRedirection();

// Static files (для uploads)
// В dev Blazor WASM часто запущен на другом origin, а API — на https://localhost:7001.
// Поэтому медиа должны физически лежать в wwwroot/uploads API и явно раздаваться API-сервером.
var webRootPath = app.Environment.WebRootPath ?? Path.Combine(app.Environment.ContentRootPath, "wwwroot");
var uploadsRootPath = Path.Combine(webRootPath, "uploads");
Directory.CreateDirectory(Path.Combine(uploadsRootPath, "images"));
Directory.CreateDirectory(Path.Combine(uploadsRootPath, "audio"));

app.UseStaticFiles();
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsRootPath),
    RequestPath = "/uploads"
});

// Routing ПЕРВЫМ!
app.UseRouting();

// CORS после Routing!
app.UseCors("AllowAll");

// Authentication и Authorization
app.UseAuthentication();
app.UseAuthorization();

// Map endpoints
app.MapControllers().RequireCors("AllowAll");

// SignalR Hubs with CORS
app.MapHub<QuizFuzz.Web.Api.Hubs.GameHub>("/hubs/game").RequireCors("AllowAll");
app.MapHub<QuizFuzz.Web.Api.Hubs.LobbyHub>("/hubs/lobby").RequireCors("AllowAll");

// Log mapped endpoints
var appLogger = app.Services.GetRequiredService<ILogger<Program>>();
appLogger.LogDebug(" [Startup] SignalR Hubs mapped:");
appLogger.LogDebug("   - GameHub: /hubs/game");
appLogger.LogDebug("   - LobbyHub: /hubs/lobby");
appLogger.LogDebug(" [Startup] CORS enabled for: {Origins}", string.Join(", ", allowedOrigins));

// Health check endpoint
app.MapHealthChecks("/health");

// Seed database
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var services = scope.ServiceProvider;
    
    try
    {
        var seedLogger = services.GetRequiredService<ILogger<Program>>();
        seedLogger.LogDebug(" [Startup] Seeding database...");
        
        await QuizFuzz.Infrastructure.Persistence.Seeds.SeedExtensions.SeedDatabaseAsync(services);
        
        seedLogger.LogDebug(" [Startup] Database seeded successfully!");
    }
    catch (Exception ex)
    {
        var seedLogger = services.GetRequiredService<ILogger<Program>>();
        seedLogger.LogDebug(ex, " [Startup] Error while seeding database");
    }
}

app.Run();
}
catch (Exception ex)
{
    #if DEBUG
    Log.Debug(ex, "Application terminated unexpectedly");
#endif
}
finally
{
    Log.CloseAndFlush();
}
