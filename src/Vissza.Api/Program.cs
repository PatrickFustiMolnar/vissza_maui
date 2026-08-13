using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Vissza.Api.Data;
using Vissza.Api.Endpoints;
using Vissza.Api.RateLimiting;
using Vissza.Api.Services;
using Vissza.Shared.Dtos;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Konfiguráció - fail fast
// ---------------------------------------------------------------------------

// Nincs beégetett tartalék érték. Egy hiányzó connection stringgel elinduló
// szerver a legrosszabb eset: úgy tűnik, működik, aztán minden kérésre elszáll.
var connectionString = builder.Configuration.GetConnectionString("Vissza")
    ?? throw new InvalidOperationException(
        "Hiányzik a ConnectionStrings:Vissza. Állítsd be user secretsben vagy " +
        "a ConnectionStrings__Vissza környezeti változóban.");

var jwtSecret = JwtService.ReadSecretOrThrow(builder.Configuration);

// ---------------------------------------------------------------------------
// Szolgáltatások
// ---------------------------------------------------------------------------

builder.Services.AddSingleton(new JwtService(jwtSecret));
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ImageUrlService>();

// Alapból automatikus felismerés, hogy a generált SQL a valódi szerverhez
// igazodjon. Ha a szerver induláskor nem elérhető (pl. offline fejlesztés),
// a Database:ServerVersion beállítással kihagyható a felismerés.
var configuredVersion = builder.Configuration["Database:ServerVersion"];
var serverVersion = string.IsNullOrWhiteSpace(configuredVersion)
    ? ServerVersion.AutoDetect(connectionString)
    : ServerVersion.Parse(configuredVersion);

builder.Services.AddDbContext<VisszaDbContext>(options => options
    .UseMySql(connectionString, serverVersion)
    // A schema.sql snake_case neveit ez képezi le, hogy ne kelljen minden
    // oszlopnevet kézzel felsorolni a DbContextben.
    .UseSnakeCaseNamingConvention());

// A kliens snake_case JSON-t vár, és az enumokat kisbetűs sztringként -
// pontosan úgy, ahogy a régi Express backend adta.
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.SerializerOptions.Converters.Add(
        new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower));
});

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new JwtService(jwtSecret).ValidationParameters;

        // E nélkül az "id" claim a hosszú WS-* URI nevére cserélődne.
        options.MapInboundClaims = false;

        options.Events = new JwtBearerEvents
        {
            // A régi backend hiányzó tokenre 401-et, érvénytelenre 403-at
            // adott. A kliens erre épül, ezért itt is így válaszolunk.
            OnChallenge = async context =>
            {
                context.HandleResponse();

                var tokenPresent = !string.IsNullOrEmpty(context.Request.Headers.Authorization);

                context.Response.StatusCode = tokenPresent
                    ? StatusCodes.Status403Forbidden
                    : StatusCodes.Status401Unauthorized;

                await context.Response.WriteAsJsonAsync(new MessageResponse(
                    tokenPresent ? "Invalid or expired token" : "Access token required"));
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(AuthRateLimiting.Configure);

// A natív app nem küld Origin fejlécet, a CORS csak böngészőre vonatkozik.
// Üres listával egyetlen weboldal sem hívhatja az API-t a felhasználó
// tokenjével - ez a szándékolt alapértelmezés.
var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];

builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy
    .WithOrigins(corsOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()));

var app = builder.Build();

// ---------------------------------------------------------------------------
// Pipeline
// ---------------------------------------------------------------------------

app.UseCors();

// A fiókonkénti limithez ismerni kell az e-mailt, az viszont a kérés
// törzsében van. Ez a middleware olvassa ki, még a limiter előtt.
app.UseLoginAttemptIdentity();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

// A feltöltött képeket a mobilapp tölti be, tehát más originből -
// ezért kell rájuk a cross-origin erőforrás-házirend.
var uploadsPath = Path.Combine(builder.Environment.ContentRootPath, "uploads");
Directory.CreateDirectory(uploadsPath);

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads",
    OnPrepareResponse = ctx =>
        ctx.Context.Response.Headers["Cross-Origin-Resource-Policy"] = "cross-origin"
});

app.MapGet("/api/health", () => Results.Ok(new { status = "ok", message = "Server is running" }));

app.MapAuthEndpoints();

app.Run();
