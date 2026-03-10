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

builder.Services.AddDbContext<FluxionDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("FluxionDb") ?? "Data Source=fluxion.db"));

builder.Services.AddScoped<IKnowledgeGraphRepository, EfCoreGraphRepository>();

// ── Semantic Kernel + Fluxion AI ────────────────────────

var aiConfig = builder.Configuration.GetSection("FluxionAI");
builder.Services.AddFluxionAI(
    provider: aiConfig["Provider"] ?? "AzureOpenAI",
    modelOrDeployment: aiConfig["ModelOrDeployment"] ?? "gpt-4o",
    endpointOrApiKey: aiConfig["EndpointOrApiKey"] ?? "",
    apiKey: aiConfig["ApiKey"]);

// ── API Infrastructure ─────────────────────────────────

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddOpenApi();

var jwtKey = builder.Configuration["Jwt:Key"] ?? "SUPER_SECRET_FLUXION_KEY_32_CHARS_LONG";
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

builder.Services.AddCors(options =>
{
    options.AddPolicy("FluxionClient", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins);
        }
        else
        {
            policy.SetIsOriginAllowed(origin => true);
        }
        policy.AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // Required for SignalR
    });
});

var app = builder.Build();

// ── Seed the Knowledge Graph ────────────────────────────

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

// ── Pipeline Configuration ──────────────────────────────

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseCors("FluxionClient");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<CurriculumHub>("/hubs/curriculum");

app.Run();
