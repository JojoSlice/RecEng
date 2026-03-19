using System.Text;
using api.Data;
using api.Features.Auth;
using api.Features.Users;
using api.Features.Videos;
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
            OnTokenValidated = context =>
            {
                var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                var claims = context.Principal?.Claims.Select(c => $"{c.Type}={c.Value}");
                logger.LogInformation($"[JWT] Token validated. Claims: {string.Join(", ", claims ?? [])}");
                return Task.CompletedTask;
            },
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins("http://localhost:8080", "http://localhost:5033")
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

app.UseHttpsRedirection();
app.UseCors();
app.UseSerilogRequestLogging();
app.UseAuthentication();
app.UseAuthorization();

Register.MapEndpoint(app);
Login.MapEndpoint(app);
Refresh.MapEndpoint(app);
Logout.MapEndpoint(app);
GetVideos.MapEndpoint(app);
GetVideo.MapEndpoint(app);
StreamVideo.MapEndpoint(app);
UploadVideo.MapEndpoint(app);
GetUserVideos.MapEndpoint(app);
UpdateUserVideo.MapEndpoint(app);
DeleteUserVideo.MapEndpoint(app);
CurrentUser.MapEndpoint(app);
UploadProfilePicture.MapEndpoint(app);
GetProfilePicture.MapEndpoint(app);
GetThumbnail.MapEndpoint(app);

app.Run();
