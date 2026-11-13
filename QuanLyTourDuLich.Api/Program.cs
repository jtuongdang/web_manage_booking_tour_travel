using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using QuanLyTourDuLich.Api.Common.Constants;
using QuanLyTourDuLich.Api.Common.Contracts;
using QuanLyTourDuLich.Api.Common.Exceptions;
using QuanLyTourDuLich.Api.Common.Modules;
using QuanLyTourDuLich.Api.Common.Security;
using QuanLyTourDuLich.Api.Common.Services;
using QuanLyTourDuLich.Api.Data;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
var signingKeyProvider = new JwtSigningKeyProvider(builder.Configuration);
builder.Services.AddSingleton<IJwtSigningKeyProvider>(signingKeyProvider);

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, HttpContextCurrentUserService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<ISystemSettingService, SystemSettingService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IFinancialLedgerWriter, FinancialLedgerWriter>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection") ?? string.Empty));

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
    };
});
builder.Services.AddExceptionHandler<AppExceptionHandler>();
builder.Services.AddOpenApi();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5173", "http://127.0.0.1:5173"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("FrontendCors", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "QuanLyTourDuLich.Api";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "QuanLyTourDuLich.Clients";

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKeyProvider.GetSigningKey(),
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = ClaimTypes.Name,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(PolicyNames.RequireAdmin, p => p.RequireRole(RoleCodes.Admin))
    .AddPolicy(PolicyNames.RequireSalesOrAdmin, p => p.RequireRole(RoleCodes.Sales, RoleCodes.Admin))
    .AddPolicy(PolicyNames.RequireOperatorOrAdmin, p => p.RequireRole(RoleCodes.Operator, RoleCodes.Admin))
    .AddPolicy(PolicyNames.RequireGuideOrOps, p => p.RequireRole(RoleCodes.Guide, RoleCodes.Operator, RoleCodes.Admin))
    .AddPolicy(PolicyNames.RequireAccountantOrAdmin, p => p.RequireRole(RoleCodes.Accountant, RoleCodes.Admin))
    .AddPolicy(PolicyNames.RequireInternalStaff, p => p.RequireRole(RoleCodes.InternalStaff));

// Discover and install all Modules/**/IModuleInstaller automatically without modifying Program.cs
builder.Services.AddApplicationModules(builder.Configuration);

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("FrontendCors");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapGet("/api/v1/health", () => Results.Ok(new
{
    status = "healthy",
    service = "QuanLyTourDuLich.Api",
    utcNow = DateTime.UtcNow
}));

await app.RunStartupInitializersAsync();

app.Run();

public partial class Program;
