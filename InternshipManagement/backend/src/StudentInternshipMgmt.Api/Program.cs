using System.Text;
using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StudentInternshipMgmt.Api.Authorization;
using StudentInternshipMgmt.Api.Middlewares;
using StudentInternshipMgmt.Application.Features.Auth;
using StudentInternshipMgmt.Application.Features.Companies;
using StudentInternshipMgmt.Application.Features.Companies.Accounts;
using StudentInternshipMgmt.Infrastructure.Persistence;
using StudentInternshipMgmt.Infrastructure.Services;
using FluentValidation; 
using FluentValidation.AspNetCore; 
using StudentInternshipMgmt.Application.Features.Students;
using StudentInternshipMgmt.Application.Features.JobPositions;
using StudentInternshipMgmt.Application.Features.PlacementRequests;
using StudentInternshipMgmt.Application.Features.Dashboard;
using StudentInternshipMgmt.Application.Features.Notifications;
using StudentInternshipMgmt.Application.Features.About;
using StudentInternshipMgmt.Api.BackgroundServices;
using StudentInternshipMgmt.Application.Common;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<ImportSettings>(builder.Configuration.GetSection("Import"));
builder.Services.AddMemoryCache(options => options.SizeLimit = 10_000);
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ILoginAttemptTracker, InMemoryLoginAttemptTracker>();
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("auth", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 20,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
                AutoReplenishment = true
            }));
    options.OnRejected = async (rejectedContext, cancellationToken) =>
    {
        var retryAfter = rejectedContext.Lease.TryGetMetadata(MetadataName.RetryAfter, out var value)
            ? value
            : TimeSpan.FromMinutes(1);
        rejectedContext.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        rejectedContext.HttpContext.Response.ContentType = "application/json";
        rejectedContext.HttpContext.Response.Headers.RetryAfter =
            Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds)).ToString(CultureInfo.InvariantCulture);
        await rejectedContext.HttpContext.Response.WriteAsJsonAsync(
            ApiResponse<object>.FailResponse("Bạn đã gửi quá nhiều yêu cầu. Vui lòng thử lại sau."),
            cancellationToken);
    };
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();
JwtSettingsValidator.Validate(jwtSettings);
var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey));

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("SameStudentOnly", policy =>
        policy.Requirements.Add(new SameStudentRequirement()));
    options.AddPolicy("SameCompanyOnly", policy =>
        policy.Requirements.Add(new SameCompanyRequirement()));
});

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddValidatorsFromAssemblyContaining<CreateStudentDtoValidator>(); 
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddScoped<ICompanyService, CompanyService>();
builder.Services.AddScoped<ICompanyAccountService, CompanyAccountService>();
builder.Services.AddScoped<IJobPositionService, JobPositionService>();
builder.Services.AddScoped<IPlacementAcceptanceService, PlacementAcceptanceService>();
builder.Services.AddScoped<IPlacementRequestService, PlacementRequestService>();
builder.Services.AddScoped<IPlacementRequestExpiryService, PlacementRequestExpiryService>();
builder.Services.AddScoped<PlacementRequestEmailNotifier>();
if (builder.Configuration.GetValue<bool>("Smtp:Enabled"))
    builder.Services.AddTransient<IEmailSender, SmtpEmailSender>();
else
    builder.Services.AddTransient<IEmailSender, NoOpEmailSender>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IAboutService, AboutService>();
builder.Services.AddHostedService<PlacementRequestExpiryWorker>();
builder.Services.AddSingleton<IAuthorizationHandler, SameStudentAuthorizationHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, SameCompanyAuthorizationHandler>();

// CORS cho môi trường Development — origin đọc từ config "Cors:AllowedOrigins",
// mặc định "http://localhost:5173" nếu config trống. Không AllowCredentials vì
// project dùng Bearer token trong header, không dùng cookie.
var corsAllowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>();
if (corsAllowedOrigins is null || corsAllowedOrigins.Length == 0)
    corsAllowedOrigins = new[] { "http://localhost:5173" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontendDev", policy =>
    {
        policy.WithOrigins(corsAllowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.CustomSchemaIds(type => type.FullName);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseSecurityHeaders();
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}
app.UseRequestLogging();
app.UseGlobalExceptionHandling();
app.UseRouting();

app.UseCors("AllowFrontendDev");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseMiddleware<MustChangePasswordMiddleware>();
app.UseRateLimiter();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program
{
}