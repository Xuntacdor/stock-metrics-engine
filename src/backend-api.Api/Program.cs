using System.Text;
using backend_api.Api.Data;
using backend_api.Api.Middleware;
using backend_api.Api.Repositories;
using backend_api.Api.Services;
using backend_api.Api.Workers;
using backend_api.Api.Hubs;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using StackExchange.Redis;
using Microsoft.OpenApi.Models;
using PayOS;
using Polly;
using Polly.Extensions.Http;
using Prometheus;
using Serilog;
using Serilog.Events;

Env.TraversePath().Load();

// ── Serilog: configure before host builds ────────────────────────────────────
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/app-.json",
        rollingInterval: RollingInterval.Day,
        formatter: new Serilog.Formatting.Json.JsonFormatter(),
        retainedFileCountLimit: 14)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Wire Serilog into the ASP.NET host
builder.Host.UseSerilog();

builder.Configuration.AddEnvironmentVariables();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSignalR();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "QuantIQ API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter JWT token: Bearer {token}"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrEmpty(connectionString))
{
    throw new InvalidOperationException("Connection String is not found in the configuration.");
}

builder.Services.AddDbContext<QuantIQContext>(options =>
    options.UseSqlServer(connectionString));

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];

if (string.IsNullOrEmpty(secretKey))
{
    throw new InvalidOperationException("SecretKey is not found in the configuration.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            ClockSkew = TimeSpan.Zero
        };
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var cacheService = context.HttpContext.RequestServices.GetRequiredService<ICacheService>();
                var token = context.SecurityToken as JwtSecurityToken;
                if (token != null && await cacheService.IsTokenBlacklistedAsync(token.RawData))
                {
                    context.Fail("Token is blacklisted(Logout)");
                }
            }
        };
    });

builder.Services.AddAuthorization();

// ── Redis: read connection string from config ─────────────────────────────────
var redisConnStr = builder.Configuration["Redis:ConnectionString"]
    ?? "localhost:6379";

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
    ConnectionMultiplexer.Connect(redisConnStr));

// ── Application services ──────────────────────────────────────────────────────
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ICacheService, CacheService>();
builder.Services.AddScoped<ISymbolService, SymbolService>();
builder.Services.AddScoped<ISymbolRepository, SymbolRepository>();

builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IPortfolioRepository, PortfolioRepository>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<IWalletRepository, WalletRepository>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IPortfolioService, PortfolioService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IWalletService, WalletService>();

builder.Services.AddScoped<ICorporateActionRepository, CorporateActionRepository>();
builder.Services.AddScoped<ICorporateActionService, CorporateActionService>();

builder.Services.AddScoped<INewsRepository, NewsRepository>();
builder.Services.AddScoped<ICommentRepository, CommentRepository>();
builder.Services.AddScoped<ILeaderboardRepository, LeaderboardRepository>();

builder.Services.AddHostedService<DividendPayoutWorker>();

// ── FPT.AI HTTP client — retry + circuit breaker via Polly ───────────────────
// Retry: up to 3 attempts, exponential back-off (2 s → 4 s → 8 s).
// Circuit breaker: opens after 5 consecutive failures, stays open for 30 s.
builder.Services.AddHttpClient("FptAi")
    .AddPolicyHandler(HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
            onRetry: (outcome, timespan, attempt, _) =>
                Log.Warning(
                    "FptAi retry {Attempt}/3 after {Delay}s — {Reason}",
                    attempt, timespan.TotalSeconds,
                    outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString())))
    .AddPolicyHandler(HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 5,
            durationOfBreak: TimeSpan.FromSeconds(30),
            onBreak: (outcome, breakDelay) =>
                Log.Error(
                    "FptAi circuit breaker OPEN for {BreakDelay}s — {Reason}",
                    breakDelay.TotalSeconds,
                    outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString()),
            onReset: () => Log.Information("FptAi circuit breaker CLOSED — requests resuming"),
            onHalfOpen: () => Log.Warning("FptAi circuit breaker HALF-OPEN — testing")));
builder.Services.AddScoped<IKycRepository, KycRepository>();
builder.Services.AddScoped<IKycService, KycService>();

var payOsClientId    = builder.Configuration["PayOS:ClientId"]    ?? throw new InvalidOperationException("PayOS:ClientId is not configured.");
var payOsApiKey      = builder.Configuration["PayOS:ApiKey"]      ?? throw new InvalidOperationException("PayOS:ApiKey is not configured.");
var payOsChecksumKey = builder.Configuration["PayOS:ChecksumKey"] ?? throw new InvalidOperationException("PayOS:ChecksumKey is not configured.");

builder.Services.AddSingleton(new PayOSClient(payOsClientId, payOsApiKey, payOsChecksumKey));
builder.Services.AddScoped<IDepositRepository, DepositRepository>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

builder.Services.AddScoped<IMarginRatioRepository, MarginRatioRepository>();
builder.Services.AddScoped<IRiskAlertRepository, RiskAlertRepository>();
builder.Services.AddScoped<IMarginRiskService, MarginRiskService>();
builder.Services.AddHostedService<RiskMonitorWorker>();

// ── Audit Trail ───────────────────────────────────────────────────────────────
builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();

// ── Price Alerts ──────────────────────────────────────────────────────────────
builder.Services.AddScoped<IPriceAlertRepository, PriceAlertRepository>();
builder.Services.AddHostedService<AlertMonitorWorker>();

// ── Screener ──────────────────────────────────────────────────────────────────
builder.Services.AddScoped<IScreenerService, ScreenerService>();

// ── Background workers ────────────────────────────────────────────────────────
builder.Services.AddHostedService<PortfolioPnLWorker>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAllOrigins",
        policy =>
        {
            policy.WithOrigins("http://localhost:4200")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
});

var app = builder.Build();

try
{
    await DbSeeder.SeedAsync(app.Services);
}
catch (Exception ex)
{
    Log.Error(ex, "[DbSeeder] Lỗi khi seed dữ liệu. App sẽ tiếp tục chạy.");
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseCors("AllowAllOrigins");

// ── Correlation ID (must be early so all subsequent log lines have the ID) ────
app.UseMiddleware<CorrelationIdMiddleware>();

// ── Prometheus HTTP metrics middleware ────────────────────────────────────────
app.UseHttpMetrics();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<MarketHub>("/hubs/market");

// ── Prometheus metrics endpoint ───────────────────────────────────────────────
app.MapMetrics("/metrics");

app.Run();
