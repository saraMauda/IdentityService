using IdentityService.Data;
using IdentityService.Messaging;
using IdentityService.Security;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;

var builder = WebApplication.CreateBuilder(args);
var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
var hasJwtSecret = !string.IsNullOrWhiteSpace(jwt.Secret);
var devClientOrigins = new[]
{
    "http://localhost:5173",
    "https://localhost:5173",
    "http://localhost:5174",
    "https://localhost:5174",
    "http://127.0.0.1:5173",
    "https://127.0.0.1:5173",
    "http://127.0.0.1:5174",
    "https://127.0.0.1:5174",
};

// Add services to the container.
builder.Services.AddDbContext<IdentityContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("IdentityConnection")));

builder.Services.AddControllers();
builder.Services.AddCors(options =>
{
    options.AddPolicy("SeminarClientDev", policy =>
    {
        policy
            .WithOrigins(devClientOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services
    .AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .ValidateDataAnnotations();

builder.Services
    .AddOptions<RabbitMqOptions>()
    .Bind(builder.Configuration.GetSection(RabbitMqOptions.SectionName))
    .ValidateDataAnnotations();

builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        var rmq = context.GetRequiredService<Microsoft.Extensions.Options.IOptions<RabbitMqOptions>>().Value;
        cfg.Host(new Uri($"rabbitmq://{rmq.Host}:{rmq.Port}{rmq.VirtualHost}"), h =>
        {
            if (!string.IsNullOrWhiteSpace(rmq.Username))
                h.Username(rmq.Username);
            if (!string.IsNullOrWhiteSpace(rmq.Password))
                h.Password(rmq.Password);
        });
    });
});

if (hasJwtSecret)
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
        var keyBytes = Encoding.UTF8.GetBytes(jwt.Secret);

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
            ValidateIssuer = !string.IsNullOrWhiteSpace(jwt.Issuer),
            ValidIssuer = jwt.Issuer,
            ValidateAudience = !string.IsNullOrWhiteSpace(jwt.Audience),
            ValidAudience = jwt.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            RoleClaimType = ClaimTypes.Role,
            NameClaimType = JwtRegisteredClaimNames.Sub,
        };
        });
}

builder.Services.AddAuthorization();
builder.Services.AddScoped<TokenService>();
builder.Services.AddScoped<IUserEventsPublisher, UserEventsPublisher>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();
app.UseCors("SeminarClientDev");

if (hasJwtSecret)
{
    app.UseAuthentication();
}
else
{
    app.Logger.LogWarning("JWT secret is missing. Authenticated endpoints and token issuance are unavailable until Jwt:Secret is configured.");
}

app.UseAuthorization();
app.MapControllers();

app.Run();
