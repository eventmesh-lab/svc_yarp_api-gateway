using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using svc_yar_api_gateway.Application.UseCases;
using svc_yar_api_gateway.Domain.Ports;
using svc_yar_api_gateway.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Configuration keys (can be set via env vars)
var eventosServiceBaseUrl = builder.Configuration["EVENTOS_SERVICE_URL"] ?? "http://localhost:5001";
var keycloakAuthority = builder.Configuration["KEYCLOAK_AUTHORITY"] ?? "https://keycloak.local/auth/realms/myrealm";
var keycloakAudience = builder.Configuration["KEYCLOAK_AUDIENCE"] ?? "eventmesh-api";

// CORS - Configuración para permitir requests desde el frontend React
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactFrontend", policy =>
    {
        policy.WithOrigins(
                builder.Configuration["CORS_ALLOWED_ORIGINS"]?.Split(',') ?? new[] { "http://localhost:3000", "http://localhost:5173" }
            )
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

// DI: registramos implementaciones concretas
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "API Gateway - EventMesh", Version = "v1" });
    // JWT Bearer definition for Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        }
    });
});

// Auth - JWT Bearer (Keycloak)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = keycloakAuthority;
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateAudience = true,
            ValidAudience = keycloakAudience
        };
        // Allow HTTP for development if needed (not recommended for prod)
        options.RequireHttpsMetadata = !builder.Environment.IsDevelopment() ? true : false;
    });

// Health checks
builder.Services.AddHealthChecks();

// HttpClient for eventos service
builder.Services.AddHttpClient("eventos", client =>
{
    client.BaseAddress = new Uri(eventosServiceBaseUrl);
    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
});

// Registrations (composition root)
// Reemplazar por registros reales (EF, HttpClient, Messaging, etc.)
builder.Services.AddScoped<IExampleRepository, InMemoryExampleRepository>();
builder.Services.AddScoped<CreateExampleUseCase>();

var app = builder.Build();

// Simple Correlation-ID middleware
app.Use(async (context, next) =>
{
    const string header = "X-Correlation-ID";
    if (!context.Request.Headers.ContainsKey(header))
    {
        context.Request.Headers[header] = Guid.NewGuid().ToString();
    }
    context.Response.OnStarting(() =>
    {
        // Use indexer to avoid exceptions when header already exists
        context.Response.Headers[header] = context.Request.Headers[header].ToString();
        return Task.CompletedTask;
    });
    await next();
});

app.UseRouting();

// CORS debe estar antes de UseAuthentication y UseAuthorization
app.UseCors("AllowReactFrontend");

app.UseAuthentication();
app.UseAuthorization();

// Swagger siempre disponible (puedes restringirlo a Development si prefieres)
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Swagger disponible en todos los entornos para facilitar pruebas
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "API Gateway - EventMesh v1");
    c.RoutePrefix = "swagger"; // Accesible en /swagger
    c.DisplayRequestDuration();
});

app.MapHealthChecks("/health");

app.MapControllers();

app.Run();
