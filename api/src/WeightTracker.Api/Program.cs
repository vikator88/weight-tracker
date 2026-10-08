using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Scalar.AspNetCore;
using WeightTracker.Api.Common;
using WeightTracker.Api.OpenApi;
using WeightTracker.Application.Common;
using WeightTracker.Application.Interfaces;
using WeightTracker.Application.UseCases.Auth;
using WeightTracker.Application.UseCases.Exercises;
using WeightTracker.Application.UseCases.Workouts;
using WeightTracker.Infra.Persistence;
using WeightTracker.Infra.Repositories;
using WeightTracker.Infra.Seed;
using WeightTracker.Infra.Services;
using WeightTracker.Migrations;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("ConnectionStrings:Default is not configured");

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException($"The '{JwtOptions.SectionName}' configuration section is missing");

// Fail fast rather than booting with a weak or absent signing key
if (Encoding.UTF8.GetByteCount(jwtOptions.SigningKey) < JwtOptions.MinimumSigningKeyBytes)
    throw new InvalidOperationException(
        $"Jwt:SigningKey must be at least {JwtOptions.MinimumSigningKeyBytes} bytes. "
        + "Supply it through configuration or the Jwt__SigningKey environment variable.");

builder.Services.AddDbContext<WeightTrackerDbContext>(options => options.UseNpgsql(connectionString));

builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IExerciseRepository, ExerciseRepository>();
builder.Services.AddScoped<IWorkoutRepository, WorkoutRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

builder.Services.AddSingleton<IClock, SystemClock>();
builder.Services.AddSingleton<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<ITokenService, JwtTokenService>();

builder.Services.AddScoped<GetAllExercisesUseCase>();
builder.Services.AddScoped<GetAllWorkoutsUseCase>();
builder.Services.AddScoped<LoginUseCase>();
builder.Services.AddScoped<RefreshTokenUseCase>();

builder.Services.AddScoped<DatabaseSeeder>();
builder.Services.AddScoped<ExceptionMapper>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Keep the claim types exactly as they were issued
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            NameClaimType = "sub",
            RoleClaimType = ClaimTypes.Role,
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddOpenApi(options =>
{
    // The built-in generator ignores the registered authentication schemes, so the bearer
    // scheme and the per-operation requirements have to be contributed explicitly.
    options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
    options.AddOperationTransformer<SecurityRequirementOperationTransformer>();
});

var app = builder.Build();

// The document is also served under Testing so the integration suite can assert its contents.
// The reference UI, the startup migration and the seeder stay Development only.
if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
    app.MapOpenApi();

if (app.Environment.IsDevelopment())
{
    app.MapScalarApiReference(options => options
        .WithTitle("Weight Tracker API")
        .AddPreferredSecuritySchemes(BearerSecuritySchemeTransformer.SchemeName)
        .EnablePersistentAuthentication());

    DatabaseMigrator.MigrateUp(connectionString);

    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().Seed(CancellationToken.None);
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

/// <summary>
/// Exposed so the integration test host can reference this entry point.
/// </summary>
public partial class Program;
