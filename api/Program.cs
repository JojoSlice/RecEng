using System.Diagnostics;
using System.Text;
using System.Threading.RateLimiting;
using api.Data;
using api.Features.Auth;
using api.Features.Users;
using api.Features.Videos;
using api.Options;
using MassTransit;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 500 * 1024 * 1024;
});

builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 500 * 1024 * 1024;
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default"))
);

builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq(
        (ctx, cfg) =>
        {
            cfg.Host(
                "rabbitmq",
                "/",
                h =>
                {
                    h.Username("receng");
                    h.Password("receng");
                }
            );
        }
    );
});

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
                var logger = context.HttpContext.RequestServices.GetRequiredService<
                    ILogger<Program>
                >();
                logger.LogWarning(context.Exception, "JWT authentication failed");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<
                    ILogger<Program>
                >();
                var claims = context.Principal?.Claims.Select(c => $"{c.Type}={c.Value}");
                logger.LogInformation(
                    $"[JWT] Token validated. Claims: {string.Join(", ", claims ?? [])}"
                );
                return Task.CompletedTask;
            },
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy(
        "auth",
        httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    Window = TimeSpan.FromMinutes(1),
                    PermitLimit = 10,
                    QueueLimit = 0,
                }
            )
    );

    options.AddPolicy(
        "upload",
        httpContext =>
        {
            var userId = httpContext.User.FindFirst("sub")?.Value;
            var key = userId ?? httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return RateLimitPartition.GetFixedWindowLimiter(
                key,
                _ => new FixedWindowRateLimiterOptions
                {
                    Window = TimeSpan.FromMinutes(1),
                    PermitLimit = 5,
                    QueueLimit = 0,
                }
            );
        }
    );

    options.AddPolicy(
        "stream",
        httpContext =>
            RateLimitPartition.GetTokenBucketLimiter(
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new TokenBucketRateLimiterOptions
                {
                    TokenLimit = 30,
                    TokensPerPeriod = 20,
                    ReplenishmentPeriod = TimeSpan.FromMinutes(1),
                    QueueLimit = 0,
                    AutoReplenishment = true,
                }
            )
    );

    options.AddPolicy(
        "read",
        httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    Window = TimeSpan.FromMinutes(1),
                    PermitLimit = 60,
                    QueueLimit = 0,
                }
            )
    );
});

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy
            .WithOrigins("http://localhost:8080", "http://localhost:5033")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
    )
);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    var devAssetsPath = Path.GetFullPath(
        Path.Combine(app.Environment.ContentRootPath, "..", "devAssets")
    );
    await DevSeeder.SeedAsync(app.Services, devAssetsPath);
}

try
{
    var ffmpegCheck = new ProcessStartInfo
    {
        FileName = "ffmpeg",
        Arguments = "-version",
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        UseShellExecute = false,
        CreateNoWindow = true,
    };
    using var checkProcess = Process.Start(ffmpegCheck);
    if (checkProcess is null)
        app.Logger.LogError(
            "ffmpeg startup check failed: could not start process — video upload and thumbnails will not work"
        );
    else
    {
        await checkProcess.WaitForExitAsync();
        if (checkProcess.ExitCode != 0)
            app.Logger.LogError(
                "ffmpeg startup check failed with exit code {ExitCode}",
                checkProcess.ExitCode
            );
        else
            app.Logger.LogInformation("ffmpeg is available");
    }
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "ffmpeg not found — video upload and thumbnails will not work");
}

app.UseHttpsRedirection();
app.UseCors();
app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

Register.MapEndpoint(app);
Login.MapEndpoint(app);
Refresh.MapEndpoint(app);
Logout.MapEndpoint(app);
GetVideos.MapEndpoint(app);
GetFollowVideos.MapEndpoint(app);
GetVideo.MapEndpoint(app);
StreamVideo.MapEndpoint(app);
UploadVideo.MapEndpoint(app);
GetUserVideos.MapEndpoint(app);
UpdateUserVideo.MapEndpoint(app);
DeleteUserVideo.MapEndpoint(app);
CurrentUser.MapEndpoint(app);
GetUser.MapEndpoint(app);
UploadProfilePicture.MapEndpoint(app);
GetProfilePicture.MapEndpoint(app);
FollowUser.MapEndpoint(app);
UnfollowUser.MapEndpoint(app);
GetFollowers.MapEndpoint(app);
GetFollowing.MapEndpoint(app);
GetThumbnail.MapEndpoint(app);
LogInteraction.MapEndpoints(app);
LikeVideo.MapEndpoint(app);
UnLikeVideo.MapEndpoint(app);

app.Run();
