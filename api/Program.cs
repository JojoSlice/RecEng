using System.Text;
using api.Data;
using api.Features.Auth;
using api.Options;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default"))
);

builder.Host.UseSerilog(
    (context, services, configuration) =>
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
);

builder
    .Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection("Jwt"))
    .Validate(
        o =>
            !string.IsNullOrWhiteSpace(o.Issuer)
            && !string.IsNullOrWhiteSpace(o.Audience)
            && !string.IsNullOrWhiteSpace(o.Key),
        "Invalid JWT configuration"
    )
    .ValidateOnStart();

var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()!;

builder
    .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
            ),
        };
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                logger.LogWarning(context.Exception, "JWT authentication failed");
                return Task.CompletedTask;
            },
        };
    });

builder.Services.AddAuthorization();
var app = builder.Build();

if (app.Environment.IsDevelopment()) { }

app.UseHttpsRedirection();
app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();

Register.MapEndpoint(app);
Login.MapEndpoint(app);
Refresh.MapEndpoint(app);
GetVideos.MapEndpoint(app);
GetVideo.MapEndpoint(app);
StreamVideo.MapEndpoint(app);

app.Run();

public partial class Program { }
