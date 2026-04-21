using System.Text;
using HotelManagement.Data;
using HotelManagement.Helpers;
using HotelManagement.Middleware;
using HotelManagement.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ═══════════════════════════════════════════════════════════════════════════
//  1. DATABASE — EF Core + SQL Server
// ═══════════════════════════════════════════════════════════════════════════
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null)));

// ═══════════════════════════════════════════════════════════════════════════
//  2. AUTHENTICATION — JWT Bearer  (Member A)
// ═══════════════════════════════════════════════════════════════════════════
var jwtSecret = builder.Configuration["Jwt:Secret"]
    ?? throw new InvalidOperationException("Jwt:Secret is not configured.");

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
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ClockSkew = TimeSpan.Zero,
    };

    // Return clean JSON on auth failures (not HTML 401 page)
    options.Events = new JwtBearerEvents
    {
        OnChallenge = async ctx =>
        {
            ctx.HandleResponse();
            ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync(
                """{"success":false,"message":"Unauthorized. Please provide a valid token."}""");
        },
        OnForbidden = async ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            ctx.Response.ContentType = "application/json";
            await ctx.Response.WriteAsync(
                """{"success":false,"message":"Forbidden. You do not have access to this resource."}""");
        },
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
    options.AddPolicy("CustomerOnly", p => p.RequireRole("Customer"));
    options.AddPolicy("Authenticated", p => p.RequireAuthenticatedUser());
});

// ═══════════════════════════════════════════════════════════════════════════
//  3. RATE LIMITING  (Member A)
// ═══════════════════════════════════════════════════════════════════════════
builder.Services.AddRateLimiter(RateLimitingHelper.ConfigurePolicies);

// ═══════════════════════════════════════════════════════════════════════════
//  4. MEMBER A SERVICES
// ═══════════════════════════════════════════════════════════════════════════
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IJwtHelper, JwtHelper>();

// ═══════════════════════════════════════════════════════════════════════════
//  5. MEMBER B SERVICES  (B: register your services below this comment)
// ═══════════════════════════════════════════════════════════════════════════
// builder.Services.AddScoped<IHotelService, HotelService>();

// ═══════════════════════════════════════════════════════════════════════════
//  6. MEMBER C SERVICES  (C: register your services below this comment)
// ═══════════════════════════════════════════════════════════════════════════
// builder.Services.AddScoped<IBookingService, BookingService>();
// builder.Services.AddScoped<IPaymentService, PaymentService>();
// builder.Services.AddScoped<IPromotionService, PromotionService>();

// ═══════════════════════════════════════════════════════════════════════════
//  7. CORS
// ═══════════════════════════════════════════════════════════════════════════
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

// ═══════════════════════════════════════════════════════════════════════════
//  8. CONTROLLERS + SWAGGER
// ═══════════════════════════════════════════════════════════════════════════
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        o.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Hotel Booking API",
        Version = "v1",
        Description = "Full-Stack .NET 9 — Use Case 3 | Hotel Booking Backend",
    });

    // JWT auth button in Swagger UI
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter: Bearer {your JWT token}",
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer",
                }
            },
            Array.Empty<string>()
        }
    });
});

// ═══════════════════════════════════════════════════════════════════════════
//  9. HEALTH CHECKS
// ═══════════════════════════════════════════════════════════════════════════
builder.Services.AddHealthChecks()
    .AddDbContextCheck<ApplicationDbContext>("database");

// ═══════════════════════════════════════════════════════════════════════════
//  BUILD
// ═══════════════════════════════════════════════════════════════════════════
var app = builder.Build();

// ── Auto-migrate on startup (dev convenience; remove in production CI/CD) ──
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
}

// ═══════════════════════════════════════════════════════════════════════════
//  MIDDLEWARE PIPELINE (ORDER MATTERS)
// ═══════════════════════════════════════════════════════════════════════════

// 1. Error handling — must be outermost
app.UseMiddleware<ErrorHandlingMiddleware>();

// 2. Swagger (dev only)
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Hotel Booking API v1");
        c.RoutePrefix = string.Empty;  // Swagger at root: https://localhost:xxxx/
    });
}

// 3. HTTPS redirect
app.UseHttpsRedirection();

// 4. CORS
app.UseCors("AllowFrontend");

// 5. Custom rate-limit logger (before built-in limiter)
app.UseMiddleware<RateLimitingMiddleware>();

// 6. Built-in ASP.NET Core rate limiter (policy-based)
app.UseRateLimiter();

// 7. Auth
app.UseAuthentication();
app.UseAuthorization();

// 8. Controllers
app.MapControllers();

// 9. Health check endpoint
app.MapHealthChecks("/health");

app.Run();