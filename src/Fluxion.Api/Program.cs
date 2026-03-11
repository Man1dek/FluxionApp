using Fluxion.AI;
using Fluxion.Core.Data;
using Fluxion.Core.Interfaces;
using Fluxion.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Fluxion.Api.Hubs;

var builder = WebApplication.CreateBuilder(args);

// ── Core Services ───────────────────────────────────────

var connStr = builder.Configuration.GetConnectionString("FluxionDb")
    ?? throw new InvalidOperationException(
        "Missing required configuration: ConnectionStrings:FluxionDb. " +
        "Set it via environment variables, User Secrets, or appsettings.");

builder.Services.AddDbContext<FluxionDbContext>(options =>
{
    if (builder.Environment.IsDevelopment())
    {
        options.UseSqlite(connStr);
    }
    else
    {
        options.UseSqlServer(connStr);
    }
});

builder.Services.AddScoped<IKnowledgeGraphRepository, EfCoreGraphRepository>();

// ── Semantic Kernel + Fluxion AI ────────────────────────

var aiProvider = builder.Configuration["FluxionAI:Provider"] ?? "AzureOpenAI";
var aiModel = builder.Configuration["FluxionAI:ModelOrDeployment"] ?? "gpt-4o";

var aiEndpoint = builder.Configuration["FluxionAI:EndpointOrApiKey"]
    ?? throw new InvalidOperationException(
        "Missing required configuration: FluxionAI:EndpointOrApiKey. " +
        "Set it via environment variables, User Secrets, or appsettings.");

var aiKey = builder.Configuration["FluxionAI:ApiKey"]
    ?? throw new InvalidOperationException(
        "Missing required configuration: FluxionAI:ApiKey. " +
        "Set it via environment variables, User Secrets, or appsettings.");

builder.Services.AddFluxionAI(
    provider: aiProvider,
    modelOrDeployment: aiModel,
    endpointOrApiKey: aiEndpoint,
    apiKey: aiKey);

// ── API Infrastructure ─────────────────────────────────

builder.Services.AddControllers();
builder.Services.AddSignalR();

var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "Missing required configuration: Jwt:Key. " +
        "Set it via environment variables, User Secrets, or appsettings.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "Fluxion",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "FluxionLearners",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// ── CORS ────────────────────────────────────────────────
// Read allowed origins from configuration. In production, set the
// "AllowedOrigins" environment variable (e.g. "https://fluxion-web.azurewebsites.net").

var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5215", "https://localhost:7215"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("FluxionClient", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Required for SignalR
    });
});

var app = builder.Build();

try
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<FluxionDbContext>();
        await db.Database.EnsureCreatedAsync();

        var graph = scope.ServiceProvider.GetRequiredService<IKnowledgeGraphRepository>();
        
        // Only seed if empty
        var existingNodes = await graph.GetAllNodesAsync();
        if (existingNodes.Count == 0)
        {
            await GraphSeeder.SeedAsync(graph);
        }
    }
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogCritical(ex, "An error occurred during API startup/database initialization.");
}

// ── Pipeline Configuration ──────────────────────────────

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("FluxionClient");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<CurriculumHub>("/hubs/curriculum");

app.Run();
