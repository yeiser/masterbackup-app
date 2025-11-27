using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using MasterBackup_API.Infrastructure.Persistence;
using MasterBackup_API.Infrastructure.Middleware;
using MasterBackup_API.Domain.Entities;
using MasterBackup_API.Infrastructure.Services;
using MasterBackup_API.Application.Common.Interfaces;
using MediatR;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.PostgreSQL;
using NpgsqlTypes;

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting MasterBackup API");

    var builder = WebApplication.CreateBuilder(args);

    // Add Serilog
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Conditional(
            evt => IsDatabaseAvailable(context.Configuration.GetConnectionString("MasterDatabase")),
            wt => wt.PostgreSQL(
                connectionString: context.Configuration.GetConnectionString("MasterDatabase")!,
                tableName: "Logs",
                needAutoCreateTable: false,
                restrictedToMinimumLevel: LogEventLevel.Information,
                columnOptions: new Dictionary<string, ColumnWriterBase>
                {
                    {"Message", new RenderedMessageColumnWriter(NpgsqlDbType.Text)},
                    {"Level", new LevelColumnWriter(true, NpgsqlDbType.Varchar)},
                    {"TimeStamp", new TimestampColumnWriter(NpgsqlDbType.TimestampTz)},
                    {"Exception", new ExceptionColumnWriter(NpgsqlDbType.Text)},
                    {"Properties", new PropertiesColumnWriter(NpgsqlDbType.Text)},
                    {"LogEvent", new LogEventSerializedColumnWriter(NpgsqlDbType.Text)}
                }
            )
        )
        .WriteTo.File(
            path: context.Configuration["Serilog:WriteTo:1:Args:path"] ?? "Logs/log-.txt",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 7,
            restrictedToMinimumLevel: LogEventLevel.Warning
        )
    );

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// Add Master Database Context
builder.Services.AddDbContext<MasterDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("MasterDatabase")));

// NOTE: Identity is NOT registered globally because we use database-per-tenant architecture.
// UserManager is created manually in each handler with the appropriate tenant context.

// Add JWT Authentication
var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new Exception("JWT Key not configured");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new Exception("JWT Issuer not configured");
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? throw new Exception("JWT Audience not configured");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

// Add HttpClient for Maileroo
builder.Services.AddHttpClient();

// Add MediatR for CQRS
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// Add Services
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// Add Background Services
builder.Services.AddHostedService<LogCleanupService>();

// Add Controllers
builder.Services.AddControllers();

// Add Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MasterBackup API",
        Version = "v1",
        Description = "Multi-tenant SaaS API with database-per-tenant architecture"
    });

    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token.",
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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "MasterBackup API v1");
    });
}

app.UseHttpsRedirection();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.UseMiddleware<TenantMiddleware>();

app.MapControllers();

// Ensure Master Database is created
using (var scope = app.Services.CreateScope())
{
    var masterDb = scope.ServiceProvider.GetRequiredService<MasterDbContext>();
    await masterDb.Database.MigrateAsync();
}

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}

// Helper function to check database availability
static bool IsDatabaseAvailable(string? connectionString)
{
    if (string.IsNullOrEmpty(connectionString))
        return false;

    try
    {
        using var connection = new Npgsql.NpgsqlConnection(connectionString);
        connection.Open();
        connection.Close();
        return true;
    }
    catch
    {
        return false;
    }
}
