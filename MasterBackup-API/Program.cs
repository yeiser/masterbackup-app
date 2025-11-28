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
using FluentValidation;
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

    // Get connection string from environment variable or configuration
    var masterDbConnectionString = Environment.GetEnvironmentVariable("MASTER_DATABASE_CONNECTION") 
                                    ?? builder.Configuration.GetConnectionString("MasterDatabase")
                                    ?? throw new Exception("Master database connection string not configured");

    Log.Information("Using database connection: {ConnectionString}", 
        masterDbConnectionString.Replace(masterDbConnectionString.Split("Password=")[1].Split(";")[0], "***"));

    // Add Serilog
    builder.Host.UseSerilog((context, services, configuration) =>
    {
        var serilogConnectionString = Environment.GetEnvironmentVariable("MASTER_DATABASE_CONNECTION") 
                                      ?? masterDbConnectionString;
        
        Log.Information("Configuring Serilog with database: {Connection}", 
            serilogConnectionString?.Split("Password=")[0] + "Password=***");
        
        var columnOptions = new Dictionary<string, ColumnWriterBase>
        {
            {"Message", new RenderedMessageColumnWriter(NpgsqlDbType.Text)},
            {"Level", new LevelColumnWriter(true, NpgsqlDbType.Varchar)},
            {"TimeStamp", new TimestampColumnWriter(NpgsqlDbType.TimestampTz)},
            {"Exception", new ExceptionColumnWriter(NpgsqlDbType.Text)},
            {"Properties", new PropertiesColumnWriter(NpgsqlDbType.Text)},
            {"LogEvent", new LogEventSerializedColumnWriter(NpgsqlDbType.Text)}
        };
        
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "MasterBackup-API")
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("System", LogEventLevel.Warning)
            .WriteTo.Console()
            .WriteTo.PostgreSQL(
                connectionString: serilogConnectionString,
                tableName: "Logs",
                needAutoCreateTable: false,
                restrictedToMinimumLevel: LogEventLevel.Information,
                columnOptions: columnOptions
            )
            .WriteTo.File(
                path: context.Configuration["Serilog:WriteTo:1:Args:path"] ?? "Logs/log-.txt",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                restrictedToMinimumLevel: LogEventLevel.Warning
            );
            
        Log.Information("Serilog configured successfully");
    });

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
    options.UseNpgsql(masterDbConnectionString));

// Add Identity with MasterDbContext (Users, Roles, etc. are in master DB)
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 8;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<MasterDbContext>()
.AddDefaultTokenProviders();

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

// Add FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Add Services
builder.Services.AddScoped<ITenantContext, TenantContext>();
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<IEmailService, EmailService>();

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
