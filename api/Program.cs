using api.Data;
using api.Features.Users;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default"))
);

var app = builder.Build();

if (app.Environment.IsDevelopment()) { }

app.UseHttpsRedirection();

app.Run();

app.MapPost("/auth/register", Register.Handle);
