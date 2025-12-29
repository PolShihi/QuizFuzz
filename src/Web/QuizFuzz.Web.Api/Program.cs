using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using QuizFuzz.Infrastructure;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// HTTP Context Accessor
builder.Services.AddHttpContextAccessor();

// Current User Service
builder.Services.AddScoped<QuizFuzz.Application.Common.Interfaces.Services.ICurrentUserService, 
    QuizFuzz.Web.Api.Services.CurrentUserService>();

// Infrastructure Layer
builder.Services.AddInfrastructure(builder.Configuration);

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
app.UseStaticFiles();

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
appLogger.LogInformation("🚀 [Startup] SignalR Hubs mapped:");
appLogger.LogInformation("   - GameHub: /hubs/game");
appLogger.LogInformation("   - LobbyHub: /hubs/lobby");
appLogger.LogInformation("🔐 [Startup] CORS enabled for: {Origins}", string.Join(", ", allowedOrigins));

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
        seedLogger.LogInformation("🌱 [Startup] Seeding database...");
        
        await QuizFuzz.Infrastructure.Persistence.Seeds.SeedExtensions.SeedDatabaseAsync(services);
        
        seedLogger.LogInformation("✅ [Startup] Database seeded successfully!");
    }
    catch (Exception ex)
    {
        var seedLogger = services.GetRequiredService<ILogger<Program>>();
        seedLogger.LogError(ex, "❌ [Startup] Error while seeding database");
    }
}

app.Run();
